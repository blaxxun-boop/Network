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

	private static void Record(ZDOID id, Vector3 pos, float now)
	{
		if (!tracks.TryGetValue(id, out MarkerTrack track))
		{
			track = new MarkerTrack();
			tracks[id] = track;
		}

		track.Add(now, pos, Network.mapTeleportThreshold.Value);
	}

	private static void RPC_MapPos(long sender, ZPackage pkg)
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
		int count = pkg.ReadInt();
		for (int i = 0; i < count; ++i)
		{
			Record(pkg.ReadZDOID(), pkg.ReadVector3(), now);
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
		private static void Postfix(ZRoutedRpc __instance) => __instance.Register<ZPackage>(RPCName, RPC_MapPos);
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
			ZDOID local = __instance.LocalPlayerCharacterID;
			if (__instance.IsReferencePositionPublic() && !local.IsNone())
			{
				positions.Add((local, PeerPosition.GetOr(local, __instance.GetReferencePosition())));
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

			ZPackage pkg = new();
			pkg.Write(positions.Count);
			foreach ((ZDOID id, Vector3 position) in positions)
			{
				pkg.Write(id);
				pkg.Write(position);
			}

			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPCName, pkg);

			if (Minimap.instance)
			{
				float now = Time.time;
				foreach ((ZDOID id, Vector3 position) in positions)
				{
					Record(id, position, now);
				}

				Prune(now);
			}
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

			int count = Mathf.Min(__instance.m_playerPins.Count, __instance.m_tempPlayerInfo.Count);
			for (int i = 0; i < count; ++i)
			{
				ZNet.PlayerInfo info = __instance.m_tempPlayerInfo[i];
				if (!tracks.TryGetValue(info.m_characterID, out MarkerTrack track))
				{
					continue;
				}

				if (track.TryGet(Time.time - track.Delay, out Vector3 pos) && pos != __instance.m_playerPins[i].m_pos)
				{
					__instance.m_playerPins[i].m_pos = pos;
					__instance.m_pinUpdateRequired = true;
				}
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
