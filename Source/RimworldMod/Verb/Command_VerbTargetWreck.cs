﻿using RimWorld;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    public class Command_VerbTargetWreck : Command
    {
        public Map targetMap;
        /*
        public override void MergeWith(Gizmo other)
        {
            base.MergeWith(other);
            Command_VerbTargetWreck command_VerbTargetShip = other as Command_VerbTargetWreck;
            if (command_VerbTargetShip == null)
            {
                Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
                return;
            }
        }*/

        public override void ProcessInput(Event ev)
        {
            Building b=null;
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            Targeter targeter = Find.Targeter;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetBuildings = true;
            Find.Targeter.BeginTargeting(parms, (Action<LocalTargetInfo>)delegate (LocalTargetInfo x)
            {
                b = x.Cell.GetFirstBuilding(targetMap);
            }, (Pawn)null, delegate { AfterTarget(b); });
        }

        public void AfterTarget(Building b)
        {
            List<IntVec3> positions = ShipInteriorMod2.FindAreaAttached(b, true);
            if (positions.NullOrEmpty())
                return;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmAbandonWreck", delegate
            {
                try
                {
                    List<Thing> things = new List<Thing>();
                    foreach (IntVec3 pos in positions)
                    {
                        things.AddRange(pos.GetThingList(targetMap));
                    }
                    foreach (Thing t in things)
                    {
                        if (t is Pawn)
                            t.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                        if (t.def.destroyable && !t.Destroyed)
                            t.Destroy(DestroyMode.Vanish);
                    }
                    foreach (IntVec3 pos in positions)
                    {
                        targetMap.terrainGrid.SetTerrain(pos, ShipInteriorMod2.spaceTerrain);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(""+e);
                }
            }));
        }
    }
}
