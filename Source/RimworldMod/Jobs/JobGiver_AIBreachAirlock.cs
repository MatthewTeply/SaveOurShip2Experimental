﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class JobGiver_AIBreachAirlock : ThinkNode_JobGiver
    {

        public float maxDistFromPoint = 99f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AIBreachAirlock obj = (JobGiver_AIBreachAirlock)base.DeepCopy(resolve);
            obj.maxDistFromPoint = maxDistFromPoint;
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
		{
            if (!pawn.RaceProps.IsMechanoid)
            {
                if (!pawn.HostileTo(Faction.OfPlayer) || pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) == 0)
                {
                    return null;
                }
            }
			Thing thing = null;
            Predicate<Thing> validator = delegate (Thing t)
			{
                if (t.Faction != pawn.Faction && pawn.CanReserve(t))
                {
                    if (t is Building_ShipAirlock a && !a.Open)
                    {
                        //only go for outerdoors when outside
                        if (!pawn.GetRoom().TouchesMapEdge && !((Building_ShipAirlock)t).Outerdoor())
                            return true;
                        else if (pawn.GetRoom().TouchesMapEdge && ((Building_ShipAirlock)t).Outerdoor())
                            return true;
                    }
                    else if (t is Building_Door d && !d.Open)
                    {
                        return true;
                    }
                }
				return false;
            };
			thing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, validator);
            if (thing != null)
			{
				//Log.Message("Breachjob for:" + thing);
				Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("BreachAirlock"), thing);
                job.expiryInterval = 2000;
                job.checkOverrideOnExpire = true;
                return job;
            }
            return null;
		}

        protected IntVec3 GetRoot(Pawn pawn)
        {
            return pawn.Position;
        }
    }
}
