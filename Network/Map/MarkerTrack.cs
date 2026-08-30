using System.Collections.Generic;
using UnityEngine;

namespace Network.Map;

internal sealed class MarkerTrack
{
	private const int MaxSnapshots = 20;

	private readonly List<(float time, Vector3 position)> snapshots = new();
	private Vector3 velocity;
	private float arrivalGap = 0.5f;
	private float lastArrival = -1f;

	public float LastArrival => lastArrival;

	// MarkerTrack.Delay sits 1.3 intervals back.
	public float Delay => Mathf.Clamp(arrivalGap * 1.3f, 0.15f, 2f);

	public void Add(float now, Vector3 pos, float teleportThreshold)
	{
		if (snapshots.Count > 0 && now <= snapshots[snapshots.Count - 1].time)
		{
			return;
		}

		if (lastArrival >= 0f)
		{
			// This prevents markers jumping around.
			arrivalGap = Mathf.Lerp(arrivalGap, Mathf.Clamp(now - lastArrival, 0.05f, 3f), 0.25f);
		}

		lastArrival = now;

		if (snapshots.Count > 0)
		{
			(float time, Vector3 position) last = snapshots[snapshots.Count - 1];
			if (Vector3.Distance(last.position, pos) > teleportThreshold)
			{
				snapshots.Clear();
				velocity = Vector3.zero;
			}
			else
			{
				float dt = now - last.time;
				velocity = dt > 0.01f ? Vector3.ClampMagnitude((pos - last.position) / dt, 50f) : Vector3.zero;
			}
		}

		snapshots.Add((now, pos));
		while (snapshots.Count > MaxSnapshots)
		{
			snapshots.RemoveAt(0);
		}
	}

	public bool TryGet(float renderTime, out Vector3 result)
	{
		result = Vector3.zero;
		if (snapshots.Count == 0)
		{
			return false;
		}

		if (renderTime <= snapshots[0].time)
		{
			result = snapshots[0].position;
			return true;
		}

		for (int i = 1; i < snapshots.Count; ++i)
		{
			if (renderTime <= snapshots[i].time)
			{
				(float time, Vector3 position) a = snapshots[i - 1], b = snapshots[i];
				result = Vector3.Lerp(a.position, b.position, Mathf.InverseLerp(a.time, b.time, renderTime));
				return true;
			}
		}

		(float time, Vector3 position) newest = snapshots[snapshots.Count - 1];
		result = newest.position + velocity * Mathf.Min(renderTime - newest.time, arrivalGap);
		return true;
	}
}
