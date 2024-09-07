using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameDoors
{
    public static class DiagonalDoorUtility
    {
        public static Vector3 DoorOffset(IntVec3 loc, Map map, Vector3 doorOffset)
        {
            (IntVec3 vec, int value)[] num = new (IntVec3, int)[4];
            num[0] = (IntVec3.NorthWest, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.West, map));
            num[1] = (IntVec3.SouthEast, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.South, map));
            loc += IntVec3.NorthEast;
            num[0].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.North, map);
            num[1].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.East, map);
            
            loc += IntVec3.West;
            num[2] = (IntVec3.NorthEast, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.North, map));
            num[3] = (IntVec3.SouthWest, DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.West, map));
            loc += IntVec3.SouthEast;
            num[2].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.East, map);
            num[3].value += DiagonalDoorUtility.AlignQualityAgainst(loc, IntVec3.South, map);
            var list = num.ToList();
            list.SortByDescending(n => n.vec == doorOffset.ToIntVec3());
            list.SortByDescending(n => n.value);
            return list[0].vec.ToVector3();
        }

        private delegate int GetAlignQualityAgainst(IntVec3 c, IntVec3 offset, Map map);

        private static GetAlignQualityAgainst AlignQualityAgainst = AccessTools.MethodDelegate<GetAlignQualityAgainst>(AccessTools.Method(typeof(Building_Door), "AlignQualityAgainst"));
    }
}
