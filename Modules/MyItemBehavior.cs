using UnityEngine;

namespace BlackMagicAPI.Modules.Items;

internal class MyItemBehavior : ItemBehavior
{
    protected override void OnItemUse(GameObject itemOwner)
    {
        SendItemSync(5, 5);
    }

    protected override void HandleItemSync(uint syncId, object[] args)
    {
        Debug.LogError($"{syncId}, {(int)args[0]}");
    }
}
