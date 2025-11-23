using System.Collections.Generic;
using Verse;

namespace NanameDoors;

public class NanameDoors : Mod
{
    public static ModContentPack content;
    
    public static NanameDoors Mod { get; private set; }

    public readonly Dictionary<ThingDef, ThingDef> nanameDoors = [];
    
    public NanameDoors(ModContentPack content) : base(content)
    {
        NanameDoors.content = content;
        Mod = this;
    }
}
