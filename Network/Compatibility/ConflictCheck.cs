using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Network.Compatibility;

internal static class ConflictCheck
{
	private static bool reported;

	private static readonly (Type type, string method)[] contestedMethods =
	{
		(typeof(ZDOMan), nameof(ZDOMan.Update)),
		(typeof(ZDOMan), nameof(ZDOMan.SendZDOToPeers2)),
		(typeof(ZDOMan), nameof(ZDOMan.SendZDOs)),
		(typeof(ZDOMan), nameof(ZDOMan.ServerSortSendZDOS)),
		(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks)),
	};

	public static void Report(string ownHarmonyId)
	{
		if (reported)
		{
			return;
		}

		reported = true;

		foreach ((Type type, string methodName) in contestedMethods)
		{
			MethodBase? method = AccessTools.DeclaredMethod(type, methodName);
			if (method == null)
			{
				Network.NetworkLogger.LogWarning($"{type.Name}.{methodName} not found.");
				continue;
			}

			Patches? patches = Harmony.GetPatchInfo(method);
			if (patches == null)
			{
				continue;
			}

			HashSet<string> otherOwners = new();
			foreach (string owner in patches.Owners)
			{
				bool handledCompatibilityPatch =
					type == typeof(ZDOMan) &&
					methodName == nameof(ZDOMan.Update) &&
					owner == Network.ReturnToSenderGUID;

				if (owner != ownHarmonyId && !handledCompatibilityPatch)
				{
					otherOwners.Add(owner);
				}
			}

			if (otherOwners.Count > 0)
			{
				Network.NetworkLogger.LogWarning($"{type.Name}.{methodName} is also patched by: {string.Join(", ", otherOwners)}. Expect one of us to win. If networking misbehaves, remove the other networking mod before reporting a bug.");
			}
		}
	}
}
