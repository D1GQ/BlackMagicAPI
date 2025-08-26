using BepInEx;
using BepInEx.Logging;
using BlackMagicAPI.Managers;
using HarmonyLib;

namespace BlackMagicAPI;

/// <inheritdoc/>
[BepInProcess("MageArena")]
[BepInDependency("com.magearena.modsync", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.d1gq.fish.utilities", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(MyGUID, PluginName, VersionString)]
public class BMAPlugin : BaseUnityPlugin
{
    internal static BMAPlugin Instance { get; private set; }
    private const string MyGUID = "com.d1gq.black.magic.api";
    internal const string PluginName = "BlackMagicAPI";
    internal const string VersionString = "3.0.0";

    private static Harmony? Harmony;
    internal static ManualLogSource Log => Instance._log;
    private ManualLogSource? _log;

    /// <inheritdoc/>
    public static string modsync = "all";
    /// <inheritdoc/>
    public static string BlackMagicSyncHash { get; internal set; } = "";

    private void Awake()
    {
        _log = Logger;
        Instance = this;
        Harmony = new(MyGUID);
        Harmony.PatchAll();
        Log.LogInfo($"BlackMagicAPI v{VersionString} loaded, (Compatibility -> v{CompatibilityManager.COMPATIBILITY_VERSION})");
        SynchronizeManager.UpdateSyncHash();
    }
}