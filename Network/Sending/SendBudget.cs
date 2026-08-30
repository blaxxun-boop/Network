using UnityEngine;

namespace Network.Sending;

internal static class SendBudget
{
	private const float SmallServerRate = 1.5f;
	private const float BoostWorkPeers = 15f;

	public static float PerPeerRate(int peerCount, float interval)
	{
		float baseline = 1f / Mathf.Max(interval, 0.01f);
		return peerCount > 0 ? baseline * Mathf.Max(1f, Mathf.Min(SmallServerRate, BoostWorkPeers / peerCount)) : 0f;
	}

	// 16 peers at 30 fps needs 10.7/frame. Carry the 0.7.
	public static int Take(ref float budget, int peerCount, float deltaTime, float interval, int maxPerFrame)
	{
		if (peerCount <= 0)
		{
			budget = 0f;
			return 0;
		}

		budget += peerCount * deltaTime * PerPeerRate(peerCount, interval);

		int scheduled = (int)budget;

		// Don't carry capped work into the next SendBudget.
		budget -= scheduled;

		// A hitch can hand me 1 second of dt. Lap once.
		if (scheduled <= peerCount)
		{
			return maxPerFrame > 0 ? Mathf.Min(scheduled, maxPerFrame) : scheduled;
		}

		scheduled = peerCount;
		budget = 0f;

		return maxPerFrame > 0 ? Mathf.Min(scheduled, maxPerFrame) : scheduled;
	}
}
