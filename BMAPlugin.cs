using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BetterVoiceDetection;

[BepInProcess("MageArena")]
[BepInDependency("com.magearena.modsync", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(MyGUID, PluginName, VersionString)]
public class BMAPlugin : BaseUnityPlugin
{
    internal static BMAPlugin Instance { get; private set; }
    private const string MyGUID = "com.d1gq.black.magic.api";
    internal const string PluginName = "BlackMagicAPI";
    private const string VersionString = "1.1.0";

    private static Harmony? Harmony;
    internal static ManualLogSource Log => Instance._log;
    private ManualLogSource? _log;

    public static string modsync = "all";

    private void Awake()
    {
        _log = Logger;
        Instance = this;
        Harmony = new(MyGUID);
        Harmony.PatchAll();
        Log.LogInfo($"BlackMagicAPI v{VersionString} loaded!");
    }
}