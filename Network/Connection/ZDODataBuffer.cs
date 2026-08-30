using System.Collections.Generic;
using HarmonyLib;

namespace Network.Connection;

public static class ZDODataBuffer
{
	private static readonly Dictionary<ZRpc, List<ZPackage>> packageBuffers = new();

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
	private static class StartBufferingOnNewConnection
	{
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				return;
			}

			List<ZPackage> packages = new();
			packageBuffers[peer.m_rpc] = packages;
			peer.m_rpc.Register<ZPackage>("ZDOData", (_, package) => packages.Add(package));
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
	private static class ClearPackageBufferOnShutdown
	{
		private static void Postfix() => packageBuffers.Clear();
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect), typeof(ZNetPeer))]
	private static class ClearPackageBufferOnDisconnect
	{
		private static void Postfix(ZNetPeer peer) => packageBuffers.Remove(peer.m_rpc);
	}

	[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
	private static class EvaluateBufferedPackages
	{
		private static void Postfix(ZDOMan __instance, ZNetPeer netPeer)
		{
			if (!packageBuffers.TryGetValue(netPeer.m_rpc, out List<ZPackage> packages))
			{
				return;
			}

			foreach (ZPackage package in packages)
			{
				__instance.RPC_ZDOData(netPeer.m_rpc, package);
			}

			packageBuffers.Remove(netPeer.m_rpc);
		}
	}
}
