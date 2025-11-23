using Verse;

namespace NanameDoors;

public static class ModCompat
{
    public static class DiagonalWalls
    {
        public static readonly string PackageId = "chv.DiagonalWalls2".ToLower();

        public static readonly bool Active = ModsConfig.IsActive(PackageId);
    }
    
    public static class MaterialSubMenu
    {
        public static readonly bool Active = ModsConfig.IsActive("cedaro.material.submenu") || ModsConfig.IsActive("WSP.GroupedBuildings");
    }
}
