﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompEngineTrail : ThingComp
    {
        CellRect rectToKill;
        private static Graphic trailGraphic = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Double", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
        private static Graphic trailGraphicSingle = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Single", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
        private static Graphic trailGraphicLarge = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/NuclearEngineTrail", ShaderDatabase.MoteGlow, new Vector2(7, 26.5f), Color.white, Color.white);
        private static Graphic trailGraphicEnergy = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Energy_Double", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
        private static Graphic trailGraphicEnergyLarge = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Energy_Large", ShaderDatabase.MoteGlow, new Vector2(11, 26.5f), Color.white, Color.white);
        private static Vector3[] offset = { new Vector3(0, 0, -5.5f), new Vector3(-5.5f, 0, 0), new Vector3(0, 0, 5.5f), new Vector3(5.5f, 0, 0) };
        private static Vector3[] offsetL = { new Vector3(0, 0, -11f), new Vector3(-11f, 0, 0), new Vector3(0, 0, 11f), new Vector3(11f, 0, 0) };
        private static Vector3[] offsetE = { new Vector3(0, 0, -9.2f), new Vector3(-9.2f, 0, 0), new Vector3(0, 0, 9.2f), new Vector3(9.2f, 0, 0) };
        public static IntVec2[] killOffset = { new IntVec2(0, -6), new IntVec2(-6, 0), new IntVec2(0, 4), new IntVec2(4, 0) };
        public static IntVec2[] killOffsetL = { new IntVec2(0, -13), new IntVec2(-13, 0), new IntVec2(0, 7), new IntVec2(7, 0) };
		public CompProperties_EngineTrail Props
        {
            get { return props as CompProperties_EngineTrail; }
        }
        private bool active = false;
        int size;
        public ShipHeatMapComp mapComp;
        public CompFlickable Flickable;
        public CompRefuelable Refuelable;
        public CompPowerTrader PowerTrader;
        public bool CanActivate()
        {
            if (Flickable.SwitchIsOn)
            {
                if (Props.energy && PowerTrader.PowerOn)
                {
                    return true;
                }
                else if (Refuelable.Fuel > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public void Activate()
        {
            if (Flickable.SwitchIsOn)
            {
                if (Props.energy && PowerTrader.PowerOn)
                {
                    PowerTrader.PowerOutput = -2000 * Props.thrust;
                    active = true;
                }
                else if (Refuelable.Fuel > 0)
                {
                    active = true;
                }
            }
        }
        public void DeActivate()
        {
            if (Props.energy)
            {
                PowerTrader.PowerOutput = -200 * Props.thrust;
            }
            active = false;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Flickable = parent.TryGetComp<CompFlickable>();
            Refuelable = parent.TryGetComp<CompRefuelable>();
            PowerTrader = parent.TryGetComp<CompPowerTrader>();
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
            size = parent.def.size.x;
            if (size > 3)
                rectToKill = parent.OccupiedRect().MovedBy(killOffsetL[parent.Rotation.AsInt]).ExpandedBy(2);
            else
                rectToKill = parent.OccupiedRect().MovedBy(killOffset[parent.Rotation.AsInt]).ExpandedBy(1);
            if (parent.Rotation.IsHorizontal)
                rectToKill.Width = rectToKill.Width * 2 - 3;
            else
                rectToKill.Height = rectToKill.Height * 2 - 3;
        }
        public override void PostDeSpawn(Map map)
        {
            mapComp = null;
            base.PostDeSpawn(map);
        }
        public override void PostDraw()
        {
            base.PostDraw();
            if (active)
            {
                if (Props.energy)
                {
                    if (size > 3)
                    {
                        trailGraphicEnergyLarge.drawSize = new Vector2(11, 26.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                        trailGraphicEnergyLarge.Draw(new Vector3(parent.DrawPos.x + offsetE[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offsetE[parent.Rotation.AsInt].z), parent.Rotation, parent);
                    }
                    else
                    {
                        trailGraphicEnergy.drawSize = new Vector2(7, 15.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                        trailGraphicEnergy.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
                    }
                }
                else
                {
                    if (size > 3)
                    {
                        trailGraphicLarge.drawSize = new Vector2(7, 26.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                        trailGraphicLarge.Draw(new Vector3(parent.DrawPos.x + offsetL[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offsetL[parent.Rotation.AsInt].z), parent.Rotation, parent);
                    }
                    else
                    {
                        Vector2 drawSize = new Vector2(7, 15.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                        if (size == 3)
                        {
                            trailGraphic.drawSize = drawSize;
                            trailGraphic.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
                        }
                        else
                        {
                            trailGraphicSingle.drawSize = drawSize;
                            trailGraphicSingle.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
                        }
                    }
                }
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (active)
            {
                if (Find.TickManager.TicksGame % 60 == 0)
                {
                    Refuelable.ConsumeFuel(Props.fuelUse);
                }
                //destroy stuff in plume
                foreach (IntVec3 cell in rectToKill)
                {
                    List<Thing> toBurn = new List<Thing>();
                    foreach (Thing t in cell.GetThingList(parent.Map))
                    {
                        if (t.def.useHitPoints)
                            toBurn.Add(t);
                    }
                    foreach (Thing t in toBurn)
                    {
                        if (t.def.altitudeLayer != AltitudeLayer.Terrain)
                            t.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 100));
                    }
                }
            }
        }
    }
}
