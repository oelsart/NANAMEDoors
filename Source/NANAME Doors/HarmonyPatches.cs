using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NanameDoors;

[StaticConstructorOnStartup]
class HarmonyPatches
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
            if (diagonalDoor.wallPos.Contains(c))
            {
                __result = PathGrid.ImpassableCost - 1;
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
        if (___map.edificeGrid[index] is Building_DiagonalDoor)
        {
            return false;
        }
        return true;
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
