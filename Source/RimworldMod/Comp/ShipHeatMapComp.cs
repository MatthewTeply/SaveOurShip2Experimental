﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;

namespace RimWorld
{
    public class ShipHeatMapComp : MapComponent
    {
        List<ShipHeatNet> cachedNets = new List<ShipHeatNet>();
        List<CompShipHeat> cachedPipes = new List<CompShipHeat>();

        public int[] grid;
        public bool heatGridDirty;
        bool loaded = false;

        public ShipHeatMapComp(Map map) : base(map)
        {
            grid = new int[map.cellIndices.NumGridCells];
            heatGridDirty = true;
        }
        public void Register(CompShipHeat comp)
        {
            cachedPipes.Add(comp);
            GenList.Shuffle<CompShipHeat>(cachedPipes);
            heatGridDirty = true;
        }
        public void DeRegister(CompShipHeat comp)
        {
            cachedPipes.Remove(comp);
            GenList.Shuffle<CompShipHeat>(cachedPipes);
            heatGridDirty = true;
        }
        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            //this only gets called when new parts are added but even so should be redone, similar to new ship cache
            if (!heatGridDirty || (Find.TickManager.TicksGame % 60 != 0 && loaded))
            {
                return;
            }
            //temp save heat to sinks
            //Log.Message("Recaching all heatnets");
            foreach (ShipHeatNet net in cachedNets)
            {
                foreach (CompShipHeatSink sink in net.Sinks)
                {
                    sink.heatStored = net.StorageUsed * sink.Props.heatCapacity / net.StorageCapacity;
                }
            }
            //rebuild all nets on map
            List<ShipHeatNet> list = new List<ShipHeatNet>();
            for (int i = 0; i < grid.Length; i++)
                grid[i] = -1;
            int gridID = 0;
            foreach(CompShipHeat comp in cachedPipes)
            {
                if (comp.parent.Map == null || grid[comp.parent.Map.cellIndices.CellToIndex(comp.parent.Position)] > -1)
                    continue;
                ShipHeatNet net = new ShipHeatNet();
                net.GridID = gridID;
                gridID++;
                HashSet<CompShipHeat> batch = new HashSet<CompShipHeat>();
                batch.Add(comp);
                AccumulateToNetNew(batch, net);
                list.Add(net);
            }
            cachedNets = list;

            base.map.mapDrawer.WholeMapChanged((MapMeshFlag)4);
            base.map.mapDrawer.WholeMapChanged((MapMeshFlag)1);
            heatGridDirty = false;
            loaded = true;
        }
        void AccumulateToNetNew(HashSet<CompShipHeat> compBatch, ShipHeatNet net)
        {
            HashSet<CompShipHeat> newBatch = new HashSet<CompShipHeat>();
            foreach (CompShipHeat comp in compBatch)
            {
                if (comp.parent == null || !comp.parent.Spawned)
                    continue;
                comp.myNet = net;
                net.Register(comp);
                foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(comp.parent))
                {
                    grid[comp.parent.Map.cellIndices.CellToIndex(cell)] = net.GridID;
                }
                foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(comp.parent))
                {
                    if (grid[comp.parent.Map.cellIndices.CellToIndex(cell)] == -1)
                    {
                        foreach (Thing t in cell.GetThingList(comp.parent.Map))
                        {
                            if (t is ThingWithComps twc && twc != null)
                            {
                                CompShipHeat heat = twc.TryGetComp<CompShipHeat>();
                                if (heat != null)
                                    newBatch.Add(heat);
                            }
                        }
                    }
                }
            }
            if (newBatch.Any())
                AccumulateToNetNew(newBatch, net);
        }

        /*void AccumulateToNet(CompShipHeat comp, ShipHeatNet net, ref List<CompShipHeat> used)
        {
            used.Add(comp);
            comp.myNet = net;
            net.Register(comp);
            if (comp.parent == null || !comp.parent.Spawned)
                return;
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(comp.parent))
            {
                grid[comp.parent.Map.cellIndices.CellToIndex(cell)] = net.GridID;
            }
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(comp.parent))
            {
                foreach(Thing t in cell.GetThingList(comp.parent.Map))
                {
                    if(t is ThingWithComps && ((ThingWithComps)t).TryGetComp<CompShipHeat>()!=null && !used.Contains(((ThingWithComps)t).TryGetComp<CompShipHeat>()))
                    {
                        AccumulateToNet(((ThingWithComps)t).TryGetComp<CompShipHeat>(), net, ref used);
                    }
                }
            }
        }*/
        //SC
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref ShipCombatOriginMap, "ShipCombatOriginMap");
            Scribe_References.Look<Map>(ref ShipCombatMasterMap, "ShipCombatMasterMap");
            Scribe_References.Look<Map>(ref ShipCombatTargetMap, "ShipCombatTargetMap");
            Scribe_References.Look<Map>(ref ShipGraveyard, "ShipCombatGraveyard");
            Scribe_References.Look<Faction>(ref ShipFaction, "ShipFaction");
            Scribe_References.Look<Lord>(ref ShipLord, "ShipLord");
            Scribe_Values.Look<bool>(ref ShipCombatMaster, "ShipCombatMaster", false);
            Scribe_Values.Look<bool>(ref InCombat, "InCombat", false);
            Scribe_Values.Look<bool>(ref IsGraveyard, "IsGraveyard", false);
            Scribe_Values.Look<bool>(ref BurnUpSet, "BurnUpSet", false);
            Scribe_Values.Look<int>(ref EngineRot, "EngineRot");
            if (InCombat)
            {
                Scribe_Values.Look<int>(ref Heading, "Heading");
                Scribe_Collections.Look<ShipCombatProjectile>(ref Projectiles, "ShipProjectiles");
                Scribe_Collections.Look<ShipCombatProjectile>(ref TorpsInRange, "ShipTorpsInRange");
                //SC cache
                Scribe_Collections.Look<Building>(ref MapRootList, "MapRootList", LookMode.Reference);
                Scribe_Collections.Look<Building>(ref MapRootListAll, "MapRootListAll", LookMode.Reference);
                Scribe_Values.Look<bool>(ref BridgeDestroyed, "BridgeDestroyed");
                shipsOnMap = null; //reset cache on load IC
                originMapComp = null;
                masterMapComp = null;
                //SC master only
                Scribe_Values.Look<bool>(ref callSlowTick, "callSlowTick");
                Scribe_Values.Look<float>(ref Range, "BattleRange");
                Scribe_Values.Look<float>(ref RangeToKeep, "RangeToKeep");
                Scribe_Values.Look<bool>(ref PlayerMaintain, "PlayerMaintain");
                Scribe_Values.Look<bool>(ref attackedTradeship, "attackedTradeship");
                Scribe_Values.Look<int>(ref BuildingCountAtStart, "BuildingCountAtStart");
                Scribe_Values.Look<bool>(ref enemyRetreating, "enemyRetreating");
                Scribe_Values.Look<bool>(ref warnedAboutRetreat, "warnedAboutRetreat");
                Scribe_Values.Look<int>(ref warnedAboutAdrift, "warnedAboutAdrift");
                Scribe_Values.Look<bool>(ref hasAnyPlayerPartDetached, "PlayerPartDetached");
                Scribe_Values.Look<bool>(ref startedBoarderLoad, "StartedBoarding");
                Scribe_Values.Look<bool>(ref launchedBoarders, "LaunchedBoarders");
                Scribe_Values.Look<int>(ref BoardStartTick, "BoardStartTick");
            }
            else if (Scribe.mode != LoadSaveMode.Saving)
            {
                MapRootList = null;
            }
        }
        //td get these into shipcache?
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
        public List<Building_ShipCloakingDevice> Cloaks = new List<Building_ShipCloakingDevice>();
        public List<CompShipLifeSupport> LifeSupports = new List<CompShipLifeSupport>();
        public List<CompHullFoamDistributor> HullFoamDistributors = new List<CompHullFoamDistributor>();
        public List<Building_ShipTurretTorpedo> TorpedoTubes = new List<Building_ShipTurretTorpedo>();
        //SC vars
        public Map ShipCombatOriginMap; //"player" map - initializes combat vars
        public Map ShipCombatMasterMap; //"AI" map - runs all non duplicate code
        public Map ShipCombatTargetMap; //target map - for proj, etc.
        public Map ShipGraveyard; //map to put destroyed ships to
        public Faction ShipFaction;
        public Lord ShipLord;
        public bool ShipCombatMaster = false; //true only on ShipCombatMasterMap
        public bool InCombat = false;
        public bool IsGraveyard = false; //temp map, will be removed in a few days
        public bool BurnUpSet = false; //force terminate map+WO if no player pawns or pods present or in flight to
        public int EngineRot;

        public float MapEnginePower;
        public int Heading; //+closer, -apart
        public int BuildingsCount;
        public int totalThreat;
        public int[] threatPerSegment = { 0, 0, 0, 0 };
        //SC cache
        public bool BridgeDestroyed = false;//calls CheckForDetach
        public List<ShipCombatProjectile> Projectiles;
        public List<ShipCombatProjectile> TorpsInRange;
        public List<Building> MapRootListAll = new List<Building>();//all bridges on map
        public List<Building> MapRootList;//primary bridges

        public Dictionary<int, SoShipCache> ShipsOnMapNew = new Dictionary<int, SoShipCache>();//bridgeId, ship
        public List<ShipCache> shipsOnMap;
        public List<ShipCache> ShipsOnMap//rebuild shipsOnMap cache if it is null
        {
            get
            {
                if (shipsOnMap == null)
                {
                    shipsOnMap = new List<ShipCache>();
                    for (int i = 0; i < MapRootList.Count; i++)
                    {
                        shipsOnMap.Add(new ShipCache());
                        shipsOnMap[i].BuildCache(MapRootList[i], i);
                    }
                }
                return shipsOnMap;
            }
        }
        public ShipHeatMapComp originMapComp;
        public ShipHeatMapComp OriginMapComp
        {
            get
            {
                if (this.originMapComp == null)
                {
                    this.originMapComp = ShipCombatOriginMap.GetComponent<ShipHeatMapComp>();
                }
                return this.originMapComp;
            }
        }
        public ShipHeatMapComp masterMapComp;
        public ShipHeatMapComp MasterMapComp
        {
            get
            {
                if (this.masterMapComp == null)
                {
                    this.masterMapComp = ShipCombatMasterMap.GetComponent<ShipHeatMapComp>();
                }
                return this.masterMapComp;
            }
        }
        //SC master only
        public bool callSlowTick = false;
        public float Range; //400 is furthest away, 0 is up close and personal
        public float RangeToKeep;
        public bool PlayerMaintain;
        public bool attackedTradeship;
        public int BuildingCountAtStart = 0;
        public bool enemyRetreating = false;
        public bool warnedAboutRetreat = false;
        public int warnedAboutAdrift = 0;
        public bool hasAnyPlayerPartDetached = false;
        public bool startedBoarderLoad = false;
        public bool launchedBoarders = false;
        public int BoardStartTick = 0;

        public List<Building> SpawnEnemyShip(PassingShip passingShip)
        {
            List<Building> cores = new List<Building>();
            ShipCombatMasterMap = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta - 0.1f + 0.002f * Rand.Range(0, 20);
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).phi - 0.01f + 0.001f * Rand.Range(0, 20);
            //EnemyShip.regionAndRoomUpdater.Enabled = false;
            float playerCombatPoints = MapThreat(this.map) * 0.9f;
            bool isDerelict = false;
            EnemyShipDef shipDef;

            if (passingShip is AttackableShip)
                shipDef = ((AttackableShip)passingShip).enemyShip;
            else if (passingShip is DerelictShip)
            {
                //derelictShip
                shipDef = ((DerelictShip)passingShip).derelictShip;
                isDerelict = true;
            }
            else if (passingShip is TradeShip)
            {
                //tradeship 0.5-2
                shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.tradeShip && def.combatPoints >= playerCombatPoints / 2 * ShipInteriorMod2.difficultySoS && def.combatPoints <= playerCombatPoints * 2 * ShipInteriorMod2.difficultySoS).RandomElement();
                if (shipDef == null)
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.tradeShip).RandomElement();
            }
            else if (ModsConfig.RoyaltyActive && Faction.OfEmpire != null && !Faction.OfEmpire.AllyOrNeutralTo(Faction.OfPlayer) || !ModsConfig.RoyaltyActive)
            {
                //empire can attack if royalty and hostile or no royalty
                //0.5-1.5
                shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && def.combatPoints >= playerCombatPoints / 2 * ShipInteriorMod2.difficultySoS && def.combatPoints <= playerCombatPoints * 3 / 2 * ShipInteriorMod2.difficultySoS).RandomElement();
                //0-1.5
                if (shipDef == null)
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && def.combatPoints <= playerCombatPoints * 1.5f * ShipInteriorMod2.difficultySoS).RandomElement();
                //Last fallback
                if (shipDef == null)
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && def.combatPoints <= 50).RandomElement();
            }
            else
            {
                //0.5-1.5
                shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && def.combatPoints >= playerCombatPoints / 2 * ShipInteriorMod2.difficultySoS && !def.imperialShip && def.combatPoints <= playerCombatPoints * 3 / 2 * ShipInteriorMod2.difficultySoS).RandomElement();
                //0-1.5
                if (shipDef == null)
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && !def.imperialShip && def.combatPoints <= playerCombatPoints * 1.5f * ShipInteriorMod2.difficultySoS).RandomElement();
                //Last fallback
                if (shipDef == null)
                    shipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverAttacks && !def.neverRandom && !def.imperialShip && def.combatPoints <= 50).RandomElement();
            }
            //set ship faction
            Faction enemyshipfac = Faction.OfAncientsHostile;
            if (shipDef.imperialShip && ModsConfig.RoyaltyActive && Faction.OfEmpire != null)
            {
                enemyshipfac = Faction.OfEmpire;
                if (Faction.OfEmpire.AllyOrNeutralTo(Faction.OfPlayer))
                    Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, -150);
            }
            else if (shipDef.mechanoidShip)
                enemyshipfac = Faction.OfMechanoids;
            else if (shipDef.pirateShip)
                enemyshipfac = Faction.OfPirates;
            MasterMapComp.ShipFaction = enemyshipfac;
            MasterMapComp.ShipLord = LordMaker.MakeNewLord(enemyshipfac, new LordJob_DefendShip(enemyshipfac, map.Center), map);
            ShipInteriorMod2.GenerateShip(shipDef, ShipCombatMasterMap, passingShip, enemyshipfac, MasterMapComp.ShipLord, out cores, !isDerelict);

            Log.Message("SOS2 spawned ship: " + shipDef.defName); //keep this on for troubleshooting
            if (isDerelict)
            {
                int time = Rand.RangeInclusive(120000, 240000);
                ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(time);
                Find.LetterStack.ReceiveLetter("ShipEncounterStart".Translate(), "ShipEncounterStartDesc".Translate(ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.NeutralEvent);
            }
            else if (passingShip is TradeShip)
            {
                Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty += 5;
                attackedTradeship = true;
                if (ModsConfig.RoyaltyActive && passingShip.Faction == Faction.OfEmpire && Faction.OfEmpire != null && Faction.OfEmpire.AllyOrNeutralTo(Faction.OfPlayer))
                    Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, -150);
                this.map.passingShipManager.RemoveShip(passingShip);
            }
            else if (passingShip is AttackableShip)
            {
                if (ModsConfig.RoyaltyActive && passingShip.Faction == Faction.OfEmpire && Faction.OfEmpire != null && Faction.OfEmpire.AllyOrNeutralTo(Faction.OfPlayer))
                    Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, -150);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipCombatStart"), TranslatorFormattedStringExtensions.Translate("ShipCombatStartDesc", shipDef.label), LetterDefOf.ThreatBig);
            }
            Log.Message("Spawned enemy cores:" + cores.Count);
            return cores; //td dont use this till all pre V2 shipdefs cleared, after throw error/caution if V1 was spawned
        }
        public int MapThreat(Map map)
        {
            int ShipThreat = 0;
            int ShipMass = 0;
            foreach (Building b in map.spawnedThings.Where(b => b is Building))
            {
                if (b.def == ShipInteriorMod2.hullPlateDef || b.def == ShipInteriorMod2.mechHullPlateDef || b.def == ShipInteriorMod2.archoHullPlateDef)
                    ShipMass += 1;
                else
                {
                    ShipMass += (b.def.size.x * b.def.size.z) * 3;
                    if (b.TryGetComp<CompShipHeat>() != null)
                        ShipThreat += b.TryGetComp<CompShipHeat>().Props.threat;
                    else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                        ShipThreat += 5;
                }
            }
            ShipThreat += ShipMass / 100;
            return ShipThreat;
        }

        public void StartShipEncounter(Building playerShipRoot, PassingShip passingShip = null, Map enemyMap = null, int range = 0)
        {
            //startup on origin
            if (playerShipRoot == null || InCombat || BurnUpSet)
            {
                Log.Message("SOS2 Error: Unable to start ship battle.");
                return;
            }
            //origin vars
            originMapComp = null;
            masterMapComp = null;
            ShipCombatOriginMap = this.map;
            ShipFaction = this.map.Parent.Faction;
            attackedTradeship = false;
            //target or create master + spawn ship
            if (enemyMap == null)
                SpawnEnemyShip(passingShip);
            else
                ShipCombatMasterMap = enemyMap;
            //master vars
            ShipCombatTargetMap = ShipCombatMasterMap;
            MasterMapComp.ShipCombatTargetMap = ShipCombatOriginMap;
            MasterMapComp.ShipCombatOriginMap = this.map;
            MasterMapComp.ShipCombatMasterMap = ShipCombatMasterMap;

            //if ship is derelict switch to "encounter"
            if (passingShip != null && passingShip is DerelictShip)
            {
                MasterMapComp.IsGraveyard = true;
                return;
            }
            MasterMapComp.ShipCombatMaster = true;
            //start caches
            StartBattleCache();
            MasterMapComp.StartBattleCache();
            //set range DL:1-9
            byte detectionLevel = 7;
            if (Sensors.Where(sensor => sensor.def.defName.Equals("Ship_SensorClusterAdv") && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
            {
                ShipCombatMasterMap.fogGrid.ClearAllFog();
                detectionLevel += 2;
            }
            else if (Sensors.Where(sensor => sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel += 1;
            if (range == 0)
            {
                if (Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                if (MasterMapComp.Sensors.Where(sensor => sensor.def.defName.Equals("Ship_SensorClusterAdv") && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                else if (MasterMapComp.Sensors.Any())
                    detectionLevel -= 1;
                if (MasterMapComp.Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                MasterMapComp.Range = 180 + detectionLevel * 20 + Rand.Range(0, 40);
                Log.Message("Enemy range at start: " + MasterMapComp.Range);
            }

            MasterMapComp.callSlowTick = true;
        }
        public void StartBattleCache()
        {
            //reset vars
            ShipGraveyard = null;
            InCombat = true;
            BurnUpSet = false;
            Heading = 0;
            MapEnginePower = 0;
            Projectiles = new List<ShipCombatProjectile>();
            TorpsInRange = new List<ShipCombatProjectile>();
            //SCM only
            if (ShipCombatMaster)
            {
                RangeToKeep = Range;
                PlayerMaintain = false;
                enemyRetreating = false;
                warnedAboutRetreat = false;
                warnedAboutAdrift = 0;
                hasAnyPlayerPartDetached = false;
                startedBoarderLoad = false;
                launchedBoarders = false;
                BoardStartTick = Find.TickManager.TicksGame + 1800;
            }
            //find one bridge per ship
            MapRootList = new List<Building>();
            foreach (Building root in MapRootListAll)
            {
                MapRootList.Add(root);
            }
            //Log.Message("Total Bridges: " + MapRootList.Count + " on map: " + this.map);
            List<Building> duplicateRoots = new List<Building>();
            for (int i = 0; i < MapRootList.Count; i++)
            {
                //Log.Message("Bridge: " + MapRootList[i] + " on map: " + this.map);
                for (int j = i + 1; j < MapRootList.Count; j++)
                {
                    if (ShipUtility.ShipBuildingsAttachedTo(MapRootList[i]).Contains(MapRootList[j]))
                        duplicateRoots.Add(MapRootList[j]);
                }
            }
            foreach (Building b in duplicateRoots)
                MapRootList.Remove(b);
            //Log.Message("Ships: " + MapRootList.Count + " on map: " + this.map);
            shipsOnMap = null;//start cache
            for (int i = 0; i < ShipsOnMap.Count; i++)
            {
                BuildingCountAtStart += ShipsOnMap[i].BuildingCountAtStart;
            }
            //Log.Message("Shipmap buildcount total " + BuildingCountAtStart);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            //heatgrid part
            /*if (!heatGridDirty)
            {
                foreach (ShipHeatNet cachedNet in cachedNets)
                {
                    cachedNet.Tick();
                }
            }*/

            if (InCombat && (this.map == ShipCombatOriginMap || this.map == ShipCombatMasterMap))
            {
                if (ShipCombatMaster)
                {
                    if (OriginMapComp.Heading == 1)
                        Range -= 0.1f * OriginMapComp.MapEnginePower;
                    else if (OriginMapComp.Heading == -1)
                        Range += 0.1f * OriginMapComp.MapEnginePower;
                    if (Heading == 1)
                        Range -= 0.1f * MapEnginePower;
                    else if (Heading == -1)
                        Range += 0.1f * MapEnginePower;
                    if (Range > 400)
                        Range = 400;
                    else if (Range < 0)
                        Range = 0;
                    //no end if player has pawns on enemy ship
                    if (Range >= 400 && enemyRetreating && !ShipCombatMasterMap.mapPawns.AnyColonistSpawned)
                    {
                        EndBattle(this.map, true);
                        return;
                    }
                }
                //deregister projectiles in own cache, spawn them on targetMap
                List<ShipCombatProjectile> toRemove = new List<ShipCombatProjectile>();
                foreach (ShipCombatProjectile proj in Projectiles)
                {
                    if (proj.range >= MasterMapComp.Range)
                    {
                        //td determine miss, remove proj
                        //factors for miss: range+,pilot console+,mass-,thrusters+
                        //factors when fired/registered:weapacc-,tac console-
                        Projectile projectile;
                        IntVec3 spawnCell;
                        if (proj.burstLoc == IntVec3.Invalid)
                            spawnCell = FindClosestEdgeCell(ShipCombatTargetMap, proj.target.Cell);
                        else
                            spawnCell = proj.burstLoc;
                        //Log.Message("Spawning " + proj.turret + " projectile on player ship at " + proj.target);
                        projectile = (Projectile)GenSpawn.Spawn(proj.spawnProjectile, spawnCell, ShipCombatTargetMap);
                        float forcedMissRadius = 1.9f;
                        float optRange = proj.turret.heatComp.Props.optRange;
                        if (optRange != 0 && proj.range > optRange)
                            forcedMissRadius = 1.9f + (proj.range - optRange) / 5;
                        //Log.Message("forcedMissRadius: " + forcedMissRadius);
                        float num = Mathf.Min(VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, proj.target.Cell - spawnCell), GenRadial.MaxRadialPatternRadius);
                        int max = GenRadial.NumCellsInRadius(num);
                        int num2 = Rand.Range(0, max);
                        IntVec3 c = proj.target.Cell + GenRadial.RadialPattern[num2];
                        c += ((c - spawnCell) * 2);
                        //Log.Message("Target cell was " + proj.target.Cell + ", actually hitting " + c);
                        projectile.Launch(proj.turret, spawnCell.ToVector3Shifted(), c, proj.target.Cell, ProjectileHitFlags.All, equipment: proj.turret);
                        toRemove.Add(proj);
                    }
                    else if ((proj.spawnProjectile.thingClass == typeof(Projectile_TorpedoShipCombat) || proj.spawnProjectile.thingClass == typeof(Projectile_ExplosiveShipCombatAntigrain)) && !TorpsInRange.Contains(proj) && MasterMapComp.Range - proj.range < 65)
                    {
                        TorpsInRange.Add(proj);
                    }
                    else
                    {
                        proj.range += proj.speed / 4;
                    }
                }
                foreach (ShipCombatProjectile proj in toRemove)
                {
                    Projectiles.Remove(proj);
                    if (TorpsInRange.Contains(proj))
                        TorpsInRange.Remove(proj);
                }

                //ship destruction code
                if (BridgeDestroyed || Find.TickManager.TicksGame % 20 == 0)
                {
                    BridgeDestroyed = false;
                    for (int i = 0; i < ShipsOnMap.Count; i++)
                    {
                        if (ShipsOnMap[i].ShipDirty)
                        {
                            CheckForDetach(i);
                            callSlowTick = true;
                        }
                    }
                    if (MapRootList.Count <= 0)//if all ships gone, end combat
                    {
                        //Log.Message("Map defeated: " + this.map);
                        EndBattle(this.map, false);
                        return;
                    }
                }
                //SCM only: call both slow ticks
                if (ShipCombatMaster && (callSlowTick || Find.TickManager.TicksGame % 60 == 0))
                {
                    OriginMapComp.SlowTick();
                    SlowTick();
                    callSlowTick = false;
                }
            }
        }
        public void SlowTick()
        {
            totalThreat = 1;
            threatPerSegment = new[] { 1, 1, 1, 1 };
            int TurretNum = 0;
            MapEnginePower = 0;
            bool anyMapEngineCanActivate = false;
            BuildingsCount = 0;
            //SCM vars
            float powerCapacity = 0;
            float powerRemaining = 0;
            bool shieldsUp = false;
            bool isPurging = false;
            bool canPurge = false;
            //threat and engine power calcs
            foreach (ShipCache ship in ShipsOnMap)
            {
                if (ShipCombatMaster)//heatpurge
                {
                    var bridge = ship.Bridges.FirstOrDefault().heatComp;
                    foreach (var battery in ship.Batteries)
                    {
                        powerCapacity += battery.Props.storedEnergyMax;
                        powerRemaining += battery.StoredEnergy;
                    }
                    shieldsUp = ship.CombatShields.Any(shield => !shield.shutDown);
                    canPurge = ship.HeatPurges.Any(purge => purge.fuelComp.Fuel > 0);
                    isPurging = ship.HeatPurges.Any(purge => purge.currentlyPurging);

                    if (!isPurging)
                    {
                        if (bridge.RatioInNetwork() > 0.8f)
                        {
                            if (shieldsUp)
                            {
                                foreach (var shield in ship.CombatShields)
                                {
                                    if (shield.flickComp == null) continue;

                                    shield.flickComp.SwitchIsOn = false;
                                }
                            }
                            if (canPurge)
                            {
                                foreach (CompShipHeatPurge purge in ship.HeatPurges)
                                {
                                    purge.currentlyPurging = true;
                                }
                            }
                        }
                        else if (bridge.RatioInNetwork() < 0.5f && !shieldsUp)
                        {
                            foreach (var shield in ship.CombatShields)
                            {
                                if (shield.flickComp == null) continue;

                                shield.flickComp.SwitchIsOn = true;
                            }
                        }
                    }
                }
                foreach (var turret in ship.Turrets)
                {
                    TurretNum++;
                    if (turret.torpComp != null && !turret.torpComp.Loaded)
                        continue;
                    totalThreat += turret.heatComp.Props.threat;
                    if (turret.heatComp.Props.maxRange > 150)//long
                    {
                        threatPerSegment[0] += turret.heatComp.Props.threat / 6;
                        threatPerSegment[1] += turret.heatComp.Props.threat / 4;
                        threatPerSegment[2] += turret.heatComp.Props.threat / 2;
                        threatPerSegment[3] += turret.heatComp.Props.threat;
                    }
                    else if (turret.heatComp.Props.maxRange > 100)//med
                    {
                        threatPerSegment[0] += turret.heatComp.Props.threat / 4;
                        threatPerSegment[1] += turret.heatComp.Props.threat / 2;
                        threatPerSegment[2] += turret.heatComp.Props.threat;
                    }
                    else if (turret.heatComp.Props.maxRange > 50)//short
                    {
                        threatPerSegment[0] += turret.heatComp.Props.threat / 2;
                        threatPerSegment[1] += turret.heatComp.Props.threat;
                    }
                    else //cqc
                        threatPerSegment[0] += turret.heatComp.Props.threat;
                }
                if (ship.Engines.FirstOrDefault() != null)
                    EngineRot = ship.Engines.FirstOrDefault().parent.Rotation.AsByte;

                foreach (var engine in ship.Engines)
                {
                    if (engine.CanActivate())
                    {
                        anyMapEngineCanActivate = true;
                        if (Heading != 0)
                        {
                            engine.Activate();
                            MapEnginePower += engine.Props.thrust;
                        }
                        else
                            engine.DeActivate();
                    }
                    else
                        engine.DeActivate();						 
                }
                BuildingsCount += ship.Buildings.Count;
            }
            //Log.Message("Engine power: " + MapEnginePower + ", ship size: " + BuildingsCount);
            if (anyMapEngineCanActivate)
                MapEnginePower *= 500f / Mathf.Pow(BuildingsCount, 1.1f);
            else
                MapEnginePower = 0;
            //Log.Message("Engine power: " + MapEnginePower + ", ship size: " + BuildingsCount);

            //SCM only: ship AI and player distance maintain
            if (ShipCombatMaster)
            {
                if (anyMapEngineCanActivate) //set AI heading
                {
                    //calc ratios
                    float longThreatRel = threatPerSegment[3] / OriginMapComp.threatPerSegment[3];
                    float medThreatRel = threatPerSegment[2] / OriginMapComp.threatPerSegment[2];
                    float shortThreatRel = threatPerSegment[1] / OriginMapComp.threatPerSegment[1];
                    float cqcThreatRel = threatPerSegment[0] / OriginMapComp.threatPerSegment[0];
                    //move to cqc
                    if (cqcThreatRel > shortThreatRel && cqcThreatRel > medThreatRel && cqcThreatRel > longThreatRel)
                    {
                        if (Range > 40)
                            Heading = 1;
                        else
                            Heading = 0;
                    }
                    //move to short range
                    else if (shortThreatRel > medThreatRel && shortThreatRel > longThreatRel)
                    {
                        if (Range > 90)
                            Heading = 1;
                        else if (Range <= 60)
                            Heading = -1;
                        else
                            Heading = 0;
                    }
                    //move to medium range
                    else if (medThreatRel > longThreatRel)
                    {
                        if (Range > 140)
                            Heading = 1;
                        else if (Range <= 110)
                            Heading = -1;
                        else
                            Heading = 0;
                    }
                    //move to long range
                    else
                    {
                        if (Range > 190)
                            Heading = 1;
                        else if (Range <= 160)
                            Heading = -1;
                        else
                            Heading = 0;
                    }
                    //retreat
                    if (totalThreat / (OriginMapComp.totalThreat * ShipInteriorMod2.difficultySoS) < 0.3f || powerRemaining / powerCapacity < 0.1f || TurretNum == 0 || BuildingsCount * 1f / BuildingCountAtStart < 0.6f)
                    {
                        Heading = -1;
                        enemyRetreating = true;
                        if (!warnedAboutRetreat)
                        {
                            Messages.Message("EnemyShipRetreating".Translate(), MessageTypeDefOf.ThreatBig);
                            warnedAboutRetreat = true;
                        }
                    }
                }
                else
                {
                    if (threatPerSegment[0] == 1 && threatPerSegment[1] == 1 && threatPerSegment[2] == 1 && threatPerSegment[3] == 1)
                    {
                        //no turrets to fight with - exit
                        EndBattle(this.map, false);
                        return;
                    }
                    if (warnedAboutAdrift == 0)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipAdrift"), this.map.Parent, MessageTypeDefOf.NegativeEvent);
                        warnedAboutAdrift = Find.TickManager.TicksGame + Rand.RangeInclusive(60000, 100000);
                    }
                    else if (Find.TickManager.TicksGame > warnedAboutAdrift)
                    {
                        EndBattle(this.map, false, warnedAboutAdrift - Find.TickManager.TicksGame);
                        return;
                    }
                    Heading = 0;
                    enemyRetreating = false;
                }
                if (PlayerMaintain)
                {
                    if (Heading == 1) //enemy moving to player
                    {
                        if (RangeToKeep > Range)
                            OriginMapComp.Heading = -1;
                        else
                            OriginMapComp.Heading = 0;
                    }
                    else if (Heading == -1) //enemy moving from player
                    {
                        if (RangeToKeep < Range)
                            OriginMapComp.Heading = 1;
                        else
                            OriginMapComp.Heading = 0;
                    }
                    else if (Heading == 0)
                    {
                        OriginMapComp.Heading = 0;
                    }
                }
                //AI boarding code
                if ((hasAnyPlayerPartDetached || Find.TickManager.TicksGame > BoardStartTick) && !startedBoarderLoad && !enemyRetreating)
                {
                    List <CompTransporter> transporters = new List<CompTransporter>();
                    float transporterMass = 0;
                    foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter))
                    {
                        if (t.Faction == Faction.OfPlayer) continue;

                        var transporter = t.TryGetComp<CompTransporter>();
                        if (transporter == null) continue;

                        if (t.def.defName.Equals("PersonalShuttle"))
                        {
                            transporters.Add(transporter);
                            transporterMass += transporter.Props.massCapacity;
                        }
                    }
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Faction != Faction.OfPlayer && transporterMass >= p.RaceProps.baseBodySize * 70 && p.Faction != Faction.OfPlayer && p.mindState.duty != null && p.kindDef.combatPower > 40)
                        {
                            TransferableOneWay tr = new TransferableOneWay();
                            tr.things.Add(p);
                            CompTransporter porter = transporters.RandomElement();
                            porter.groupID = 0;
                            porter.AddToTheToLoadList(tr, 1);
                            p.mindState.duty.transportersGroup = 0;
                            transporterMass -= p.RaceProps.baseBodySize * 70;
                        }
                    }
                    startedBoarderLoad = true;
                }
                if (startedBoarderLoad && !launchedBoarders && !enemyRetreating)
                {
                    //abort and reset if player on ship
                    if (this.map.mapPawns.AllPawnsSpawned.Where(o => o.Faction == Faction.OfPlayer).Any())
                    {
                        foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
                        {
                            if (t.TryGetComp<CompTransporter>().innerContainer.Any || t.TryGetComp<CompTransporter>().AnythingLeftToLoad)
                                t.TryGetComp<CompTransporter>().CancelLoad();
                        }
                        startedBoarderLoad = false;
                    }
                    else //board
                    {
                        bool allOnPods = true;
                        foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned.Where(o => o.Faction != Faction.OfPlayer))
                        {
                            if (p.mindState?.duty?.transportersGroup == 0 && p.MannedThing() == null)
                                allOnPods = false;
                        }
                        if (allOnPods) //launch
                        {
                            List<CompShuttleLaunchable> transporters = new List<CompShuttleLaunchable>();
                            foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
                            {
                                var transporter = t.TryGetComp<CompTransporter>();
                                if (!(transporter?.innerContainer.Any ?? false)) continue;

                                var launchable = t.TryGetComp<CompShuttleLaunchable>();
                                if (launchable == null) continue;

                                transporters.Add(launchable);
                            }
                            if (transporters.Count > 0)
                            {
                                transporters[0].TryLaunch(ShipCombatOriginMap.Parent, new TransportPodsArrivalAction_ShipAssault(ShipCombatOriginMap.Parent));
                                OriginMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_AssaultShip(ShipFaction, false, false, false, true, false), ShipCombatOriginMap, new List<Pawn>());
                            }
                            launchedBoarders = true;
                        }
                    }
                }
            }
        }
        //cache functions
        public void DirtyShip(Building building)//called ondestroy of ship part
        {
            //Log.Message("Attempting to dirty ship");
            for (int j = 0; j < ShipsOnMap.Count; j++)
            {
                for (int i = ShipsOnMap[j].BuildingsByGeneration.Count - 1; i >= 0; i--)
                {
                    if (ShipsOnMap[j].BuildingsByGeneration[i].Contains(building))
                    {
                        ShipsOnMap[j].ShipDirty = true;
                        if (i < ShipsOnMap[j].ShipDirtyGen)
                            ShipsOnMap[j].ShipDirtyGen = i;
                        //Log.Message("ship " + j + " is dirty at generation " + i);
                        break;
                    }
                }
            }
        }
        public void CheckForDetach(int shipIndex)
        {
            List<IntVec3> detached = new List<IntVec3>();
            //Log.Message("Checking for detach on ship " + shipIndex);
            if (MapRootList[shipIndex].Destroyed || !MapRootList[shipIndex].Spawned)//if primary destroyed
            {
                //Log.Message("Main bridge "+ MapRootList[shipIndex]+" on ship " + shipIndex + " destroyed");
                foreach (var bridge in ShipsOnMap[shipIndex].BridgesAtStart)//cheack each bridge on ship
                {
                    if (bridge.Destroyed || !bridge.Spawned) continue;//if destroyed, keep trying

                    MapRootList[shipIndex] = bridge;//if not destroyed replace main bridge
                    //Log.Message("Replacing main bridge on ship " + shipIndex + " to "+ bridge);
                    break;
                }
                if (MapRootList[shipIndex].Destroyed || !MapRootList[shipIndex].Spawned)//no intact bridges found = ship destroyed
                {
                    //Log.Message("Destroyed ship " + shipIndex);
                    RemoveShipFromBattle(shipIndex);
                    return;
                }
                //bridge destroyed, save area, rebuild cache, remove from area, detach area
                HashSet<IntVec3> ShipAreaAtStart = ShipsOnMap[shipIndex].ShipAreaAtStart;
                ShipsOnMap[shipIndex].BuildCache(MapRootList[shipIndex], shipIndex);//rebuild cache for affected ship
                HashSet<IntVec3> StillAttached = new HashSet<IntVec3>();
                //Log.Message("Checking dirty ship " + shipIndex + " at generation " + ShipDirtyGen[shipIndex]);
                foreach (var b in ShipsOnMap[shipIndex].Buildings)
                {
                    StillAttached.Add(b.Position);
                }
                detached = ShipAreaAtStart.Except(StillAttached).ToList();

            }
            else //normal detach
            {
                ShipsOnMap[shipIndex].RebuildCacheFromGeneration(this.map);
                HashSet<IntVec3> StillAttached = new HashSet<IntVec3>();
                //Log.Message("Checking dirty ship " + shipIndex + " at generation " + ShipDirtyGen[shipIndex]);
                foreach (var b in ShipsOnMap[shipIndex].Buildings)
                {
                    StillAttached.Add(b.Position);
                }
                detached = ShipsOnMap[shipIndex].ShipAreaAtStart.Except(StillAttached).ToList();
            }
            ShipsOnMap[shipIndex].Detach(shipIndex, this.map, detached);
        }
        public void RemoveShipFromBattle(int shipIndex, Building b = null, Faction fac = null)
        {
            if (MapRootList.Count > 1)//move to graveyard if not last ship
            {
                if (b == null)
                {
                    foreach (IntVec3 at in ShipsOnMap[shipIndex].ShipAreaAtStart)
                    {
                        if (at.GetFirstBuilding(this.map) != null)
                        {
                            b = at.GetFirstBuilding(this.map);
                            break;
                        }
                    }
                }
                if (b != null)
                {
                    if (ShipGraveyard == null)
                        SpawnGraveyard();
                    ShipInteriorMod2.MoveShip(b, ShipGraveyard, new IntVec3(0, 0, 0), fac);
                }
            }
            else if (fac != null)//last ship hacked
            {
                foreach (Building building in ShipsOnMap[shipIndex].Buildings)
                {
                    if (building.def.CanHaveFaction)
                        building.SetFaction(Faction.OfPlayer);
                }
            }
            MapRootList.RemoveAt(shipIndex);
            ShipsOnMap.Remove(ShipsOnMap[shipIndex]);
            //Log.Message("Ships remaining: " + MapRootList.Count);
        }
        public void SpawnGraveyard()//if not present, create a graveyard
        {
            float adj;
            if (ShipCombatMaster)
                adj = Rand.Range(-0.075f, -0.125f);
            else
                adj = Rand.Range(0.025f, 0.075f);
            ShipGraveyard = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), this.map.Size, DefDatabase<WorldObjectDef>.GetNamed("ShipEnemy"));
            ShipGraveyard.fogGrid.ClearAllFog();
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta + adj;
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).phi - 0.01f + 0.001f * Rand.Range(0, 20);
            var graveMapComp = ShipGraveyard.GetComponent<ShipHeatMapComp>();
            graveMapComp.IsGraveyard = true;
            if (ShipCombatMaster)
            {
                graveMapComp.ShipFaction = MasterMapComp.ShipFaction;
                graveMapComp.ShipCombatMasterMap = this.map;
                graveMapComp.ShipCombatOriginMap = graveMapComp.MasterMapComp.ShipCombatOriginMap;
                //graveMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_DefendShip(ShipFaction, map.Center), map);
            }
            else
            {
                graveMapComp.ShipFaction = OriginMapComp.ShipFaction;
                graveMapComp.ShipCombatOriginMap = this.map;
                graveMapComp.ShipCombatMasterMap = graveMapComp.OriginMapComp.ShipCombatMasterMap;
            }
        }
        public void ShipBuildingsOff()
        {
            foreach (Building b in ShipCombatOriginMap.listerBuildings.allBuildingsColonist)
            {
                if (b.TryGetComp<CompEngineTrail>() != null)
                    b.TryGetComp<CompEngineTrail>().DeActivate();
            }
            foreach (Building b in ShipCombatMasterMap.listerBuildings.allBuildingsNonColonist)
            {
                if (b.TryGetComp<CompEngineTrail>() != null)
                    b.TryGetComp<CompEngineTrail>().DeActivate();
                else if (b.TryGetComp<CompShipCombatShield>() != null)
                    b.TryGetComp<CompFlickable>().SwitchIsOn = false;
            }
        }
        public void EndBattle(Map loser, bool fled, int burnTimeElapsed = 0)
        {
            OriginMapComp.InCombat = false;
            MasterMapComp.InCombat = false;
            OriginMapComp.ShipBuildingsOff();
            MasterMapComp.ShipCombatMaster = false;
            if (OriginMapComp.ShipGraveyard != null)
                OriginMapComp.ShipGraveyard.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
            if (MasterMapComp.ShipGraveyard != null)
                MasterMapComp.ShipGraveyard.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
            if (loser == ShipCombatMasterMap)
            {
                if (!fled)//master lost
                {
                    MasterMapComp.IsGraveyard = true;
                    if (OriginMapComp.attackedTradeship)
                        Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty += 15;
                    ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
                    Find.LetterStack.ReceiveLetter("WinShipBattle".Translate(), "WinShipBattleDesc".Translate(ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.PositiveEvent);
                }
                else //master fled
                {
                    MasterMapComp.BurnUpSet = true;
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipRetreated"), MessageTypeDefOf.ThreatBig);
                }
            }
            else
            {
                if (!fled)//origin lost
                {
                    ShipCombatOriginMap.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 120000));
                    //Find.GameEnder.CheckOrUpdateGameOver();
                }
                //origin fled or lost: if origin has grave with a ship, start combat on it
                if (OriginMapComp.ShipGraveyard != null && !OriginMapComp.attackedTradeship)
                {
                    Building bridge = OriginMapComp.ShipGraveyard.listerBuildings.allBuildingsColonist.Where(x => x is Building_ShipBridge).FirstOrDefault();
                    if (bridge != null)
                    {
                        //OriginMapComp.IsGraveyard = true;
                        OriginMapComp.ShipGraveyard.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, null, ShipCombatMasterMap);
                    }
                }
                else //origin fled or lost with no graveyard
                {
                    if (ShipCombatMasterMap.mapPawns.AnyColonistSpawned)//pawns on master: give origin some time
                    {
                        ShipCombatOriginMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 120000));
                    }
                    else
                        MasterMapComp.BurnUpSet = true;
                }
            }
        }
        //proj
        public IntVec3 FindClosestEdgeCell(Map map, IntVec3 targetCell)
        {
            Rot4 dir;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            if (mapComp.Heading == 1)//target advancing - shots from front
            {
                dir = new Rot4(mapComp.EngineRot);
            }
            else if (mapComp.Heading == -1)//target retreating - shots from back
            {
                dir = new Rot4(mapComp.EngineRot + 2);
            }
            else //shots from closest edge
            {
                if (targetCell.x < map.Size.x / 2 && targetCell.x < targetCell.z && targetCell.x < (map.Size.z) - targetCell.z)
                    dir = Rot4.West;
                else if (targetCell.x > map.Size.x / 2 && map.Size.x - targetCell.x < targetCell.z && map.Size.x - targetCell.x < (map.Size.z) - targetCell.z)
                    dir = Rot4.East;
                else if (targetCell.z > map.Size.z / 2)
                    dir = Rot4.North;
                else
                    dir = Rot4.South;
            }
            return CellFinder.RandomEdgeCell(dir, map);
        }
    }

    public class ShipCache
    {
        public HashSet<IntVec3> ShipAreaAtStart = new HashSet<IntVec3>();
        public List<Building_ShipBridge> BridgesAtStart = new List<Building_ShipBridge>();
        public bool ShipDirty;
        public int ShipDirtyGen;
        public int BuildingCountAtStart;

        public List<HashSet<Building>> BuildingsByGeneration = new List<HashSet<Building>>();
        public HashSet<Building> Buildings = new HashSet<Building>();
        public List<Building_ShipTurret> Turrets = new List<Building_ShipTurret>();
        public List<CompPowerBattery> Batteries = new List<CompPowerBattery>();
        public List<CompShipHeatSink> HeatSinks = new List<CompShipHeatSink>();
        public List<CompShipCombatShield> CombatShields = new List<CompShipCombatShield>();
        public List<CompEngineTrail> Engines = new List<CompEngineTrail>();
        public List<Building_ShipBridge> Bridges = new List<Building_ShipBridge>();
        public List<CompShipHeatPurge> HeatPurges = new List<CompShipHeatPurge>();
        //public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
        //public List<Building_ShipCloakingDevice> Cloaks = new List<Building_ShipCloakingDevice>();
        //public List<CompShipLifeSupport> LifeSupports = new List<CompShipLifeSupport>();
        //public List<CompHullFoamDistributor> FoamDistributors = new List<CompHullFoamDistributor>();

        void GetBuildingsByGeneration(ref List<HashSet<Building>> generations, ref HashSet<Building> buildings, Map map)
        {
            if (generations.NullOrEmpty()) //Error state - bridge not found
            {
                return;
            }
            HashSet<Building> nextGen = new HashSet<Building>();
            HashSet<Building> currentGen = generations[generations.Count - 1];
            HashSet<IntVec3> cellsTodo = new HashSet<IntVec3>();
            foreach (Building building in currentGen)
            {
                cellsTodo.AddRange(GenAdj.CellsOccupiedBy(building));
                cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(building));
            }
            foreach (IntVec3 cell in cellsTodo)
            {
                if (cell.InBounds(map))
                {
                    foreach (Thing t in cell.GetThingList(map))
                    {
                        if (t is Building building)
                        {
                            if (building.def.mineable == false && buildings.Add(building))
                            {
                                nextGen.Add(building);
                            }
                        }
                    }
                }
            }
            if (nextGen.Count > 0) //continue if buildings found
            {
                generations.Add(nextGen);
                GetBuildingsByGeneration(ref generations, ref buildings, map);
            }
        }
        public void BuildCache(Building shipRoot, int index)//at combat start
        {
            BuildingsByGeneration = new List<HashSet<Building>>();
            HashSet<Building> firstGen = new HashSet<Building>();
            firstGen.Add(shipRoot);
            BuildingsByGeneration.Add(firstGen);
            GetBuildingsByGeneration(ref BuildingsByGeneration, ref Buildings, shipRoot.Map);
            ShipDirty = false;
            ShipDirtyGen = int.MaxValue;
            BuildingCountAtStart = 0;
            CacheComps(true, index);
        }
        public void RebuildCacheFromGeneration(Map map)//revert if any ship part is destroyed
        {
            List<HashSet<Building>> newGenerations = new List<HashSet<Building>>();
            HashSet<Building> newBuildings = new HashSet<Building>();
            for (int i = 0; i < ShipDirtyGen; i++)
            {
                newGenerations.Add(BuildingsByGeneration[i]);
                foreach (Building b in BuildingsByGeneration[i])
                    newBuildings.Add(b);
            }
            BuildingsByGeneration = newGenerations;
            Buildings = newBuildings;
            GetBuildingsByGeneration(ref BuildingsByGeneration, ref Buildings, map);
            CacheComps(false);
        }
        void CacheComps(bool resetCache, int index = -1)
        {
            if (resetCache)
            {
                ShipAreaAtStart = new HashSet<IntVec3>();
                BridgesAtStart = new List<Building_ShipBridge>();
                BuildingCountAtStart = 0;
            }
            Turrets = new List<Building_ShipTurret>();
            Batteries = new List<CompPowerBattery>();
            HeatSinks = new List<CompShipHeatSink>();
            CombatShields = new List<CompShipCombatShield>();
            Engines = new List<CompEngineTrail>();
            Bridges = new List<Building_ShipBridge>();
            HeatPurges = new List<CompShipHeatPurge>();

            foreach (var building in Buildings)
            {
                if (building is Building_ShipTurret turret)
                    Turrets.Add(turret);
                else if (building.TryGetComp<CompPowerBattery>() != null)
                    Batteries.Add(building.GetComp<CompPowerBattery>());
                else if (building.TryGetComp<CompShipHeatSink>() != null)
                {
                    HeatSinks.Add(building.GetComp<CompShipHeatSink>());
                    if (building.TryGetComp<CompShipHeatPurge>() != null)
                        HeatPurges.Add(building.GetComp<CompShipHeatPurge>());
                }
                else if (building.TryGetComp<CompEngineTrail>() != null)
                {
                    Engines.Add(building.TryGetComp<CompEngineTrail>());
                }
                //else if (building.TryGetComp<CompShipCombatShield>() != null)
                //    CombatShields.Add(building.GetComp<CompShipCombatShield>());
                else if (building is Building_ShipBridge bridge)
                {
                    if (!bridge.Destroyed)
                    {
                        Bridges.Add(bridge);
                        if (resetCache)
                        {
                            BridgesAtStart.Add(bridge);
                            bridge.shipIndex = index;
                            Log.Message("Added bridge: " + bridge + " on ship: " + index);
                        }
                    }
                }
                /*else if (building.TryGetComp<CompHullFoamDistributor>() != null)
                    FoamDistributors.Add(building.GetComp<CompHullFoamDistributor>());
                else if (building.TryGetComp<CompShipLifeSupport>() != null)
                    LifeSupports.Add(building.GetComp<CompShipLifeSupport>());*/

                if (resetCache)
                {
                    BuildingCountAtStart++;
                    foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
                    {
                        if (!ShipAreaAtStart.Contains(pos))
                            ShipAreaAtStart.Add(building.Position);
                    }
                }
            }
            if (resetCache)
            {
                //Log.Message("Ship area is " + ShipAreaAtStart.Count);
                //Log.Message("Ship mass is " + BuildingCountAtStart);
            }
        }
        public void Detach(int shipIndex, Map map, List<IntVec3> detached)
        {
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            if (detached.Count > 0)
            {
                //Log.Message("Detaching " + detached.Count + " tiles");
                ShipInteriorMod2.AirlockBugFlag = true;
                List<Thing> toDestroy = new List<Thing>();
                List<Thing> toReplace = new List<Thing>();
                bool detachThing = false;
                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minZ = int.MaxValue;
                int maxZ = int.MinValue;
                foreach (IntVec3 at in detached)
                {
                    //Log.Message("Detaching location " + at);
                    foreach (Thing t in at.GetThingList(map))
                    {
                        if (t is Building_ShipBridge) //stopgap for invalid state bug
                        {
                            if (mapComp.MapRootList.Contains(t))
                            {
                                Log.Message("Tried removing primary bridge from ship, aborting detach.");
                                ShipInteriorMod2.AirlockBugFlag = false;
                                mapComp.RemoveShipFromBattle(shipIndex);
                                return;
                            }
                        }
                        if (!(t is Pawn) && !(t is Blueprint))
                            toDestroy.Add(t);
                        else if (t is Pawn)
                        {
                            if (t.Faction != Faction.OfPlayer && Rand.Chance(0.75f))
                                toDestroy.Add(t);
                        }
                        if (t is Building)
                        {
                            detachThing = true;
                            if (t.TryGetComp<CompRoofMe>() != null)
                            {
                                toReplace.Add(t);
                            }
                            else if (t.def.IsEdifice())
                            {
                                toReplace.Add(t);
                            }
                        }
                        if (t.Position.x < minX)
                            minX = t.Position.x;
                        if (t.Position.x > maxX)
                            maxX = t.Position.x;
                        if (t.Position.z < minZ)
                            minZ = t.Position.z;
                        if (t.Position.z > maxZ)
                            maxZ = t.Position.z;
                    }
                    map.terrainGrid.RemoveTopLayer(at, false);
                }
                foreach (Thing t in toReplace)
                {
                    if (t.def.IsEdifice())
                    {
                        Thing replacement = ThingMaker.MakeThing(ShipInteriorMod2.wreckedBeamDef);
                        replacement.Position = t.Position;
                        if (t.def.destroyable && !t.Destroyed)
                            t.Destroy();
                        replacement.SpawnSetup(map, false);
                        toDestroy.Add(replacement);
                    }
                    else
                    {
                        Thing replacement = ThingMaker.MakeThing(ShipInteriorMod2.wreckedHullPlateDef);
                        replacement.Position = t.Position;
                        if (t.def.destroyable && !t.Destroyed)
                            t.Destroy();
                        replacement.SpawnSetup(map, false);
                        toDestroy.Add(replacement);
                    }
                }
                if (detachThing)
                {
                    DetachedShipPart part = (DetachedShipPart)ThingMaker.MakeThing(ThingDef.Named("DetachedShipPart"));
                    part.Position = new IntVec3(minX, 0, minZ);
                    part.xSize = maxX - minX + 1;
                    part.zSize = maxZ - minZ + 1;
                    part.wreckage = new byte[part.xSize, part.zSize];
                    foreach (Thing t in toDestroy)
                    {
                        if (t is Pawn)
                            t.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                        else if (t.def == ShipInteriorMod2.wreckedBeamDef)
                            part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 1;
                        else if (t.def == ShipInteriorMod2.wreckedHullPlateDef)
                            part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 2;
                    }
                    part.SpawnSetup(map, false);
                }
                foreach (Thing t in toDestroy)
                {
                    if (t is Building && map.IsPlayerHome && t.def.blueprintDef != null)
                    {
                        GenConstruct.PlaceBlueprintForBuild(t.def, t.Position, map, t.Rotation, Faction.OfPlayer, t.Stuff);
                    }
                    map.terrainGrid.RemoveTopLayer(t.Position, false);
                    if (t.def.destroyable && !t.Destroyed)
                        t.Destroy(DestroyMode.Vanish);
                }
                foreach (IntVec3 c in detached)
                {
                    map.roofGrid.SetRoof(c, null);
                }
                ShipInteriorMod2.AirlockBugFlag = false;
                if (map == mapComp.ShipCombatOriginMap)
                    mapComp.hasAnyPlayerPartDetached = true;
                foreach (IntVec3 pos in detached)
                    ShipAreaAtStart.Remove(pos);
            }
            ShipDirty = false;
            ShipDirtyGen = int.MaxValue;
        }
    }
}
