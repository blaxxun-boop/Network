using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Network.Sending;

public static class SendPriority
{
	private static readonly Dictionary<int, float> prefabBonuses = new();
	private static readonly List<ZDO> prioritized = new();
	private static readonly List<ZDO> remaining = new();

	// Stay under vanilla's 150 m staleness credit or scenery starves.
	private static float GetBonus(ZDO zdo)
	{
		int prefabHash = zdo.GetPrefab();
		if (prefabBonuses.TryGetValue(prefabHash, out float bonus))
		{
			return bonus;
		}

		if (!ZNetScene.instance)
		{
			return 0f;
		}

		GameObject prefab = ZNetScene.instance.GetPrefab(prefabHash);
		if (!prefab)
		{
			return 0f;
		}

		if (prefab.TryGetComponent(out Player _))
		{
			bonus = 120f;
		}
		else if (prefab.TryGetComponent(out Ship _))
		{
			bonus = 80f;
		}
		else if (prefab.TryGetComponent(out Character _))
		{
			bonus = 40f;
		}
		else
		{
			bonus = 0f;
		}

		prefabBonuses[prefabHash] = bonus;
		return bonus;
	}

	[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ServerSortSendZDOS))]
	private static class PrioritizeActors
	{
		private static void Postfix(List<ZDO> objects)
		{
			if (!Network.ImprovementEnabled(Network.prioritizeActors) || objects.Count < 2)
			{
				return;
			}

			prioritized.Clear();
			remaining.Clear();

			foreach (ZDO zdo in objects)
			{
				float bonus = GetBonus(zdo);
				if (bonus > 0f)
				{
					zdo.m_tempSortValue -= bonus;
					prioritized.Add(zdo);
				}
				else
				{
					remaining.Add(zdo);
				}
			}

			if (prioritized.Count == 0)
			{
				remaining.Clear();
				return;
			}

			prioritized.Sort(ZDOMan.ServerSendCompare);
			SortedMerge.Into(objects, prioritized, remaining, ZDOMan.ServerSendCompare);
			prioritized.Clear();
			remaining.Clear();
		}
	}

	[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
	private static class ClearPrefabCacheOnSceneLoad
	{
		private static void Postfix() => prefabBonuses.Clear();
	}
}
