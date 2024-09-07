using Verse;

namespace NanameDoors
{
    internal class NanameDoors : Mod
    {
        public NanameDoors(ModContentPack content) : base(content)
        {
            NanameDoors.content = content;
        }

        public static ModContentPack content;
    }
}
