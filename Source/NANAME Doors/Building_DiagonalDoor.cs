using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace NanameDoors
{
    [StaticConstructorOnStartup]
    public class Building_DiagonalDoor : Building_Door
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.Init();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            this.Init();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.doorOffset, "doorOffset", IntVec3.NorthEast.ToVector3());
        }

        private void Init()
        {
            this.map = this.Map;
            this.cellIndices = this.map.cellIndices;
            this.isFenceGate = this.def.HasModExtension<FenceGateMoverGraphics>();
            if (this.isFenceGate)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    this.doorSideWallGraphic = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences");
                    this.doorSideWallGraphicDiagonalRight = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences_DiagonalRight");
                    this.doorSideWallGraphicDiagonalLeft = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/GateSideFences_DiagonalLeft");
                });
                this.doorSideWallTexScale = 1f;
                this.doorOffsetFactor = 0f;
            }
            else
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    this.doorSideWallGraphic = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls");
                    this.doorSideWallGraphicDiagonalRight = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls_DiagonalRight");
                    this.doorSideWallGraphicDiagonalLeft = GraphicDatabase.Get<Graphic_Appearances>("NanameDoors/DoorSideWalls_DiagonalLeft");
                });
                this.doorSideWallTexScale = 1.3667f;
                this.doorOffsetFactor = 0.25f;
            }

            this.DrawDoorSideWall(this.DrawPos, false);
        }

        public override void Draw()
        {
            var drawLoc = this.DrawPos;
            this.doorOffset = DiagonalDoorUtility.DoorOffset(base.Position, base.Map, this.doorOffset);
            this.DrawDoorSideWall(drawLoc, true);
            float offsetDist = 0.45f * Mathf.Clamp01((float)this.ticksSinceOpen / (float)this.TicksToOpenNow);
            float altitude;
            if (this.isFenceGate && this.doorOffset.z > 0f) altitude = AltitudeLayer.BuildingOnTop.AltitudeFor();
            else altitude = AltitudeLayer.DoorMoveable.AltitudeFor();
            this.DrawMovers(drawLoc, offsetDist, this.Graphic, altitude, new Vector3(this.isFenceGate ? 2f : 1.42f, 1f, this.isFenceGate ? 2f : 0.9f), this.Graphic.ShadowGraphic);
        }

        protected void DrawMovers(Vector3 drawPos, float offsetDist, Graphic graphic, float altitude, Vector3 drawScaleFactor, Graphic_Shadow shadowGraphic)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 vector;
                Mesh mesh;
                var flip = this.doorOffset.x + this.doorOffset.z == 0;
                vector = i == 0 ? new Vector3(-1f, 0f, -1f) : new Vector3(1f, 0f, 1f);
                if (this.isFenceGate)
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
                if (this.isFenceGate && this.doorOffset.z > 0f)
                {
                    vector2.x += this.doorOffset.x * this.fenceGateOffset.x;
                    vector2.z += this.fenceGateOffset.z;
                }
                Graphic drawGraphic = this.isFenceGate ? this.def.GetModExtension<FenceGateMoverGraphics>().graphics[i].GetColoredVersion(graphic.Shader, this.DrawColor, this.DrawColorTwo) : graphic;
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2 + doorOffset * doorOffsetFactor, Quaternion.Euler(0f, this.isFenceGate ? 0f : flip ? -45f : 45f, 0f), drawScaleFactor), drawGraphic.MatAt(base.Rotation, this), 0);
                if (shadowGraphic != null)
                {
                    shadowGraphic.DrawWorker(vector2, base.Rotation, this.def, this, 0f);
                }
            }
        }

        protected void DrawDoorSideWall(Vector3 drawPos, bool actuallyDraw)
        {
            foreach(var c in this.OccupiedRect())
            {
                linkGrid(this.map.linkGrid)[cellIndices.CellToIndex(c)] = LinkFlags.None;
            }
            for (int i = 0; i < 2; i++)
            {
                Vector3 wallDrawPos;
                if (i == 0)
                {
                    wallDrawPos = new Vector3(drawPos.x - doorOffset.x * 0.5f, 0f, drawPos.z + doorOffset.z * 0.5f);
                }
                else
                {
                    wallDrawPos = new Vector3(drawPos.x + doorOffset.x * 0.5f, 0f, drawPos.z - doorOffset.z * 0.5f);
                }
                Vector3 wallOffset = wallDrawPos - drawPos;
                IntVec3 wallPos = new IntVec3((int)(wallDrawPos.x - 0.5f), 0, (int)(wallDrawPos.z - 0.5f));
                int num = 0;
                int num2 = 1;
                Thing adjacentWall = null;
                Graphic graphic = null;
                for (int j = 0; j < 4; j++)
                {
                    if (GenAdj.CardinalDirections[j].x + wallOffset.x * 2f != 0f && GenAdj.CardinalDirections[j].z + wallOffset.z * 2f != 0f)
                    {
                        IntVec3 adjacentWallPos = wallPos + GenAdj.CardinalDirections[j];
                        
                        foreach (var thing in adjacentWallPos.GetThingList(this.Map))
                        {
                            if (this.isFenceGate)
                            {
                                if (thing.def.graphicData?.linkFlags.HasFlag(LinkFlags.Fences) ?? false)
                                {
                                    adjacentWall = thing;
                                    num += num2;
                                    if (thing.def.modContentPack.PackageId == "chv.diagonalwalls2")
                                    {
                                        if (j == 0 && (adjacentWallPos + IntVec3.East).GetThingList(this.Map).Any(t => t.def.graphicData?.linkFlags.HasFlag(LinkFlags.Fences) ?? false))
                                        {
                                            graphic = this.doorSideWallGraphicDiagonalRight;
                                        }
                                        else if (graphic == null)
                                        {
                                            graphic = this.doorSideWallGraphicDiagonalLeft;
                                        }
                                    }
                                    else
                                    {
                                        graphic = this.doorSideWallGraphic;
                                    }
                                }
                            }
                            else
                            {
                                if (thing.def.graphicData?.linkFlags.HasFlag(LinkFlags.Wall) ?? false)
                                {
                                    adjacentWall = thing;
                                    num += num2;
                                    if (thing.def.modContentPack.PackageId == "chv.diagonalwalls2")
                                    {
                                        if (j % 2 == 0 && (adjacentWallPos + IntVec3.East).GetThingList(this.Map).Any(t => t.def.graphicData?.linkFlags.HasFlag(LinkFlags.Wall) ?? false))
                                        {
                                            graphic = this.doorSideWallGraphicDiagonalRight;
                                        }
                                        else if (graphic == null)
                                        {
                                            graphic = this.doorSideWallGraphicDiagonalLeft;
                                        }
                                    }
                                    else
                                    {
                                        graphic = this.doorSideWallGraphic;
                                    }
                                }
                            }
                        }
                    }
                    num2 *= 2;
                }
                if (adjacentWall != null)
                {
                    float altitude = this.isFenceGate ? AltitudeLayer.Building.AltitudeFor(1f) : wallOffset.z > 0f ? AltitudeLayer.Building.AltitudeFor(1f) : AltitudeLayer.Building.AltitudeFor();
                    wallDrawPos.y = altitude;
                    LinkDirections linkSet = (LinkDirections)num;
                    if (this.isFenceGate && linkSet.HasFlag(LinkDirections.Left))
                    {
                        wallDrawPos.x -= 0.025f;
                    }
                    else if (this.isFenceGate && linkSet.HasFlag(LinkDirections.Right))
                    {
                        wallDrawPos.x += 0.025f;
                    }
                    linkGrid(this.map.linkGrid)[cellIndices.CellToIndex(wallPos)] = this.def.graphicData.linkFlags;
                    if (!actuallyDraw) continue;

                    if (wallPos != this.previousWallPos[i])
                    {
                        this.previousWallPos[i] = wallPos;
                        if (this.isFenceGate)
                        {
                            this.map.mapDrawer.SectionAt(adjacentWall.Position).GetLayer(typeof(SectionLayer_BridgeProps)).Regenerate();
                        }
                        else
                        {
                            this.map.mapDrawer.SectionAt(adjacentWall.Position).GetLayer(typeof(SectionLayer_ThingsGeneral)).Regenerate();
                        }
                    }

                    Material material = MaterialAtlasPool.SubMaterialFromAtlas(graphic.GetColoredVersion(adjacentWall.Graphic.Shader, adjacentWall.DrawColor, Color.white).MatSingleFor(adjacentWall), linkSet);
                    Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(wallDrawPos, Quaternion.identity, new Vector3(this.doorSideWallTexScale, 0f, this.doorSideWallTexScale)), material, 0);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (this.Open)
            {
                foreach (IntVec3 c in this.OccupiedRect().Where(c => c != this.Position))
                {
                    List<Thing> thingList = c.GetThingList(base.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Pawn p;
                        if ((p = (thingList[i] as Pawn)) != null)
                        {
                            this.CheckFriendlyTouched(p);
                        }
                    }
                }
            }
            if (this.ticksUntilClose > 0)
            {
                foreach (IntVec3 c2 in this.OccupiedRect().Where(c => c != this.Position))
                {
                    if (base.Map.thingGrid.CellContains(c2, ThingCategory.Pawn))
                    {
                        this.ticksUntilClose = 110;
                        break;
                    }
                }
            }
        }

        private Vector3 doorOffset;

        private Graphic doorSideWallGraphic;

        private Graphic doorSideWallGraphicDiagonalLeft;

        private Graphic doorSideWallGraphicDiagonalRight;

        private float doorSideWallTexScale;

        private IntVec3[] previousWallPos = new IntVec3[2];

        private float doorOffsetFactor;

        private bool isFenceGate;

        private readonly Vector3 fenceGateOffset = new Vector3(-0.1417f, 0f, -0.25f);

        private Map map;

        private CellIndices cellIndices;

        private readonly AccessTools.FieldRef<LinkGrid, LinkFlags[]> linkGrid = AccessTools.FieldRefAccess<LinkFlags[]>(typeof(LinkGrid), "linkGrid");
    }
}
