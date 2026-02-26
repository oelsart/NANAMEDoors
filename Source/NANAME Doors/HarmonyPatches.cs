using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NanameDoors;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("com.harmony.rimworld.nanamedoors");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

//DiagonalDoorの時StuckOpen無効化
[HarmonyPatch(typeof(Building_Door), "StuckOpen", MethodType.Getter)]
public static class Patch_Building_Door_StuckOpen
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
    {
        var codes = instructions.ToList();
        var label = ILGenerator.DefineLabel();
        codes[0] = codes[0].WithLabels(label);
        codes.InsertRange(0,
        [
            CodeInstruction.LoadArgument(0),
            new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
            new CodeInstruction(OpCodes.Brfalse_S, label),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ret)
        ]);
        return codes;
    }
}

[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
public static class Patch_Map_FinalizeInit
{
    public static void Prefix(Map __instance)
    {
        foreach (var door in __instance.listerThings.GetThingsOfType<Building_DiagonalDoor>())
        {
            door.PreFinalizeInit();
        }
    }
}

//DoorからDoorへ移動する時コストが加算されるが、斜め移動の場合それを無視
[HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
public static class Patch_PathGrid_CalculatedCostAt
{
    public static void Postfix(IntVec3 c, IntVec3 prevCell, Map ___map, ref int __result)
    {
        if (c.GetEdifice(___map) is Building_DiagonalDoor diagonalDoor)
        {
            if (diagonalDoor.wallPos.Contains(c) && __result < PathGrid.ImpassableCost - 100)
            {
                __result += 100;
                return;
            }
            if (prevCell.IsValid && prevCell.GetEdifice(___map) == diagonalDoor)
            {
                __result -= 45; //ドアからドアへ移動する時のコストを斜め移動時打ち消し
            }
        }
    }
}

[HarmonyPatch(typeof(BuildingSource), "SetBuildingData")]
public static class Patch_BuildingSource_SetBuildingData
{
    public static bool Prefix(int index, Map ___map)
    {
        return ___map.edificeGrid[index] is not Building_DiagonalDoor;
    }
}

[HarmonyPatch(typeof(Pawn_PathFollower), "NextCellDoorToWaitForOrManuallyOpen")]
public static class Patch_Pawn_PathFollower_NextCellDoorToWaitForOrManuallyOpen
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var pos = codes.FindLastIndex(c => c.opcode == OpCodes.Ldloc_0);
        var labelLdloc = generator.DefineLabel();
        var labelLdnull = codes[pos - 1].operand;
        codes[pos] = codes[pos].WithLabels(labelLdloc);
        codes.InsertRange(pos,
        [
        CodeInstruction.LoadLocal(0),
        new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
        new CodeInstruction(OpCodes.Brfalse_S, labelLdloc),
        CodeInstruction.LoadLocal(0),
        CodeInstruction.Call(typeof(Building_DiagonalDoor), "get_DrawPos"),
        CodeInstruction.LoadLocal(0),
        CodeInstruction.LoadField(typeof(Building_DiagonalDoor), "doorOffset"),
        new CodeInstruction(OpCodes.Ldc_R4, 0.5f),
        CodeInstruction.Call(typeof(Vector3), "op_Multiply", [typeof(Vector3), typeof(float)]),
        CodeInstruction.Call(typeof(Vector3), "op_Subtraction"),
        CodeInstruction.Call(typeof(IntVec3), "FromVector3", [typeof(Vector3)]),
        CodeInstruction.LoadArgument(0),
        CodeInstruction.LoadField(typeof(Pawn_PathFollower), "nextCell"),
        CodeInstruction.Call(typeof(IntVec3), "op_Equality"),
        new CodeInstruction(OpCodes.Brtrue_S, labelLdnull),
        ]);
        return codes;
    }
}

[HarmonyPatch(typeof(Designator_Dropdown), "SetupFloatMenu")]
public static class Patch_Designator_Dropdown_SetupFloatMenu
{
    private static readonly AccessTools.FieldRef<Designator_Build, ThingDef> stuffDef = AccessTools.FieldRefAccess<Designator_Build, ThingDef>("stuffDef");

    private static readonly AccessTools.FieldRef<Designator_Build, bool> writeStuff = AccessTools.FieldRefAccess<Designator_Build, bool>("writeStuff");

    private static readonly AccessTools.FieldRef<Designator_Dropdown, bool> activeDesignatorSet = AccessTools.FieldRefAccess<Designator_Dropdown, bool>("activeDesignatorSet");

    private static bool Prepare()
    {
        return !ModCompat.MaterialSubMenu.Active;
    }

    public static bool Prefix(Designator_Dropdown __instance, List<Designator> ___elements, ref Window __result)
    {
        List<FloatMenuOption> list = null;
        Designator_Build designator = null;
        var flag = false;
        for (var i = 0; i < 2; i++)
        {
            if (___elements.ElementAtOrDefault(i) is not Designator_Build designator_Build) continue;
            if (designator_Build.PlacingDef is not ThingDef { MadeFromStuff: true } thingDef) continue;

            if (!NanameDoors.Mod.nanameDoors.ContainsKey(thingDef) &&
                !NanameDoors.Mod.nanameDoors.ContainsValue(thingDef)) continue;
            flag = true;
            list ??= [];
            designator ??= designator_Build;
            foreach (var item in from d in designator_Build.Map.resourceCounter.AllCountedAmounts.Keys
                     orderby d.stuffProps?.commonality ?? float.PositiveInfinity descending, d.BaseMarketValue
                     select d)
            {
                if (!item.IsStuff || !item.stuffProps.CanMake(thingDef) || (!DebugSettings.godMode &&
                                                                            designator_Build.Map.listerThings
                                                                                .ThingsOfDef(item).Count <= 0)) continue;
                var localStuffDef = item;
                var str = designator_Build.sourcePrecept == null ? GenLabel.ThingLabel(thingDef, localStuffDef) : ((string)"ThingMadeOfStuffLabel".Translate(localStuffDef.LabelAsStuff, designator_Build.sourcePrecept.Label));
                str = str.CapitalizeFirst();
                FloatMenuOption floatMenuOption = new(str, () =>
                {
                    if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(designator_Build.TutorTagSelect))
                    {
                        return;
                    }
                    designator_Build.CurActivateSound?.PlayOneShotOnCamera();
                    Find.DesignatorManager.Select(designator_Build);
                    stuffDef(designator_Build) = localStuffDef;
                    writeStuff(designator_Build) = true;
                    __instance.SetActiveDesignator(designator_Build);
                }, item)
                {
                    tutorTag = "SelectStuff-" + thingDef.defName + "-" + localStuffDef.defName
                };
                list.Add(floatMenuOption);
            }
        }

        if (!flag) return true;
        __result = new FloatMenu(list)
        {
            onCloseCallback = () =>
            {
                activeDesignatorSet(__instance) = true;
                writeStuff(designator) = true;
            }
        };
        return false;
    }
}

[HarmonyPatch("NoCrowdedContextMenu.Utilities.MenuOptionUtility", "OnBuildingPickerCreated")]
public static class Patch_MenuOptionUtility_OnBuildingPickerCreated
{
    private static bool Prepare() => ModCompat.ReplaceContextMenu.Active;
    
    public static void Prefix(List<Designator> buildings)
    {
        if (buildings.ElementAtOrDefault(0) is not Designator_Build designator_Build ||
            buildings.ElementAtOrDefault(1) is not Designator_Build designator_Build2 ||
            designator_Build.PlacingDef is not ThingDef { MadeFromStuff: true } thingDef ||
            designator_Build2.PlacingDef is not ThingDef { MadeFromStuff: true } thingDef2 ||
            !NanameDoors.Mod.nanameDoors.ContainsKey(thingDef) ||
            !NanameDoors.Mod.nanameDoors.ContainsValue(thingDef2)) return;

        var count = designator_Build.Map.resourceCounter.AllCountedAmounts.Keys
            .Count(item => !item.IsStuff || !item.stuffProps.CanMake(thingDef) || (!DebugSettings.godMode &&
                designator_Build.Map.listerThings.ThingsOfDef(item).Count <= 0));

        for (var i = 0; i < count - 1; i++)
        {
            buildings.Insert(0, designator_Build);
        }
        for (var i = 0; i < count - 1; i++)
        {
            buildings.Add(designator_Build2);
        }
    }
}