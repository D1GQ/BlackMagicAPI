using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Delegating;
using FishNet.Serializing;
using FishNet.Transporting;
using System.Collections;
using UnityEngine;

namespace BlackMagicAPI.Modules.Items;

/// <summary>
/// Abstract base class for item behavior logic.
/// Provides the core interface for item functionality and initialization.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public abstract class ItemBehavior : NetworkBehaviour, IInteractable, IItemInteraction
{
    /// <inheritdoc/>
    [SerializeField]
    [Tooltip("The visual representation of the item in the game world, be sure that it's parenting to this object!")]
    public GameObject? ItemRender;

    /// <inheritdoc/>
    [SerializeField]
    [Tooltip("Sound played when the item is picked up")]
    public AudioClip? EquipSound;

    /// <inheritdoc/>
    [SerializeField]
    [Tooltip("Sound played when the item is dropped")]
    public AudioClip? DropSound;

    [SerializeField]
    [Tooltip("Display name of the item, (DO NOT SET)")]
    internal string Name = "???";

    [SerializeField]
    [Tooltip("Unique identifier for the item type, (DO NOT SET)")]
    internal int Id;

    [SerializeField]
    [Tooltip("Keep item on death, (DO NOT SET)")]
    internal bool KeepOnDeath;

    /// <summary>
    /// Initializes the network components when the item is created.
    /// </summary>
    protected virtual void Awake()
    {
        NetworkInitialize();
    }

    /// <summary>
    /// Gets the unique identifier for this item type.
    /// </summary>
    /// <returns>The item's ID</returns>
    public int GetItemID() => Id;

    /// <summary>
    /// Generates the interaction text displayed to players.
    /// </summary>
    /// <param name="player">The player attempting to interact</param>
    /// <returns>Formatted interaction text</returns>
    public string DisplayInteractUI(GameObject player) => $"Grasp {Name}";

    /// <summary>
    /// Initializes the item when the item is equipped.
    /// </summary>
    public void ItemInit()
    {
        ItemRender?.SetActive(true);
        PlayEquipSound();
        if (!waitingInitItem)
        {
            StartCoroutine(CoWaitInitItem());
        }
    }

    /// <summary>
    /// Initializes the item for observers (other clients).
    /// </summary>
    public void ItemInitObs()
    {
        ItemInit();
    }

    private bool waitingInitItem;
    private IEnumerator CoWaitInitItem()
    {
        waitingInitItem = true;
        float wait = 0f;
        while (transform?.parent?.name != "pikupact")
        {
            wait += Time.deltaTime;
            if (wait >= 10f)
            {
                waitingInitItem = false;
                yield break;
            }

            yield return null;
        }

        OnItemEquipped(transform.parent.parent.GetComponent<PlayerMovement>());
        waitingInitItem = false;
    }

    /// <summary>
    /// Handles interaction with the item by a player.
    /// </summary>
    /// <param name="player">The player interacting with the item</param>
    public void Interact(GameObject player)
    {
        player.GetComponent<PlayerInventory>().Pickup(gameObject);
    }

    /// <summary>
    /// Handles primary interaction/use of the item.
    /// </summary>
    /// <param name="player">The player using the item</param>
    public void Interaction(GameObject player)
    {
        OnItemUse(player.GetComponent<PlayerMovement>());
    }

    /// <summary>
    /// Virtual method for handling item-specific usage logic.
    /// </summary>
    /// <param name="itemOwner">The player using the item</param>
#pragma warning disable CS0618 // Type or member is obsolete
    protected virtual void OnItemUse(PlayerMovement itemOwner) { OnItemUse(itemOwner.gameObject); }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Virtual method for handling item-specific Equipped logic.
    /// </summary>
    /// <param name="itemOwner">The player using the item</param>
    protected virtual void OnItemEquipped(PlayerMovement itemOwner) { }

    /// <summary>
    /// Placeholder for secondary interaction (unused).
    /// </summary>
    /// <param name="subject">The interaction subject</param>
    public void Interaction2(GameObject subject) { }

    /// <summary>
    /// Drops the item into the game world.
    /// </summary>
    public void DropItem()
    {
        LayerMask mask = 192;
        if (Physics.Raycast(transform.position, Vector3.down, out var raycastHit, 100f, ~mask))
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            transform.position = raycastHit.point;
        }
        ItemRender?.SetActive(true);
        PlayDropSound();
    }

    /// <summary>
    /// Hides the item's visual representation.
    /// </summary>
    public void HideItem()
    {
        ItemRender?.SetActive(false);
    }

    /// <summary>
    /// Plays the drop sound effect.
    /// </summary>
    public void PlayDropSound()
    {
        if (DropSound == null) return;
        GetComponent<AudioSource>()?.PlayOneShot(DropSound);
    }

    private void PlayEquipSound()
    {
        if (EquipSound == null) return;
        GetComponent<AudioSource>()?.PlayOneShot(EquipSound);
    }

    /// <summary>
    /// Resets the item's scale and rotation to default values.
    /// </summary>
    public virtual void SetScale()
    {
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    internal void SetAudioClips(AudioClip drop, AudioClip equip)
    {
        DropSound = drop;
        EquipSound = equip;
    }

    /// <summary>
    /// Called automatically when a item prefab is created programmatically.
    /// Allows for custom initialization of item prefabs.
    /// </summary>
    /// <param name="prefab">The GameObject of the created spell prefab</param>
    public virtual void OnPrefabCreatedAutomatically(GameObject prefab) { }

    private bool netInit;
    private void NetworkInitialize()
    {
        if (netInit) return; netInit = true;
        RegisterObserversRpc(0, new ClientRpcDelegate(HandleSyncClientRpc));
    }

    /// <summary>
    /// Sends synchronization data to clients and self.
    /// </summary>
    /// <param name="syncId">Identifier for the sync operation</param>
    /// <param name="args">Data to synchronize</param>
    protected void SendItemSync(uint syncId, params object[] args)
    {
        if (!IsClientInitialized)
        {
            NetworkManager networkManager = NetworkManager;
            networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            return;
        }

        PooledWriter writer = WriterPool.Retrieve();
        writer.Write(syncId);
        var dataWriter = new DataWriter();
        foreach (var arg in args)
            dataWriter.Write(arg);
        dataWriter.WriteFromBuffer(writer);
        dataWriter.Dispose();
        SendObserversRpc(0, writer, Channel.Reliable, DataOrderType.Default, false, false, false);
        writer.Store();
    }

    /// <summary>
    /// Handles received synchronization data from clients that used SendItemSync.
    /// </summary>
    /// <param name="syncId">Identifier for the sync operation</param>
    /// <param name="args">Received synchronization data</param>
    protected virtual void HandleItemSync(uint syncId, object[] args) { }

    private void HandleSyncClientRpc(PooledReader reader, Channel channel)
    {
        var syncId = reader.Read<uint>();
        var dataWriter = new DataWriter();
        dataWriter.ReadToBuffer(reader);
        HandleItemSync(syncId, dataWriter.GetObjectBuffer());
        dataWriter.Dispose();
    }

    /// <summary>
    /// (Deprecated) Virtual method for handling item-specific usage logic.
    /// </summary>
    /// <param name="itemOwner">The player GameObject using the item</param>
    /// <remarks>
    /// This method is obsolete. Override OnItemUse(PlayerMovement) instead.
    /// </remarks>
    [Obsolete("This method is deprecated. Please override OnItemUse(PlayerMovement) instead.")]
    protected virtual void OnItemUse(GameObject itemOwner) { }
}