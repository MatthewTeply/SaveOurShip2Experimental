﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class Building_ShipCloakingDevice : Building
    {
        public bool active;
        public CompPowerTrader powerComp;
        public CompShipHeatSource heatComp;
        public CompFlickable flickComp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.Map.GetComponent<ShipHeatMapComp>().Cloaks.Add(this);
            powerComp = this.TryGetComp<CompPowerTrader>();
            heatComp = this.TryGetComp<CompShipHeatSource>();
            flickComp = this.TryGetComp<CompFlickable>();
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                if (powerComp.PowerOn && flickComp.SwitchIsOn && (heatComp.myNet!=null || (this.GetRoom()!=null && this.GetRoom().OpenRoofCount==0)))
                    active = true;
                else
                    active = false;
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.Map.GetComponent<ShipHeatMapComp>().Cloaks.Remove(this);
            base.DeSpawn(mode);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (active)
            {
                stringBuilder.AppendLine("Active");//TranslatorFormattedStringExtensions.Translate(
            }
            else
            {
                stringBuilder.AppendLine("Inactive");
                if ((this.GetRoom() == null || this.GetRoom().OpenRoofCount > 0) && heatComp.myNet == null)
                    stringBuilder.AppendLine("<color=red>In vacuum and not connected to heat net</color>");
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref active, "active", false);
        }
    }
}
