using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static NanameDoors.ModCompat;

namespace NanameDoors;

[StaticConstructorOnStartup]
public static class GenerateDefs
{
    static GenerateDefs()
    {
        var GiveShortHash = AccessTools.MethodDelegate<GetGiveShortHash>(AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash"));
        var NewBlueprintDef_Thing = AccessTools.MethodDelegate<GetNewBlueprintDef_Thing>(AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing"));
        var NewFrameDef_Thing = AccessTools.MethodDelegate<GetNewFrameDef_Thing>(AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing"));
        var takenHashes = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
        var designationCategories = new List<DesignationCategoryDef>();
        foreach (var doorDef in DefDatabase<ThingDef>.AllDefs.Where(d => d.thingClass == typeof(Building_Door)).ToArray())
        {
            var newDef = new ThingDef();
            foreach (var field in typeof(ThingDef).GetFields())
            {
                if (!field.IsLiteral) field.SetValue(newDef, field.GetValue(doorDef));
            }
            newDef.defName = newDef.defName + "_Diagonal";
            newDef.label = "NAD.Diagonal".Translate() + newDef.label;
            newDef.thingClass = typeof(Building_DiagonalDoor);
            newDef.size = new IntVec2(2, 2);
            newDef.graphicData = new GraphicData();
            newDef.graphicData.CopyFrom(doorDef.graphicData);
            switch (doorDef.defName)
            {
                case "FenceGate":
                    newDef.modExtensions = [.. newDef.modExtensions.AddItem(new FenceGateMoverGraphics("NanameDoors/FenceGateMovers"))];
                    newDef.graphicData.linkFlags = LinkFlags.Fences;
                    break;
                case "VFEArch_AnimalGate":
                    newDef.modExtensions = [.. newDef.modExtensions.AddItem(new FenceGateMoverGraphics("NanameDoors/AnimalGateMovers"))];
                    newDef.graphicData.linkFlags = LinkFlags.Fences;
                    break;
                default:
                    newDef.graphicData.linkFlags = LinkFlags.Wall | LinkFlags.Rock;
                    break;
            }
            newDef.drawerType = DrawerType.MapMeshAndRealTime;
            newDef.shortHash = 0;
            GiveShortHash(newDef, typeof(ThingDef), takenHashes[typeof(ThingDef)]);
            newDef.modContentPack = NanameDoors.content;
            DefGenerator.AddImpliedDef(newDef);
            DefDatabase<BuildableDef>.Add(newDef);
            var bluePrintDef = NewBlueprintDef_Thing(newDef, false);
            bluePrintDef.shortHash = 0;
            GiveShortHash(bluePrintDef, typeof(ThingDef), takenHashes[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(bluePrintDef);
            var frameDef = NewFrameDef_Thing(newDef);
            frameDef.shortHash = 0;
            GiveShortHash(frameDef, typeof(ThingDef), takenHashes[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(frameDef);

            if (doorDef.BuildableByPlayer)
            {
                designationCategories.Add(doorDef.designationCategory);
                if (doorDef.designatorDropdown is null)
                {
                    var dropdown = new DesignatorDropdownGroupDef
                    {
                        defName = doorDef.defName
                    };
                    doorDef.designatorDropdown = dropdown;
                    newDef.designatorDropdown = dropdown;
                }
                else if (MaterialSubMenu.Active)
                {
                    newDef.designatorDropdown = doorDef.designatorDropdown;
                }
            }
            NanameDoors.Mod.nanameDoors[doorDef] = newDef;
        }
        
        foreach (var designationCategory in designationCategories)
        {
            designationCategory.ResolveReferences();
        }
    }

    private delegate void GetGiveShortHash(Def def, Type defType, HashSet<ushort> takenHashes);

    private delegate ThingDef GetNewBlueprintDef_Thing(ThingDef def, bool isInstallBlueprint, ThingDef normalBlueprint = null, bool hotReload = false);

    private delegate ThingDef GetNewFrameDef_Thing(ThingDef def, bool hodReload = false);
}
