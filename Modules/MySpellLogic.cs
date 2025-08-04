using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Network;
using UnityEngine;

namespace BlackMagicAPI.Modules;

internal class MySpellLogic : SpellLogic
{
    private int itemId;
    public override void CastSpell(GameObject playerObj, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        Debug.Log(itemId);
    }

    public override void WriteData(DataWriter dataWriter, PageController page, GameObject playerObj, Vector3 spawnPos, Vector3 viewDirectionVector, int level)
    {
        dataWriter.Write(page.GetItemID());
    }

    public override void SyncData(object[] values)
    {
        itemId = (int)values[0];
    }
}
