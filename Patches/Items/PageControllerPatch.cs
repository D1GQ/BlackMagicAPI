using BlackMagicAPI.Helpers;
using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Network;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(PageController))]
internal class PageControllerPatch
{
    private static bool IsCustomSpell(PageController __instance) => __instance.spellprefab.GetComponent<SpellLogic>() != null;
    private static SpellLogic GetSpellPrefab(PageController __instance) => __instance.spellprefab.GetComponent<SpellLogic>();

    [HarmonyPatch(nameof(PageController.CastSpellServer))]
    [HarmonyPrefix]
    private static bool CastSpellServer_Prefix(PageController __instance, GameObject ownerobj, Vector3 fwdVector, int level, Vector3 spawnpos)
    {
        if (IsCustomSpell(__instance))
        {
            if (!__instance.IsClientInitialized)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
                return false;
            }
            Channel channel = Channel.Reliable;
            PooledWriter pooledWriter = WriterPool.Retrieve();
            pooledWriter.WriteGameObject(ownerobj);
            pooledWriter.WriteVector3(fwdVector);
            pooledWriter.WriteInt32(level);
            pooledWriter.WriteVector3(spawnpos);

            var dataWriter = new DataWriter();
            GetSpellPrefab(__instance).WriteData(dataWriter, __instance, ownerobj, spawnpos, fwdVector, level);
            dataWriter.WriteFromBuffer(pooledWriter);
            dataWriter.Dispose();

            __instance.SendServerRpc(0U, pooledWriter, channel, DataOrderType.Default);
            pooledWriter.Store();

            return false;
        }

        return true;
    }

    [HarmonyPatch]
    private static class RpcReaderServerCastSpellServerPatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<PageController>("RpcReader___Server_CastSpellServer");

        [HarmonyPrefix]
        private static bool Prefix(PageController __instance, PooledReader PooledReader0, Channel channel, NetworkConnection conn)
        {
            if (IsCustomSpell(__instance))
            {
                GameObject ownerobj = PooledReader0.ReadGameObject();
                Vector3 fwdVector = PooledReader0.ReadVector3();
                int level = PooledReader0.ReadInt32();
                Vector3 spawnpos = PooledReader0.ReadVector3();
                var dataWriter = new DataWriter();
                dataWriter.ReadToBuffer(PooledReader0);
                if (__instance.IsServerInitialized)
                {
                    CastSpellObs_Write(__instance, ownerobj, fwdVector, level, spawnpos, dataWriter);
                }

                return false;
            }

            return true;
        }
    }

    private static void CastSpellObs_Write(PageController __instance, GameObject ownerobj, Vector3 fwdVector, int level, Vector3 spawnpos, DataWriter dataWriter)
    {
        if (!__instance.IsServerInitialized)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            return;
        }
        Channel channel = Channel.Reliable;
        PooledWriter pooledWriter = WriterPool.Retrieve();
        pooledWriter.WriteGameObject(ownerobj);
        pooledWriter.WriteVector3(fwdVector);
        pooledWriter.WriteInt32(level);
        pooledWriter.WriteVector3(spawnpos);
        dataWriter.WriteFromBuffer(pooledWriter);
        dataWriter.Dispose();

        __instance.SendObserversRpc(1U, pooledWriter, channel, DataOrderType.Default, false, false, false);
        pooledWriter.Store();
    }

    private static void CastSpellObs_Logic(PageController __instance, GameObject ownerobj, Vector3 fwdVector, int level, Vector3 spawnpos, PooledReader PooledReader0)
    {
        if (ownerobj != null)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(__instance.spellprefab, spawnpos, Quaternion.identity);
            if (gameObject.TryGetComponent<SpellLogic>(out var spell))
            {
                var dataWriter = new DataWriter();
                dataWriter.ReadToBuffer(PooledReader0);
                spell.SyncData(dataWriter.GetObjectBuffer());
                dataWriter.Dispose();
                spell.CastSpell(ownerobj, spawnpos, fwdVector, level);
            }
        }
    }

    [HarmonyPatch]
    private static class RpcReader___Observers_CastSpellObsPatch
    {
        private static MethodBase TargetMethod()
        {
            var methods = typeof(PageController)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.Name.StartsWith("RpcReader___Observers_CastSpellObs"))
                .ToList();

            if (methods.Count == 0)
                throw new Exception("Could not find RpcReader___Observers_CastSpellObs method");

            return methods[0];
        }

        [HarmonyPrefix]
        private static bool Prefix(PageController __instance, PooledReader PooledReader0, Channel channel)
        {
            if (IsCustomSpell(__instance))
            {

                GameObject ownerobj = PooledReader0.ReadGameObject();
                Vector3 fwdVector = PooledReader0.ReadVector3();
                int level = PooledReader0.ReadInt32();
                Vector3 spawnpos = PooledReader0.ReadVector3();
                if (__instance.IsClientInitialized)
                {
                    CastSpellObs_Logic(__instance, ownerobj, fwdVector, level, spawnpos, PooledReader0);
                }

                return false;
            }

            return true;
        }
    }
}