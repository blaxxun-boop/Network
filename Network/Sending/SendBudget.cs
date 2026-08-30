using UnityEngine;

namespace Network.Sending;

internal static class SendBudget
{
	private const float SmallServerRate = 1.5f;
	private const float BoostWorkPeers = 15f;

	public static float PerPeerRate(int count, float interval)
	{
		float baseline = 1f / Mathf.Max(interval, 0.01f);
		return count > 0 ? baseline * Mathf.Max(1f, Mathf.Min(SmallServerRate, BoostWorkPeers / count)) : 0f;
	}

	// 16 peers at 30 fps needs 10.7/frame. Carry the 0.7.
	public static int Take(ref float budget, int count, float dt, float interval, int cap)
	{
		if (count <= 0)
		{
			budget = 0f;
			return 0;
		}

		budget += count * dt * PerPeerRate(count, interval);

		int serve = (int)budget;

		// Don't carry capped work into the next SendBudget.
		budget -= serve;

		// A hitch can hand me 1 second of dt. Lap once.
		if (serve > count)
		{
			serve = count;
			budget = 0f;
		}

		return cap > 0 ? Mathf.Min(serve, cap) : serve;
	}
}
