using HarmonyLib;
using RimWorld;
using System.Linq;
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

    public virtual void PreFinalizeInit()
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
        float offsetDist = 0.45f * OpenPct;
        float altitude;
        if (isFenceGate && doorOffset.z > 0f) altitude = AltitudeLayer.BuildingOnTop.AltitudeFor();
        else altitude = AltitudeLayer.DoorMoveable.AltitudeFor();
        DrawMovers(drawLoc, offsetDist, Graphic, altitude, new Vector3(isFenceGate ? 2f : 1.42f, 1f, isFenceGate ? 2f : 0.9f), Graphic.ShadowGraphic);
    }

    new protected void DrawMovers(Vector3 drawPos, float offsetDist, Graphic graphic, float altitude, Vector3 drawScaleFactor, Graphic_Shadow shadowGraphic)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 vector;
            Mesh mesh;
            var flip = doorOffset.x + doorOffset.z == 0;
            vector = i == 0 ? new Vector3(-1f, 0f, -1f) : new Vector3(1f, 0f, 1f);
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
            Rot4 rotation = flip ? Rot4.West : Rot4.North;
            rotation.Rotate(RotationDirection.Clockwise);
            vector = rotation.AsQuat * vector;
            Vector3 vector2 = drawPos;
            vector2.y = altitude;
            vector2 += vector * offsetDist;
            if (isFenceGate && doorOffset.z > 0f)
            {
                vector2.x += doorOffset.x * fenceGateOffset.x;
                vector2.z += fenceGateOffset.z;
            }
            Graphic drawGraphic = isFenceGate ? def.GetModExtension<FenceGateMoverGraphics>().graphics[i].GetColoredVersion(graphic.Shader, DrawColor, DrawColorTwo) : graphic;
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2 + (doorOffset * doorOffsetFactor), Quaternion.Euler(0f, isFenceGate ? 0f : flip ? -45f : 45f, 0f), drawScaleFactor), drawGraphic.MatAt(base.Rotation, this), 0);
            shadowGraphic?.DrawWorker(vector2, Rotation, def, this, 0f);
        }
    }

    protected void PrintDoorSideWall(Vector3 drawPos, SectionLayer layer)
    {
        foreach (var c in this.OccupiedRect())
        {
            linkGrid(Map.linkGrid)[Map.cellIndices.CellToIndex(c)] = LinkFlags.None;
        }
        for (int i = 0; i < 2; i++)
        {
            Vector3 wallDrawPos;
            if (i == 0)
            {
                wallDrawPos = new Vector3(drawPos.x - (doorOffset.x * 0.5f), 0f, drawPos.z + (doorOffset.z * 0.5f));
            }
            else
            {
                wallDrawPos = new Vector3(drawPos.x + (doorOffset.x * 0.5f), 0f, drawPos.z - (doorOffset.z * 0.5f));
            }
            Vector3 wallOffset = wallDrawPos - drawPos;
            IntVec3 wallPos = IntVec3.FromVector3(wallDrawPos);
            int num = 0;
            int num2 = 1;
            Thing adjacentWall = null;
            Graphic graphic = null;
            for (int j = 0; j < 4; j++)
            {
                if (GenAdj.CardinalDirections[j].x + (wallOffset.x * 2f) != 0f && GenAdj.CardinalDirections[j].z + (wallOffset.z * 2f) != 0f)
                {
                    IntVec3 adjacentWallPos = wallPos + GenAdj.CardinalDirections[j];

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
                float altitude = isFenceGate ? AltitudeLayer.Building.AltitudeFor(1f) : wallOffset.z > 0f ? AltitudeLayer.Building.AltitudeFor(1f) : AltitudeLayer.Building.AltitudeFor();
                wallDrawPos.y = altitude;
                LinkDirections linkSet = (LinkDirections)num;
                if (isFenceGate && linkSet.HasFlag(LinkDirections.Left))
                {
                    wallDrawPos.x -= 0.025f;
                }
                else if (isFenceGate && linkSet.HasFlag(LinkDirections.Right))
                {
                    wallDrawPos.x += 0.025f;
                }
                linkGrid(Map.linkGrid)[Map.cellIndices.CellToIndex(wallPos)] = def.graphicData.linkFlags;

                if (wallPos != this.wallPos[i])
                {
                    var prevWallPos = this.wallPos[i];
                    this.wallPos[i] = wallPos;
                    Map.pathing.RecalculatePerceivedPathCostAt(prevWallPos);
                    Map.pathing.RecalculatePerceivedPathCostAt(wallPos);
                    if (isFenceGate)
                    {
                        Map.mapDrawer.MapMeshDirty(adjacentWall.Position, MapMeshFlagDefOf.Terrain);
                    }
                    else
                    {
                        Map.mapDrawer.MapMeshDirty(adjacentWall.Position, MapMeshFlagDefOf.Things);
                    }
                }
                if (layer is null) continue;

                Material material = MaterialAtlasPool.SubMaterialFromAtlas(graphic.GetColoredVersion(adjacentWall.Graphic?.Shader ?? graphic.Shader, adjacentWall.DrawColor, Color.white).MatSingleFor(adjacentWall), linkSet);
                Printer_Plane.PrintPlane(layer, wallDrawPos, new Vector2(doorSideWallTexScale, doorSideWallTexScale), material, 0f, false, null, null, 0.01f, 0f);
            }
        }
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref doorOffset, "doorOffset", IntVec3.NorthEast.ToVector3());
    }
}
