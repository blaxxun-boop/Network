using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Network.Compatibility;

namespace Network;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency(ReturnToSenderGUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
[BepInIncompatibility("VitByr.VBNetTweaks")]
public class Network : BaseUnityPlugin
{
	private const string ModName = "Network";
	private const string ModVersion = "1.1.0";
	private const string ModGUID = "org.bepinex.plugins.network";

	internal const string ReturnToSenderGUID = "redseiko.valheim.returntosender";

	public static readonly ManualLogSource NetworkLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

	public static ConfigEntry<Toggle> enableNetworkingImprovements = null!;
	public static ConfigEntry<Toggle> reportPatchConflicts = null!;
	public static ConfigEntry<Toggle> adaptiveZdoScheduler = null!;
	public static ConfigEntry<float> sendInterval = null!;
	public static ConfigEntry<int> maxPeersPerFrame = null!;
	public static ConfigEntry<Toggle> increaseZdoBatchSize = null!;
	public static ConfigEntry<int> zdoQueueLimit = null!;
	public static ConfigEntry<Toggle> refreshPeerInterestPosition = null!;
	public static ConfigEntry<Toggle> prioritizeActors = null!;
	public static ConfigEntry<Toggle> useImprovedSteamSettings = null!;
	public static ConfigEntry<float> timeoutConnected = null!;
	public static ConfigEntry<int> sendRateMax = null!;
	public static ConfigEntry<int> sendRateMin = null!;
	public static ConfigEntry<int> sendBufferSize = null!;
	public static ConfigEntry<Toggle> smoothMapMarkers = null!;
	public static ConfigEntry<float> mapSendInterval = null!;
	public static ConfigEntry<float> mapTeleportThreshold = null!;

	public static bool ImprovementEnabled(ConfigEntry<Toggle> feature) => enableNetworkingImprovements.Value == Toggle.On && feature.Value == Toggle.On;

	public enum Toggle
	{
		On = 1,
		Off = 0,
	}

	private enum ConfigScope
	{
		Installation,
		ServerHost,
		Client,
	}

	private sealed class ConfigurationManagerAttributes
	{
		public int? Order;
	}

	public void Awake()
	{
		int order = 0;

		enableNetworkingImprovements = Bind("1 - General", "Enable Networking Improvements", Toggle.On, "Master switch for every added feature. Off leaves only Steam's send-limit removal and the early ZDOData connection guard active. The connection timeout stays vanilla. Config values are never synced; change them separately on each server, host or client.", ConfigScope.Installation);

		reportPatchConflicts = Bind("1 - General", "Report Patch Conflicts", Toggle.On, "Warn when another mod patches the same networking methods.", ConfigScope.Installation);

		adaptiveZdoScheduler = Bind("2 - ZDO Sending", "Adaptive ZDO Scheduler", Toggle.On, "Replace vanilla's one-peer-per-frame queue with the adaptive round-robin scheduler.", ConfigScope.Installation);

		sendInterval = Bind("2 - ZDO Sending", "Send Interval", 0.05f, "Slowest allowed full lap. The default targets 20/s, boosts small servers to 30/s, then tapers between 10 and 15 peers. Actual rate cannot exceed framerate. Lower costs real CPU and upstream.", ConfigScope.Installation, new AcceptableValueRange<float>(0.01f, 0.2f));

		maxPeersPerFrame = Bind("2 - ZDO Sending", "Max Peers Per Frame", 0, "Emergency brake on how much sending work one frame may do. 0 is no cap and the work is already spread evenly across frames, so leave it there unless you are chasing a stutter.", ConfigScope.Installation, new AcceptableValueRange<int>(0, 128));

		increaseZdoBatchSize = Bind("2 - ZDO Sending", "Increase Batch Size", Toggle.On, "Use the configured batch size instead of vanilla's 10240-byte limit.", ConfigScope.Installation);

		zdoQueueLimit = Bind("2 - ZDO Sending", "Batch Size", 20480, "Bytes per ZDO batch (vanilla 10240). Raising this or lowering the send interval costs upstream bandwidth, so watch your host.", ConfigScope.Installation, new AcceptableValueRange<int>(10240, 262144));

		refreshPeerInterestPosition = Bind("2 - ZDO Sending", "Refresh Peer Interest Position", Toggle.On, "Use a peer's live character ZDO position when the server chooses nearby objects to send.", ConfigScope.ServerHost);

		prioritizeActors = Bind("3 - Send Priority", "Prioritize Players And Creatures", Toggle.On, "Bias players, ships and creatures ahead of scenery within vanilla's existing object and ownership tiers.", ConfigScope.ServerHost);

		useImprovedSteamSettings = Bind("4 - Steam", "Use Improved Steam Settings", Toggle.On, "Use the timeout, rate and buffer settings below. Off removes Steam's send limit, uses a 100 MB client buffer and leaves the timeout vanilla.", ConfigScope.Installation);

		timeoutConnected = Bind("4 - Steam", "Connection Timeout", 120000f, "Milliseconds before Steam drops an idle connection (vanilla 30000). Applied before each Steam socket opens.", ConfigScope.Installation, new AcceptableValueRange<float>(30000f, 600000f));

		sendRateMax = Bind("4 - Steam", "Send Rate Ceiling", 50000000, "Maximum bytes/sec Steam's bandwidth estimator may use (vanilla 153600). This is permission, not a target. Lower it if you want a hard per-connection ceiling.", ConfigScope.Installation, new AcceptableValueRange<int>(153600, 50000000));

		// One vanilla constant feeds both. I put the floor back after raising the ceiling.
		sendRateMin = Bind("4 - Steam", "Send Rate Floor", 153600, "Bytes/sec Steam is allowed to throttle down to. Low is good - it's a floor, not a target. Set it to 50000000 to match the ceiling and get the old pinned behaviour back.", ConfigScope.Installation, new AcceptableValueRange<int>(16384, 50000000));

		sendBufferSize = Bind("4 - Steam", "Send Buffer Size", 4194304, "Maximum bytes Steam may hold pending per connection (Steam default 524288). Four MB leaves room for bursts without the 100 MB backlog older Network releases allowed.", ConfigScope.Installation, new AcceptableValueRange<int>(524288, 16777216));

		smoothMapMarkers = Bind("5 - Map Markers", "Smooth Player Markers", Toggle.On, "Interpolate other players' map markers instead of letting them sit still for two seconds and jump.", ConfigScope.Installation);

		mapSendInterval = Bind("5 - Map Markers", "Position Send Interval", 0.5f, "Seconds between marker position broadcasts (vanilla player lists update every 2.0). Clients work out their own interpolation delay from what actually arrives.", ConfigScope.ServerHost, new AcceptableValueRange<float>(0.1f, 2f));

		mapTeleportThreshold = Bind("5 - Map Markers", "Teleport Threshold", 50f, "Metres of movement between updates that counts as a teleport, where the marker snaps instead of sliding.", ConfigScope.Client, new AcceptableValueRange<float>(10f, 500f));

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);
		ReturnToSenderCompatibility.Handle(harmony);

		return;

		ConfigEntry<T> Bind<T>(string section, string name, T defaultValue, string description, ConfigScope scope, AcceptableValueBase? acceptableValues = null)
		{
			string where = scope switch
			{
				ConfigScope.ServerHost => "Server/Host",
				ConfigScope.Client => "Client",
				_ => "This installation",
			};

			return Config.Bind(section, name, defaultValue, new ConfigDescription($"{description} [Runs on: {where}]", acceptableValues, new ConfigurationManagerAttributes { Order = --order }));
		}
	}

	private void Start()
	{
		if (ImprovementEnabled(reportPatchConflicts))
		{
			ConflictCheck.Report(ModGUID);
		}
	}
}
