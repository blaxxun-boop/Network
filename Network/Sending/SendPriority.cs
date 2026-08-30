using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Network.Sending;

public static class SendPriority
{
	private static readonly Dictionary<int, float> prefabBonus = new();
	private static readonly List<ZDO> promoted = new();
	private static readonly List<ZDO> rest = new();

	// Stay under vanilla's 150 m staleness credit or scenery starves.
	private static float Bonus(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefabBonus.TryGetValue(prefab, out float bonus))
		{
			return bonus;
		}

		if (!ZNetScene.instance)
		{
			return 0f;
		}

		GameObject go = ZNetScene.instance.GetPrefab(prefab);
		if (!go)
		{
			return 0f;
		}

		if (go.TryGetComponent(out Player _))
		{
			bonus = 120f;
		}
		else if (go.TryGetComponent(out Ship _))
		{
			bonus = 80f;
		}
		else if (go.TryGetComponent(out Character _))
		{
			bonus = 40f;
		}
		else
		{
			bonus = 0f;
		}

		prefabBonus[prefab] = bonus;
		return bonus;
	}

	[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ServerSortSendZDOS))]
	private static class BiasTowardsActors
	{
		private static void Postfix(List<ZDO> objects)
		{
			if (!Network.ImprovementEnabled(Network.prioritizeActors) || objects.Count < 2)
			{
				return;
			}

			promoted.Clear();
			rest.Clear();

			foreach (ZDO zdo in objects)
			{
				float bonus = Bonus(zdo);
				if (bonus > 0f)
				{
					zdo.m_tempSortValue -= bonus;
					promoted.Add(zdo);
				}
				else
				{
					rest.Add(zdo);
				}
			}

			if (promoted.Count == 0)
			{
				return;
			}

			// Sort shit again after vanilla does theirs
			promoted.Sort(ZDOMan.ServerSendCompare);
			SortedMerge.Into(objects, promoted, rest, ZDOMan.ServerSendCompare);
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class ClearPrefabCacheOnSceneLoad
	{
		private static void Postfix() => prefabBonus.Clear();
	}
}
