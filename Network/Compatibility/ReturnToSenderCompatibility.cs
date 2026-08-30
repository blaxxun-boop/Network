using System.Reflection;
using HarmonyLib;

namespace Network.Compatibility;

internal static class ReturnToSenderCompatibility
{
	private static readonly MethodInfo? updateMethod = AccessTools.DeclaredMethod(typeof(ZDOMan), nameof(ZDOMan.Update));

	public static void Handle(Harmony harmony)
	{
		if (!IsActive())
		{
			return;
		}

		if (!Network.ImprovementEnabled(Network.adaptiveZdoScheduler))
		{
			Network.NetworkLogger.LogInfo("ReturnToSender will control ZDO scheduling because Network's adaptive scheduler is disabled.");
			return;
		}

		if (!Remove(harmony))
		{
			Network.NetworkLogger.LogWarning("Could not remove ReturnToSender's ZDO scheduler. Network's adaptive scheduler will not run.");
			return;
		}

		Network.NetworkLogger.LogInfo("ReturnToSender detected. Its ZDO scheduler was disabled so Network's adaptive scheduler can run.");
	}

	private static bool IsActive()
	{
		if (updateMethod == null)
		{
			return false;
		}

		Patches? patches = Harmony.GetPatchInfo(updateMethod);
		if (patches == null)
		{
			return false;
		}

		foreach (Patch patch in patches.Transpilers)
		{
			if (patch.owner == Network.ReturnToSenderGUID)
			{
				return true;
			}
		}

		return false;
	}

	private static bool Remove(Harmony harmony)
	{
		if (updateMethod == null)
		{
			return false;
		}

		harmony.Unpatch(updateMethod, HarmonyPatchType.Transpiler, Network.ReturnToSenderGUID);
		return !IsActive();
	}
}
