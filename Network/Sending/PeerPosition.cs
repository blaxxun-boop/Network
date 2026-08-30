using UnityEngine;

namespace Network.Sending;

internal static class PeerPosition
{
	public static bool TryGet(ZDOID id, out Vector3 position)
	{
		ZDO? zdo = ZDOMan.instance?.GetZDO(id);
		if (zdo == null)
		{
			position = Vector3.zero;
			return false;
		}

		position = zdo.GetPosition();
		return true;
	}

	public static Vector3 GetOr(ZDOID id, Vector3 fallback) => TryGet(id, out Vector3 position) ? position : fallback;
}
