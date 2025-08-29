using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BlackMagicAPI.Managers;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
    internal const string VersionString = "3.0.1";

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
        StartCoroutine(CoWaitForChainloaderToLog());
    }

    private IEnumerator CoWaitForChainloaderToLog()
    {
        while (!Chainloader._loaded && !SplashScreen.isFinished)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        ModSyncManager.LogAll();
    }
}