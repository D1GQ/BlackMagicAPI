using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Delegating;
using FishNet.Serializing;
using FishNet.Transporting;
using System.Collections;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

internal class CustomSpellPage : NetworkBehaviour
{
    internal SpellLogic? spellLogicPrefab;
    internal int pageId;
    internal float Cooldown;
    internal Material? pageMaterial;
    private float timer;

    private void Awake()
    {
        NetworkInitialize();
    }

    internal void Cast(GameObject ownerobj, Vector3 viewDirectionVector, int level, Vector3 spawnpos)
    {
        var spellLogic = Instantiate(spellLogicPrefab, spawnpos, Quaternion.identity);
        spellLogic?.CastSpell(ownerobj, spawnpos, viewDirectionVector, level);
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;

            if (timer < 2)
            {
                timer = 0;
                ReinstateEmis();
            }
        }
    }

    private void ReinstateEmis()
    {
        StartCoroutine(LerpEmis(pageMaterial, 1000f, "_emissi"));
    }

    private IEnumerator LerpEmis(Material? mat, float targetVal, string paramName)
    {
        if (mat != null)
        {
            float timer = 0f;
            float startval = mat.GetFloat(paramName);
            while (timer < 0.2f && mat != null)
            {
                mat.SetFloat(paramName, Mathf.Lerp(startval, targetVal, timer * 5f));
                yield return null;
                timer += Time.deltaTime;
            }
            if (mat != null)
            {
                mat.SetFloat(paramName, targetVal);
            }
        }
    }

    private bool netInit;
    private void NetworkInitialize()
    {
        if (netInit) return; netInit = true;
        RegisterServerRpc(0, new ServerRpcDelegate(HandleCastSpellServerCmd));
        RegisterObserversRpc(1, new ClientRpcDelegate(HandleCastSpellClientRpc));
    }

    private void CmdCastSpell(GameObject playerObj, Vector3 viewDirectionVector, int castingLevel, Vector3 spawnPos)
    {
        if (IsClientInitialized)
        {
            NetworkManager networkManager = NetworkManager;
            networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            return;
        }

        PooledWriter pooledWriter = WriterPool.Retrieve();
        pooledWriter.WriteGameObject(playerObj);
        pooledWriter.WriteVector3(viewDirectionVector);
        pooledWriter.WriteInt32(castingLevel);
        pooledWriter.WriteVector3(spawnPos);
        SendServerRpc(0, pooledWriter, Channel.Reliable, DataOrderType.Default);
        pooledWriter.Store();
    }

    private void HandleCastSpellServerCmd(PooledReader reader, Channel channel, NetworkConnection conn)
    {
        GameObject playerObj = reader.ReadGameObject();
        Vector3 viewDirectionVector = reader.ReadVector3();
        int castingLevel = reader.ReadInt32();
        Vector3 spawnPos = reader.ReadVector3();

        RpcCastSpell(playerObj, viewDirectionVector, castingLevel, spawnPos);
    }

    private void RpcCastSpell(GameObject playerObj, Vector3 viewDirectionVector, int castingLevel, Vector3 spawnPos)
    {
        if (IsClientInitialized)
        {
            NetworkManager networkManager = NetworkManager;
            networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            return;
        }

        PooledWriter pooledWriter = WriterPool.Retrieve();
        pooledWriter.WriteGameObject(playerObj);
        pooledWriter.WriteVector3(viewDirectionVector);
        pooledWriter.WriteInt32(castingLevel);
        pooledWriter.WriteVector3(spawnPos);
        SendObserversRpc(1, pooledWriter, Channel.Reliable, DataOrderType.Default, false, false, false);
        pooledWriter.Store();
    }

    private void HandleCastSpellClientRpc(PooledReader reader, Channel channel)
    {
        GameObject playerObj = reader.ReadGameObject();
        Vector3 viewDirectionVector = reader.ReadVector3();
        int castingLevel = reader.ReadInt32();
        Vector3 spawnPos = reader.ReadVector3();

        Cast(playerObj, viewDirectionVector, castingLevel, spawnPos);
    }
}
