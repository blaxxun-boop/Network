using System.Collections.Generic;
using HarmonyLib;
using Network.Sending;
using UnityEngine;

namespace Network.Map;

public static class MapPositions
{
	private const string RPCName = "Network_MapPos";

	private static float sendTimer;
	private static readonly List<(ZDOID id, Vector3 position)> positions = new();

	private static readonly Dictionary<ZDOID, MarkerTrack> tracks = new();
	private static readonly List<ZDOID> stale = new();

	private static void Record(ZDOID id, Vector3 position, float now)
	{
		if (!tracks.TryGetValue(id, out MarkerTrack track))
		{
			track = new MarkerTrack();
			tracks[id] = track;
		}

		track.Add(now, position, Network.mapTeleportThreshold.Value);
	}

	private static void RPC_ReceiveMapPos(long sender, ZPackage package)
	{
		if (!Network.ImprovementEnabled(Network.smoothMapMarkers) || !ZNet.instance || ZNet.instance.IsServer())
		{
			return;
		}

		ZNetPeer? server = ZNet.instance.GetServerPeer();
		if (server == null || sender != server.m_uid)
		{
			return;
		}

		float now = Time.time;
		int count = package.ReadInt();
		for (int i = 0; i < count; ++i)
		{
			Record(package.ReadZDOID(), package.ReadVector3(), now);
		}

		Prune(now);
	}

	private static void Prune(float now)
	{
		stale.Clear();
		foreach (KeyValuePair<ZDOID, MarkerTrack> track in tracks)
		{
			if (now - track.Value.LastArrival > 30f)
			{
				stale.Add(track.Key);
			}
		}

		foreach (ZDOID id in stale)
		{
			tracks.Remove(id);
		}
	}

	[HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
	private static class RegisterRPC
	{
		private static void Postfix(ZRoutedRpc __instance) => __instance.Register<ZPackage>(RPCName, RPC_ReceiveMapPos);
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Update))]
	private static class BroadcastPositions
	{
		private static void Postfix(ZNet __instance)
		{
			if (!Network.ImprovementEnabled(Network.smoothMapMarkers) || !__instance.IsServer())
			{
				return;
			}

			sendTimer += Time.deltaTime;
			if (sendTimer < Network.mapSendInterval.Value)
			{
				return;
			}

			sendTimer %= Network.mapSendInterval.Value;

			positions.Clear();
			ZDOID localPlayer = __instance.LocalPlayerCharacterID;
			if (__instance.IsReferencePositionPublic() && !localPlayer.IsNone())
			{
				positions.Add((localPlayer, PeerPosition.GetOr(localPlayer, __instance.GetReferencePosition())));
			}

			foreach (ZNetPeer peer in __instance.GetPeers())
			{
				if (peer.IsReady() && peer.m_publicRefPos && !peer.m_characterID.IsNone())
				{
					positions.Add((peer.m_characterID, PeerPosition.GetOr(peer.m_characterID, peer.m_refPos)));
				}
			}

			if (positions.Count == 0)
			{
				return;
			}

			ZPackage package = new();
			package.Write(positions.Count);
			foreach ((ZDOID id, Vector3 position) in positions)
			{
				package.Write(id);
				package.Write(position);
			}

			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCName, package);

			if (!Minimap.instance)
			{
				return;
			}

			float now = Time.time;
			foreach ((ZDOID id, Vector3 position) in positions)
			{
				Record(id, position, now);
			}

			Prune(now);
		}
	}

	[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerPins))]
	private static class SmoothPlayerPins
	{
		private static void Postfix(Minimap __instance)
		{
			if (!Network.ImprovementEnabled(Network.smoothMapMarkers) || tracks.Count == 0)
			{
				return;
			}

			float renderTime = Time.time;
			int pinCount = Mathf.Min(__instance.m_playerPins.Count, __instance.m_tempPlayerInfo.Count);
			for (int i = 0; i < pinCount; ++i)
			{
				ZNet.PlayerInfo playerInfo = __instance.m_tempPlayerInfo[i];
				if (!tracks.TryGetValue(playerInfo.m_characterID, out MarkerTrack track))
				{
					continue;
				}

				if (!track.TryGet(renderTime - track.Delay, out Vector3 position) || position == __instance.m_playerPins[i].m_pos)
				{
					continue;
				}

				__instance.m_playerPins[i].m_pos = position;
				__instance.m_pinUpdateRequired = true;
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
	private static class ClearTracksOnShutdown
	{
		private static void Postfix()
		{
			tracks.Clear();
			sendTimer = 0f;
		}
	}
}
