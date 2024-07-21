﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using System.Reflection.Emit;
using Verse.AI;

namespace NanameDoors
{
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
    public static class Building_Door_StuckOpen_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            var codes = instructions.ToList();
            var label = ILGenerator.DefineLabel();
            codes[0] = codes[0].WithLabels(label);
            codes.InsertRange(0, new List<CodeInstruction>
            {
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }
    }

    //DiagonalDoor内での斜め移動許可
    [HarmonyPatch(typeof(PathFinder), "BlocksDiagonalMovement", typeof(int), typeof(PathingContext), typeof(bool))]
    public static class PathFinder_BlocksDiagonalMovement_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Isinst && (c.operand as Type) == typeof(Building_Door));
            var label = (Label)codes[pos + 1].operand;
            codes.InsertRange(pos + 2, new List<CodeInstruction>
            {
                CodeInstruction.LoadLocal(0),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brtrue_S, label),
            });
            return codes;
        }
    }

    //DoorからDoorへ移動する時コストが加算されるが、斜め移動の場合それを無視
    [HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class PathGrid_CalculatedCostAt_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && (c.operand as MethodInfo) == AccessTools.Method(typeof(GridsUtility), "GetEdifice")) - 5;
            codes[pos] = CodeInstruction.Call(typeof(IntVec3), "AdjacentToCardinal", new Type[] { typeof(IntVec3) });
            codes.Insert(pos, CodeInstruction.LoadArgument(1));
            return codes;
        }
    }

    //DiagonalDoor内では斜め移動のみを許可する
    [HarmonyPatch(typeof(PathFinder), "FindPath", typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning))]
    public static class PathFinder_FindPath_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldstr && (string)c.operand == "Edifices") + 2;
            var labelGoTo = (Label)codes[pos + 11].operand;
            var labelFalse = ILGenerator.DefineLabel();
            codes[pos] = codes[pos].WithLabels(labelFalse);
            codes.InsertRange(pos, new List<CodeInstruction>
            {
                CodeInstruction.LoadLocal(51),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                CodeInstruction.LoadLocal(33, true),
                CodeInstruction.LoadLocal(41),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                CodeInstruction.LoadLocal(42),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(IntVec3), new Type[] { typeof(int), typeof(int), typeof(int) })),
                CodeInstruction.Call(typeof(IntVec3), "AdjacentToCardinal", new Type[] { typeof(IntVec3) }),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.Call(typeof(PathFinder), "PfProfilerEndSample"),
                new CodeInstruction(OpCodes.Br, labelGoTo)
            });
            return codes;
        }
    }

    [HarmonyPatch(typeof(GhostUtility), "GhostGraphicFor")]
    public static class GhostUtility_GhostGraphicFor_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && (c.operand as MethodInfo) == AccessTools.PropertyGetter(typeof(IntVec2), "One")) + 3;
            var labelTrue = (Label)codes[pos - 9].operand;
            var labelFalse = (Label)codes[pos - 1].operand;
            codes[pos - 1] = new CodeInstruction(OpCodes.Brtrue_S, labelTrue);
            codes.InsertRange(pos, new List<CodeInstruction>
            {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadField(typeof(ThingDef), "thingClass"),
                new CodeInstruction(OpCodes.Ldtoken, typeof(Building_DiagonalDoor)),
                CodeInstruction.Call(typeof(Type), "GetTypeFromHandle"),
                CodeInstruction.Call(typeof(Type), "op_Equality"),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse)
            });
            return codes;
        }
    }
}
