using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using BepInEx;
using HarmonyLib;
using Steamworks;

namespace Network;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Network : BaseUnityPlugin
{
	private const string ModName = "Network";
	private const string ModVersion = "1.0.0";
	private const string ModGUID = "org.bepinex.plugins.network";

	public void Awake()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);
	}
	
	[HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
	private static class IncreaseSendingLimit
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4 && instruction.OperandIs(153600))
				{
					instruction.operand = 50000000;
				}
				yield return instruction;
			}
		}

		private static void Postfix()
		{
			if (CSteamAPIContext.GetSteamClient() != IntPtr.Zero)
			{
				GCHandle handle = GCHandle.Alloc(100000000, GCHandleType.Pinned);
				SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, handle.AddrOfPinnedObject());
				handle.Free();
			}
		}
	}
}
