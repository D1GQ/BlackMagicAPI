using BlackMagicAPI.Modules.Spells;

namespace BlackMagicAPI.Modules;

internal class MyItemData : ItemData
{
    public override string Name => "Test";

    public override bool DebugForceSpawn => true;
}
