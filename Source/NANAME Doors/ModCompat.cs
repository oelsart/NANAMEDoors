using Verse;

namespace NanameDoors;

public static class ModCompat
{
    public static class DiagonalWalls
    {
        public static string PackageId = "chv.DiagonalWalls2".ToLower();

        public static readonly bool Active = ModsConfig.IsActive(PackageId);

        public static readonly DesignationCategoryDef DesignationCategoryDef;

        static DiagonalWalls()
        {
            if (Active)
            {
                DesignationCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamed("chv_Diagonal");
            }
        }
    }
}
