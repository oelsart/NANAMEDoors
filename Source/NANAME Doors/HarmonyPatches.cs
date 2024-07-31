using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using System.Reflection.Emit;
using Verse.AI;
using UnityEngine;

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

    //DiagonalDoor内での斜め移動許可
    [HarmonyPatch(typeof(PathFinder), "BlocksDiagonalMovement", typeof(int), typeof(PathingContext), typeof(bool))]
    public static class Patch_PathFinder_BlocksDiagonalMovement
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Isinst && (c.operand as Type) == typeof(Building_Door));
            var label = (Label)codes[pos + 1].operand;
            codes.InsertRange(pos + 2, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brtrue_S, label),
            });
            return codes;
        }
    }

    //DoorからDoorへ移動する時コストが加算されるが、斜め移動の場合それを無視
    [HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class Patch_PathGrid_CalculatedCostAt
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && (c.operand as MethodInfo) == AccessTools.Method(typeof(GridsUtility), "GetEdifice")) - 5;
            codes[pos] = CodeInstruction.Call(typeof(IntVec3), "AdjacentToCardinal", new Type[] { typeof(IntVec3) });
            codes.Insert(pos, new CodeInstruction(OpCodes.Ldarg_1));
            return codes;
        }
    }

    //DiagonalDoor内では斜め移動のみを許可する
    [HarmonyPatch(typeof(PathFinder), "FindPath", typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning))]
    public static class Patch_PathFinder_FindPath
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
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(53)),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(PathFinder), "edificeGrid"),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(PathFinder), "map"),
                CodeInstruction.LoadField(typeof(Map), "cellIndices"),
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(35)),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(IntVec3) })),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(35)),
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(43)),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(44)),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(IntVec3), new Type[] { typeof(int), typeof(int), typeof(int) })),
                CodeInstruction.Call(typeof(IntVec3), "AdjacentToCardinal", new Type[] { typeof(IntVec3) }),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                 new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(PathFinder), "PfProfilerEndSample"),
                new CodeInstruction(OpCodes.Br, labelGoTo)
            });
            return codes;
        }
    }

    [HarmonyPatch(typeof(GhostUtility), "GhostGraphicFor")]
    public static class Patch_GhostUtility_GhostGraphicFor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && (c.operand as MethodInfo) == AccessTools.PropertyGetter(typeof(ThingDef), "IsDoor")) + 2;
            var labelTrue = (Label)codes[pos - 4].operand;
            var labelFalse = (Label)codes[pos - 1].operand;
            codes[pos - 1] = new CodeInstruction(OpCodes.Brtrue_S, labelTrue);
            codes.InsertRange(pos, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.LoadField(typeof(ThingDef), "thingClass"),
                new CodeInstruction(OpCodes.Ldtoken, typeof(Building_DiagonalDoor)),
                CodeInstruction.Call(typeof(Type), "GetTypeFromHandle"),
                CodeInstruction.Call(typeof(Type), "op_Equality"),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse)
            });
            return codes;
        }
    }

    [HarmonyPatch(typeof(Building_Door), "WillCloseSoon", MethodType.Getter)]
    public static class Patch_Building_Door_WillCloseSoon
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_0) - 1;
            var label = generator.DefineLabel();
            codes[pos] = codes[pos].WithLabels(label);
            codes.InsertRange(pos, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Patch_Building_Door_WillCloseSoon), "WillCloseSoon"),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }

        public static bool WillCloseSoon(Building_Door door)
        {
            foreach (IntVec3 intVec in door.OccupiedRect())
            {
                for (int i = 0; i < 5; i++)
                {
                    IntVec3 c = intVec + GenAdj.CardinalDirectionsAndInside[i];
                    if (c.InBounds(door.Map))
                    {
                        List<Thing> thingList = c.GetThingList(door.Map);
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            Pawn pawn = thingList[j] as Pawn;
                            if (pawn != null && !pawn.HostileTo(door) && !pawn.Downed && (pawn.Position == intVec || (pawn.pather.Moving && pawn.pather.nextCell == intVec)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Building_Door), "BlockedOpenMomentary", MethodType.Getter)]
    public static class Patch_Building_Door_BlockedOpenMomentary
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var pos = 0;
            var label = generator.DefineLabel();
            codes[pos] = codes[pos].WithLabels(label);
            codes.InsertRange(pos, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Patch_Building_Door_BlockedOpenMomentary), "BlockedOpenMomentary"),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }

        public static bool BlockedOpenMomentary(Building_Door door)
        {
            foreach (IntVec3 c in door.OccupiedRect())
            {
                List<Thing> thingList = c.GetThingList(door.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Pawn)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Region), "Allows")]
    public static class Patch_Region_Allows
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_S && (c.operand as LocalBuilder).LocalIndex == 4);
            var pos2 = codes.FindIndex(c => codes.IndexOf(c) > pos && c.opcode == OpCodes.Bne_Un_S);
            var label = codes[pos + 1].operand;
            codes.RemoveRange(pos, pos2 - pos + 1);
            codes.InsertRange(pos, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(4)),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Patch_Region_Allows), "Allows"),
                new CodeInstruction(OpCodes.Brtrue_S, label)
            });
            return codes;
        }

        public static bool Allows(ByteGrid avoidGrid, Region region)
        {
            if (avoidGrid != null)
            {
                foreach (IntVec3 intVec in region.door.OccupiedRect())
                {
                    if (avoidGrid[intVec] == 255 && intVec.GetRegion(region.Map, RegionType.Set_Passable) == region)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(RegionDirtyer), "Notify_ThingAffectingRegionsDespawned")]
    public static class Patch_RegionDirtyer_Notify_ThingAffectingRegionsDespawned
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label = generator.DefineLabel();
            codes[0] = codes[0].WithLabels(label);

            codes.InsertRange(0, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(RegionDirtyer), "Notify_ThingAffectingRegionsSpawned"),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
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
            codes.InsertRange(pos, new CodeInstruction[]
            {
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Isinst, typeof(Building_DiagonalDoor)),
            new CodeInstruction(OpCodes.Brfalse_S, labelLdloc),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(Building_DiagonalDoor), "get_DrawPos"),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.LoadField(typeof(Building_DiagonalDoor), "doorOffset"),
            new CodeInstruction(OpCodes.Ldc_R4, 0.5f),
            CodeInstruction.Call(typeof(Vector3), "op_Multiply", new Type[] { typeof(Vector3), typeof(float) }),
            CodeInstruction.Call(typeof(Vector3), "op_Subtraction"),
            CodeInstruction.Call(typeof(IntVec3), "FromVector3", new Type[] { typeof(Vector3) }),
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(Pawn_PathFollower), "nextCell"),
            CodeInstruction.Call(typeof(IntVec3), "op_Equality"),
            new CodeInstruction(OpCodes.Brtrue_S, labelLdnull),
            });
            return codes;
        }
    }
}
