using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Network.Compatibility;

internal static class ConflictCheck
{
	private static bool done;

	private static readonly (Type type, string method)[] contested =
	{
		(typeof(ZDOMan), nameof(ZDOMan.Update)),
		(typeof(ZDOMan), nameof(ZDOMan.SendZDOToPeers2)),
		(typeof(ZDOMan), nameof(ZDOMan.SendZDOs)),
		(typeof(ZDOMan), nameof(ZDOMan.ServerSortSendZDOS)),
		(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks)),
	};

	public static void Report(string selfId)
	{
		if (done)
		{
			return;
		}

		done = true;

		foreach ((Type type, string methodName) in contested)
		{
			MethodBase? method = AccessTools.DeclaredMethod(type, methodName);
			if (method == null)
			{
				Network.NetworkLogger.LogWarning($"{type.Name}.{methodName} not found.");
				continue;
			}

			Patches? info = Harmony.GetPatchInfo(method);
			if (info == null)
			{
				continue;
			}

			HashSet<string> others = new();
			foreach (string owner in info.Owners)
			{
				bool handledReturnToSender = type == typeof(ZDOMan) && methodName == nameof(ZDOMan.Update) && owner == Network.ReturnToSenderGUID;
				if (owner != selfId && !handledReturnToSender)
				{
					others.Add(owner);
				}
			}

			if (others.Count > 0)
			{
				Network.NetworkLogger.LogWarning($"{type.Name}.{methodName} is also patched by: {string.Join(", ", others)}. Expect one of us to win. If networking misbehaves, remove the other networking mod before reporting a bug.");
			}
		}
	}
}
