using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Network.Connection;

public static class SteamSettings
{
	private static bool Set<T>(ESteamNetworkingConfigValue setting, ESteamNetworkingConfigDataType dataType, T value) where T : struct
	{
		GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
		try
		{
			IntPtr valuePointer = handle.AddrOfPinnedObject();
			if (ZNet.instance && ZNet.instance.IsDedicated())
			{
				return SteamGameServerNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, dataType, valuePointer);
			}

			return SteamNetworkingUtils.SetConfigValue(setting, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, dataType, valuePointer);
		}
		finally
		{
			handle.Free();
		}
	}

	private static bool SetInt(ESteamNetworkingConfigValue setting, int value) => Set(setting, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, value);

	private static bool SetFloat(ESteamNetworkingConfigValue setting, float value) => Set(setting, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, value);

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
