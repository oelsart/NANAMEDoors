using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NanameDoors
{
    public static class DiagonalDoorUtility
    {
        public static Vector3 DoorOffset(IntVec3 loc, Map map, bool preferFences, Vector3 doorOffset)
        {
            MethodInfo AlignQualityAgainst = AccessTools.Method(typeof(DoorUtility), "AlignQualityAgainst");
            (IntVec3 vec, int value)[] num = new (IntVec3, int)[4];
            num[0] = (IntVec3.NorthWest, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.West, map, preferFences));
            num[1] = (IntVec3.SouthEast, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.South, map, preferFences));
            loc += IntVec3.NorthEast;
            num[0].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.North, map, preferFences);
            num[1].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.East, map, preferFences);
            
            loc += IntVec3.West;
            num[2] = (IntVec3.NorthEast, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.North, map, preferFences));
            num[3] = (IntVec3.SouthWest, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.West, map, preferFences));
            loc += IntVec3.SouthEast;
            num[2].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.East, map, preferFences);
            num[3].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.South, map, preferFences);
            var list = num.ToList();
            list.SortByDescending(n => n.vec == doorOffset.ToIntVec3());
            list.SortByDescending(n => n.value);
            return list[0].vec.ToVector3();
        }

        private delegate int GetAlignQualityAgainst(IntVec3 c, IntVec3 offset, Map map, bool preferFences);

        private static GetAlignQualityAgainst AlignQualityAgainst = AccessTools.MethodDelegate<GetAlignQualityAgainst>(AccessTools.Method(typeof(DoorUtility), "AlignQualityAgainst"));
    }
}
