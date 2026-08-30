using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Network.Connection;

public static class SteamSettings
{
	private static bool SetInt(ESteamNetworkingConfigValue setting, int value)
	{
		GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
		try
		{
			return ZNet.instance && ZNet.instance.IsDedicated()
				? SteamGameServerNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, handle.AddrOfPinnedObject())
				: SteamNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, handle.AddrOfPinnedObject());
		}
		finally
		{
			handle.Free();
		}
	}

	private static bool SetFloat(ESteamNetworkingConfigValue setting, float value)
	{
		GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
		try
		{
			return ZNet.instance && ZNet.instance.IsDedicated()
				? SteamGameServerNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, handle.AddrOfPinnedObject())
				: SteamNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, handle.AddrOfPinnedObject());
		}
		finally
		{
			handle.Free();
		}
	}

	[HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
	private static class IncreaseSendingLimit
	{
		private static void Postfix()
		{
			bool applied;
			if (Network.ImprovementEnabled(Network.useImprovedSteamSettings))
			{
				applied = SetFloat(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, Network.timeoutConnected.Value);
				applied &= SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, Network.sendBufferSize.Value);
				applied &= SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, Network.sendRateMin.Value);
				applied &= SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, Math.Max(Network.sendRateMax.Value, Network.sendRateMin.Value));
			}
			else
			{
				applied = SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, 50000000);
				applied &= SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, 50000000);
				if (!ZNet.instance || !ZNet.instance.IsDedicated())
				{
					applied &= SetInt(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, 100000000);
				}
			}

			if (!applied)
			{
				Network.NetworkLogger.LogWarning("Steam rejected a socket setting. The ZDO changes still work, but check this machine's Steam initialization.");
			}
		}
	}
}
