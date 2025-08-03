using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameDoors;

public static class DiagonalDoorUtility
{
    private delegate int GetAlignQualityAgainst(IntVec3 c, IntVec3 offset, Map map, bool preferFences);

    private static readonly GetAlignQualityAgainst AlignQualityAgainst = AccessTools.MethodDelegate<GetAlignQualityAgainst>(AccessTools.Method(typeof(DoorUtility), "AlignQualityAgainst"));

    public static Vector3 DoorOffset(IntVec3 loc, Map map, bool preferFences, Vector3 doorOffset)
    {
        (IntVec3 vec, int value)[] num = new (IntVec3, int)[4];
        num[0] = (IntVec3.NorthWest, AlignQualityAgainst(loc, IntVec3.West, map, preferFences));
        num[1] = (IntVec3.SouthEast, AlignQualityAgainst(loc, IntVec3.South, map, preferFences));
        loc += IntVec3.NorthEast;
        num[0].value += AlignQualityAgainst(loc, IntVec3.North, map, preferFences);
        num[1].value += AlignQualityAgainst(loc, IntVec3.East, map, preferFences);

        loc += IntVec3.West;
        num[2] = (IntVec3.NorthEast, AlignQualityAgainst(loc, IntVec3.North, map, preferFences));
        num[3] = (IntVec3.SouthWest, AlignQualityAgainst(loc, IntVec3.West, map, preferFences));
        loc += IntVec3.SouthEast;
        num[2].value += AlignQualityAgainst(loc, IntVec3.East, map, preferFences);
        num[3].value += AlignQualityAgainst(loc, IntVec3.South, map, preferFences);
        var list = num.ToList();
        list.SortByDescending(n => n.vec == doorOffset.ToIntVec3());
        list.SortByDescending(n => n.value);
        return list[0].vec.ToVector3();
    }
}
