using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Network.Sending;

public static class PeerSendRate
{
	private static int cursor;
	private static float budget;

	private static void ResetState()
	{
		cursor = 0;
		budget = 0f;
	}

	// Vanilla's gate and one-peer frames cap this around 19 Hz.
	[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOToPeers2))]
	private static class ServeEveryPeerOnAnInterval
	{
		private static bool Prefix(ZDOMan __instance, float dt)
		{
			if (!Network.ImprovementEnabled(Network.adaptiveZdoScheduler))
			{
				ResetState();
				return true;
			}

			int count = __instance.m_peers.Count;
			int serve = SendBudget.Take(ref budget, count, dt, Network.sendInterval.Value, Network.maxPeersPerFrame.Value);
			if (serve <= 0)
			{
				return false;
			}

			for (int i = 0; i < serve; ++i)
			{
				ZDOMan.ZDOPeer peer = __instance.m_peers[(cursor + i) % count];
				if (peer?.m_peer?.m_socket?.IsConnected() == true)
				{
					__instance.SendZDOs(peer, false);
				}
			}

			cursor = (cursor + serve) % count;
			// Reset vanilla's loop in case this patch stops.
			__instance.m_sendTimer = 0f;
			__instance.m_nextSendPeer = -1;
			return false;
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
	private static class ResetOnShutdown
	{
		private static void Postfix()
		{
			ResetState();
		}
	}

	[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOs))]
	private static class ConfigureSendZdos
	{
		private static int BatchSize() => Network.ImprovementEnabled(Network.increaseZdoBatchSize) ? Network.zdoQueueLimit.Value : 10240;

		private static void Prefix(ZDOMan.ZDOPeer peer)
		{
			if (Network.ImprovementEnabled(Network.refreshPeerInterestPosition) && ZNet.instance.IsServer() && PeerPosition.TryGet(peer.m_peer.m_characterID, out Vector3 position))
			{
				peer.m_peer.m_refPos = position;
			}
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> code = new(instructions);
			MethodInfo batchSize = AccessTools.DeclaredMethod(typeof(ConfigureSendZdos), nameof(BatchSize));
			List<CodeInstruction> targets = code.FindAll(i => i.opcode == OpCodes.Ldc_I4 && i.OperandIs(10240));

			// I need both, it will send nothing if I don't have them.
			if (targets.Count != 2)
			{
				Network.NetworkLogger.LogWarning($"ZDO batch size patch expected 2 constants, found {targets.Count}. Leaving vanilla sizes alone.");
				return code;
			}

			foreach (CodeInstruction instruction in targets)
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = batchSize;
			}

			return code;
		}
	}
}
