using System.Reflection;
using HarmonyLib;

namespace Network.Compatibility;

internal static class ReturnToSenderCompatibility
{
	private static readonly MethodInfo? update = AccessTools.DeclaredMethod(typeof(ZDOMan), nameof(ZDOMan.Update));

	public static bool IsActive()
	{
		if (update == null)
		{
			return false;
		}

		Patches? patches = Harmony.GetPatchInfo(update);
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

	public static bool Remove(Harmony harmony)
	{
		if (update == null)
		{
			return false;
		}

		harmony.Unpatch(update, HarmonyPatchType.Transpiler, Network.ReturnToSenderGUID);
		return !IsActive();
	}
}
