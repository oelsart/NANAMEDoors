using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static NanameDoors.ModCompat;

namespace NanameDoors;

[StaticConstructorOnStartup]
public class Building_DiagonalDoor : Building_Door
{
    private Vector3 doorOffset;

    private Graphic doorSideWallGraphic;

    private Graphic doorSideWallGraphicDiagonalLeft;

    private Graphic doorSideWallGraphicDiagonalRight;

    private float doorOffsetFactor;

    private float doorSideWallTexScale;

    private bool isFenceGate;

    private readonly Vector3 fenceGateOffset = new(-0.1417f, 0f, -0.25f);

    public readonly IntVec3[] wallPos = new IntVec3[2];

    private static readonly AccessTools.FieldRef<LinkGrid, LinkFlags[]> linkGrid = AccessTools.FieldRefAccess<LinkFlags[]>(typeof(LinkGrid), "linkGrid");

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            Init();
        }
    }

    public void PreFinalizeInit()
    {
        Init();
    }

    private void Init()
    {
        isFenceGate = def.HasModExtension<FenceGateMoverGraphics>();
        if (isFenceGate)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                doorSideWallGraphic = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences");
                if (DiagonalWalls.Active)
                {
                    doorSideWallGraphicDiagonalRight = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences_DiagonalRight");
                    doorSideWallGraphicDiagonalLeft = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences_DiagonalLeft");
                }
            });
            doorSideWallTexScale = 1f;
            doorOffsetFactor = 0f;
        }
        else
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                doorSideWallGraphic = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls");
                if (DiagonalWalls.Active)
                {
                    doorSideWallGraphicDiagonalRight = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls_DiagonalRight");
                    doorSideWallGraphicDiagonalLeft = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls_DiagonalLeft");
                }
            });
            doorSideWallTexScale = 1.3667f;
            doorOffsetFactor = 0.25f;
        }

        doorOffset = DiagonalDoorUtility.DoorOffset(Position, Map, def.building.preferConnectingToFences, doorOffset);
        PrintDoorSideWall(DrawPos, null);
    }

    public override void Print(SectionLayer layer)
    {
        doorOffset = DiagonalDoorUtility.DoorOffset(Position, Map, def.building.preferConnectingToFences, doorOffset);
        PrintDoorSideWall(DrawPos, layer);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var offsetDist = 0.45f * OpenPct;
        float altitude;
        if (isFenceGate && doorOffset.z > 0f) altitude = AltitudeLayer.BuildingOnTop.AltitudeFor();
        else altitude = AltitudeLayer.DoorMoveable.AltitudeFor();
        DrawMovers(drawLoc, offsetDist, Graphic, altitude, new Vector3(isFenceGate ? 2f : 1.42f, 1f, isFenceGate ? 2f : 0.9f), Graphic.ShadowGraphic);
    }

    protected new void DrawMovers(Vector3 drawPos, float offsetDist, Graphic graphic, float altitude, Vector3 drawScaleFactor, Graphic_Shadow shadowGraphic)
    {
        for (var i = 0; i < 2; i++)
        {
            Mesh mesh;
            var flip = doorOffset.x + doorOffset.z == 0;
            var vector = i == 0 ? new Vector3(-1f, 0f, -1f) : new Vector3(1f, 0f, 1f);
            if (isFenceGate)
            {
                if (!flip) vector *= -1f;
                mesh = flip ? MeshPool.plane10Flip : MeshPool.plane10;
                altitude -= i * 0.03846154f;
            }
            else
            {
                mesh = i == 0 ? MeshPool.plane10 : MeshPool.plane10Flip;
            }
            var rotation = flip ? Rot4.West : Rot4.North;
            rotation.Rotate(RotationDirection.Clockwise);
            vector = rotation.AsQuat * vector;
            var vector2 = drawPos;
            vector2.y = altitude;
            vector2 += vector * offsetDist;
            if (isFenceGate && doorOffset.z > 0f)
            {
                vector2.x += doorOffset.x * fenceGateOffset.x;
                vector2.z += fenceGateOffset.z;
            }
            var drawGraphic = isFenceGate ? def.GetModExtension<FenceGateMoverGraphics>().graphics[i].GetColoredVersion(graphic.Shader, DrawColor, DrawColorTwo) : graphic;
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2 + (doorOffset * doorOffsetFactor), Quaternion.Euler(0f, isFenceGate ? 0f : flip ? -45f : 45f, 0f), drawScaleFactor), drawGraphic.MatAt(Rotation, this), 0);
            shadowGraphic?.DrawWorker(vector2, Rotation, def, this, 0f);
        }
    }

    protected void PrintDoorSideWall(Vector3 drawPos, SectionLayer layer)
    {
        foreach (var c in this.OccupiedRect())
        {
            linkGrid(Map.linkGrid)[Map.cellIndices.CellToIndex(c)] = LinkFlags.None;
        }
        for (var i = 0; i < 2; i++)
        {
            var wallDrawPos = i == 0
                ? new Vector3(drawPos.x - (doorOffset.x * 0.5f), 0f, drawPos.z + (doorOffset.z * 0.5f))
                : new Vector3(drawPos.x + (doorOffset.x * 0.5f), 0f, drawPos.z - (doorOffset.z * 0.5f));
            var wallOffset = wallDrawPos - drawPos;
            var wallPosision = IntVec3.FromVector3(wallDrawPos);
            var num = 0;
            var num2 = 1;
            Thing adjacentWall = null;
            Graphic graphic = null;
            for (var j = 0; j < 4; j++)
            {
                if (GenAdj.CardinalDirections[j].x + (wallOffset.x * 2f) != 0f && GenAdj.CardinalDirections[j].z + (wallOffset.z * 2f) != 0f)
                {
                    var adjacentWallPos = wallPosision + GenAdj.CardinalDirections[j];

                    foreach (var thing in adjacentWallPos.GetThingList(Map))
                    {
                        if (isFenceGate)
                        {
                            if (thing.def.graphicData?.linkFlags.HasFlag(LinkFlags.Fences) ?? false)
                            {
                                adjacentWall = thing;
                                num += num2;
                                if (thing.def.modContentPack?.PackageId == DiagonalWalls.PackageId)
                                {
                                    if (j == 0 && (adjacentWallPos + IntVec3.East).GetThingList(Map).Any(t => t.def.graphicData?.linkFlags.HasFlag(LinkFlags.Fences) ?? false))
                                    {
                                        graphic = doorSideWallGraphicDiagonalRight;
                                    }
                                    else graphic ??= doorSideWallGraphicDiagonalLeft;
                                }
                                else
                                {
                                    graphic = doorSideWallGraphic;
                                }
                            }
                        }
                        else
                        {
                            if (thing.def.graphicData?.linkFlags.HasFlag(LinkFlags.Wall) ?? false)
                            {
                                adjacentWall = thing;
                                num += num2;
                                if (thing.def.modContentPack?.PackageId == DiagonalWalls.PackageId)
                                {
                                    if (j % 2 == 0 && (adjacentWallPos + IntVec3.East).GetThingList(Map).Any(t => t.def.graphicData?.linkFlags.HasFlag(LinkFlags.Wall) ?? false))
                                    {
                                        graphic = doorSideWallGraphicDiagonalRight;
                                    }
                                    else graphic ??= doorSideWallGraphicDiagonalLeft;
                                }
                                else
                                {
                                    graphic = doorSideWallGraphic;
                                }
                            }
                        }
                    }
                }
                num2 *= 2;
            }
            if (adjacentWall != null)
            {
                var altitude = isFenceGate ? AltitudeLayer.Building.AltitudeFor(1f) : wallOffset.z > 0f ? AltitudeLayer.Building.AltitudeFor(1f) : AltitudeLayer.Building.AltitudeFor();
                wallDrawPos.y = altitude;
                var linkSet = (LinkDirections)num;
                switch (isFenceGate)
                {
                    case true when linkSet.HasFlag(LinkDirections.Left):
                        wallDrawPos.x -= 0.025f;
                        break;
                    case true when linkSet.HasFlag(LinkDirections.Right):
                        wallDrawPos.x += 0.025f;
                        break;
                }
                linkGrid(Map.linkGrid)[Map.cellIndices.CellToIndex(wallPosision)] = def.graphicData.linkFlags;

                if (wallPosision != this.wallPos[i])
                {
                    var prevWallPos = this.wallPos[i];
                    this.wallPos[i] = wallPosision;
                    Map.pathing.RecalculatePerceivedPathCostAt(prevWallPos);
                    Map.pathing.RecalculatePerceivedPathCostAt(wallPosision);
                    Map.mapDrawer.MapMeshDirty(adjacentWall.Position,
                        isFenceGate ? MapMeshFlagDefOf.Terrain : MapMeshFlagDefOf.Things);
                }
                if (layer is null) continue;

                var material = MaterialAtlasPool.SubMaterialFromAtlas(graphic.GetColoredVersion(adjacentWall.Graphic?.Shader ?? graphic.Shader, adjacentWall.DrawColor, Color.white).MatSingleFor(adjacentWall), linkSet);
                Printer_Plane.PrintPlane(layer, wallDrawPos, new Vector2(doorSideWallTexScale, doorSideWallTexScale), material);
            }
        }
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref doorOffset, "doorOffset", new Vector3(1f, 0f, 1f));
    }
}
