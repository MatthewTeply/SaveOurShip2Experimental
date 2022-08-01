﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HugsLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using HarmonyLib;
using System.Text;
using UnityEngine;
using HugsLib.Utils;
using Verse.AI.Group;
using HugsLib.Settings;
using RimWorld.QuestGen;
using RimworldMod;
using System.Net;
using System.IO;
using RimworldMod.VacuumIsNotFun;
using System.Collections;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;
using System.Linq.Expressions;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class ShipInteriorMod2 : ModBase
	{
		public static HugsLib.Utils.ModLogger instLogger;

		public static readonly float HeatPushMult = 50f;
		public static readonly float crittersleepBodySize = 0.7f;
		public static bool ArchoStuffEnabled = true;//unassigned???
		public static bool SoSWin = false;
		static bool loadedGraphics = false;

		public static bool AirlockBugFlag = false;//shipmove
		public static Building shipOriginRoot = null;//used for patched original launch code
		public static Map shipOriginMap = null;//used to check for shipmove map size problem, reset after move

		public static Graphic shipZeroEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_Off",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
		public static Graphic shipOneEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_On_slow",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
		public static Graphic shipTwoEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_On_mid",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
		public static Graphic projectileEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/EnemyProjectile",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
		public static Graphic shipZero = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_Off",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipOne = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_slow",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipTwo = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_mid",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipThree = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_fast",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shuttlePlayer = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Shuttle_Icon_Player",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shuttleEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Shuttle_Icon_Enemy",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
		public static Graphic ruler = GraphicDatabase.Get(typeof(Graphic_Single), "UI/ShipRangeRuler",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic projectile = GraphicDatabase.Get(typeof(Graphic_Single), "UI/ShipProjectile",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipBarEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Enemy",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipBarPlayer = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Player",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		public static Graphic shipBarNeutral = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Neutral",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);

		public static Texture2D PowerTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.45f, 0.425f, 0.1f));
		public static Texture2D HeatTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.1f, 0.1f));
		public static Texture2D Splash = ContentFinder<Texture2D>.Get("SplashScreen");

		public static ThingDef MechaniteFire = ThingDef.Named("MechaniteFire");
		public static ThingDef ArchotechSpore = ThingDef.Named("ShipArchotechSpore");
		public static ThingDef HoloEmitterDef = ThingDef.Named("Apparel_HologramRelay");
		public static ThingDef beamDef = ThingDef.Named("Ship_Beam");
		public static ThingDef beamMechDef = ThingDef.Named("Ship_BeamMech");
		public static ThingDef beamArchotechDef = ThingDef.Named("Ship_BeamArchotech");
		public static ThingDef wreckedBeamDef = ThingDef.Named("Ship_Beam_Wrecked");
		public static ThingDef wreckedAirlockDef = ThingDef.Named("ShipAirlockWrecked");
		public static ThingDef wreckedHullPlateDef = ThingDef.Named("ShipHullTileWrecked");
		public static ThingDef hullPlateDef = ThingDef.Named("ShipHullTile");
		public static ThingDef mechHullPlateDef = ThingDef.Named("ShipHullTileMech");
		public static ThingDef archoHullPlateDef = ThingDef.Named("ShipHullTileArchotech");
		public static ThingDef hullFoamDef = ThingDef.Named("ShipHullfoamTile");
		public static TerrainDef hullFloorDef = TerrainDef.Named("FakeFloorInsideShip");
		public static TerrainDef mechHullFloorDef = TerrainDef.Named("FakeFloorInsideShipMech");
		public static TerrainDef archoHullFloorDef = TerrainDef.Named("FakeFloorInsideShipArchotech");
		public static TerrainDef wreckedHullFloorDef = TerrainDef.Named("ShipWreckageTerrain");
		public static TerrainDef hullFoamFloorDef = TerrainDef.Named("FakeFloorInsideShipFoam");
		public static RoofDef shipRoofDef = DefDatabase<RoofDef>.GetNamed("RoofShip");

		public static HediffDef hypoxia = HediffDef.Named("SpaceHypoxia");
		public static HediffDef ArchoLung = HediffDef.Named("SoSArchotechLung");
		public static HediffDef ArchoSkin = HediffDef.Named("SoSArchotechSkin");
		public static HediffDef bubbleHediff = HediffDef.Named("SpaceBeltBubbleHediff");
		public static HediffDef SoSHologramArchotech = HediffDef.Named("SoSHologramArchotech");
		public static Backstory hologramBackstory;
		public static MemeDef Archism = DefDatabase<MemeDef>.GetNamed("Structure_Archist", false);
		public static BiomeDef OuterSpaceBiome = DefDatabase<BiomeDef>.GetNamed("OuterSpaceBiome");
		public static StorytellerDef Sara = DefDatabase<StorytellerDef>.GetNamed("Sara");
		public static ResearchProjectDef PillarAProject = ResearchProjectDef.Named("ArchotechPillarA");
		public static ResearchProjectDef PillarBProject = ResearchProjectDef.Named("ArchotechPillarB");

		public static List<ThingDef> randomPlants = DefDatabase<ThingDef>.AllDefs.Where(t => t.plant != null && !t.defName.Contains("Anima")).ToList();
		public static Dictionary<ThingDef, ThingDef> wreckDictionary = new Dictionary<ThingDef, ThingDef>();

		static ShipInteriorMod2()
		{
			hologramBackstory = new Backstory();
            hologramBackstory.identifier = "SoSHologram";
            hologramBackstory.slot = BackstorySlot.Childhood;
            hologramBackstory.title = "machine persona";
            hologramBackstory.titleFemale = "machine persona";
            hologramBackstory.titleShort = "persona";
            hologramBackstory.titleShortFemale = "persona";
            hologramBackstory.baseDesc = "{PAWN_nameDef} is a machine persona. {PAWN_pronoun} interacts with the world via a hologram, which cannot leave the map where {PAWN_possessive} core resides.";
            hologramBackstory.shuffleable = false;
            typeof(Backstory).GetField("bodyTypeFemaleResolved", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hologramBackstory, BodyTypeDefOf.Female);
            typeof(Backstory).GetField("bodyTypeMaleResolved", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hologramBackstory, BodyTypeDefOf.Male);
            hologramBackstory.spawnCategories = new List<string>();
            hologramBackstory.spawnCategories.Add("SoSHologram");
			hologramBackstory.requiredWorkTags = WorkTags.AllWork;
			hologramBackstory.shuffleable = true;
			BackstoryDatabase.AddBackstory(hologramBackstory);
		}

		public override string ModIdentifier
		{
			get { return "ShipInteriorMod2"; }
		}

		public override void Initialize()
		{
			// Must be manually patched as SectionLayer_Terrain is internal
			var regenerateMethod = AccessTools.TypeByName("SectionLayer_Terrain").GetMethod("Regenerate");
			var regeneratePostfix = typeof(SectionRegenerateHelper).GetMethod("Postfix");
			HarmonyInst.Patch(regenerateMethod, postfix: new HarmonyMethod(regeneratePostfix));

			//Similarly, with firefighting
			//TODO - temporarily disabled until we can figure out why we're getting "invalid IL" errors
			/*var FirefightMethod = AccessTools.TypeByName("JobGiver_FightFiresNearPoint").GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
            var FirefightPostfix = typeof(FixFireBugC).GetMethod("Postfix");
            HarmonyInst.Patch(FirefightMethod, postfix: new HarmonyMethod(FirefightPostfix));*/
		}

		public static SettingHandle<double> difficultySoS;
		public static SettingHandle<bool> easyMode;
		public static SettingHandle<int> minTravelTime;
		public static SettingHandle<int> maxTravelTime;
		public static SettingHandle<bool> renderPlanet;
		public static SettingHandle<double> frequencySoS;
		public static SettingHandle<bool> useVacuumPathfinding;
		public static SettingHandle<bool> useSplashScreen;
		public static SettingHandle<int> offsetUIx;
		public static SettingHandle<int> offsetUIy;

		public override void DefsLoaded()
		{
			base.DefsLoaded();
			difficultySoS = Settings.GetHandle("difficultySoS", "Difficulty factor",
				"Affects the size and strength of enemy ships.", 1.0);
			easyMode = Settings.GetHandle("easyMode", "Easy Mode",
				"If checked will prevent player pawns dying to PD and pods landing in your ship",
				false);
			frequencySoS = Settings.GetHandle("frequencySoS", "Ship Combat Frequency",
				"Higher values mean less cooldown time between ship battles.", 1.0);
			minTravelTime = Settings.GetHandle("minTravelTime", "Minimum Travel Time",
				"Minimum number of years that pass when traveling to a new world.", 5);
			maxTravelTime = Settings.GetHandle("maxTravelTime", "Maximum Travel Time",
				"Maximum number of years that pass when traveling to a new world.", 100);
			renderPlanet = Settings.GetHandle("renderPlanet", "Dynamic Planet Rendering",
				"If checked, orbital maps will show a day/night cycle on the planet. Disable this option if the game runs slowly in space.",
				false);
			useVacuumPathfinding = Settings.GetHandle("useVacuumPathfinding", "Use Vacuum Pathfinding?",
				"If checked, pawns without EVA gear will attempt to avoid vacuum areas. This can break compatibility with other mods which alter pathfinding. Restart the game after changing this setting.",
				true);
			useSplashScreen = Settings.GetHandle("useSplashScreen", "SoS Splash Screen",
				"If checked, RimWorld will use SoS2's new splash screen. Restart the game after changing this setting.",
				true);
			offsetUIx = Settings.GetHandle("offsetUIx", "Ship UI offset x",
				"UI offset horizontal from the center of your screen.", 0);
			offsetUIy = Settings.GetHandle("offsetUIy", "Ship UI offset y",
				"UI offset vertical from bellow the pawn bar.", 0);

			if (useSplashScreen)
				((UI_BackgroundMain)UIMenuBackgroundManager.background).overrideBGImage = Splash;

			foreach(ThingDef drug in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				if (drug.category == ThingCategory.Item && drug.IsDrug && drug.IsPleasureDrug)
				{
					CompBuildingConsciousness.drugs.Add(drug);
				}
			}
			CompBuildingConsciousness.drugs.Add(ThingDef.Named("Meat_Human"));

			foreach(ThingDef apparel in DefDatabase<ThingDef>.AllDefsListForReading)
            {
				if(apparel.IsApparel && apparel.thingClass!=typeof(ApparelHolographic))
                {
					if (apparel.apparel.layers.Contains(ApparelLayerDefOf.Overhead) || apparel.apparel.layers.Contains(ApparelLayerDefOf.Shell) || (apparel.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) && (apparel.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) || apparel.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs))))
						CompBuildingConsciousness.apparel.Add(apparel);
                }
            }

			wreckDictionary.Add(ThingDef.Named("ShipHullTile"), ThingDef.Named("ShipHullTileWrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipHullTileMech"), ThingDef.Named("ShipHullTileWrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipHullTileArchotech"), ThingDef.Named("ShipHullTileWrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_Beam"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_BeamMech"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_BeamArchotech"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_Beam_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_BeamMech_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("Ship_BeamArchotech_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_SolarGenerator"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_PassiveCooler"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_PassiveCoolerAdvanced"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_PassiveVent"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_SolarGeneratorArchotech"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_SolarGeneratorMech"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipInside_PassiveCoolerArchotech"), ThingDef.Named("Ship_Beam_Wrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipAirlockArchotech"), ThingDef.Named("ShipAirlockWrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipAirlockMech"), ThingDef.Named("ShipAirlockWrecked"));
			wreckDictionary.Add(ThingDef.Named("ShipAirlock"), ThingDef.Named("ShipAirlockWrecked"));

			/*foreach (TraitDef AITrait in DefDatabase<TraitDef>.AllDefs.Where(t => t.exclusionTags.Contains("AITrait")))
            {
                typeof(TraitDef).GetField("commonality", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(AITrait, 0);
            }*/
			//foreach (EnemyShipDef def in DefDatabase<EnemyShipDef>.AllDefs)
			//{
			/*def.ConvertToSymbolTable();
			def.ConvertToBigString();
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "RecycledShips");
			DirectoryInfo dir = new DirectoryInfo(path);
			if (!dir.Exists)
				dir.Create();
			string filename = Path.Combine(path, def.defName + ".xml");
			SafeSaver.Save(filename, "Defs", () =>
			{
				Scribe.EnterNode("EnemyShipDef");
				Scribe_Values.Look<string>(ref def.defName, "defName");
				Scribe_Values.Look<string>(ref def.label, "label");
				Scribe_Values.Look<int>(ref def.combatPoints, "combatPoints", 0);
				Scribe_Values.Look<int>(ref def.randomTurretPoints, "randomTurretPoints", 0);
				Scribe_Values.Look<int>(ref def.cargoValue, "cargoValue", 0);
				Scribe_Values.Look<bool>(ref def.neverRandom, "neverRandom");
				Scribe_Values.Look<bool>(ref def.neverAttacks, "neverAttacks");
				Scribe_Values.Look<bool>(ref def.spaceSite, "spaceSite");
				Scribe_Values.Look<bool>(ref def.imperialShip, "imperialShip");
				Scribe_Values.Look<bool>(ref def.pirateShip, "pirateShip");
				Scribe_Values.Look<bool>(ref def.bountyShip, "bountyShip");
				Scribe_Values.Look<bool>(ref def.mechanoidShip, "mechanoidShip");
				Scribe_Values.Look<bool>(ref def.fighterShip, "fighterShip");
				Scribe_Values.Look<bool>(ref def.carrierShip, "carrierShip");
				Scribe_Values.Look<bool>(ref def.tradeShip, "tradeShip");
				Scribe_Values.Look<bool>(ref def.startingShip, "startingShip");
				Scribe_Values.Look<bool>(ref def.startingDungeon, "startingDungeon");
				Scribe.EnterNode("core");
				Scribe_Values.Look<string>(ref def.core.shapeOrDef, "shapeOrDef");
				Scribe_Values.Look<int>(ref def.core.x, "x");
				Scribe_Values.Look<int>(ref def.core.z, "z");
				Scribe_Values.Look<Rot4>(ref def.core.rot, "rot");
				Scribe.ExitNode();
				Scribe.EnterNode("symbolTable");
				foreach(char key in def.symbolTable.Keys)
				{
					Scribe.EnterNode("li");
					char realKey = key;
					Scribe_Values.Look<char>(ref realKey, "key"); ;
					ShipShape realShape = def.symbolTable[key];
					Scribe_Deep.Look<ShipShape>(ref realShape, "value");
					Scribe.ExitNode();
				}
				Scribe.ExitNode();
				Scribe_Values.Look<string>(ref def.bigString, "bigString");
			});*/
			//def.ConvertFromBigString();
			//def.ConvertFromSymbolTable();
			//}
		}

        public override void SceneLoaded(Scene scene)
        {
            base.SceneLoaded(scene);

			if (!loadedGraphics)
			{
				foreach (ThingDef thingToResolve in CompShuttleCosmetics.GraphicsToResolve.Keys)
				{
					Graphic_Single[] graphicsResolved = new Graphic_Single[CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics.Count];
					Graphic_Multi[] graphicsHoverResolved = new Graphic_Multi[CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover.Count];

					for (int i = 0; i < CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics.Count; i++)
					{
						Graphic_Single graphic = new Graphic_Single();
						GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i].texPath, ShaderDatabase.Cutout, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i].drawSize, Color.white, Color.white, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i], 0, null, "");
						graphic.Init(req);
						graphicsResolved[i] = graphic;
					}
					for (int i = 0; i < CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover.Count; i++)
					{
						Graphic_Multi graphic = new Graphic_Multi();
						GraphicRequest req = new GraphicRequest(typeof(Graphic_Multi), CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i].texPath, ShaderDatabase.Cutout, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i].drawSize, Color.white, Color.white, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i], 0, null, "");
						graphic.Init(req);
						graphicsHoverResolved[i] = graphic;
					}

					CompShuttleCosmetics.graphics.Add(thingToResolve.defName, graphicsResolved);
					CompShuttleCosmetics.graphicsHover.Add(thingToResolve.defName, graphicsHoverResolved);
				}
				loadedGraphics = true;
			}
		}
		public static int FindWorldTile()
		{
			for (int i = 0; i < 420; i++)//Find.World.grid.TilesCount
			{
				if (!Find.World.worldObjects.AnyWorldObjectAt(i) && TileFinder.IsValidTileForNewSettlement(i))
				{
					//Log.Message("Generating orbiting ship at tile " + i);
					return i;
				}
			}
			return -1;
		}
		public static void GeneratePlayerShipMap(IntVec3 size, Map origin)
		{
			WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
			orbiter.radius = 150;
			orbiter.theta = -3;
			orbiter.SetFaction(Faction.OfPlayer);
			orbiter.Tile = FindWorldTile();
			Find.WorldObjects.Add(orbiter);
			var mapComp = origin.GetComponent<ShipHeatMapComp>();
			mapComp.ShipCombatOriginMap = MapGenerator.GenerateMap(size, orbiter, orbiter.MapGeneratorDef);
			mapComp.ShipCombatOriginMap.fogGrid.ClearAllFog();
		}
		public static void GenerateImpactSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipEngineImpactSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}
		public static WorldObject GenerateArchotechPillarBSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("MoonPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
			return impactSite;
		}
		public static void GenerateArchotechPillarCSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TribalPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}
		public static void GenerateArchotechPillarDSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("InsectPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}

		public static void GenerateShip(EnemyShipDef shipDef, Map map, PassingShip passingShip, Faction fac, Lord lord, out Building core, bool shieldActive = true, bool clearArea = false, bool wreckEverything = false)
		{
			List<ShipShape> partsToGenerate = new List<ShipShape>();
			List<IntVec3> cargoCells = new List<IntVec3>();
			List<IntVec3> cellsToFog = new List<IntVec3>();
			IntVec3 c = map.Center;
			if (shipDef.saveSysVer == 2)
				c = new IntVec3(shipDef.offsetX, 0, shipDef.offsetZ);

			if (clearArea)
			{
				IntVec3 min = new IntVec3(map.Size.x, 0, map.Size.z);
				IntVec3 max = new IntVec3(0, 0, 0);
				foreach (ShipShape shape in shipDef.parts)
				{
					if (shape.x < min.x)
						min.x = shape.x;
					if (shape.x > max.x)
						max.x = shape.x;
					if (shape.z < min.z)
						min.z = shape.z;
					if (shape.z > max.z)
						max.z = shape.z;
				}
				CellRect rect = new CellRect(c.x + min.x, c.z + min.z, c.x + max.x - min.x, c.z + max.z - min.z);
				List<Thing> DestroyTheseThings = new List<Thing>();
				foreach (IntVec3 pos in rect.Cells)
				{
					foreach (Thing t in map.thingGrid.ThingsAt(pos))
					{
						if (t.def.mineable || t.def.fillPercent > 0.5f)
							DestroyTheseThings.Add(t);
					}
				}
				foreach (Thing t in DestroyTheseThings)
				{
					t.Destroy();
				}
			}

			core = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
			core.SetFaction(fac);
			Rot4 corerot = shipDef.core.rot;
			GenSpawn.Spawn(core, new IntVec3(c.x + shipDef.core.x, 0, c.z + shipDef.core.z), map, corerot);

			//check if custom replacers for pawns are set and present in the game
			bool crewOver = false;
			bool marineOver = false;
			bool heavyOver = false;
			if (shipDef.crewDef != null && DefDatabase<PawnKindDef>.GetNamed(shipDef.crewDef) != null)
				crewOver = true;
			if (shipDef.marineDef != null && DefDatabase<PawnKindDef>.GetNamed(shipDef.marineDef) != null)
				marineOver = true;
			if (shipDef.marineHeavyDef != null && DefDatabase<PawnKindDef>.GetNamed(shipDef.marineHeavyDef) != null)
				heavyOver = true;
			foreach (ShipShape shape in shipDef.parts)
			{
				try
				{
					if (shape.shapeOrDef.Equals("Circle"))
					{
						List<IntVec3> border = new List<IntVec3>();
						List<IntVec3> interior = new List<IntVec3>();
						CircleUtility(c.x + shape.x, c.z + shape.z, shape.width, ref border, ref interior);
						GenerateHull(border, interior, fac, map);
						cellsToFog.AddRange(interior);
					}
					else if (shape.shapeOrDef.Equals("Rect"))
					{
						List<IntVec3> border = new List<IntVec3>();
						List<IntVec3> interior = new List<IntVec3>();
						RectangleUtility(c.x + shape.x, c.z + shape.z, shape.width, shape.height, ref border, ref interior);
						GenerateHull(border, interior, fac, map);
						cellsToFog.AddRange(interior);
					}
					else if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
					{
						PawnGenerationRequest req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(shape.stuff), fac);
						Pawn pawn = PawnGenerator.GeneratePawn(req);
						if (lord != null)
							lord.AddPawn(pawn);
						GenSpawn.Spawn(pawn, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map);
					}
					else if (DefDatabase<EnemyShipPartDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						partsToGenerate.Add(shape);
					}
					else if (DefDatabase<PawnKindDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						string pawnGen = shape.shapeOrDef;
						if (crewOver && shape.shapeOrDef.Equals("SpaceCrew"))
							pawnGen = shipDef.crewDef;
						else if (marineOver && shape.shapeOrDef.Equals("SpaceCrewMarine"))
							pawnGen = shipDef.marineDef;
						else if (heavyOver && shape.shapeOrDef.Equals("SpaceCrewMarineHeavy"))
							pawnGen = shipDef.marineHeavyDef;
						PawnGenerationRequest req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(pawnGen), fac);
						Pawn pawn = PawnGenerator.GeneratePawn(req);
						if (lord != null)
							lord.AddPawn(pawn);
						GenSpawn.Spawn(pawn, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map);
					}
					else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						Thing thing;
						ThingDef def = ThingDef.Named(shape.shapeOrDef);
						if (def.defName != "Ship_Engine_Interplanetary" || def.defName != "Ship_Engine_Interplanetary_Large" || WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("JTDriveToo"))
						{
							if (wreckEverything && wreckDictionary.ContainsKey(def))
								thing = ThingMaker.MakeThing(wreckDictionary[def]);
							else if (def.MadeFromStuff)
							{
								if (shape.stuff != null)
									thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
								else
									thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
							}
							else
								thing = ThingMaker.MakeThing(def);
							if (thing.TryGetComp<CompColorable>() != null && shape.color != Color.clear)
								thing.SetColor(shape.color);
							if (thing.def.CanHaveFaction && thing.def != wreckedHullPlateDef && thing.def.thingClass != typeof(Building_ArchotechPillar))
								thing.SetFaction(fac);
							if (thing.TryGetComp<CompPowerBattery>() != null)
								thing.TryGetComp<CompPowerBattery>().AddEnergy(thing.TryGetComp<CompPowerBattery>().AmountCanAccept);
							if (thing.TryGetComp<CompRefuelable>() != null)
								thing.TryGetComp<CompRefuelable>().Refuel(thing.TryGetComp<CompRefuelable>().Props.fuelCapacity * Rand.Gaussian(0.5f, 0.125f));
							if (thing.TryGetComp<CompShipCombatShield>() != null)
							{
								thing.TryGetComp<CompShipCombatShield>().radiusSet = 40;
								thing.TryGetComp<CompShipCombatShield>().radius = 40;
								if (shape.radius != 0)
								{
									thing.TryGetComp<CompShipCombatShield>().radiusSet = shape.radius;
									thing.TryGetComp<CompShipCombatShield>().radius = shape.radius;
								}
							}
							if (thing.def.stackLimit > 1)
							{
								thing.stackCount = (int)Math.Min(Rand.RangeInclusive(5, 30), thing.def.stackLimit);
								if (thing.stackCount * thing.MarketValue > 500)
									thing.stackCount = (int)Mathf.Max(500 / thing.MarketValue, 1);
							}
							GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map, shape.rot);
							if (shape.shapeOrDef.Equals("ShipHullTile") || shape.shapeOrDef.Equals("ShipHullTileMech") || shape.shapeOrDef.Equals("ShipHullTileArchotech"))
								cellsToFog.Add(thing.Position);
						}
					}
					else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						TerrainDef terrain = DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef);
						IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
						if (shipDef.saveSysVer == 2)
							pos = new IntVec3(c.x + shape.x, 0, c.z + shape.z);
						if (pos.InBounds(map))
							map.terrainGrid.SetTerrain(pos, terrain);
						if (terrain.fertility > 0 && pos.GetEdifice(map) == null)
						{
							Plant plant = ThingMaker.MakeThing(randomPlants.RandomElement()) as Plant;
							if (plant != null)
							{
								plant.Growth = 1;
								plant.Position = pos;
								plant.SpawnSetup(map, false);
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.Warning("Ship part was not generated properly: "+ shape.shapeOrDef + " at " +  c.x + shape.x + ", " + c.z + shape.z + " Shipdef pos: |"+ shape.x + "," + shape.z + ",0,*|\n" + e);
				}
			}
			int randomTurretPoints = shipDef.randomTurretPoints;
			partsToGenerate.Shuffle();
			foreach (ShipShape shape in partsToGenerate)
			{
				EnemyShipPartDef def = DefDatabase<EnemyShipPartDef>.GetNamed(shape.shapeOrDef);
				if (randomTurretPoints >= def.randomTurretPoints)
					randomTurretPoints -= def.randomTurretPoints;
				else
					def = DefDatabase<EnemyShipPartDef>.GetNamed("Cargo");

				if (def.defName.Equals("CasketFilled"))
				{
					Thing thing = ThingMaker.MakeThing(ThingDefOf.CryptosleepCasket);
					thing.SetFaction(fac);
					Pawn sleeper = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Slave, Faction.OfAncients, forceGenerateNewPawn: true, certainlyBeenInCryptosleep: true));
					((Building_CryptosleepCasket)thing).TryAcceptThing(sleeper);
					GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map, shape.rot);
				}
				else if (def.defName.Length > 8 && def.defName.Substring(def.defName.Length - 8) == "_SPAWNER")
				{
					Thing thing;
					PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(def.defName.Substring(0, def.defName.Length - 8));
					if (kind != null)
						thing = PawnGenerator.GeneratePawn(kind);
					else
						thing = ThingMaker.MakeThing(ThingDef.Named(def.defName.Substring(0, def.defName.Length - 8)));
					if (thing is Pawn p)
                    {
						if (p.RaceProps.IsMechanoid)
							p.SetFactionDirect(Faction.OfMechanoids);
						else if (p.RaceProps.BloodDef.defName.Equals("Filth_BloodInsect"))
							p.SetFactionDirect(Faction.OfInsects);
						p.ageTracker.AgeBiologicalTicks = 36000000;
						p.ageTracker.AgeChronologicalTicks = 36000000;
						if (lord != null)
							lord.AddPawn(p);
					}
					else if (thing is Hive)
						thing.SetFactionDirect(Faction.OfInsects);
					else
						thing.SetFaction(fac);
					GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map);
				}
				else if (!def.defName.Equals("Cargo"))
				{
					ThingDef thingy = def.things.RandomElement();
					Thing thing;
					PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(thingy.defName);
					if (kind != null)
						thing = PawnGenerator.GeneratePawn(kind);
					else
						thing = ThingMaker.MakeThing(thingy);
					if (thing.def.CanHaveFaction)
					{
						if (thing is Pawn p)
						{
							if (p.RaceProps.IsMechanoid)
								p.SetFactionDirect(Faction.OfMechanoids);
							else if (p.RaceProps.BloodDef.defName.Equals("Filth_BloodInsect"))
								p.SetFactionDirect(Faction.OfInsects);
							p.ageTracker.AgeBiologicalTicks = 36000000;
							p.ageTracker.AgeChronologicalTicks = 36000000;
							if (lord != null)
								lord.AddPawn(p);
						}
						else if (thing is Hive)
							thing.SetFactionDirect(Faction.OfInsects);
						else
							thing.SetFaction(fac);
					}
					if (thing.TryGetComp<CompColorable>() != null)
						thing.TryGetComp<CompColorable>().SetColor(shape.color);
					GenSpawn.Spawn(thing, new IntVec3(c.x + shape.x, 0, c.z + shape.z), map);
				}
				else
				{
					for (int ecks = c.x + shape.x; ecks <= c.x + shape.x + shape.width; ecks++)
					{
						for (int zee = c.z + shape.z; zee <= c.z + shape.z + shape.height; zee++)
						{
							cargoCells.Add(new IntVec3(ecks, 0, zee));
						}
					}
				}
			}
			if (cargoCells.Any())
			{
				List<Thing> loot;
				if (passingShip is TradeShip)
				{
					loot = ((TradeShip)passingShip).GetDirectlyHeldThings().ToList();
				}
				else
				{
					ThingSetMakerParams parms = default(ThingSetMakerParams);
					parms.makingFaction = fac;
					parms.totalMarketValueRange = new FloatRange(shipDef.cargoValue * 0.75f, shipDef.cargoValue * 1.25f);
					parms.traderDef = DefDatabase<TraderKindDef>.AllDefs.Where(t => t.orbital == true).RandomElement();
					loot = ThingSetMakerDefOf.TraderStock.root.Generate(parms).InRandomOrder().ToList();
					//So, uh, this didn't work the way I thought it did. Hence ships had way, waaaaaaay too much loot. Fixing that now.
					float actualTotalValue = parms.totalMarketValueRange.Value.RandomInRange * 10;
					if (actualTotalValue == 0)
						actualTotalValue = 500f;
					if (actualTotalValue > 5000f)
						actualTotalValue = 5000f;
					List<Thing> actualLoot = new List<Thing>();
					while (loot.Count > 0 && actualTotalValue > 0)
					{
						Thing random = loot.RandomElement();
						actualTotalValue -= random.MarketValue;
						actualLoot.Add(random);
						loot.Remove(random);
					}
					loot = actualLoot;
				}
				foreach (Thing t in loot)
				{
					IntVec3 cell = cargoCells.RandomElement();
					GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
					cargoCells.Remove(cell);
					if (t is Pawn p && !p.RaceProps.Animal)
						t.SetFactionDirect(fac);
				}
			}

			//fog
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			foreach (Room r in map.regionGrid.allRooms)
				r.Temperature = 21;
			if (shipDef.defName != "MechanoidMoonBase" && !clearArea)
			{
				foreach (IntVec3 cell in cellsToFog)
				{
					if (cell.GetRoom(map) == null || (cell.GetRoom(map).OpenRoofCount == 0 && !cell.GetRoom(map).IsDoorway))
						map.fogGrid.fogGrid[map.cellIndices.CellToIndex(cell)] = true;

				}
			}
			map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Things | MapMeshFlag.FogOfWar);
			foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				Building b = t as Building;
				if (b == null)
					continue;
				if (b.TryGetComp<CompPowerTrader>() != null)
				{
					CompPowerTrader trader = b.TryGetComp<CompPowerTrader>();
					trader.PowerOn = true;
				}
				if (b is Building_ShipTurret)
					((Building_ShipTurret)b).burstCooldownTicksLeft = 300;
				if (b is Building_ShipTurretTorpedo)
				{
					CompChangeableProjectilePlural torps = ((Building_ShipTurretTorpedo)b).gun.TryGetComp<CompChangeableProjectilePlural>();
					for (int i = 0; i < torps.Props.maxTorpedoes; i++)
						torps.LoadShell(ThingDef.Named("ShipTorpedo_HighExplosive"), 1);
					IntVec3 vec;
					GenAdj.TryFindRandomAdjacentCell8WayWithRoom(b, out vec);
					for (int i = 0; i < Rand.RangeInclusive(1, 4); i++)
					{
						GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDef.Named("ShipTorpedo_HighExplosive")), vec, map, ThingPlaceMode.Near);
					}
				}
				else if (b is Building_ShipBridge)
					((Building_ShipBridge)b).ShipName = shipDef.label;
				else if (!shieldActive && b.TryGetComp<CompShipCombatShield>() != null)
					b.TryGetComp<CompFlickable>().SwitchIsOn = false;
			}
			if (Current.ProgramState == ProgramState.Playing)
				map.mapDrawer.RegenerateEverythingNow();
		}
		private static void GenerateHull(List<IntVec3> border, List<IntVec3> interior, Faction fac, Map map)
		{
			foreach (IntVec3 vec in border)
			{
				if (!GenSpawn.WouldWipeAnythingWith(vec, Rot4.South, beamDef, map, (Thing x) => x.def.category == ThingCategory.Building) && !vec.GetThingList(map).Where(t => t.def == hullPlateDef || t.def == mechHullPlateDef || t.def == archoHullPlateDef).Any())
				{
					Thing wall = ThingMaker.MakeThing(beamDef);
					wall.SetFaction(fac);
					GenSpawn.Spawn(wall, vec, map);
				}
			}
			foreach (IntVec3 vec in interior)
			{
				Thing floor = ThingMaker.MakeThing(hullPlateDef);
				if (fac == Faction.OfPlayer)
					floor.SetFaction(fac);
				GenSpawn.Spawn(floor, vec, map);
			}
		}
		public static void RectangleUtility(int xCorner, int zCorner, int x, int z, ref List<IntVec3> border,
			ref List<IntVec3> interior)
		{
			for (int ecks = xCorner; ecks < xCorner + x; ecks++)
			{
				for (int zee = zCorner; zee < zCorner + z; zee++)
				{
					if (ecks == xCorner || ecks == xCorner + x - 1 || zee == zCorner || zee == zCorner + z - 1)
						border.Add(new IntVec3(ecks, 0, zee));
					else
						interior.Add(new IntVec3(ecks, 0, zee));
				}
			}
		}
		public static void CircleUtility(int xCenter, int zCenter, int radius, ref List<IntVec3> border,
			ref List<IntVec3> interior)
		{
			border = CircleBorder(xCenter, zCenter, radius);
			int reducedRadius = radius - 1;
			while (reducedRadius > 0)
			{
				List<IntVec3> newCircle = CircleBorder(xCenter, zCenter, reducedRadius);
				foreach (IntVec3 vec in newCircle)
					interior.Add(vec);
				reducedRadius--;
			}
			interior.Add(new IntVec3(xCenter, 0, zCenter));
		}
		public static List<IntVec3> CircleBorder(int xCenter, int zCenter, int radius)
		{
			HashSet<IntVec3> border = new HashSet<IntVec3>();
			bool foundDiagonal = false;
			int radiusSquared = radius * radius;
			IntVec3 pos = new IntVec3(radius, 0, 0);
			AddOctants(pos, ref border);
			while (!foundDiagonal)
			{
				int left = ((pos.x - 1) * (pos.x - 1)) + (pos.z * pos.z);
				int up = ((pos.z + 1) * (pos.z + 1)) + (pos.x * pos.x);
				if (Math.Abs(radiusSquared - up) > Math.Abs(radiusSquared - left))
					pos = new IntVec3(pos.x - 1, 0, pos.z);
				else
					pos = new IntVec3(pos.x, 0, pos.z + 1);
				AddOctants(pos, ref border);
				if (pos.x == pos.z)
					foundDiagonal = true;
			}

			List<IntVec3> output = new List<IntVec3>();
			foreach (IntVec3 vec in border)
			{
				output.Add(new IntVec3(vec.x + xCenter, 0, vec.z + zCenter));
			}

			return output;
		}
		private static void AddOctants(IntVec3 pos, ref HashSet<IntVec3> border)
		{
			border.Add(pos);
			border.Add(new IntVec3(pos.x * -1, 0, pos.z));
			border.Add(new IntVec3(pos.x, 0, pos.z * -1));
			border.Add(new IntVec3(pos.x * -1, 0, pos.z * -1));
			border.Add(new IntVec3(pos.z, 0, pos.x));
			border.Add(new IntVec3(pos.z * -1, 0, pos.x));
			border.Add(new IntVec3(pos.z, 0, pos.x * -1));
			border.Add(new IntVec3(pos.z * -1, 0, pos.x * -1));
		}

		public static void MoveShip(Building core, Map targetMap, IntVec3 adjustment, Faction fac = null, byte rotNum = 0)
		{
			List<Thing> toSave = new List<Thing>();
			List<Building> shipParts = ShipUtility.ShipBuildingsAttachedTo(core);
			List<Zone> zonesToCopy = new List<Zone>();
			bool movedZones = false;
			List<Tuple<IntVec3, TerrainDef>> terrainToCopy = new List<Tuple<IntVec3, TerrainDef>>();
			List<IntVec3> targetArea = new List<IntVec3>();
			List<IntVec3> sourceArea = new List<IntVec3>();
			List<IntVec3> fireExplosions = new List<IntVec3>();
			IntVec3 rot = new IntVec3(0, 0, 0);
			int rotb = 4 - rotNum;

			shipOriginMap = null;
			Map sourceMap = core.Map;
			if (targetMap == null)
				targetMap = core.Map;

			//clear LZ
			List<Thing> thingsToDestroy = new List<Thing>();
			foreach (Thing saveThing in shipParts)
			{
				if (saveThing is Building)
				{
					//moving to a diff map, remove things from caches
					if (sourceMap != targetMap)
                    {
						if (saveThing is Building_ShipBridge b)
						{
							var mapComp = sourceMap.GetComponent<ShipHeatMapComp>();
							if (mapComp.MapRootListAll.Contains(saveThing))
							{
								//Log.Message("SM-Removed: " + saveThing + " from " + sourceMap);
								b.mapComp = null;
								mapComp.MapRootListAll.Remove(saveThing as Building);
							}
						}
						else if (saveThing is Building_ShipTurret t)
							t.mapComp = null;
					}

					//area from things, things from pos
					foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(saveThing))
					{
						if (!targetArea.Contains(pos + adjustment))
						{
							sourceArea.Add(pos);
							targetArea.Add(pos + adjustment);
							foreach (Thing t in (pos + adjustment).GetThingList(targetMap))
							{
								if (!thingsToDestroy.Contains(t))
									thingsToDestroy.Add(t);
							}
							if (!targetMap.IsSpace())
								targetMap.snowGrid.SetDepth(pos + adjustment, 0f);
						}
					}
				}
				toSave.Add(saveThing);
			}
			//move live pawns out of target area, destroy non buildings
			foreach (Thing thing in thingsToDestroy)
			{
				if (thing is Pawn && (!((Pawn)thing).Dead || !((Pawn)thing).Downed))
				{
					((Pawn)thing).pather.StopDead();
					while (targetArea.Contains(thing.Position))
						thing.Position = (CellFinder.RandomClosewalkCellNear(thing.Position, targetMap, 50));
				}
				else if (!thing.Destroyed)
					thing.Destroy();
			}
			//all - save things
			foreach (Building hullTile in shipParts)
			{
				List<Thing> allTheThings = hullTile.Position.GetThingList(hullTile.Map);
				foreach (Thing theItem in allTheThings)
				{
					if (theItem.Map.zoneManager.ZoneAt(theItem.Position) != null && !zonesToCopy.Contains(theItem.Map.zoneManager.ZoneAt(theItem.Position)))
					{
						zonesToCopy.Add(theItem.Map.zoneManager.ZoneAt(theItem.Position));
					}
					if (!toSave.Contains(theItem) && !(theItem is Building_SteamGeyser))
					{
						toSave.Add(theItem);
					}
					UnRoofTilesOverThing(theItem);
				}
				if (hullTile.Map.terrainGrid.TerrainAt(hullTile.Position).layerable && !(hullTile.Map.terrainGrid.TerrainAt(hullTile.Position) == hullFloorDef) && !(hullTile.Map.terrainGrid.TerrainAt(hullTile.Position) == mechHullFloorDef) && !(hullTile.Map.terrainGrid.TerrainAt(hullTile.Position) == archoHullFloorDef))
				{
					terrainToCopy.Add(new Tuple<IntVec3, TerrainDef>(hullTile.Position, hullTile.Map.terrainGrid.TerrainAt(hullTile.Position)));
					hullTile.Map.terrainGrid.RemoveTopLayer(hullTile.Position, false);
				}
			}
			AirlockBugFlag = true;
			//all - move things
			foreach (Thing spawnThing in toSave)
			{
				if (!spawnThing.Destroyed)
				{
					try
					{
						//pre move
						if (spawnThing.TryGetComp<CompEngineTrail>() != null && !spawnThing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ArchotechExoticParticles")))
						{
							spawnThing.TryGetComp<CompEngineTrail>().active = false;
							if (targetMap.IsSpace() && !sourceMap.IsSpace())
							{
								if (spawnThing.Rotation.AsByte == 0)
									fireExplosions.Add(spawnThing.Position + new IntVec3(0, 0, -3));
								else if (spawnThing.Rotation.AsByte == 1)
									fireExplosions.Add(spawnThing.Position + new IntVec3(-3, 0, 0));
								else if (spawnThing.Rotation.AsByte == 2)
									fireExplosions.Add(spawnThing.Position + new IntVec3(0, 0, 3));
								else
									fireExplosions.Add(spawnThing.Position + new IntVec3(3, 0, 0));
							}
						}
						else if (spawnThing.TryGetComp<CompEngineTrailEnergy>() != null)
						{
							spawnThing.TryGetComp<CompEngineTrailEnergy>().active = false;
						}

						//move
						if (spawnThing.Spawned)
							spawnThing.DeSpawn();

						int adjz = 0;
						int adjx = 0;
						if (rotb == 3)
						{
							//CCW rot, breaks non rot, uneven things
							if (spawnThing.def.rotatable)
							{
								spawnThing.Rotation = new Rot4(spawnThing.Rotation.AsByte + rotb);
							}
							else if (spawnThing.def.rotatable == false && spawnThing.def.size.x % 2 == 0)
								adjx -= 1;
							rot.x = targetMap.Size.x - spawnThing.Position.z + adjx;
							rot.z = spawnThing.Position.x;
							spawnThing.Position = rot + adjustment;
						}
						else if (rotb == 2)
						{
							//flip using 2x CCW rot
							if (spawnThing.def.rotatable)
							{
								spawnThing.Rotation = new Rot4(spawnThing.Rotation.AsByte + rotb);
							}
							else if (spawnThing.def.rotatable == false && spawnThing.def.size.x % 2 == 0)
								adjx -= 1;
							if (spawnThing.def.rotatable == false && spawnThing.def.size.x != spawnThing.def.size.z)
							{
								if (spawnThing.def.size.z % 2 == 0)//5x2
									adjz -= 1;
								else//6x3,6x7
									adjz += 1;
							}
							rot.x = targetMap.Size.x - spawnThing.Position.z + adjx;
							rot.z = spawnThing.Position.x;
							IntVec3 tempPos = rot;
							rot.x = targetMap.Size.x - tempPos.z + adjx;
							rot.z = tempPos.x + adjz;
							spawnThing.Position = rot + adjustment;
						}
						else
							spawnThing.Position += adjustment;
						try
						{
							if (!spawnThing.Destroyed)
							{
								spawnThing.SpawnSetup(targetMap, false);
							}
						}
						catch (Exception e)
						{
							Log.Warning(e.Message + "\n" + e.StackTrace);
						}

						//post move
						if (fac != null && spawnThing is Building && spawnThing.def.CanHaveFaction)
							spawnThing.SetFaction(fac);
						if (spawnThing.TryGetComp<CompPower>() != null)
						{
							spawnThing.TryGetComp<CompPower>().ResetPowerVars();
							//targetMap.mapDrawer.MapMeshDirty(spawnThing.Position, MapMeshFlag.PowerGrid, false, false);
							//spawnThing.TryGetComp<CompPower>().SetUpPowerVars();
						}
						else if (spawnThing is Pawn)
						{
							Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.Remove(((Pawn)spawnThing).thingIDNumber);
						}
					}
					catch (Exception e)
					{
						//Log.Warning(e.Message+"\n"+e.StackTrace);
						Log.Error(e.Message);
					}
				}
			}
			AirlockBugFlag = false;
			if (zonesToCopy.Count != 0)
				movedZones = true;
			//all - move zones
			foreach (Zone theZone in zonesToCopy)
			{
				sourceMap.zoneManager.DeregisterZone(theZone);
				theZone.zoneManager = targetMap.zoneManager;
				List<IntVec3> newCells = new List<IntVec3>();
				foreach (IntVec3 cell in theZone.cells)
				{
					if (rotb == 2)
					{
						rot.x = targetMap.Size.x - cell.x;
						rot.z = targetMap.Size.z - cell.z;
						newCells.Add(rot + adjustment);
					}
					else if (rotb == 3)
					{
						rot.x = targetMap.Size.x - cell.z;
						rot.z = cell.x;
						newCells.Add(rot + adjustment);
					}
					else
						newCells.Add(cell + adjustment);
				}
				theZone.cells = newCells;
				targetMap.zoneManager.RegisterZone(theZone);
			}
			//all - move terrain
			foreach (Tuple<IntVec3, TerrainDef> tup in terrainToCopy)
			{
				if (!targetMap.terrainGrid.TerrainAt(tup.Item1).layerable || targetMap.terrainGrid.TerrainAt(tup.Item1) == hullFloorDef || targetMap.terrainGrid.TerrainAt(tup.Item1) == mechHullFloorDef || targetMap.terrainGrid.TerrainAt(tup.Item1) == archoHullFloorDef)
					if (rotb == 2)
					{
						rot.x = targetMap.Size.x - tup.Item1.x;
						rot.z = targetMap.Size.z - tup.Item1.z;
						targetMap.terrainGrid.SetTerrain(rot + adjustment, tup.Item2);
					}
					else if (rotb == 3)
					{
						rot.x = targetMap.Size.x - tup.Item1.z;
						rot.z = tup.Item1.x;
						targetMap.terrainGrid.SetTerrain(rot + adjustment, tup.Item2);
					}
					else
						targetMap.terrainGrid.SetTerrain(tup.Item1 + adjustment, tup.Item2);
			}
			typeof(ZoneManager).GetMethod("RebuildZoneGrid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(targetMap.zoneManager, new object[0]);
			typeof(ZoneManager).GetMethod("RebuildZoneGrid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sourceMap.zoneManager, new object[0]);
			//normalize temp in ship
			foreach (Room room in targetMap.regionGrid.allRooms)
			{
				if (!ExposedToOutside(room) && room.Temperature < -99f)
					room.Temperature = 21f;
			}
			//takeoff - explosions
			foreach (IntVec3 pos in fireExplosions)
			{
				GenExplosion.DoExplosion(pos, sourceMap, 3.9f, DamageDefOf.Flame, null, -1, -1f, null, null,
					null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
			}
			//takeoff - draw fuel
			if (targetMap.IsSpace() && !sourceMap.IsSpace())
			{
				float fuelNeeded = 0f;
				float fuelStored = 0f;
				//int nukeEngines = 0;
				List<Building> engines = new List<Building>();

				foreach (Thing saveThing in shipParts)
				{
					toSave.Add(saveThing);
					if (saveThing.TryGetComp<CompEngineTrail>() != null && !saveThing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ArchotechExoticParticles")))
					{
						engines.Add((Building)saveThing);
						if (saveThing.TryGetComp<CompRefuelable>() != null)
							fuelStored += saveThing.TryGetComp<CompRefuelable>().Fuel;
						//nuclear counts x2
						if (saveThing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
						{
							fuelStored += saveThing.TryGetComp<CompRefuelable>().Fuel;
							//nukeEngines++;
						}
					}
					if (saveThing is Building && saveThing.def != hullPlateDef && saveThing.def != mechHullPlateDef && saveThing.def != archoHullPlateDef)
						fuelNeeded += (saveThing.def.size.x * saveThing.def.size.z) * 3f;
					else if (saveThing.def == hullPlateDef || saveThing.def == mechHullPlateDef || saveThing.def == archoHullPlateDef)
						fuelNeeded += 1f;
				}
				foreach (Building engine in engines)
				{
					engine.GetComp<CompRefuelable>().ConsumeFuel(fuelNeeded * engine.TryGetComp<CompRefuelable>().Fuel / fuelStored);
				}
				/*//takeoff - fallout
				if (nukeEngines>0 && Rand.Chance(0.05f * nukeEngines))
				{
					IncidentParms parms = new IncidentParms();
					parms.target = sourceMap;
					parms.forced = true;
					QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDef.Named("ToxicFallout"), null, parms), Find.TickManager.TicksGame);
					Find.Storyteller.incidentQueue.Add(qi);
				}*/
			}
			//landing - remove space map
			if (sourceMap != targetMap && !targetMap.IsSpace())
			{
				if (!sourceMap.spawnedThings.Any((Thing x) => x is Pawn && !x.Destroyed))
				{
					WorldObject oldParent = sourceMap.Parent;
					Current.Game.DeinitAndRemoveMap(sourceMap);
					Find.World.worldObjects.Remove(oldParent);
				}
			}
			//regen affected map layers
			List<Section> sourceSec = new List<Section>();
			foreach (IntVec3 pos in sourceArea)
			{
				if (movedZones)
				{
					var sec = sourceMap.mapDrawer.SectionAt(pos);
					if (!sourceSec.Contains(sec))
						sourceSec.Add(sec);
				}
			}
			foreach (Section sec in sourceSec)
			{
				if (movedZones)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
			}
			List<Section> targetSec = new List<Section>();
			foreach (IntVec3 pos in targetArea)
			{
				if (movedZones)
				{
					var sec = targetMap.mapDrawer.SectionAt(pos);
					if (!targetSec.Contains(sec))
						targetSec.Add(sec);
				}
			}
			foreach (Section sec in targetSec)
			{
				if (movedZones)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
			}
			Log.Message("Moved ship with building " + core);
			/*Things = 1,
			FogOfWar = 2,
			Buildings = 4,
			GroundGlow = 8,
			Terrain = 16,
			Roofs = 32,
			Snow = 64,
			Zone = 128,
			PowerGrid = 256,
			BuildingsDamage = 512*/
			//targetMap.mapDrawer.RegenerateEverythingNow();
			//sourceMap.mapDrawer.RegenerateEverythingNow();
			//foreach (IntVec3 pos in posToClear)
			//sourceMap.mapDrawer.MapMeshDirty(pos, MapMeshFlag.PowerGrid);
			//rewire - call next tick
			/*foreach (Thing powerThing in targetMap.listerThings.AllThings)
			{
				CompPower powerComp = powerThing.TryGetComp<CompPower>();
				if (powerComp != null)
				{
					typeof(CompPower).GetMethod("TryManualReconnect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(powerComp, new object[0]);
					//Traverse.Create<CompPower>().Method("TryManualReconnect", powerComp);
					//Traverse.Create(powerComp).Method("TryManualReconnect");
					//powerComp.ResetPowerVars();
				}
			}*/
			/*if (sourceMap.IsSpace() && !sourceMap.IsSpace() || (!sourceMap.IsSpace() && sourceMap.IsSpace())
			{
				foreach (Building powerThing in shipParts)
				{
					CompPower powerComp = powerThing.TryGetComp<CompPower>();
					if (powerComp is CompPowerTrader && powerComp.Props.transmitsPower == false)
					{
						CompPower compPower = PowerConnectionMaker.BestTransmitterForConnector(powerThing.Position, powerThing.Map);
						powerComp.ConnectToTransmitter(compPower, false);
					}
					powerThing.Map.mapDrawer.MapMeshDirty(powerThing.Position, MapMeshFlag.PowerGrid);
					powerThing.Map.mapDrawer.MapMeshDirty(powerThing.Position, MapMeshFlag.Things);
				}
			}*/
		}
		public static void UnRoofTilesOverThing(Thing t)
		{
			foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(t))
				t.Map.roofGrid.SetRoof(pos, null);
		}

		public static bool IsHologram(Pawn pawn)
		{
			return pawn.health.hediffSet.GetHediffs<HediffPawnIsHologram>().Any();
		}
		public static bool ExposedToOutside(Room room)
		{
			return room == null || room.OpenRoofCount > 0 || room.TouchesMapEdge;
		}
		public static byte EVAlevel(Pawn pawn)
		{
			/*
			8 - natural, unremovable, boosted: no rechecks
			7 - boosted EVA: reset on equip change
			6 - natural, unremovable: no rechecks
			5 - proper EVA: reset on equip/hediff change
			4 - active belt: reset on end
			3 - inactive belt: trigger in weather
			2 - skin only: reset on hediff change
			1 - air only: reset on hediff change
			0 - none: dead soon
			*/
			if (Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.TryGetValue(pawn.thingIDNumber, out byte eva))
				return eva;
			byte result = EVAlevelSlow(pawn);
			//Log.Message("EVA slow lvl: " + result + " on pawn " + pawn.Name);
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache[pawn.thingIDNumber] = result;
			return result;
		}
		public static byte EVAlevelSlow(Pawn pawn)
		{
			if (pawn.RaceProps.IsMechanoid || pawn.health.hediffSet.GetHediffs<HediffPawnIsHologram>().Any() || !pawn.RaceProps.IsFlesh)
				return 8;
			if (pawn.def.tradeTags?.Contains("AnimalInsectSpace") ?? false)
				return 6;
			if (pawn.apparel == null)
				return 0;
			bool hasHelmet = false;
			bool hasSuit = false;
			bool hasBelt = false;
			foreach (Apparel app in pawn.apparel.WornApparel)
			{
				if (app.def.apparel.tags.Contains("EVA"))
				{
					if (app.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
						hasHelmet = true;
					if (app.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) || app.def.apparel.layers.Contains(ApparelLayerDefOf.Middle))
						hasSuit = true;
				}
				else if (app.def.defName.Equals("Apparel_SpaceSurvivalBelt"))
				{
					hasBelt = true;
				}
			}
			if (hasHelmet && hasSuit)
				return 7;
			bool hasLung = false;
			bool hasSkin = false;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(ArchoLung) != null)
				hasLung = true;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(ArchoSkin) != null)
				hasSkin = true;
			if (hasLung && hasSkin)
				return 5;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(bubbleHediff) != null)
				return 4;
			if (hasBelt)
				return 3;
			if (hasSkin)
				return 2;
			if (hasLung)
				return 1;
			return 0;
		}
	}
	//harmony patches
	//skyfaller in ShuttleMod

	//GUI
	[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
	public static class ShipCombatOnGUI
	{
		[HarmonyPostfix]
		public static void DrawShipRange(ColonistBar __instance)
		{
			Map mapPlayer = Find.Maps.Where(m => m.GetComponent<ShipHeatMapComp>().InCombat && !m.GetComponent<ShipHeatMapComp>().ShipCombatMaster).FirstOrDefault();
			if (mapPlayer != null)
			{
				var playerShipComp = mapPlayer.GetComponent<ShipHeatMapComp>();
				var enemyShipComp = mapPlayer.GetComponent<ShipHeatMapComp>().MasterMapComp;
				if (playerShipComp.MapRootList.Count == 0 || playerShipComp.MapRootList[0] == null || !playerShipComp.MapRootList[0].Spawned)
					return;
				if (!playerShipComp.InCombat && playerShipComp.IsGraveyard)
				{
					Map m = playerShipComp.ShipGraveyard;
					playerShipComp = m.GetComponent<ShipHeatMapComp>();
				}
				float screenHalf = (float)UI.screenWidth / 2 + ShipInteriorMod2.offsetUIx;

				//player heat & energy bars
				float baseY = __instance.Size.y + 40 + ShipInteriorMod2.offsetUIy;
				for (int i = 0; i < playerShipComp.MapRootList.Count; i++)
				{
					try
					{
						baseY += 45;
						string str = ((Building_ShipBridge)playerShipComp.MapRootList[i]).ShipName;
						int strSize = 0;
						if (playerShipComp.MapRootList.Count > 1)
						{
							strSize = 5 + str.Length * 8;
						}
						Rect rect2 = new Rect(screenHalf - 630 - strSize, baseY - 40, 395 + strSize, 35);
						Verse.Widgets.DrawMenuSection(rect2);
						if (playerShipComp.MapRootList.Count > 1)
							Widgets.Label(rect2.ContractedBy(7), str);

						PowerNet net = playerShipComp.MapRootList[i].TryGetComp<CompPower>().PowerNet;
						float capacity = 0;
						foreach (CompPowerBattery bat in net.batteryComps)
							capacity += bat.Props.storedEnergyMax;
						Rect rect3 = new Rect(screenHalf - 630, baseY - 40, 200, 35);
						Widgets.FillableBar(rect3.ContractedBy(6), net.CurrentStoredEnergy() / capacity,
							ShipInteriorMod2.PowerTex);
						Text.Font = GameFont.Small;
						rect3.y += 7;
						rect3.x = screenHalf - 615;
						rect3.height = Text.LineHeight;
						Widgets.Label(rect3, "Energy: " + Mathf.Round(net.CurrentStoredEnergy()));

						ShipHeatNet net2 = playerShipComp.MapRootList[i].TryGetComp<CompShipHeat>().myNet;
						if (net2 != null)
						{
							Rect rect4 = new Rect(screenHalf - 435, baseY - 40, 200, 35);
							Widgets.FillableBar(rect4.ContractedBy(6), net2.StorageUsed / net2.StorageCapacity,
								ShipInteriorMod2.HeatTex);
							rect4.y += 7;
							rect4.x = screenHalf - 420;
							rect4.height = Text.LineHeight;
							Widgets.Label(rect4, "Heat: " + Mathf.Round(net2.StorageUsed));
						}
					}
					catch (Exception e)
					{
						Log.Warning("Ship UI failed on ship: " + i + "\n" + e);
					}
				}
				//enemy heat & energy bars
				baseY = __instance.Size.y + 40 + ShipInteriorMod2.offsetUIy;
				for (int i = 0; i < enemyShipComp.MapRootList.Count; i++)
				{
                    try
					{
						baseY += 45;
						string str = ((Building_ShipBridge)enemyShipComp.MapRootList[i]).ShipName;
						Rect rect2 = new Rect(screenHalf + 235, baseY - 40, 395, 35);
						Verse.Widgets.DrawMenuSection(rect2);

						ShipHeatNet net2 = enemyShipComp.MapRootList[i].GetComp<CompShipHeat>().myNet;
						if (net2 != null)
						{
							Rect rect4 = new Rect(screenHalf + 235, baseY - 40, 200, 35);
							Widgets.FillableBar(rect4.ContractedBy(6), net2.StorageUsed / net2.StorageCapacity,
								ShipInteriorMod2.HeatTex);
							rect4.y += 7;
							rect4.x = screenHalf + 255;
							rect4.height = Text.LineHeight;
							Widgets.Label(rect4, "Heat: " + Mathf.Round(net2.StorageUsed));
						}

						PowerNet net = enemyShipComp.MapRootList[i].GetComp<CompPower>().PowerNet;
						float capacity = 0;
						foreach (CompPowerBattery bat in net.batteryComps)
							capacity += bat.Props.storedEnergyMax;
						Rect rect3 = new Rect(screenHalf + 430, baseY - 40, 200, 35);
						Widgets.FillableBar(rect3.ContractedBy(6), net.CurrentStoredEnergy() / capacity,
							ShipInteriorMod2.PowerTex);
						Text.Font = GameFont.Small;
						rect3.y += 7;
						rect3.x = screenHalf + 445;
						rect3.height = Text.LineHeight;
						Widgets.Label(rect3, "Energy: " + Mathf.Round(net.CurrentStoredEnergy()));
					}
					catch (Exception e)
					{
						Log.Warning("Ship UI failed on ship: " + i + "\n" + e);
					}
				}

				//range bar
				baseY = __instance.Size.y + 85 + ShipInteriorMod2.offsetUIy;
				Rect rect = new Rect(screenHalf - 225, baseY - 40, 450, 50);
				Verse.Widgets.DrawMenuSection(rect);
				Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 200, baseY - 38, 400, 46),
					new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.ruler.MatSingle.mainTexture);
				switch (playerShipComp.Heading)
				{
					case -1:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 223, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.shipOne.MatSingle.mainTexture);
						break;
					case 1:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 235, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.shipOne.MatSingle.mainTexture);
						break;
					default:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 235, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.shipZero.MatSingle.mainTexture);
						break;
				}
				switch (enemyShipComp.Heading)
				{
					case -1:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 216 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.shipOneEnemy.MatSingle.mainTexture);
						break;
					case 1:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.shipOneEnemy.MatSingle.mainTexture);
						break;
					default:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.shipZeroEnemy.MatSingle.mainTexture);
						break;
				}
				foreach (ShipCombatProjectile proj in playerShipComp.Projectiles)
				{
					if (proj.turret != null)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 210 + proj.range, baseY - 12, 12, 12),
							new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.projectile.MatSingle.mainTexture);
					} 
				}
				foreach (ShipCombatProjectile proj in enemyShipComp.Projectiles)
				{
					if (proj.turret != null)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 210 - proj.range + enemyShipComp.Range, baseY - 24, 12, 12), 
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.projectileEnemy.MatSingle.mainTexture);
					}
				}
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
					int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
					if (obj.destinationTile == playerShipComp.ShipCombatMasterMap.Tile && initialTile == mapPlayer.Tile) 
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + rng * enemyShipComp.Range, baseY - 16, 12, 12),
							new Rect(0, 0, 1, 1), (Texture2D)ShipInteriorMod2.shuttlePlayer.MatSingle.mainTexture);
					}
					else if (obj.destinationTile == mapPlayer.Tile && initialTile == playerShipComp.ShipCombatMasterMap.Tile && obj.Faction != Faction.OfPlayer)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + (1 - rng) * enemyShipComp.Range, baseY - 20, 12, 12),
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.shuttleEnemy.MatSingle.mainTexture);
					}
					else if (obj.destinationTile == mapPlayer.Tile && initialTile == playerShipComp.ShipCombatMasterMap.Tile && obj.Faction == Faction.OfPlayer)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + (1 - rng) * enemyShipComp.Range, baseY - 20, 12, 12),
							new Rect(0, 0, -1, 1), (Texture2D)ShipInteriorMod2.shuttlePlayer.MatSingle.mainTexture);
					}
				}
				if (Mouse.IsOver(rect))
				{
					string iconTooltipText = TranslatorFormattedStringExtensions.Translate("ShipCombatTooltip");
					if (!iconTooltipText.NullOrEmpty())
					{
						TooltipHandler.TipRegion(rect, iconTooltipText);
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawGroupFrame")]
	public static class ShipIconOnPawnBar
	{
		[HarmonyPostfix]
		public static void DrawShip(int group, ColonistBarColonistDrawer __instance)
		{
			List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
			foreach (ColonistBar.Entry entry in entries)
			{
				if (entry.group == group && entry.pawn == null && entry.map.IsSpace())
				{
					Rect rect = (Rect)typeof(ColonistBarColonistDrawer)
					.GetMethod("GroupFrameRect", BindingFlags.NonPublic | BindingFlags.Instance)
					.Invoke(__instance, new object[] { group });
					var mapComp = entry.map.GetComponent<ShipHeatMapComp>();
					if (mapComp.IsGraveyard) //wreck
						Verse.Widgets.DrawTextureFitted(rect, ShipInteriorMod2.shipBarNeutral.MatSingle.mainTexture, 1);
					else if (entry.map.ParentFaction == Faction.OfPlayer)//player
						Verse.Widgets.DrawTextureFitted(rect, ShipInteriorMod2.shipBarPlayer.MatSingle.mainTexture, 1);
					else //enemy
						Verse.Widgets.DrawTextureFitted(rect, ShipInteriorMod2.shipBarEnemy.MatSingle.mainTexture, 1);
				}
			}
		}
	}

	[HarmonyPatch(typeof(LetterStack), "LettersOnGUI")]
	public static class TimerOnGUI
	{
		[HarmonyPrefix]
		public static bool DrawShipTimer(ref float baseY)
		{
			Map map = Find.CurrentMap;
			if (map != null && map.IsSpace())
			{
				var timecomp = map.Parent.GetComponent<TimedForcedExitShip>();
				if (timecomp != null && timecomp.ForceExitAndRemoveMapCountdownActive)
				{
					float num = (float)UI.screenWidth - 200f;
					Rect rect = new Rect(num, baseY - 16f, 193f, 26f);
					Text.Anchor = TextAnchor.MiddleRight;
					string detectionCountdownTimeLeftString = timecomp.ForceExitAndRemoveMapCountdownTimeLeftString;
					string text = "ShipBurnUpCountdown".Translate(detectionCountdownTimeLeftString);
					float x = Text.CalcSize(text).x;
					Rect rect2 = new Rect(rect.xMax - x, rect.y, x, rect.height);
					if (Mouse.IsOver(rect2))
					{
						Widgets.DrawHighlight(rect2);
					}
					TooltipHandler.TipRegionByKey(rect2, "ShipBurnUpCountdownTip", detectionCountdownTimeLeftString);
					Widgets.Label(rect2, text);
					Text.Anchor = TextAnchor.UpperLeft;
					baseY -= 26f;
				}
			}
			return true;
		}
	}

	//biome
	[HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
	public class RenderPlanetBehindMap
	{
		static RenderTexture target = new RenderTexture(textureSize, textureSize, 16);
		static Texture2D virtualPhoto = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
		public static Material PlanetMaterial = MaterialPool.MatFrom(virtualPhoto);

		const int textureSize = 2048;
		const float altitude = 1100f;

		public static bool renderedThatAlready = false;
		static BiomeDef outerSpaceBiome = DefDatabase<BiomeDef>.GetNamed("OuterSpaceBiome");

		[HarmonyPrefix]
		public static void PreDraw()
		{
			Map map = Find.CurrentMap;

			// if we aren't in space, abort!
			if ((renderedThatAlready && !ShipInteriorMod2.renderPlanet) || map.Biome != outerSpaceBiome)
			{
				return;
			}
			//TODO replace this when interplanetary travel is ready
			//Find.PlaySettings.showWorldFeatures = false;
			RenderTexture oldTexture = Find.WorldCamera.targetTexture;
			RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
			Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
			Find.WorldCameraDriver.altitude = altitude;
			Find.WorldCameraDriver.GetType()
				.GetField("desiredAltitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(Find.WorldCameraDriver, altitude);

			float num = (float)UI.screenWidth / (float)UI.screenHeight;

			Find.WorldCameraDriver.Update();
			Find.World.renderer.CheckActivateWorldCamera();
			Find.World.renderer.DrawWorldLayers();
			WorldRendererUtility.UpdateWorldShadersParams();
			//TODO replace this when interplanetary travel is ready
			/*List<WorldLayer> layers = (List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer);
            foreach(WorldLayer layer in layers)
            {
                if (layer is WorldLayer_Stars)
                    layer.Render();
            }
            Find.PlaySettings.showWorldFeatures = false;*/
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = target;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = num;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

			Find.WorldCamera.targetTexture = target;
			Find.WorldCamera.aspect = num;
			Find.WorldCamera.Render();

			RenderTexture.active = target;
			virtualPhoto.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
			virtualPhoto.Apply();
			RenderTexture.active = null;

			Find.WorldCamera.targetTexture = oldTexture;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
			Find.World.renderer.CheckActivateWorldCamera();

			if (!((List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer)).FirstOrFallback().ShouldRegenerate)
				renderedThatAlready = true;
		}
	}

	[HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]
	public static class GenerateSpaceSubMesh
	{
		public static TerrainDef spaceTerrain = TerrainDef.Named("EmptySpace");
		[HarmonyPrefix]
		public static bool GenerateMesh(SectionLayer __instance, Section ___section)
		{
			if (__instance.GetType().Name != "SectionLayer_Terrain")
				return true;

			bool foundSpace = false;
			foreach (IntVec3 cell in ___section.CellRect.Cells)
			{
				TerrainDef terrain1 = ___section.map.terrainGrid.TerrainAt(cell);
				if (terrain1 == spaceTerrain)
				{
					foundSpace = true;
					Printer_Mesh.PrintMesh(__instance, Matrix4x4.TRS(cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, Vector3.one), MeshMakerPlanes.NewPlaneMesh(1f), RenderPlanetBehindMap.PlanetMaterial);
				}
			}
			if (!foundSpace)
			{
				for (int i = 0; i < __instance.subMeshes.Count; i++)
				{
					if (__instance.subMeshes[i].material == RenderPlanetBehindMap.PlanetMaterial)
					{
						__instance.subMeshes.RemoveAt(i);
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("Biome", MethodType.Getter)]
	public static class SpaceBiomeGetter
	{
		[HarmonyPrefix]
		public static bool interceptBiome(Map __instance, out bool __state)
		{
			__state = __instance.info?.parent != null &&
						   (__instance.info.parent is WorldObjectOrbitingShip || __instance.info.parent is SpaceSite || __instance.info.parent is MoonBase || __instance.Parent.AllComps.Any(comp => comp is MoonPillarSiteComp));
			return !__state;
		}

		[HarmonyPostfix]
		public static void getSpaceBiome(Map __instance, ref BiomeDef __result, bool __state)
		{
			if (__state)
				__result = ShipInteriorMod2.OuterSpaceBiome;
		}
	}

	[HarmonyPatch(typeof(MapTemperature))]
	[HarmonyPatch("OutdoorTemp", MethodType.Getter)]
	public static class FixOutdoorTemp
	{
		[HarmonyPostfix]
		public static void GetSpaceTemp(ref float __result, Map ___map)
		{
			if (___map.IsSpace()) __result = -100f;
		}
	}

	[HarmonyPatch(typeof(MapTemperature))]
	[HarmonyPatch("SeasonalTemp", MethodType.Getter)]
	public static class FixSeasonalTemp
	{
		[HarmonyPostfix]
		public static void getSpaceTemp(ref float __result, Map ___map)
		{
			if (___map.IsSpace()) __result = -100f;
		}
	}

	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch("OpenRoofCount", MethodType.Getter)]
	public static class SpaceRoomCheck //check if cache is invalid, if roofed and in space run postfix to check the room
	{
		[HarmonyPrefix]
		public static bool DoOnlyOnCaching(ref int ___cachedOpenRoofCount, out bool __state)
		{
			__state = false;
			if (___cachedOpenRoofCount == -1)
				__state = true;
			return true;
		}
		[HarmonyPostfix]
		public static int NoAirNoRoof(int __result, Room __instance, ref int ___cachedOpenRoofCount, bool __state)
		{
			if (__state && __result == 0 && __instance.Map.IsSpace() && !__instance.TouchesMapEdge && !__instance.IsDoorway)
			{
				foreach (IntVec3 vec in __instance.BorderCells)
				{
					bool hasShipPart = false;
					foreach (Thing t in vec.GetThingList(__instance.Map))
					{
						if (t is Building)
						{
							Building b = t as Building;
							if (b.def.building.shipPart)
								hasShipPart = true;
						}
					}
					if (!hasShipPart)
					{
						___cachedOpenRoofCount = 1;
						return ___cachedOpenRoofCount;
					}
				}
				foreach (IntVec3 tile in __instance.Cells)
				{
					if (tile.GetRoof(__instance.Map) != ShipInteriorMod2.shipRoofDef)
					{
						___cachedOpenRoofCount = 1;
						return ___cachedOpenRoofCount;
					}
				}
			}
			return ___cachedOpenRoofCount;
		}
	}

	[HarmonyPatch(typeof(GenTemperature), "EqualizeTemperaturesThroughBuilding")]
	public static class NoVentingToSpace //block vents and open airlocks in vac, closed airlocks vent slower
	{
		public static bool Prefix(Building b, ref float rate, bool twoWay)
		{
			if (!b.Map.IsSpace())
				return true;
			if (twoWay) //vent
			{
				IntVec3 vec = b.Position + b.Rotation.FacingCell;
				Room room = vec.GetRoom(b.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					return false;
				}
				vec = b.Position - b.Rotation.FacingCell;
				room = vec.GetRoom(b.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					return false;
				}
				return true;
			}
			if (b is Building_ShipAirlock a)
			{
				if (a.Open && a.Outerdoor())
					return false;
				else
					rate = 0.5f;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "EqualizeTemperature")]
	public static class ExposedToVacuum
	{
		[HarmonyPostfix]
		public static void setShipTemp(RoomTempTracker __instance)
		{
			Room room = (Room)typeof(RoomTempTracker)
				.GetField("room", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
			if (room.Map.terrainGrid.TerrainAt(IntVec3.Zero) != GenerateSpaceSubMesh.spaceTerrain)
				return;
			if (room.Role != RoomRoleDefOf.None && room.OpenRoofCount > 0)
				__instance.Temperature = -100f;
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "WallEqualizationTempChangePerInterval")]
	public static class TemperatureDoesntDiffuseFastInSpace
	{
		[HarmonyPostfix]
		public static void RadiativeHeatTransferOnly(ref float __result, RoomTempTracker __instance)
		{
			if (((Room)typeof(RoomTempTracker)
					.GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)).Map.IsSpace())
			{
				__result *= 0.01f;
			}
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
	public static class TemperatureDoesntDiffuseFastInSpaceToo
	{
		[HarmonyPostfix]
		public static void RadiativeHeatTransferOnly(ref float __result, RoomTempTracker __instance)
		{
			if (((Room)typeof(RoomTempTracker)
					.GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)).Map.IsSpace())
			{
				__result *= 0.01f;
			}
		}
	}

	[HarmonyPatch(typeof(GlobalControls), "TemperatureString")]
	public static class ShowBreathability
	{
		[HarmonyPostfix]
		public static void CheckO2(ref string __result)
		{
			if (!Find.CurrentMap.IsSpace()) return;

			if (ShipInteriorMod2.ExposedToOutside(UI.MouseCell().GetRoom(Find.CurrentMap)))
			{
				__result += " (Vacuum)";
			}
			else
            {
				if (Find.CurrentMap.GetComponent<ShipHeatMapComp>().LifeSupports.Where(s => s.active).Any())
					__result += " (Breathable Atmosphere)";
				else
					__result += " (Non-Breathable Atmosphere)";
			}
		}
	}

	[HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
	public static class CannotBurnInSpace
	{
		[HarmonyPostfix]
		public static void extinguish(Fire __instance)
		{
			if (!(__instance is MechaniteFire) && __instance.Spawned && __instance.Map.IsSpace())
			{
				Room room = __instance.Position.GetRoom(__instance.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
					__instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100, 0, -1f, null, null, null,
						DamageInfo.SourceCategory.ThingOrUnknown, null));
			}
		}
	}

	[HarmonyPatch(typeof(Plant), "TickLong")]
	public static class KillThePlantsInSpace
	{
		[HarmonyPostfix]
		public static void Extinguish(Plant __instance)
		{
			if (__instance.Spawned && __instance.Map.IsSpace())
			{
				Room room = __instance.Position.GetRoom(__instance.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					__instance.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 10, 0, -1f, null, null, null,
						DamageInfo.SourceCategory.ThingOrUnknown, null));
				}
			}
		}
	}

	//map
	[HarmonyPatch(typeof(CompShipPart), "CompGetGizmosExtra")]
	public static class NoGizmoInSpace
	{
		[HarmonyPrefix]
		public static bool CheckBiome(CompShipPart __instance, out bool __state)
		{
			__state = false;
			if (__instance.parent.Map != null && __instance.parent.Map.IsSpace())
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void ReturnEmpty(ref IEnumerable<Gizmo> __result, bool __state)
		{
			if (__state)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(SettleInExistingMapUtility), "SettleCommand")]
	public static class NoSpaceSettle
	{
		[HarmonyPostfix]
		public static void Nope(Command __result, Map map)
		{
			if (map.IsSpace())
			{
				__result.disabled = true;
				__result.disabledReason = "Cannot settle space sites";
			}
		}
	}

	[HarmonyPatch(typeof(Building), "ClaimableBy")]
	public static class NoClaimingEnemyShip
	{
		[HarmonyPostfix]
		public static void Nope(Building __instance, ref bool __result)
		{
			if (__instance.Map.IsSpace() && __instance.Map.GetComponent<ShipHeatMapComp>().ShipCombatMaster)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(MapDeiniter), "Deinit")]
	public static class RemoveSpaceMap
	{
		public static void Postfix()
		{
			AccessExtensions.Utility.RecacheSpaceMaps();
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub")]
	public static class NoTradersInSpace
	{
		[HarmonyPostfix]
		public static void Nope(IncidentParms parms, ref bool __result)
		{
			if (parms.target != null && parms.target is Map map && map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(ExitMapGrid))]
	[HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
	public static class InSpaceNoOneCanHearYouRunAway
	{
		[HarmonyPostfix]
		public static void NoEscape(Map ___map, ref bool __result)
		{
			if (___map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(TileFinder), "TryFindNewSiteTile")]
	public static class NoQuestsNearTileZero
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CheckNonZeroTile(out int tile, int minDist, int maxDist, bool allowCaravans,
			TileFinderMode tileFinderMode, int nearThisTile, ref bool __result)
		{
			Func<int, int> findTile = delegate (int root) {
				int minDist2 = minDist;
				int maxDist2 = maxDist;
				Predicate<int> validator = (int x) =>
					!Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x, null);
				int result;
				if (TileFinder.TryFindPassableTileWithTraversalDistance(root, minDist2, maxDist2, out result,
					validator: validator, ignoreFirstTilePassability: false, tileFinderMode, false))
				{
					return result;
				}

				return -1;
			};
			int arg;
			if (nearThisTile != -1)
			{
				arg = nearThisTile;
			}
			else if (!TileFinder.TryFindRandomPlayerTile(out arg, allowCaravans,
				(int x) => findTile(x) != -1 && (Find.World.worldObjects.MapParentAt(x) == null ||
												 !(Find.World.worldObjects.MapParentAt(x) is WorldObjectOrbitingShip))))
			{
				tile = -1;
				__result = false;
				return;
			}

			tile = findTile(arg);
			__result = (tile != -1);
		}
	}

	[HarmonyPatch(typeof(QuestNode_GetMap), "IsAcceptableMap")]
	public static class NoQuestsInSpace
	{
		[HarmonyPostfix]
		public static void Fixpost(Map map, ref bool __result)
		{
			if (map.Parent != null && map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(QuestGen_Get), "GetMap")]
	public static class InSpaceNoQuestsCanUseThis
	{
		[HarmonyPostfix]
		public static void NoQuestsTargetSpace(ref Map __result)
		{
			if (__result != null && __result.IsSpace())
			{
				//retry and exclude space maps
				Log.Message("Tried to fire quest in space map, retrying.");
				Map map = Find.Maps.Where(m => m.IsPlayerHome && !m.IsSpace() && m.mapPawns.FreeColonists.Count >= 1).FirstOrDefault();
				if (map == null)
					map = Find.Maps.Where(m => m.IsPlayerHome && m.mapPawns.FreeColonists.Count >= 1).FirstOrDefault();
				__result = map;
			}
		}
	}

	[HarmonyPatch(typeof(RCellFinder), "TryFindRandomExitSpot")]
	public static class NoPrisonBreaksInSpace
	{
		[HarmonyPostfix]
		public static void NoExits(Pawn pawn, ref bool __result)
		{
			if (pawn.Map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(RoofCollapseCellsFinder), "ConnectsToRoofHolder")]
	public static class NoRoofCollapseInSpace
	{
		[HarmonyPostfix]
		public static void ZeroGee(ref bool __result, Map map)
		{
			if (map.IsSpace()) __result = true;
		}
	}

	[HarmonyPatch(typeof(RoofCollapseUtility), "WithinRangeOfRoofHolder")]
	public static class NoRoofCollapseInSpace2
	{
		[HarmonyPostfix]
		public static void ZeroGee(ref bool __result, Map map)
		{
			if (map.IsSpace()) __result = true;
		}
	}

	[HarmonyPatch(typeof(FogGrid), "FloodUnfogAdjacent")]
	public static class DoNotSpamMePlease
	{
		[HarmonyPrefix]
		public static bool CheckBiome(Map ___map, out bool __state)
		{
			__state = false;
			if (___map != null && ___map.IsSpace())
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void NoMoreAreaSpam(FogGrid __instance, Map ___map, IntVec3 c, bool __state)
		{
			if (__state)
			{
				__instance.Unfog(c);
				for (int i = 0; i < 4; i++)
				{
					IntVec3 intVec = c + GenAdj.CardinalDirections[i];
					if (intVec.InBounds(___map) && intVec.Fogged(___map))
					{
						Building edifice = intVec.GetEdifice(___map);
						if (edifice == null || !edifice.def.MakeFog)
						{
							FloodFillerFog.FloodUnfog(intVec, ___map);
						}
						else
						{
							__instance.Unfog(intVec);
						}
					}
				}
				for (int j = 0; j < 8; j++)
				{
					IntVec3 c2 = c + GenAdj.AdjacentCells[j];
					if (c2.InBounds(___map))
					{
						Building edifice2 = c2.GetEdifice(___map);
						if (edifice2 != null && edifice2.def.MakeFog)
						{
							__instance.Unfog(c2);
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker_CallAid), "CallAid")]
	public static class CallAidInSpace
	{
		[HarmonyPrefix]
		public static bool SpaceAidHasEVA(RoyalTitlePermitWorker_CallAid __instance, Pawn caller, Map map, IntVec3 spawnPos, Faction faction, bool free, float biocodeChance = 1f)
		{
			if (map != null && map.IsSpace())
			{
				IncidentParms incidentParms = new IncidentParms();
				incidentParms.target = map;
				incidentParms.faction = faction;
				incidentParms.raidArrivalModeForQuickMilitaryAid = true;
				incidentParms.biocodeApparelChance = biocodeChance;
				incidentParms.biocodeWeaponsChance = biocodeChance;
				incidentParms.spawnCenter = spawnPos;
				if (__instance.def.royalAid.pawnKindDef != null)
				{
					incidentParms.pawnKind = __instance.def.royalAid.pawnKindDef;
					//if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Trooper)
					//return false;
					if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Janissary)
						incidentParms.pawnKind = DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Marine_Space");
					else if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Cataphract)
						incidentParms.pawnKind = DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Cataphract_Space");
					incidentParms.pawnCount = __instance.def.royalAid.pawnCount;
				}
				else
				{
					incidentParms.points = (float)__instance.def.royalAid.points;
				}
				faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
				if (IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms))
				{
					if (!free)
					{
						caller.royalty.TryRemoveFavor(faction, __instance.def.royalAid.favorCost);
					}
					caller.royalty.GetPermit(__instance.def, faction).Notify_Used();
					return false;
				}
				Log.Error(string.Concat(new object[] { "Could not send aid to map ", map, " from faction ", faction }));
				return false;
			}
			else
				return true;
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker_CallLaborers), "CallLaborers")]
	public static class CallLaborersInSpace
	{
		[HarmonyPrefix]
		public static bool SpaceLaborersHaveEVA(RoyalTitlePermitWorker_CallAid __instance, Pawn pawn, Map map, Faction faction, bool free)
		{
			if (map != null && map.IsSpace())
			{
				if (faction.HostileTo(Faction.OfPlayer))
				{
					return false;
				}
				QuestScriptDef permit_CallLaborers = QuestScriptDefOf.Permit_CallLaborers;
				Slate slate = new Slate();
				slate.Set<Map>("map", map, false);
				slate.Set<int>("laborersCount", __instance.def.royalAid.pawnCount, false);
				slate.Set<Faction>("permitFaction", faction, false);
				slate.Set<PawnKindDef>("laborersPawnKind", DefDatabase<PawnKindDef>.GetNamed("Empire_Space_Laborer"), false);
				slate.Set<float>("laborersDurationDays", __instance.def.royalAid.aidDurationDays, false);
				QuestUtility.GenerateQuestAndMakeAvailable(permit_CallLaborers, slate);
				pawn.royalty.GetPermit(__instance.def, faction).Notify_Used();
				if (!free)
				{
					pawn.royalty.TryRemoveFavor(faction, __instance.def.royalAid.favorCost);
				}
				return false;
			}
			else
				return true;
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker), "AidDisabled")]
	public static class RoyalTitlePermitWorkerInSpace
	{
		[HarmonyPostfix]
		public static void AllowSpacePermits(Map map, ref bool __result)
		{
			if (map != null && map.IsSpace() && __result == true)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(Site), "PostMapGenerate")]
	public static class RaidsStartEarly
	{
		public static void Postfix(Site __instance)
		{
			if (__instance.parts.Where(part => part.def.tags.Contains("SoSMayday")).Any())
			{
				__instance.GetComponent<TimedDetectionRaids>().StartDetectionCountdown(Rand.Range(6000, 12000), 1);
			}
		}
	}

	//sensor
	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch("AnyPawnBlockingMapRemoval", MethodType.Getter)]
	public static class KeepMapAlive
	{
		public static void Postfix(MapPawns __instance, ref bool __result)
		{
			Map mapPlayer = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault())?.Map;
			if (mapPlayer != null)
			{
				foreach (Building_ShipAdvSensor sensor in mapPlayer.GetComponent<ShipHeatMapComp>().Sensors)
				{
					if (sensor.observedMap != null && sensor.observedMap.Map != null && sensor.observedMap.Map.mapPawns == __instance)
						__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(SettlementDefeatUtility), "IsDefeated")]
	public static class NoInstaWin
	{
		public static void Postfix(Map map, ref bool __result)
		{
			Map mapPlayer = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault())?.Map;
			if (mapPlayer != null)
			{
				foreach (Building_ShipAdvSensor sensor in mapPlayer.GetComponent<ShipHeatMapComp>().Sensors)
				{
					if (sensor.observedMap != null && sensor.observedMap.Map == map)
					{
						__result = false;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(TimedDetectionRaids), "CompTick")]
	public static class NoScanRaids
	{
		public static bool Prefix(TimedDetectionRaids __instance)
		{
			return ((MapParent)__instance.parent).HasMap && ((MapParent)__instance.parent).Map.mapPawns.AnyColonistSpawned;
		}
	}

	//comms
	[HarmonyPatch(typeof(Building_CommsConsole), "GetFailureReason")]
	public class NoCommsWhenCloaked
	{
		public static void Postfix(Pawn myPawn, ref FloatMenuOption __result)
		{
			foreach (Building_ShipCloakingDevice cloak in myPawn.Map.GetComponent<ShipHeatMapComp>().Cloaks)
			{
				if (cloak.active && cloak.Map == myPawn.Map)
				{
					__result = new FloatMenuOption("CannotUseCloakEnabled".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(TradeShip), "TryOpenComms")]
	public static class ReplaceCommsIfPirate
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void OpenActualComms(TradeShip __instance, Pawn negotiator)
		{
			if (!__instance.CanTradeNow)
			{
				return;
			}
			int bounty = Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty;

			DiaNode diaNode = new DiaNode("TradeShipComms".Translate() + __instance.TraderName);

			//trade normally if no bounty or low bounty with social check
			DiaOption diaOption = new DiaOption("TradeShipTradeWith".Translate());
			diaOption.action = delegate
			{
				Find.WindowStack.Add(new Dialog_Trade(negotiator, __instance, false));
				LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.Critical);
				PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(__instance.Goods.OfType<Pawn>(), "LetterRelatedPawnsTradeShip".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
				TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradeGoodsMustBeNearBeacon, Array.Empty<string>());
			};
			diaOption.resolveTree = true;
			diaNode.options.Add(diaOption);
			if (negotiator.skills.GetSkill(SkillDefOf.Social).levelInt * 2 < bounty)
			{
				diaOption.Disable("TradeShipTradeDecline".Translate(__instance.TraderName));
			}

			//if in space add pirate option
			if (__instance.Map.IsSpace())
			{
				DiaOption diaOption2 = new DiaOption("TradeShipPirate".Translate());
				diaOption2.action = delegate
				{
					Building bridge = __instance.Map.listerBuildings.AllBuildingsColonistOfClass<Building_ShipBridge>().FirstOrDefault();
					if (Rand.Chance(0.025f * negotiator.skills.GetSkill(SkillDefOf.Social).levelInt + negotiator.Map.GetComponent<ShipHeatMapComp>().ShipThreat(__instance.Map) / 400 - bounty / 40))
					{
						//social + shipstr vs bounty for piracy dialog
						Find.WindowStack.Add(new Dialog_Pirate(__instance.Map.listerBuildings.allBuildingsColonist.Where(t => t.def.defName.Equals("ShipSalvageBay")).Count(), __instance));
						bounty += 4;
						Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
					}
					else
					{
						//check failed, ship is fleeing
						bounty += 1;
						Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
						if (__instance.Faction == Faction.OfEmpire)
							Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, -25, false, true, HistoryEventDefOf.AttackedCaravan, null);
						DiaNode diaNode2 = new DiaNode(__instance.TraderName + "TradeShipTryingToFlee".Translate());
						DiaOption diaOption21 = new DiaOption("TradeShipAttack".Translate());
						diaOption21.action = delegate
						{
							negotiator.Map.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, (TradeShip)__instance);
							if (ModsConfig.IdeologyActive)
								IdeoUtility.Notify_PlayerRaidedSomeone(__instance.Map.mapPawns.FreeColonists);
						};
						diaOption21.resolveTree = true;
						diaNode2.options.Add(diaOption21);
						DiaOption diaOption22 = new DiaOption("TradeShipFlee".Translate());
						diaOption22.action = delegate
						{
							__instance.Depart();
						};
						diaOption22.resolveTree = true;
						diaNode2.options.Add(diaOption22);
						Find.WindowStack.Add(new Dialog_NodeTree(diaNode2, true, false, null));

					}
				};
				diaOption2.resolveTree = true;
				diaNode.options.Add(diaOption2);

			}
			//pay bounty, gray if not enough money
			if (bounty > 1)
			{
				DiaOption diaOption3 = new DiaOption("TradeShipPayBounty".Translate(2500 * bounty));
				diaOption3.action = delegate
				{
					TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, 2500 * bounty, __instance.Map, null);
					bounty = 0;
					Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
				};
				diaOption3.resolveTree = true;
				diaNode.options.Add(diaOption3);

				if (AmountSendableSilver(__instance.Map) < 2500 * bounty)
				{
					diaOption3.Disable("NotEnoughForBounty".Translate(2500 * bounty));
				}
			}
			//quit
			DiaOption diaOption4 = new DiaOption("(" + "Disconnect".Translate() + ")");
			diaOption4.resolveTree = true;
			diaNode.options.Add(diaOption4);
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, false, null));
		}
		private static int AmountSendableSilver(Map map)
		{
			return (from t in TradeUtility.AllLaunchableThingsForTrade(map, null)
					where t.def == ThingDefOf.Silver
					select t).Sum((Thing t) => t.stackCount);
		}
	}

	//ship
	[HarmonyPatch(typeof(ShipUtility), "ShipBuildingsAttachedTo")]
	public static class FindAllTheShipParts
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FindShipPartsReally(Building root, ref List<Building> __result)
		{
			if (root == null || root.Destroyed)
			{
				__result = new List<Building>();
				return;
			}

			var map = root.Map;
			var containedBuildings = new HashSet<Building>();
			var cellsTodo = new HashSet<IntVec3>();
			var cellsDone = new HashSet<IntVec3>();

			cellsTodo.AddRange(GenAdj.CellsOccupiedBy(root));
			cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root));

			while (cellsTodo.Count > 0)
			{
				var current = cellsTodo.First();
				cellsTodo.Remove(current);
				cellsDone.Add(current);

				var containedThings = current.GetThingList(map);
				if (!containedThings.Any(thing => (thing as Building)?.def.building.shipPart ?? false))
				{
					continue;
				}

				foreach (var thing in containedThings)
				{
					if (thing is Building building)
					{
						if (containedBuildings.Add(building))
						{
							cellsTodo.AddRange(
								GenAdj.CellsOccupiedBy(building).Concat(GenAdj.CellsAdjacentCardinal(building))
									.Where(cell => !cellsDone.Contains(cell))
							);
						}
					}
				}
			}

			__result = containedBuildings.ToList();
		}
	}

	[HarmonyPatch(typeof(ShipUtility), "LaunchFailReasons")]
	public static class FindLaunchFailReasons
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FindLaunchFailReasonsReally(Building rootBuilding, ref IEnumerable<string> __result)
		{
			List<string> newResult = new List<string>();
			List<Building> shipParts = ShipUtility.ShipBuildingsAttachedTo(rootBuilding);

			if (!FindEitherThing(shipParts, ThingDefOf.Ship_Engine, ThingDef.Named("Ship_Engine_Small"), ThingDef.Named("Ship_Engine_Large")))
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_Engine.label);
			if (!FindEitherThing(shipParts, ThingDefOf.Ship_SensorCluster, ThingDef.Named("Ship_SensorClusterAdv"), null))
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_SensorCluster.label);
			if (!FindEitherThing(shipParts, ThingDef.Named("ShipPilotSeat"), ThingDefOf.Ship_ComputerCore,
				ThingDef.Named("ShipPilotSeatMini"), ThingDef.Named("ShipArchotechSpore")))
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDef.Named("ShipPilotSeat"));

			float fuelNeeded = 0f;
			float fuelHad = 0f;
			foreach (Building part in shipParts)
			{
				if (part.def == ThingDefOf.Ship_Engine || part.def.defName.Equals("Ship_Engine_Small"))
				{
					if (part.TryGetComp<CompRefuelable>() != null)
						fuelHad += part.TryGetComp<CompRefuelable>().Fuel;
				}
				else if (part.def.defName.Equals("Ship_Engine_Large"))
				{
					if (part.TryGetComp<CompRefuelable>() != null)
						fuelHad += part.TryGetComp<CompRefuelable>().Fuel * 2;
				}

				if (part.def != ShipInteriorMod2.hullPlateDef && part.def != ShipInteriorMod2.archoHullPlateDef && part.def != ShipInteriorMod2.mechHullPlateDef)
					fuelNeeded += (part.def.size.x * part.def.size.z) * 3f;
				else
					fuelNeeded += 1f;
			}

			if (fuelHad < fuelNeeded)
			{
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMoreChemfuel", fuelHad, fuelNeeded));
			}

			bool hasPilot = false;
			foreach (Building part in shipParts)
			{
				if ((part.def == ThingDef.Named("ShipPilotSeat") || part.def == ThingDef.Named("ShipPilotSeatMini")) &&
					part.TryGetComp<CompMannable>().MannedNow && part.TryGetComp<CompPowerTrader>().PowerOn)
					hasPilot = true;
				else if ((part.def == ThingDefOf.Ship_ComputerCore || part.def == ThingDef.Named("ShipArchotechSpore")) && part.TryGetComp<CompPowerTrader>().PowerOn)
					hasPilot = true;
			}

			if (!hasPilot)
			{
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportNeedPilot"));
			}

			/*bool fullPodFound = false;
			foreach (Building part in shipParts)
			{
				if (part.def == ThingDefOf.Ship_CryptosleepCasket || part.def == ThingDef.Named("ShipInside_CryptosleepCasket"))
				{
					Building_CryptosleepCasket pod = part as Building_CryptosleepCasket;
					if (pod != null && pod.HasAnyContents)
					{
						fullPodFound = true;
						break;
					}
				}
			}
			if (!fullPodFound)
			{
				__result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportNoFullPods"));
			}*/

			__result = newResult;
		}

		private static bool FindTheThing(List<Building> shipParts, ThingDef theDef)
		{
			if (!shipParts.Any((Building pa) => pa.def == theDef))
			{
				return false;
			}

			return true;
		}

		private static bool FindEitherThing(List<Building> shipParts, ThingDef theDef, ThingDef theOtherDef, ThingDef theThirdDef, ThingDef yetAnotherDef = null)
		{
			if (!shipParts.Any((Building pa) => pa.def == theDef) &&
				!shipParts.Any((Building pa) => pa.def == theOtherDef) &&
				!shipParts.Any((Building pa) => pa.def == theThirdDef) &&
				!shipParts.Any((Building pa) => pa.def == yetAnotherDef))
			{
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(ShipCountdown), "InitiateCountdown", new Type[] { typeof(Building) })]
	public static class InitShipRefs
	{
		[HarmonyPrefix]
		public static bool SaveStatics(Building launchingShipRoot)
		{
			ShipInteriorMod2.shipOriginRoot = launchingShipRoot;
			return true;
		}
	}

	[HarmonyPatch(typeof(ShipCountdown), "CountdownEnded")]
	public static class SaveShip
	{
		[HarmonyPrefix]
		public static bool SaveShipAndRemoveItemStacks()
		{
			if (ShipInteriorMod2.shipOriginRoot != null)
			{
				ScreenFader.StartFade(UnityEngine.Color.clear, 1f);
				WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
				orbiter.radius = 150;
				orbiter.theta = -3;
				orbiter.SetFaction(Faction.OfPlayer);
				orbiter.Tile = ShipInteriorMod2.FindWorldTile();
				Find.WorldObjects.Add(orbiter);
				Map myMap = MapGenerator.GenerateMap(ShipInteriorMod2.shipOriginRoot.Map.Size, orbiter, orbiter.MapGeneratorDef);
				myMap.fogGrid.ClearAllFog();

				ShipInteriorMod2.MoveShip(ShipInteriorMod2.shipOriginRoot, myMap, new IntVec3(0, 0, 0));
				myMap.weatherManager.TransitionTo(DefDatabase<WeatherDef>.GetNamed("OuterSpaceWeather"));
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelOrbitAchieved"),
					TranslatorFormattedStringExtensions.Translate("LetterOrbitAchieved"), LetterDefOf.PositiveEvent);
				ShipInteriorMod2.shipOriginRoot = null;
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(GameConditionManager), "ConditionIsActive")]
	public static class SpacecraftAreHardenedAgainstSolarFlares
	{
		[HarmonyPostfix]
		public static void Nope(ref bool __result, GameConditionManager __instance, GameConditionDef def)
		{
			if (def == GameConditionDefOf.SolarFlare && __instance.ownerMap != null &&
				__instance.ownerMap.IsSpace())
				__result = false;
		}
	}

	[HarmonyPatch(typeof(GameConditionManager))]
	[HarmonyPatch("ElectricityDisabled", MethodType.Getter)]
	public static class SpacecraftAreAlsoHardenedInOnePointOne
	{
		[HarmonyPostfix]
		public static void PowerOn(GameConditionManager __instance, ref bool __result)
		{
			if (__instance.ownerMap.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(Designator_Dropdown), "GetDesignatorCost")]
	public class FixDropdownDisplay
	{
		public static void Postfix(Designator des, ref ThingDef __result)
		{
			Designator_Place designator_Place = des as Designator_Place;
			if (designator_Place != null)
			{
				BuildableDef placingDef = designator_Place.PlacingDef;
				if (placingDef.designationCategory.defName.Equals("Ship"))
				{
					__result = (ThingDef)placingDef;
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoofGrid), "GetCellExtraColor")]
	public static class ShowHullTilesOnRoofGrid
	{
		[HarmonyPostfix]
		public static void HullsAreColorful(RoofGrid __instance, int index, ref Color __result)
		{
			if (__instance.RoofAt(index) == ShipInteriorMod2.shipRoofDef)
				__result = Color.clear;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ShouldRemoveExistingFloorFirst")]
	public static class DontRemoveShipFloors
	{
		[HarmonyPostfix]
		public static void CheckShipFloor(Blueprint blue, ref bool __result)
		{
			if (blue.Map.terrainGrid.TerrainAt(blue.Position) == CompRoofMe.hullTerrain || blue.Map.terrainGrid.TerrainAt(blue.Position) == CompRoofMe.archotechHullTerrain)
			{
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
	public static class RecreateShipTile
	{
		[HarmonyPostfix]
		public static void NoClearTilesPlease(TerrainGrid __instance, IntVec3 c)
		{
			Map map = (Map)typeof(TerrainGrid).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(__instance);
			foreach (Thing t in map.thingGrid.ThingsAt(c))
			{
				if (t.TryGetComp<CompRoofMe>() != null)
				{
					map.roofGrid.SetRoof(c, ShipInteriorMod2.shipRoofDef);
					if (!map.terrainGrid.TerrainAt(c).layerable)
					{
						if (!t.TryGetComp<CompRoofMe>().Props.archotech)
							map.terrainGrid.SetTerrain(c, CompRoofMe.hullTerrain);
						else
							map.terrainGrid.SetTerrain(c, CompRoofMe.archotechHullTerrain);
					}
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoofGrid), "SetRoof")] //roofing ship tiles makes ship roof
	public static class RebuildShipRoof
	{
		[HarmonyPrefix]
		public static bool SetNewRoof(IntVec3 c, RoofDef def, Map ___map, ref CellBoolDrawer ___drawerInt, ref RoofDef[]  ___roofGrid)
		{
			if (def == null || def.isThickRoof)
				return true;
			foreach (Thing t in c.GetThingList(___map))
			{
				if (t.def == ShipInteriorMod2.hullPlateDef || t.def == ShipInteriorMod2.archoHullPlateDef || t.def == ShipInteriorMod2.mechHullPlateDef)
				{
					if (___roofGrid[___map.cellIndices.CellToIndex(c)] == def)
					{
						return false;
					}
					___roofGrid[___map.cellIndices.CellToIndex(c)] = ShipInteriorMod2.shipRoofDef;
					___map.glowGrid.MarkGlowGridDirty(c);
					Region validRegionAt_NoRebuild = ___map.regionGrid.GetValidRegionAt_NoRebuild(c);
					if (validRegionAt_NoRebuild != null)
					{
						validRegionAt_NoRebuild.District.Notify_RoofChanged();
					}
					if (___drawerInt != null)
					{
						___drawerInt.SetDirty();
					}
					___map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCells")]
	[HarmonyPatch(new Type[] { typeof(IEnumerable<IntVec3>), typeof(Map), typeof(List<Thing>) })]
	public static class SealHole
	{
		[HarmonyPostfix]
		public static void ShipRoofIsDestroyed(IEnumerable<IntVec3> cells, Map map)
		{
			foreach (IntVec3 cell in cells)
			{
				if (map.IsSpace() && !cell.Roofed(map))
				{
					var mapComp = map.GetComponent<ShipHeatMapComp>();
					if (mapComp.HullFoamDistributors.Count > 0)
					{
						foreach (CompHullFoamDistributor dist in mapComp.HullFoamDistributors)
						{
							if (dist.parent.TryGetComp<CompRefuelable>().Fuel > 0 && dist.parent.TryGetComp<CompPowerTrader>().PowerOn)
							{
								dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
								map.roofGrid.SetRoof(cell, ShipInteriorMod2.shipRoofDef);
								//Log.Message("rebuilt roof at:" + cell);
								break;
							}
						}
					}
				}

			}
		}
	}

	//weapons
	[HarmonyPatch(typeof(BuildingProperties))]
	[HarmonyPatch("IsMortar", MethodType.Getter)]
	public static class TorpedoesCanBeLoaded
	{
		[HarmonyPostfix]
		public static void CheckThisOneToo(BuildingProperties __instance, ref bool __result)
		{
			if (__instance?.turretGunDef?.HasComp(typeof(CompChangeableProjectilePlural)) ?? false)
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(ITab_Shells))]
	[HarmonyPatch("SelStoreSettingsParent", MethodType.Getter)]
	public static class TorpedoesHaveShellTab
	{
		[HarmonyPostfix]
		public static void CheckThisOneThree(ITab_Shells __instance, ref IStoreSettingsParent __result)
		{
			Building_ShipTurret building_TurretGun = Find.Selector.SingleSelectedObject as Building_ShipTurret;
			if (building_TurretGun != null)
			{
				__result = (IStoreSettingsParent)typeof(ITab_Storage)
					.GetMethod("GetThingOrThingCompStoreSettingsParent",
						BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(__instance, new object[] { building_TurretGun.gun });
				return;
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "CheckForFreeInterceptBetween")]
	public static class OnePointThreeSpaceProjectiles
	{
		public static void Postfix(Projectile __instance, ref bool __result)
		{
			if (__instance is Projectile_SoSFake)
				__result = false;
		}
	}
	
	[HarmonyPatch(typeof(Projectile), "Launch")]
	[HarmonyPatch(new Type[] {
		typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo),
		typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef)
	})]
	public static class TransferAmplifyBonus
	{
		[HarmonyPostfix]
		public static void OneMoreFactor(Projectile __instance, Thing equipment)
		{
			if (__instance is Projectile_ExplosiveShipCombat && equipment is Building_ShipTurret &&
				((Building_ShipTurret)equipment).AmplifierDamageBonus > 0)
			{
				typeof(Projectile)
					.GetField("weaponDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
					.SetValue(__instance, 1 + ((Building_ShipTurret)equipment).AmplifierDamageBonus);
			}
		}
	}

	//buildings
	[HarmonyPatch(typeof(Building), "Destroy")]
	public static class NotifyCombatManager
	{
		[HarmonyPrefix]
		public static bool ShipPartIsDestroyed(Building __instance, DestroyMode mode, out Tuple<IntVec3, Faction, Map> __state)
		{
			__state = null;
			if (!__instance.def.CanHaveFaction || (mode != DestroyMode.KillFinalize && mode != DestroyMode.Deconstruct) || __instance is Frame)
				return true;
			var mapComp = __instance.Map.GetComponent<ShipHeatMapComp>();
			if (!mapComp.InCombat)
				return true;
			mapComp.DirtyShip(__instance);
			if (mode != DestroyMode.Deconstruct && __instance.def.blueprintDef != null)
			{
				if (mapComp.HullFoamDistributors.Count > 0 && (__instance.def == ShipInteriorMod2.beamDef || __instance.def.defName == "Ship_Beam_Unpowered" || __instance.def.defName == "ShipInside_PassiveCooler" || __instance.def.defName == "ShipInside_SolarGenerator"))
				{
					foreach (CompHullFoamDistributor dist in mapComp.HullFoamDistributors)
					{
						if (dist.parent.TryGetComp<CompRefuelable>().Fuel > 0 && dist.parent.TryGetComp<CompPowerTrader>().PowerOn)
						{
							dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
							__state = new Tuple<IntVec3, Faction, Map>(__instance.Position, __instance.Faction, __instance.Map);
							return true;
						}
					}
				}
				if (__instance.Faction == Faction.OfPlayer)
					GenConstruct.PlaceBlueprintForBuild(__instance.def, __instance.Position, __instance.Map,
					__instance.Rotation, Faction.OfPlayer, __instance.Stuff);
			}
			return true;
		}

		[HarmonyPostfix]
		public static void ReplaceShipPart(Tuple<IntVec3, Faction, Map> __state)
		{
			if (__state != null)
			{
				Thing newWall = ThingMaker.MakeThing(ThingDef.Named("HullFoamWall"));
				newWall.SetFaction(__state.Item2);
				GenPlace.TryPlaceThing(newWall, __state.Item1, __state.Item3, ThingPlaceMode.Direct);
			}
		}
	}

	[HarmonyPatch(typeof(SectionLayer_BuildingsDamage), "PrintDamageVisualsFrom")]
	public class FixBuildingDraw
	{
		public static bool Prefix(Building b)
		{
			if (b.Map == null)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(Room), "Notify_ContainedThingSpawnedOrDespawned")]
	public static class AirlockBugFix
	{
		[HarmonyPrefix]
		public static bool FixTheAirlockBug(Room __instance)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				typeof(Room).GetField("statsAndRoleDirty", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, true);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_Turret), "PreApplyDamage")]
	public static class HardpointsHelpTurrets
	{
		public static bool Prefix(ref DamageInfo dinfo, Building_Turret __instance)
		{
			if (__instance.Position.GetFirstThingWithComp<CompShipPart>(__instance.Map) != null)
				dinfo.SetAmount(dinfo.Amount / 4);
			return true;
		}
	}

	[HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
	public static class ReactorsCanBeRefueled
	{
		[HarmonyPostfix]
		public static void CheckClass(ThingRequestGroup group, ThingDef def, ref bool __result)
		{
			if (group == ThingRequestGroup.Refuelable && def.HasComp(typeof(CompRefuelableOverdrivable)))
				__result = true;
		}
	}

	[HarmonyPatch(typeof(CompPower))]
	[HarmonyPatch("PowerNet", MethodType.Getter)]
	public static class FixPowerBug
	{
		public static void Postfix(CompPower __instance, ref PowerNet __result)
		{
			if (!(__instance.parent.ParentHolder is MinifiedThing) && __instance.Props.transmitsPower && __result == null && __instance.parent.Map.GetComponent<ShipHeatMapComp>().InCombat)
			{
				__instance.transNet = __instance.parent.Map.powerNetGrid.TransmittedPowerNetAt(__instance.parent.Position);
				if (__instance.transNet != null)
				{
					__instance.transNet.connectors.Add(__instance);
					if (__instance is CompPowerBattery)
						__instance.transNet.batteryComps.Add((CompPowerBattery)__instance);
					else if (__instance is CompPowerTrader)
						__instance.transNet.powerComps.Add((CompPowerTrader)__instance);
					__result = __instance.transNet;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ShortCircuitUtility), "DoShortCircuit")]
	public static class NoShortCircuitCapacitors
	{
		[HarmonyPrefix]
		public static bool disableEventQuestionMark(Building culprit, out bool __state)
		{
			__state = false;
			PowerNet powerNet = culprit.PowerComp.PowerNet;
			if (powerNet.batteryComps.Any((CompPowerBattery x) =>
				x.parent.def == ThingDef.Named("ShipCapacitor") || x.parent.def == ThingDef.Named("ShipCapacitorSmall")))
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void tellThePlayerTheDayWasSaved(Building culprit, bool __state)
		{
			if (__state)
			{
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelShortCircuit"), TranslatorFormattedStringExtensions.Translate("LetterLabelShortCircuitShipDesc"),
					LetterDefOf.NegativeEvent, new TargetInfo(culprit.Position, culprit.Map, false), null);
			}
		}
	}

	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	public static class ConduitWipe
	{
		[HarmonyPostfix]
		public static void PerhapsNoConduitHere(ref bool __result, BuildableDef newEntDef, BuildableDef oldEntDef)
		{
			ThingDef newDef = newEntDef as ThingDef;
			if (oldEntDef.defName == "ShipHeatConduit")
			{
				if (newDef != null)
				{
					foreach (CompProperties comp in newDef.comps)
					{
						if (comp is CompProperties_ShipHeat)
							__result = true;
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(CompScanner))]
	[HarmonyPatch("CanUseNow", MethodType.Getter)]
	public static class NoUseInSpace
	{
		[HarmonyPostfix]
		public static bool Postfix(bool __result, CompScanner __instance)
		{
			if (__instance.parent.Map.IsSpace())
				return false;
			return __result;
		}
	}

	//crypto
	[HarmonyPatch(typeof(Building_CryptosleepCasket), "FindCryptosleepCasketFor")]
	public static class AllowCrittersleepCaskets
	{
		[HarmonyPrefix]
		public static bool BlockExecution()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CrittersCanSleepToo(ref Building_CryptosleepCasket __result, Pawn p, Pawn traveler,
			bool ignoreOtherReservations = false)
		{
			foreach (var current in GetCryptosleepDefs())
			{
				if (current == ThingDef.Named("Cryptonest"))
					continue;
				var building_CryptosleepCasket =
					(Building_CryptosleepCasket)GenClosest.ClosestThingReachable(p.Position, p.Map,
						ThingRequest.ForDef(current), PathEndMode.InteractionCell,
						TraverseParms.For(traveler), 9999f,
						delegate (Thing x) {
							bool arg_33_0;
							if (x.def.defName == "CrittersleepCasket" &&
								p.BodySize <= ShipInteriorMod2.crittersleepBodySize &&
								((ThingOwner)typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
									.GetValue((Building_CryptosleepCasket)x)).Count < 8 ||
								x.def.defName == "CrittersleepCasketLarge" &&
								p.BodySize <= ShipInteriorMod2.crittersleepBodySize &&
								((ThingOwner)typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
									.GetValue((Building_CryptosleepCasket)x)).Count < 32)
							{
								var traveler2 = traveler;
								LocalTargetInfo target = x;
								var ignoreOtherReservations2 = ignoreOtherReservations;
								arg_33_0 = traveler2.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
							}
							else
							{
								arg_33_0 = false;
							}

							return arg_33_0;
						});
				if (building_CryptosleepCasket != null)
				{
					__result = building_CryptosleepCasket;
					return;
				}

				building_CryptosleepCasket = (Building_CryptosleepCasket)GenClosest.ClosestThingReachable(
					p.Position, p.Map, ThingRequest.ForDef(current), PathEndMode.InteractionCell,
					TraverseParms.For(traveler), 9999f,
					delegate (Thing x) {
						bool arg_33_0;
						if (x.def.defName != "CrittersleepCasketLarge" && x.def.defName != "CrittersleepCasket" &&
							!((Building_CryptosleepCasket)x).HasAnyContents)
						{
							var traveler2 = traveler;
							LocalTargetInfo target = x;
							var ignoreOtherReservations2 = ignoreOtherReservations;
							arg_33_0 = traveler2.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
						}
						else
						{
							arg_33_0 = false;
						}

						return arg_33_0;
					});
				if (building_CryptosleepCasket != null) __result = building_CryptosleepCasket;
			}
		}

		private static IEnumerable<ThingDef> GetCryptosleepDefs()
		{
			return ModLister.HasActiveModWithName("PsiTech")
				? DefDatabase<ThingDef>.AllDefs.Where(def =>
					def != ThingDef.Named("PTPsychicTraier") &&
					typeof(Building_CryptosleepCasket).IsAssignableFrom(def.thingClass))
				: DefDatabase<ThingDef>.AllDefs.Where(def =>
					typeof(Building_CryptosleepCasket).IsAssignableFrom(def.thingClass));
		}
	}

	[HarmonyPatch(typeof(JobDriver_CarryToCryptosleepCasket), "MakeNewToils")]
	public static class JobDriverFix
	{
		[HarmonyPrefix]
		public static bool BlockExecution()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FillThatCasket(ref IEnumerable<Toil> __result,
			JobDriver_CarryToCryptosleepCasket __instance)
		{
			Pawn Takee = (Pawn)typeof(JobDriver_CarryToCryptosleepCasket)
				.GetMethod("get_Takee", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(__instance, new object[0]);
			Building_CryptosleepCasket DropPod =
				(Building_CryptosleepCasket)typeof(JobDriver_CarryToCryptosleepCasket)
					.GetMethod("get_DropPod", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(__instance, new object[0]);
			List<Toil> myResult = new List<Toil>();
			__instance.FailOnDestroyedOrNull(TargetIndex.A);
			__instance.FailOnDestroyedOrNull(TargetIndex.B);
			__instance.FailOnAggroMentalState(TargetIndex.A);
			__instance.FailOn(() => !DropPod.Accepts(Takee));
			myResult.Add(Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
				.FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
				.FailOn(() =>
					(DropPod.def.defName != "CrittersleepCasket" &&
					 DropPod.def.defName != "CrittersleepCasketLarge") && DropPod.GetDirectlyHeldThings().Count > 0)
				.FailOn(() => !Takee.Downed)
				.FailOn(() =>
					!__instance.pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly, false, mode: TraverseMode.ByPawn))
				.FailOnSomeonePhysicallyInteracting(TargetIndex.A));
			myResult.Add(Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false));
			myResult.Add(Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell));
			Toil prepare = Toils_General.Wait(500);
			prepare.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
			prepare.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
			myResult.Add(prepare);
			myResult.Add(new Toil
			{
				initAction = delegate { DropPod.TryAcceptThing(Takee, true); },
				defaultCompleteMode = ToilCompleteMode.Instant
			});
			__result = myResult;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class EggFix
	{
		[HarmonyPostfix]
		public static void FillThatNest(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
		{
			if (pawn == null || clickPos == null)
				return;
			IntVec3 c = IntVec3.FromVector3(clickPos);
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				foreach (Thing current in c.GetThingList(pawn.Map))
				{
					if (current.def.IsWithinCategory(ThingCategoryDef.Named("EggsFertilized")) &&
						pawn.CanReserveAndReach(current, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) &&
						findCryptonestFor(current, pawn, true) != null)
					{
						string text2 = "Carry to cryptonest";
						JobDef jDef = DefDatabase<JobDef>.GetNamed("CarryToCryptonest");
						Action action2 = delegate {
							Building_CryptosleepCasket building_CryptosleepCasket =
								findCryptonestFor(current, pawn, false);
							if (building_CryptosleepCasket == null)
							{
								building_CryptosleepCasket = findCryptonestFor(current, pawn, true);
							}

							if (building_CryptosleepCasket == null)
							{
								Messages.Message(
									TranslatorFormattedStringExtensions.Translate("CannotCarryToCryptosleepCasket") + ": " +
									TranslatorFormattedStringExtensions.Translate("NoCryptosleepCasket"), current, MessageTypeDefOf.RejectInput);
								return;
							}

							Job job = new Job(jDef, current, building_CryptosleepCasket);
							job.count = current.stackCount;
							int eggsAlreadyInNest =
								(typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.Instance | BindingFlags.NonPublic)
									.GetValue(building_CryptosleepCasket) as ThingOwner).Count;
							if (job.count + eggsAlreadyInNest > 16)
								job.count = 16 - eggsAlreadyInNest;
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						};
						string label = text2;
						Action action = action2;
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
							new FloatMenuOption(label, action, MenuOptionPriority.Default, null, current, 0f, null,
								null), pawn, current, "ReservedBy"));
					}
				}
			}
		}

		static Building_CryptosleepCasket findCryptonestFor(Thing egg, Pawn p, bool ignoreOtherReservations)
		{
			Building_CryptosleepCasket building_CryptosleepCasket =
				(Building_CryptosleepCasket)GenClosest.ClosestThingReachable(p.Position, p.Map,
					ThingRequest.ForDef(ThingDef.Named("Cryptonest")), PathEndMode.InteractionCell,
					TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, delegate (Thing x) {
						bool arg_33_0;
						if (((ThingOwner)typeof(Building_CryptosleepCasket)
							.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
							.GetValue((Building_CryptosleepCasket)x)).TotalStackCount < 16)
						{
							LocalTargetInfo target = x;
							bool ignoreOtherReservations2 = ignoreOtherReservations;
							arg_33_0 = p.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
						}
						else
						{
							arg_33_0 = false;
						}

						return arg_33_0;
					}, null, 0, -1, false, RegionType.Set_Passable, false);
			if (building_CryptosleepCasket != null)
			{
				return building_CryptosleepCasket;
			}

			return null;
		}
	}

	[HarmonyPatch(typeof(Building_Casket), "Tick")]
	public static class EggsDontHatch
	{
		[HarmonyPrefix]
		public static bool Nope(Building_Casket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				List<ThingComp> comps = (List<ThingComp>)typeof(ThingWithComps)
					.GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				if (comps != null)
				{
					int i = 0;
					int count = comps.Count;
					while (i < count)
					{
						comps[i].CompTick();
						i++;
					}
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "GetFloatMenuOptions")]
	public static class CantEnterCryptonest
	{
		[HarmonyPrefix]
		public static bool Nope(Building_CryptosleepCasket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void AlsoNope(IEnumerable<FloatMenuOption> __result, Building_CryptosleepCasket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				__result = new List<FloatMenuOption>();
			}
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "TryAcceptThing")]
	public static class UpdateCasketGraphicsA
	{
		[HarmonyPostfix]
		public static void UpdateIt(Building_CryptosleepCasket __instance)
		{
			if (__instance.Map != null && __instance.Spawned)
				__instance.Map.mapDrawer.MapMeshDirty(__instance.Position,
					MapMeshFlag.Buildings | MapMeshFlag.Things);
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "EjectContents")]
	public static class UpdateCasketGraphicsB
	{
		[HarmonyPostfix]
		public static void UpdateIt(Building_CryptosleepCasket __instance)
		{
			if (__instance.Map != null && __instance.Spawned)
				__instance.Map.mapDrawer.MapMeshDirty(__instance.Position,
					MapMeshFlag.Buildings | MapMeshFlag.Things);
		}
	}

	//EVA
	[HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
	public static class H_SpaceZoomies
	{
		[HarmonyPostfix]
		public static void GoFast(Pawn_PathFollower __instance, Pawn ___pawn)
		{
			if (___pawn.Map.terrainGrid.TerrainAt(__instance.nextCell) == GenerateSpaceSubMesh.spaceTerrain &&
				ShipInteriorMod2.EVAlevel(___pawn)>6)
			{
				__instance.nextCellCostLeft /= 4;
				__instance.nextCellCostTotal /= 4;
			}
		}
	}
	[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
	public static class ApparelTracker_Notify_Added
	{
		internal static void Postfix(Pawn_ApparelTracker __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance?.pawn?.thingIDNumber);
		}
	}
	[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved))]
	public static class ApparelTracker_Notify_Removed
	{
		internal static void Postfix(Pawn_ApparelTracker __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance?.pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), "ApplyOnPawn")]
	public static class LungInstall
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part, Recipe_InstallArtificialBodyPart __instance)
		{
			if (__instance.recipe.addsHediff.defName.Equals("SoSArchotechLung"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_RemoveBodyPart), "ApplyOnPawn")]
	public static class LungRemove
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part)
		{
			if (part.def.defName.Equals("SoSArchotechLung"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_InstallImplant), "ApplyOnPawn")]
	public static class SkinInstall
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part, Recipe_InstallImplant __instance)
		{
			if (__instance.recipe.addsHediff.defName.Equals("SoSArchotechSkin"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_RemoveImplant), "ApplyOnPawn")]
	public static class SkinRemove
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part)
		{
			if (part.def.defName.Equals("SoSArchotechSkin"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class DeathRemove
	{
		internal static void Postfix(Pawn __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance.thingIDNumber);
		}
	}

	//pawns
	[HarmonyPatch(typeof(PreceptComp_Apparel), "GiveApparelToPawn")]
	public static class PreventIdeoApparel
	{
		[HarmonyPrefix]
		public static bool Nope(Pawn pawn)
		{
			if (pawn.kindDef.defName.Contains("Space"))
			{
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(PawnRelationWorker), "CreateRelation")]
	public static class PreventRelations
	{
		[HarmonyPrefix]
		public static bool Nope(Pawn generated, Pawn other)
		{
			if (!generated.RaceProps.Humanlike || !other.RaceProps.Humanlike || generated.kindDef.defName.Contains("Space") || other.kindDef.defName.Contains("Space"))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Trigger_UrgentlyHungry), "ActivateOn")]
	public static class MechsDontEat
	{
		[HarmonyPrefix]
		public static bool DisableMaybe(Lord lord, out bool __state)
		{
			__state = false;
			foreach (Pawn p in lord.ownedPawns)
			{
				if (p.RaceProps.IsMechanoid)
				{
					__state = true;
					return false;
				}
			}
			return true;
		}

		[HarmonyPostfix]
		public static void Okay(ref bool __result, bool __state)
		{
			if (__state)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TransferableUtility), "CanStack")]
	public static class MechsCannotStack
	{
		[HarmonyPrefix]
		public static bool Nope(Thing thing, ref bool __result)
		{
			if (thing is Pawn && ((Pawn)thing).RaceProps.IsMechanoid)
			{
				__result = false;
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class AnimalsHaveGizmosToo
	{
		public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.TryGetComp<CompArcholife>() != null)
			{
				List<Gizmo> giz = new List<Gizmo>();
				giz.AddRange(__result);
				giz.AddRange(__instance.TryGetComp<CompArcholife>().CompGetGizmosExtra());
				__result = giz;
			}
		}
	}

	[HarmonyPatch(typeof(CompSpawnerPawn), "TrySpawnPawn")]
	public static class SpaceCreaturesAreHungry
	{
		[HarmonyPostfix]
		public static void HungerLevel(ref Pawn pawn, bool __result)
		{
			if (__result && (pawn?.Map?.IsSpace() ?? false) && pawn.needs?.food?.CurLevel != null)
				pawn.needs.food.CurLevel = 0.2f;
		}
	}

	[HarmonyPatch(typeof(Pawn_FilthTracker), "GainFilth", new Type[] { typeof(ThingDef), typeof(IEnumerable<string>) })]
	public static class RadioactiveAshIsRadioactive
	{
		[HarmonyPostfix]
		public static void OhNoISteppedInIt(ThingDef filthDef, Pawn_FilthTracker __instance)
		{
			if (filthDef.defName.Equals("Filth_SpaceReactorAsh"))
			{
				Pawn pawn = (Pawn)typeof(Pawn_FilthTracker)
					.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				int damage = Rand.RangeInclusive(1, 2);
				pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage));
				float num = 0.025f;
				num *= pawn.GetStatValue(StatDefOf.ToxicSensitivity, true);
				if (num != 0f)
				{
					HealthUtility.AdjustSeverity(pawn, HediffDefOf.ToxicBuildup, num);
				}
			}
		}
	}

	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch("AllPawns", MethodType.Getter)]
	public class FixCaravanThreading
	{
		public static void Postfix(ref List<Pawn> __result)
		{
			__result = __result.ListFullCopy();
		}
	}

	[HarmonyPatch(typeof(Pawn_MindState), "Notify_DamageTaken")]
	public static class ShipTurretIsNull
	{
		[HarmonyPrefix]
		public static bool AnimalsFlee(DamageInfo dinfo, Pawn_MindState __instance)
		{
			if (dinfo.Instigator is Building_ShipTurret)
			{
				if (Traverse.Create<Pawn_MindState>().Method("CanStartFleeingBecauseOfPawnAction", __instance.pawn).GetValue<bool>())
				{
					__instance.StartFleeingBecauseOfPawnAction(dinfo.Instigator);
					return false;
				}
			}
			return true;
		}
	}

	//world&fac gen
	[HarmonyPatch(typeof(Scenario), "PostWorldGenerate")]
	public static class SelectiveWorldGeneration
	{
		[HarmonyPrefix]
		public static bool Replace(Scenario __instance)
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				Current.ProgramState = ProgramState.MapInitializing;
				//FactionGenerator.EnsureRequiredEnemies(Find.GameInitData.playerFaction); TODO possibly need to replace for 1.3
				Current.ProgramState = ProgramState.Playing;

				WorldSwitchUtility.SoonToBeObsoleteWorld.worldPawns = null;
				WorldSwitchUtility.SoonToBeObsoleteWorld.factionManager = null;
				WorldComponent obToRemove = null;
				List<UtilityWorldObject> uwos = new List<UtilityWorldObject>();
				foreach (WorldObject ob in WorldSwitchUtility.SoonToBeObsoleteWorld.worldObjects.AllWorldObjects)
				{
					if (ob is UtilityWorldObject && !(ob is PastWorldUWO))
						uwos.Add((UtilityWorldObject)ob);
				}

				foreach (WorldComponent comp in WorldSwitchUtility.SoonToBeObsoleteWorld.components)
				{
					if (comp is PastWorldUWO2)
						obToRemove = comp;
				}

				WorldSwitchUtility.SoonToBeObsoleteWorld.components.Remove(obToRemove);
				foreach (UtilityWorldObject uwo in uwos)
				{
					((List<WorldObject>)typeof(WorldObjectsHolder)
						.GetField("worldObjects", BindingFlags.Instance | BindingFlags.NonPublic)
						.GetValue(WorldSwitchUtility.SoonToBeObsoleteWorld.worldObjects)).Remove(uwo);
					typeof(WorldObjectsHolder)
						.GetMethod("RemoveFromCache", BindingFlags.Instance | BindingFlags.NonPublic)
						.Invoke(WorldSwitchUtility.SoonToBeObsoleteWorld.worldObjects, new object[] { uwo });

				}

				List<WorldComponent> modComps = new List<WorldComponent>();
				foreach (WorldComponent comp in WorldSwitchUtility.SoonToBeObsoleteWorld.components)
				{
					if (!(comp is TileTemperaturesComp) && !(comp is WorldGenData) && !(comp is PastWorldUWO2))
						modComps.Add(comp);
				}

				foreach (WorldComponent comp in modComps)
					WorldSwitchUtility.SoonToBeObsoleteWorld.components.Remove(comp);

				if (!WorldSwitchUtility.planetkiller)
					WorldSwitchUtility.PastWorldTracker.PastWorlds.Add(
						WorldSwitchUtility.PreviousWorldFromWorld(WorldSwitchUtility.SoonToBeObsoleteWorld));
				else
					WorldSwitchUtility.planetkiller = false;

				Find.World.components.Remove(Find.World.components.Where(c => c is PastWorldUWO2).FirstOrDefault());
				Find.World.components.Add(WorldSwitchUtility.PastWorldTracker);
				foreach (UtilityWorldObject uwo in uwos)
				{
					Find.WorldObjects.Add(uwo);
				}

				WorldComponent toReplace;
				foreach (WorldComponent comp in modComps)
				{
					toReplace = null;
					foreach (WorldComponent otherComp in Find.World.components)
					{
						if (otherComp.GetType() == comp.GetType())
							toReplace = otherComp;
					}

					if (toReplace != null)
						Find.World.components.Remove(toReplace);
					Find.World.components.Add(comp);
				}

				if (!ModsConfig.IdeologyActive)
					WorldSwitchUtility.SelectiveWorldGenFlag = false;
				WorldSwitchUtility.CacheFactions(Current.CreatingWorld.info.name);
				WorldSwitchUtility.RespawnShip();

				RenderPlanetBehindMap.renderedThatAlready = false;

				//Prevent forced events from firing during the intervening years
				foreach (ScenPart part in Find.Scenario.AllParts)
				{
					if (part.def.defName.Equals("CreateIncident"))
					{
						Type createIncident = typeof(ScenPart).Assembly.GetType("RimWorld.ScenPart_CreateIncident");
						createIncident.GetField("occurTick", BindingFlags.Instance | BindingFlags.NonPublic)
							.SetValue(part,
								(float)createIncident
									.GetProperty("IntervalTicks", BindingFlags.Instance | BindingFlags.NonPublic)
									.GetValue(part, null) + Current.Game.tickManager.TicksAbs);
					}
				}

				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(WorldGenStep_Factions), "GenerateFresh")]
	public static class SelectiveWorldGenerationToo
	{
		[HarmonyPrefix]
		public static bool DontReplace()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				Find.GameInitData.playerFaction = WorldSwitchUtility.SavedPlayerFaction;
				Find.World.factionManager.Add(WorldSwitchUtility.SavedPlayerFaction);
			}

			return true;
		}

		[HarmonyPostfix]
		public static void LoadNow()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				WorldSwitchUtility.LoadUniqueIDsFactionsAndWorldPawns();
				foreach (Faction fac in Find.FactionManager.AllFactions)
				{
					if (fac.def.hidden)
					{
						foreach (Faction fac2 in Find.FactionManager.AllFactions)
						{
							if (fac != fac2)
							{
								fac.TryMakeInitialRelationsWith(fac2);
							}
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
	public static class DontRegenerateHiddenFactions
	{
		[HarmonyPrefix]
		public static bool PossiblyReplace()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				WorldSwitchUtility.FactionRelationFlag = true;
				return false;
			}

			return true;
		}

		[HarmonyPostfix]
		public static void Replace(Dictionary<FactionDef, int> factionCounts)
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				int i = 0;
				foreach (FactionDef current in DefDatabase<FactionDef>.AllDefs)
				{
					for (int j = 0; j < ((factionCounts != null && factionCounts.ContainsKey(current)) ? factionCounts[current] : current.requiredCountAtGameStart); j++)
					{
						if (!current.hidden)
						{
							Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(current));
							Find.FactionManager.Add(faction);
							i++;
						}
					}
				}

				IEnumerable<Faction> source = Find.World.factionManager.AllFactionsListForReading.Where((Faction x) => !x.def.isPlayer && !x.Hidden && !x.temporary);
				if (source.Any())
				{
					int num3 = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * new FloatRange(75f, 85f).RandomInRange * Find.World.info.overallPopulation.GetScaleFactor());
					num3 -= Find.WorldObjects.Settlements.Count;
					for (int j = 0; j < num3; j++)
					{
						Faction faction2 = source.RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
						Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
						settlement.SetFaction(faction2);
						settlement.Tile = TileFinder.RandomSettlementTileFor(faction2);
						settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
						Find.WorldObjects.Add(settlement);
					}
				}
				Find.IdeoManager.SortIdeos();

				WorldSwitchUtility.FactionRelationFlag = false;
			}
		}
	}

	[HarmonyPatch(typeof(Page_SelectScenario), "BeginScenarioConfiguration")]
	public static class DoNotWipeGame
	{
		[HarmonyPrefix]
		public static bool UseTheFlag()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MainMenuDrawer), "Init")]
	public static class SelectiveWorldGenCancel
	{
		[HarmonyPrefix]
		public static bool CancelFlag()
		{
			WorldSwitchUtility.SelectiveWorldGenFlag = false;
			return true;
		}
	}

	[HarmonyPatch(typeof(Page), "CanDoBack")]
	public static class NoGoingBackWhenMakingANewScenario
	{
		[HarmonyPostfix]
		public static void Nope(ref bool __result)
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(World), "GetUniqueLoadID")]
	public static class FixLoadID
	{
		[HarmonyPostfix]
		public static void NewID(World __instance, ref string __result)
		{
			__result = "World" + __instance.info.name;
		}
	}

	[HarmonyPatch(typeof(Game), "LoadGame")]
	public static class LoadPreviousWorlds
	{
		[HarmonyPrefix]
		public static bool PurgeIt()
		{
			WorldSwitchUtility.PurgePWT();
			return true;
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("AllFactionsVisible", MethodType.Getter)]
	public static class OnlyThisPlanetsVisibleFactions
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref IEnumerable<Faction> __result)
		{
			if (Current.ProgramState == ProgramState.Playing)
				__result = WorldSwitchUtility.FactionsOnCurrentWorld(__result).Where(x => !x.def.hidden);
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("AllFactions", MethodType.Getter)]
	public static class OnlyThisPlanetsFactions
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref IEnumerable<Faction> __result)
		{
			__result = WorldSwitchUtility.FactionsOnCurrentWorld(__result);
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("AllFactionsInViewOrder", MethodType.Getter)]
	public static class OnlyThisPlanetsFactionsInViewOrder
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref IEnumerable<Faction> __result)
		{
			__result = FactionManager.GetInViewOrder(WorldSwitchUtility.FactionsOnCurrentWorld(__result));
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("AllFactionsVisibleInViewOrder", MethodType.Getter)]
	public static class OnlyThisPlanetsFactionsVisibleInViewOrder
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref IEnumerable<Faction> __result)
		{
			__result = FactionManager.GetInViewOrder(WorldSwitchUtility.FactionsOnCurrentWorld(__result))
				.Where(x => !x.def.hidden);
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("AllFactionsListForReading", MethodType.Getter)]
	public static class OnlyThisPlanetsFactionsForReading
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref IEnumerable<Faction> __result)
		{
			__result = WorldSwitchUtility.FactionsOnCurrentWorld(__result);
		}
	}

	[HarmonyPatch(typeof(FactionManager), "GetFactions")]
	public static class NewFactionTempFix
	{
		public static void Postfix(ref IEnumerable<Faction> __result)
		{
			__result = WorldSwitchUtility.FactionsOnCurrentWorld(__result);
		}
	}

	[HarmonyPatch(typeof(FactionManager))]
	[HarmonyPatch("FirstFactionOfDef")]
	public static class OnlyThisPlanetsFirstFactions
	{
		[HarmonyPostfix]
		public static void FilterTheFactions(ref Faction __result, FactionDef facDef)
		{
			__result = Find.FactionManager.AllFactions.Where(x => x.def == facDef).FirstOrDefault();
		}
	}

	[HarmonyPatch(typeof(FactionManager), "RecacheFactions")]
	public static class NoRecache
	{
		[HarmonyPrefix]
		public static bool CheckFlag()
		{
			return !WorldSwitchUtility.NoRecache;
		}
	}

	[HarmonyPatch(typeof(Faction), "RelationWith")]
	public static class FactionRelationsAcrossWorlds
	{
		[HarmonyPrefix]
		public static bool RunOriginalMethod(Faction __instance, Faction other, out bool __state)
		{
			__state = false;
			if (Current.ProgramState != ProgramState.Playing)
				return true;
			if (__instance == Faction.OfPlayer || other == Faction.OfPlayer)
				return true;
			if (WorldSwitchUtility.PastWorldTracker.WorldFactions.Keys.Contains(Find.World.info.name))
			{
				if (WorldSwitchUtility.PastWorldTracker.WorldFactions[Find.World.info.name].myFactions
					.Contains(__instance.GetUniqueLoadID()) && WorldSwitchUtility.PastWorldTracker
					.WorldFactions[Find.World.info.name].myFactions.Contains(other.GetUniqueLoadID()))
					return true;
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void ReturnDummy(ref FactionRelation __result, bool __state)
		{
			if (__state)
			{
				__result = new FactionRelation();
			}
		}
	}

	[HarmonyPatch(typeof(ThingOwnerUtility), "GetAllThingsRecursively")]
	[HarmonyPatch(new Type[] { typeof(IThingHolder), typeof(bool) })]
	public static class FixThatPawnGenerationBug
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
				return false;
			return true;
		}

		[HarmonyPostfix]
		public static void ReturnEmptyList(ref List<Thing> __result)
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
			{
				__result = new List<Thing>();
			}
		}
	}

	[HarmonyPatch(typeof(ThingOwnerUtility), "GetAllThingsRecursively")]
	[HarmonyPatch(new Type[] { typeof(IThingHolder), typeof(List<Thing>), typeof(bool), typeof(Predicate<IThingHolder>) })]
	public static class FixThatPawnGenerationBug2
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
	public static class FixThatPawnGenerationBug3
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnGroupMakerUtility), "TryGetRandomFactionForCombatPawnGroup")]
	public static class NoRaidsFromPreviousPlanets
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void Replace(ref bool __result, float points, out Faction faction,
			Predicate<Faction> validator = null, bool allowNonHostileToPlayer = false, bool allowHidden = false,
			bool allowDefeated = false, bool allowNonHumanlike = true)
		{
			List<Faction> source = WorldSwitchUtility.FactionsOnCurrentWorld(Find.FactionManager.AllFactions).Where(
				delegate (Faction f) {
					int arg_E3_0;
					if ((allowHidden || !f.def.hidden) && (allowDefeated || !f.defeated) &&
						(allowNonHumanlike || f.def.humanlikeFaction) &&
						(allowNonHostileToPlayer || f.HostileTo(Faction.OfPlayer)) &&
						f.def.pawnGroupMakers != null)
					{
						if (f.def.pawnGroupMakers.Any((PawnGroupMaker x) =>
							x.kindDef == PawnGroupKindDefOf.Combat) && (validator == null || validator(f)))
						{
							arg_E3_0 = ((points >= f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
								? 1
								: 0);
							return arg_E3_0 != 0;
						}
					}

					arg_E3_0 = 0;
					return arg_E3_0 != 0;
				}).ToList<Faction>();
			__result = source.TryRandomElementByWeight((Faction f) => f.def.RaidCommonalityFromPoints(points),
				out faction);
		}
	}

	[HarmonyPatch(typeof(WorldGrid), "RawDataToTiles")]
	public static class FixWorldLoadBug
	{
		[HarmonyPrefix]
		public static bool SelectiveLoad()
		{
			return !WorldSwitchUtility.LoadWorldFlag;
		}
	}
	
	//mechanite "fire"
	[HarmonyPatch(typeof(Fire), "TrySpread")]
	public static class SpreadMechanites
	{
		public static bool Prefix(Fire __instance)
		{
			if (__instance is MechaniteFire)
				return false;
			return true;
		}

		public static void Postfix(Fire __instance)
		{
			if (__instance is MechaniteFire)
			{
				IntVec3 position = __instance.Position;
				bool flag;
				if (Rand.Chance(0.8f))
				{
					position = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
					flag = true;
				}
				else
				{
					position = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
					flag = false;
				}
				if (!position.InBounds(__instance.Map))
				{
					return;
				}
				if (!flag)
				{
					CellRect startRect = CellRect.SingleCell(__instance.Position);
					CellRect endRect = CellRect.SingleCell(position);
					if (GenSight.LineOfSight(__instance.Position, position, __instance.Map, startRect, endRect))
					{
						((MechaniteSpark)GenSpawn.Spawn(ThingDef.Named("MechaniteSpark"), __instance.Position, __instance.Map)).Launch(__instance, position, position, ProjectileHitFlags.All);
					}
				}
				else
				{
					MechaniteFire existingFire = position.GetFirstThing<MechaniteFire>(__instance.Map);
					if (existingFire != null)
					{
						existingFire.fireSize += 0.1f;
					}
					else
					{
						MechaniteFire obj = (MechaniteFire)ThingMaker.MakeThing(ShipInteriorMod2.MechaniteFire);
						obj.fireSize = Rand.Range(0.1f, 0.2f);
						GenSpawn.Spawn(obj, position, __instance.Map, Rot4.North);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
	public static class ComplexFlammability
	{
		public static bool Prefix(Fire __instance)
		{
			if (__instance is MechaniteFire)
				return false;
			return true;
		}
		public static void Postfix(Fire __instance)
		{
			if (__instance is MechaniteFire)
			{
				bool flag = false;
				List<Thing> flammableList = new List<Thing>();
				if (__instance.parent == null)
				{
					List<Thing> list = __instance.Map.thingGrid.ThingsListAt(__instance.Position);
					for (int i = 0; i < list.Count; i++)
					{
						Thing thing = list[i];
						if (thing is Building_Door)
						{
							flag = true;
						}
						if (!(thing is MechaniteFire) && thing.def.useHitPoints)
						{
							flammableList.Add(list[i]);
							if (__instance.parent == null && __instance.fireSize > 0.4f && list[i].def.category == ThingCategory.Pawn && Rand.Chance(FireUtility.ChanceToAttachFireCumulative(list[i], 150f)))
							{
								list[i].TryAttachFire(__instance.fireSize * 0.2f);
							}
						}
					}
				}
				else
				{
					flammableList.Add(__instance.parent);
				}
				if (flammableList.Count == 0 && __instance.Position.GetTerrain(__instance.Map).extinguishesFire)
				{
					__instance.Destroy();
					return;
				}
				Thing thing2 = (__instance.parent != null) ? __instance.parent : ((flammableList.Count <= 0) ? null : flammableList.RandomElement());
				if (thing2 != null && (!(__instance.fireSize < 0.4f) || thing2 == __instance.parent || thing2.def.category != ThingCategory.Pawn))
				{
					IntVec3 pos = __instance.Position;
					Map map = __instance.Map;
					((MechaniteFire)__instance).DoFireDamage(thing2);
					if (thing2.Destroyed)
						GenExplosion.DoExplosion(pos, map, 1.9f, DefDatabase<DamageDef>.GetNamed("BombMechanite"), null);
				}
				if (__instance.Spawned)
				{
					float num = __instance.fireSize * 16f;
					if (flag)
					{
						num *= 0.15f;
					}
					GenTemperature.PushHeat(__instance.Position, __instance.Map, num);
					if (Rand.Value < 0.4f)
					{
						float radius = __instance.fireSize * 3f;
						SnowUtility.AddSnowRadial(__instance.Position, __instance.Map, radius, 0f - __instance.fireSize * 0.1f);
					}
					__instance.fireSize += 0.1f;
					if (__instance.fireSize > 1.75f)
					{
						__instance.fireSize = 1.75f;
					}
					if (__instance.Map.weatherManager.RainRate > 0.01f && Rand.Value < 6f)
					{
						__instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f));
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
	public static class FixFireBugA
	{
		public static void Postfix(Thing item)
		{
			if (item.HasAttachment(ShipInteriorMod2.MechaniteFire))
			{
				item.GetAttachment(ShipInteriorMod2.MechaniteFire).Destroy();
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_JobTracker), "IsCurrentJobPlayerInterruptible")]
	public static class FixFireBugB
	{
		public static void Postfix(Pawn_JobTracker __instance, ref bool __result)
		{
			if (((Pawn)(typeof(Pawn_JobTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))).HasAttachment(ShipInteriorMod2.MechaniteFire))
			{
				__result = false;
			}
		}
	}

	//[HarmonyPatch(typeof(JobGiver_FightFiresNearPoint),"TryGiveJob")]
	public class FixFireBugC //Manually patched since *someone* made this an internal class!
	{
		public void Postfix(ref Job __result, Pawn pawn)
		{
			Thing thing = GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, pawn.Map, ThingRequest.ForDef(ShipInteriorMod2.MechaniteFire), PathEndMode.Touch, TraverseParms.For(pawn), 25);
			if (thing != null)
			{
				__result = JobMaker.MakeJob(JobDefOf.BeatFire, thing);
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_ExtinguishSelf), "TryGiveJob")]
	public static class FixFireBugD
	{
		public static void Postfix(Pawn pawn, ref Job __result)
		{
			if (Rand.Value < 0.1f)
			{
				Fire fire = (Fire)pawn.GetAttachment(ShipInteriorMod2.MechaniteFire);
				if (fire != null)
				{
					__result = JobMaker.MakeJob(JobDefOf.ExtinguishSelf, fire);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ThinkNode_ConditionalBurning), "Satisfied")]
	public static class FixFireBugE
	{
		public static void Postfix(Pawn pawn, ref bool __result)
		{
			__result = __result || pawn.HasAttachment(ShipInteriorMod2.MechaniteFire);
		}
	}

	[HarmonyPatch(typeof(Fire), "SpawnSmokeParticles")]
	public static class FixFireBugF
	{
		public static bool Prefix(Fire __instance)
		{
			return !(__instance is MechaniteFire);
		}
	}

	//archo
	[HarmonyPatch(typeof(IncidentWorker_FarmAnimalsWanderIn), "TryFindRandomPawnKind")]
	public static class NoArchoCritters
	{
		public static void Postfix(ref PawnKindDef kind, ref bool __result, Map map)
		{
			__result = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef x) => x.RaceProps.Animal && x.RaceProps.wildness < 0.35f && (!x.race.tradeTags?.Contains("AnimalInsectSpace") ?? true) && map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(x.race)).TryRandomElementByWeight((PawnKindDef k) => 0.420000017f - k.RaceProps.wildness, out kind);
		}
	}

	[HarmonyPatch(typeof(ScenPart_StartingAnimal), "RandomPets")]
	public static class NoArchotechPets
	{
		public static void Postfix(ref IEnumerable<PawnKindDef> __result)
		{
			List<PawnKindDef> newResult = new List<PawnKindDef>();
			foreach (PawnKindDef def in __result)
			{
				if (!def.race.HasComp(typeof(CompArcholife)))
					newResult.Add(def);
			}
			__result = newResult;
		}
	}

	[HarmonyPatch(typeof(MainTabWindow_Research), "PostOpen")]
	public static class HideArchoStuff
	{
		public static void Postfix(MainTabWindow_Research __instance)
		{
			if (!WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechUplink"))
			{
				IEnumerable tabs = (IEnumerable)typeof(MainTabWindow_Research).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				TabRecord archoTab = null;
				foreach (TabRecord tab in tabs)
				{
					if (tab.label.Equals("Archotech"))
						archoTab = tab;
				}
				tabs.GetType().GetMethod("Remove").Invoke(tabs, new object[] { archoTab });
			}
		}
	}

	[HarmonyPatch(typeof(Widgets), "RadioButtonLabeled")]
	public static class HideArchoStuffToo
	{
		public static bool Prefix(string labelText)
		{
			if (labelText.Equals("Sacrifice to archotech spore") && !WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechUplink"))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
	public static class DrawArchotechGifts
	{
		public static void Postfix(ref float __result, ref Rect rect, ResearchProjectDef project)
		{
			float yMin = rect.yMin;
			bool first = false;
			foreach (ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
			{
				if (def.research == project)
				{
					if (!first)
					{
						first = true;
						Widgets.LabelCacheHeight(ref rect, TranslatorFormattedStringExtensions.Translate("ArchoGift") + ":");
						rect.yMin += 24f;
					}
					Widgets.HyperlinkWithIcon(hyperlink: new Dialog_InfoCard.Hyperlink(def.thing), rect: new Rect(rect.x, rect.yMin, rect.width, 24f));
					rect.yMin += 24f;
				}
			}
			__result = rect.yMin - yMin + __result;
		}
	}

	[HarmonyPatch(typeof(JobDriver_Meditate), "MeditationTick")]
	public static class MeditateToArchotechs
	{
		public static void Postfix(JobDriver_Meditate __instance)
		{
			int num = GenRadial.NumCellsInRadius(MeditationUtility.FocusObjectSearchRadius);
			for (int i = 0; i < num; i++)
			{
				IntVec3 c = __instance.pawn.Position + GenRadial.RadialPattern[i];
				if (c.InBounds(__instance.pawn.Map))
				{
					Building_ArchotechSpore spore = c.GetFirstThing<Building_ArchotechSpore>(__instance.pawn.Map);
					if (spore != null)
					{
						spore.MeditationTick();
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(RitualObligationTargetWorker_GraveWithTarget), "LabelExtraPart")]
	public static class NoDeathSpam
	{
		public static bool Prefix(RitualObligation obligation)
		{
			return obligation.targetA.Thing != null && obligation.targetA.Thing is Corpse && ((Corpse)obligation.targetA.Thing).InnerPawn != null;

		}
	}

	[HarmonyPatch(typeof(RitualObligationTargetWorker_Altar), "GetTargetsWorker")]
	public static class ArchotechSporesAreHoly
	{
		public static void Postfix(RitualObligation obligation, Map map, Ideo ideo, ref IEnumerable<TargetInfo> __result)
		{
			if (ideo.memes.Contains(ShipInteriorMod2.Archism) && map.listerThings.ThingsOfDef(ShipInteriorMod2.ArchotechSpore).Any())
			{
				List<TargetInfo> newResult = new List<TargetInfo>();
				newResult.AddRange(__result);
				foreach (Thing spore in map.listerThings.ThingsOfDef(ShipInteriorMod2.ArchotechSpore))
				{
					newResult.Add(spore);
				}
				__result = newResult;
			}
		}
	}

	[HarmonyPatch(typeof(IdeoBuildingPresenceDemand), "BuildingPresent")]
	public static class ArchotechSporesCountAsAltars
	{
		public static void Postfix(ref bool __result, Map map, IdeoBuildingPresenceDemand __instance)
		{
			if (__instance.parent.ideo.memes.Contains(ShipInteriorMod2.Archism) && map.listerThings.ThingsOfDef(ShipInteriorMod2.ArchotechSpore).Any())
				__result = true;
		}
	}

	[HarmonyPatch(typeof(IdeoBuildingPresenceDemand), "RequirementsSatisfied")]
	public static class ArchotechSporesCountAsAltarsToo
	{
		public static void Postfix(ref bool __result, Map map, IdeoBuildingPresenceDemand __instance)
		{
			if (__instance.parent.ideo.memes.Contains(ShipInteriorMod2.Archism) && map.listerThings.ThingsOfDef(ShipInteriorMod2.ArchotechSpore).Any())
				__result = true;
		}
	}

	[HarmonyPatch(typeof(ExecutionUtility), "DoExecutionByCut")]
	public static class ArchotechSporesAbsorbBrains
	{
		public static void Postfix(Pawn victim)
		{
			Building_ArchotechSpore ArchotechSpore = victim.Corpse.Position.GetFirstThing<Building_ArchotechSpore>(victim.Corpse.Map);
			if (ArchotechSpore != null)
			{
				SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
				FleckMaker.Static(ArchotechSpore.Position, victim.Corpse.Map, FleckDefOf.PsycastAreaEffect, 10f);
				victim.health.AddHediff(HediffDefOf.MissingBodyPart, victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).First());
				ArchotechSpore.AbsorbMind(victim);
			}
		}
	}

	[HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
	public static class AddArchoDialogOption
	{
		public static void Postfix(Pawn negotiator, Faction faction, ref DiaNode __result)
		{
			if (faction.def.CanEverBeNonHostile && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("ArchotechBroadManipulation")) >= ResearchProjectDef.Named("ArchotechBroadManipulation").CostApparent)
			{
				Building_ArchotechSpore spore = null;
				foreach (Map map in Find.Maps)
				{
					if (map.IsSpace())
					{
						foreach (Thing t in map.spawnedThings)
						{
							if (t is Building_ArchotechSpore)
							{
								spore = (Building_ArchotechSpore)t;
								break;
							}
						}
					}
				}
				DiaOption increase = new DiaOption(TranslatorFormattedStringExtensions.Translate("ArchotechGoodwillPlus"));
				DiaOption decrease = new DiaOption(TranslatorFormattedStringExtensions.Translate("ArchotechGoodwillMinus"));
				increase.action = delegate
				{
					faction.TryAffectGoodwillWith(Faction.OfPlayer, 10, canSendMessage: false);
					spore.fieldStrength -= 3;
				};
				increase.linkLateBind = (() => FactionDialogMaker.FactionDialogFor(negotiator, faction));
				if (spore == null || spore.fieldStrength < 3)
				{
					increase.disabled = true;
					increase.disabledReason = "Insufficient psychic field strength";
				}
				decrease.action = delegate
				{
					faction.TryAffectGoodwillWith(Faction.OfPlayer, -10, canSendMessage: false);
					spore.fieldStrength -= 3;
				};
				decrease.linkLateBind = (() => FactionDialogMaker.FactionDialogFor(negotiator, faction));
				if (spore == null || spore.fieldStrength < 3)
				{
					decrease.disabled = true;
					decrease.disabledReason = "Insufficient psychic field strength";
				}
				if (spore != null)
				{
					__result.options.Add(increase);
					__result.options.Add(decrease);
				}
			}
		}
	}

	//ideology
	[HarmonyPatch(typeof(IdeoManager), "CanRemoveIdeo")]
	public static class IdeosDoNotDisappear
	{
		public static void Postfix(Ideo ideo, ref bool __result)
		{
			List<Faction> factions = (List<Faction>)typeof(FactionManager).GetField("allFactions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Find.FactionManager);
			foreach (Faction allFaction in factions)
			{
				if (allFaction.ideos != null && allFaction.ideos.AllIdeos.Contains(ideo))
				{
					__result = false;
					return;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Scenario), "PostIdeoChosen")]
	public static class NotNowIdeology
	{
		public static bool ArchoFlag = false;

		public static bool Prefix()
		{
			if (ArchoFlag)
			{
				ArchoFlag = false;
				return false;
			}
			return true;
		}
	}
	
	//holographic race
	[HarmonyPatch(typeof(Pawn_ApparelTracker))]
	[HarmonyPatch("PsychologicallyNude", MethodType.Getter)]
	public static class HologramNudityIsOkay
	{
		[HarmonyPostfix]
		public static void DoNotFearMyHolographicJunk(Pawn_ApparelTracker __instance, ref bool __result)
		{
			if (ShipInteriorMod2.IsHologram(__instance.pawn))
			{
				__result = __instance.pawn.story.traits.HasTrait(TraitDefOf.Nudist);
			}
		}
	}

	[HarmonyPatch(typeof(ApparelUtility), "HasPartsToWear")]
	public static class HologramsCannotWear
    {
		public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
			if (ShipInteriorMod2.IsHologram(p) && apparel.thingClass != typeof(ApparelHolographic) && !apparel.apparel.tags.Contains("HologramGear"))
				__result = false;
        }
    }

	[HarmonyPatch(typeof(Pawn),"Kill")]
	public static class CorpseRemoval
    {
		public static void Postfix(Pawn __instance)
        {
			if(ShipInteriorMod2.IsHologram(__instance))
            {
				if(__instance.Corpse!=null)
					__instance.Corpse.Destroy();
				if(!__instance.health.hediffSet.GetHediffs<HediffPawnIsHologram>().FirstOrDefault().consciousnessSource.Destroyed)
				ResurrectionUtility.Resurrect(__instance);
            }
        }
    }

	[HarmonyPatch(typeof(MeditationUtility), "CanMeditateNow")]
	public static class HologramsCanMeditate
    {
		public static bool Prefix(Pawn pawn)
        {
			return !ShipInteriorMod2.IsHologram(pawn);
        }

		public static void Postfix(Pawn pawn, ref bool __result)
        {
			if(ShipInteriorMod2.IsHologram(pawn))
            {
				__result = pawn.Awake();
			}
        }
    }

	[HarmonyPatch(typeof(GatheringsUtility), "ShouldGuestKeepAttendingGathering")]
	public static class HologramsCanParty
	{
		public static bool Prefix(Pawn p, ref bool __result)
		{
			if (p.needs?.food == null || p.needs?.rest == null)
			{
				return HologramResult(p, out __result);
			}
			return true;
		}

		private static bool HologramResult(Pawn p, out bool __result)
		{
			__result = (!p.Downed && (p.needs?.food == null || !p.needs.food.Starving) && p.health.hediffSet.BleedRateTotal <= 0f && (p.needs?.rest == null || (int)p.needs.rest.CurCategory < 3) && !p.health.hediffSet.HasTendableNonInjuryNonMissingPartHediff(false) && RestUtility.Awake(p) && !p.InAggroMentalState && !p.IsPrisoner);
			return false;
		}
	}

	[HarmonyPatch(typeof(LovePartnerRelationUtility), "GetLovinMtbHours")]
	public static class HologramsCanGetSome
    {
		public static bool Prefix(Pawn pawn, Pawn partner)
        {
			return !ShipInteriorMod2.IsHologram(pawn) && !ShipInteriorMod2.IsHologram(partner);
        }

		public static void Postfix(Pawn pawn, Pawn partner, ref float __result)
        {
			if(ShipInteriorMod2.IsHologram(pawn) || ShipInteriorMod2.IsHologram(partner))
            {
				if (pawn.Dead || partner.Dead)
				{
					__result = - 1f;
					return;
				}
				if (DebugSettings.alwaysDoLovin)
				{
					__result = 0.1f;
					return;
				}
				if ((pawn.needs.food != null && pawn.needs.food.Starving) || (partner.needs.food !=null && partner.needs.food.Starving))
				{
					__result = -1f;
					return;
				}
				if (pawn.health.hediffSet.BleedRateTotal > 0f || partner.health.hediffSet.BleedRateTotal > 0f)
				{
					__result = -1f;
					return;
				}
				MethodInfo SingleFactor = typeof(LovePartnerRelationUtility).GetMethod("LovinMtbSinglePawnFactor", BindingFlags.NonPublic | BindingFlags.Static);
				float num = (float)SingleFactor.Invoke(null, new object[] { pawn });
				if (num <= 0f)
				{
					__result = -1f;
					return;
				}
				float num2 = (float)SingleFactor.Invoke(null, new object[] { partner });
				if (num2 <= 0f)
				{
					__result = -1f;
					return;
				}
				float num3 = 12f;
				num3 *= num;
				num3 *= num2;
				num3 /= Mathf.Max(pawn.relations.SecondaryLovinChanceFactor(partner), 0.1f);
				num3 /= Mathf.Max(partner.relations.SecondaryLovinChanceFactor(pawn), 0.1f);
				num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, pawn.relations.OpinionOf(partner));
				num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, partner.relations.OpinionOf(pawn));
				if (pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicLove))
				{
					num3 /= 4f;
				}
				__result = num3;
			}
        }
    }

	[HarmonyPatch(typeof(Pawn_RelationsTracker), "SecondaryLovinChanceFactor")]
	public static class HologramsAreRomantic
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			if (ModLister.HasActiveModWithName("Humanoid Alien Races"))
				return instructions;

			List<CodeInstruction> newInstructions = new List<CodeInstruction>();

			newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
			newInstructions.Add(CodeInstruction.LoadField(typeof(Pawn_RelationsTracker), "pawn"));
			newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			newInstructions.Add(CodeInstruction.Call(typeof(HologramsAreRomantic), "AreCompatible"));
			newInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4, 0));

			int index = 0;
			foreach(CodeInstruction instr in instructions)
            {
				if (index >= 9)
					newInstructions.Add(instr);
				index++;
            }
			return newInstructions;
		}

		public static int AreCompatible(Pawn pawnA, Pawn pawnB)
        {
			return (pawnA != pawnB && pawnA.RaceProps.intelligence == Intelligence.Humanlike && pawnB.RaceProps.intelligence == Intelligence.Humanlike) ? 1 : 0;
        }
	}

	[HarmonyPatch(typeof(Pawn_RelationsTracker), "CompatibilityWith")]
	public static class HologramsAreCompatible
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			if (ModLister.HasActiveModWithName("Humanoid Alien Races"))
				return instructions;

			List<CodeInstruction> newInstructions = new List<CodeInstruction>();

			newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
			newInstructions.Add(CodeInstruction.LoadField(typeof(Pawn_RelationsTracker),"pawn"));
			newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
			newInstructions.Add(CodeInstruction.Call(typeof(HologramsAreRomantic),"AreCompatible"));
			newInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4, 0));

			int index = 0;
			foreach (CodeInstruction instr in instructions)
			{
				if (index >= 9)
					newInstructions.Add(instr);
				index++;
			}
			return newInstructions;
		}
	}

	[HarmonyPatch(typeof(CaravanFormingUtility), "AllSendablePawns")]
	public static class NoEscapeForHolograms
    {
		public static void Postfix(ref List<Pawn> __result)
        {
			List<Pawn> newList = new List<Pawn>();
			foreach(Pawn pawn in __result)
            {
				if (!ShipInteriorMod2.IsHologram(pawn))
					newList.Add(pawn);
            }
			__result = newList;
        }
    }

	[HarmonyPatch(typeof(PawnRenderer), "ShellFullyCoversHead")]
	public static class DoesNotActuallyCoverHead
    {
		public static void Postfix(PawnRenderer __instance, ref bool __result)
        {
			foreach(ApparelGraphicRecord apparel in __instance.graphics.apparelGraphics)
			{
				if (apparel.sourceApparel.def == ShipInteriorMod2.HoloEmitterDef)
					__result = false;
			}
		}
    }

	[HarmonyPatch(typeof(ThoughtWorker_AgeReversalDemanded), "CanHaveThought")]
	public static class NoHologramAgeReversal
    {
		public static void Postfix(ref bool __result, Pawn pawn)
        {
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = false;
        }
    }

	[HarmonyPatch(typeof(Apparel), "get_WornGraphicPath")]
	public static class HologramClothingAppearance
    {
		public static void Postfix(ref string __result, Apparel __instance)
        {
			if(__instance is ApparelHolographic)
            {
				__result = ((ApparelHolographic)__instance).apparelToMimic.apparel.wornGraphicPath;
            }
        }
    }

	[HarmonyPatch(typeof(ApparelRequirement), "IsMet")]
	public static class HoloApparelIdeologyRole
    {
		public static void Postfix(ref bool __result, ApparelRequirement __instance, Pawn p)
        {
			foreach (Apparel itemApparel in p.apparel.WornApparel)
			{
				if (itemApparel is ApparelHolographic)
				{
					ApparelHolographic item = (ApparelHolographic)itemApparel;
					bool flag = false;
					for (int i = 0; i < __instance.bodyPartGroupsMatchAny.Count; i++)
					{
						if (item.apparelToMimic.apparel.bodyPartGroups.Contains(__instance.bodyPartGroupsMatchAny[i]))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						if (__instance.requiredDefs != null && __instance.requiredDefs.Contains(item.apparelToMimic))
						{
							__result = true;
							return;
						}
						if (__instance.requiredTags != null)
						{
							for (int j = 0; j < __instance.requiredTags.Count; j++)
							{
								if (item.apparelToMimic.apparel.tags.Contains(__instance.requiredTags[j]))
								{
									__result = true;
									return;
								}
							}
						}
						if (__instance.allowedTags != null)
						{
							for (int k = 0; k < __instance.allowedTags.Count; k++)
							{
								if (item.apparelToMimic.apparel.tags.Contains(__instance.allowedTags[k]))
								{
									__result = true;
									return;
								}
							}
						}
					}
				}
			}
		}
    }

	[HarmonyPatch(typeof(IncidentWorker_DiseaseHuman), "PotentialVictimCandidates")]
	public static class HologramsDontGetDiseases
    {
		public static void Postfix(ref IEnumerable<Pawn> __result)
        {
			List<Pawn> newResult = new List<Pawn>();
			foreach(Pawn pawn in __result)
            {
				if (!ShipInteriorMod2.IsHologram(pawn))
					newResult.Add(pawn);
            }
			__result = newResult;
        }
    }

	[HarmonyPatch(typeof(ImmunityHandler), "AnyHediffMakesFullyImmuneTo")]
	public static class HologramsDontGetDiseasesToo
    {
		public static void Postfix(ref bool __result, ImmunityHandler __instance, ref Hediff sourceHediff)
        {
			if (ShipInteriorMod2.IsHologram(__instance.pawn))
			{
				__result = true;
				sourceHediff = __instance.pawn.health.hediffSet.GetHediffs<HediffPawnIsHologram>().FirstOrDefault();
			}
        }
    }

	[HarmonyPatch(typeof(SkillRecord), "Interval")]
	public static class MachineHologramsPerfectMemory
    {
		public static bool Prefix(SkillRecord __instance)
        {
			return !ShipInteriorMod2.IsHologram(__instance.Pawn);
        }
    }

	[HarmonyPatch(typeof(SickPawnVisitUtility), "CanVisit")]
	public static class NoVisitingHolograms
    {
		public static void Postfix(Pawn sick, ref bool __result)
        {
			if (ShipInteriorMod2.IsHologram(sick))
				__result = false;
        }
    }

	[HarmonyPatch(typeof(WorkGiver_Warden_DeliverFood), "JobOnThing")]
	public static class NoFeedingTheHolograms
	{
		public static bool Prefix(Thing t)
		{
			return !(t is Pawn) || !ShipInteriorMod2.IsHologram((Pawn)t);
		}

		public static void Postfix(Thing t, ref Job __result)
		{
			if (t is Pawn && ShipInteriorMod2.IsHologram((Pawn)t))
				__result = null;
		}
	}

	[HarmonyPatch(typeof(Pawn_StoryTracker), "get_SkinColor")]
	public static class SkinColorPostfixPostfix
    {
		[HarmonyPriority(Priority.Last)]
		public static void Postfix(Pawn ___pawn, ref Color __result, Pawn_StoryTracker __instance)
        {
			if (ShipInteriorMod2.IsHologram(___pawn))
				__result = __instance.skinColorOverride.Value;
        }
    }

	[HarmonyPatch(typeof(GenStep_Fog), "Generate")]
	public static class UnfogVault
    {
		public static void Postfix(Map map)
        {
			foreach (Thing casket in map.listerThings.ThingsOfDef(ThingDef.Named("Ship_AvatarCasket")))
			{
				FloodFillerFog.FloodUnfog(casket.Position, map);
			}
		}
    }

	[HarmonyPatch(typeof(JobDriver_InteractAnimal), "RequiredNutritionPerFeed")]
	public static class TameWildHologram
	{
		public static bool Prefix(Pawn animal)
		{
			return animal.needs.food != null;
		}

		public static void Postfix(Pawn animal, ref float __result)
		{
			if (animal.needs.food == null)
				__result = 0;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_InteractAnimal), "HasFoodToInteractAnimal")]
	public static class TameWildHologramToo
	{
		public static bool Prefix(Pawn tamee)
		{
			return tamee.needs.food != null;
		}

		public static void Postfix(Pawn tamee, ref bool __result)
		{
			if (tamee.needs.food == null)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_InteractAnimal), "TakeFoodForAnimalInteractJob")]
	public static class TameWildHologramThree
	{
		public static bool Prefix(Pawn tamee)
		{
			return tamee.needs.food != null;
		}

		public static void Postfix(Pawn tamee, ref Job __result)
		{
			if (tamee.needs.food == null)
			{
				__result = JobMaker.MakeJob(JobDefOf.Goto, tamee.Position);
			}
		}
	}

	[HarmonyPatch(typeof(JobDriver_InteractAnimal), "StartFeedAnimal")]
	public static class TameWildHologramFour
	{
		public static bool Prefix(JobDriver_InteractAnimal __instance, TargetIndex tameeInd)
		{
			return ((Pawn)__instance.pawn.CurJob.GetTarget(tameeInd)).needs.food != null;
		}

		public static void Postfix(JobDriver_InteractAnimal __instance, TargetIndex tameeInd, ref Toil __result)
		{
			if (((Pawn)__instance.pawn.CurJob.GetTarget(tameeInd)).needs.food == null)
				__result = Toils_General.Wait(10);
		}
	}

	[HarmonyPatch(typeof(JobDriver_InteractAnimal), "FeedToils")]
	public static class TameWildHologramFive
	{
		public static bool Prefix(JobDriver_InteractAnimal __instance)
		{
			return ((Pawn)__instance.job.targetA.Thing).needs.food != null;
		}

		public static void Postfix(JobDriver_InteractAnimal __instance, ref IEnumerable<Toil> __result)
		{
			if (((Pawn)__instance.job.targetA.Thing).needs.food == null)
			{
				List<Toil> newResult = new List<Toil>();
				newResult.Add(Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch));
				__result = newResult;
			}
		}
	}

	[HarmonyPatch(typeof(Toils_Ingest), "FinalizeIngest")]
	public static class HologramsDoNotEatIMeanItSeriously
	{
		public static void Postfix(Pawn ingester, TargetIndex ingestibleInd, ref Toil __result)
		{
			if (ingester.needs.food == null)
			{
				__result.initAction = delegate
				{
					Pawn actor = ingester;
					Job curJob = actor.jobs.curJob;
					Thing thing = curJob.GetTarget(ingestibleInd).Thing;
					thing.Ingested(ingester, 0);
				};
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_EatInGatheringArea), "TryGiveJob")]
	public static class HologramsParty
	{
		public static bool Prefix(Pawn pawn)
		{
			return pawn.needs.food != null;
		}

		public static void Postfix(Pawn pawn, ref Job __result)
		{
			if (pawn.needs.food == null)
			{
				__result = null;
			}
		}
	}

	[HarmonyPatch(typeof(PawnApparelGenerator), "CanUsePair")]
	public static class NoHolographicGearOnGeneratedPawns
	{
		public static void Postfix(ThingStuffPair pair, Pawn pawn, ref bool __result)
		{
			if (pair.thing.IsApparel && pair.thing.apparel.tags != null && pair.thing.apparel.tags.Contains("HologramGear") && pawn.health.hediffSet.GetHediffs<HediffPawnIsHologram>().Count() == 0)
			{
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(PawnWeaponGenerator), "GetWeaponCommonalityFromIdeo")]
	public static class NoHolographicWeaponsOnGeneratedPawns
	{
		static WeaponClassDef hologramClass = DefDatabase<WeaponClassDef>.GetNamed("HologramGear");

		public static void Postfix(ThingStuffPair pair, Pawn pawn, ref float __result)
		{
			if (pair.thing.IsWeapon && pair.thing.weaponClasses != null && pair.thing.weaponClasses.Contains(hologramClass) && pawn.health.hediffSet.GetHediffs<HediffPawnIsHologram>().Count() == 0)
			{
				__result = 0f;
			}
		}
	}
	
	//storyteller
	[HarmonyPatch(typeof(Map), "get_PlayerWealthForStoryteller")]
	public static class TechIsWealth
	{
		static SimpleCurve wealthCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0,0), new CurvePoint(3800,0), new CurvePoint(150000,400000f), new CurvePoint(420000,700000f), new CurvePoint(666666,1000000f)});
		static SimpleCurve componentCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0,0), new CurvePoint(10,5000), new CurvePoint(100, 25000), new CurvePoint(1000, 150000) });

		public static void Postfix(Map __instance, ref float __result)
        {
			if (Find.Storyteller.def != ShipInteriorMod2.Sara)
				return;
			float num = ResearchToWealth();
			int numComponents = 0;
			foreach (Building building in __instance.listerBuildings.allBuildingsColonist.Where(b => b.def.costList != null))
			{
				if (building.def.costList.Any(tdc => tdc.thingDef == ThingDefOf.ComponentIndustrial))
					numComponents++;
				if (building.def.costList.Any(tdc => tdc.thingDef == ThingDefOf.ComponentSpacer))
					numComponents += 10;
			}
			num += componentCurve.Evaluate(numComponents);
			//Log.Message("Sara Spacer calculates threat points should be " + wealthCurve.Evaluate(num) + " based on " + ResearchToWealth() + " research and " + numComponents + " component-based buildings");
			__result = wealthCurve.Evaluate(num);
        }

		static float ResearchToWealth()
        {
			float num = 0;
			foreach(ResearchProjectDef proj in DefDatabase<ResearchProjectDef>.AllDefs)
            {
				if (proj.IsFinished)
					num += proj.baseCost;
			}
			if (num > 100000)
				num = 100000;
			return num;
        }
    }

	//progression
	[HarmonyPatch(typeof(Scenario))]
	[HarmonyPatch("Category", MethodType.Getter)]
	public static class FixThatBugInParticular
	{
		[HarmonyPrefix]
		public static bool NoLongerUndefined(Scenario __instance)
		{
			if (((ScenarioCategory)typeof(Scenario).GetField("categoryInt", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(__instance)) == ScenarioCategory.Undefined)
				typeof(Scenario).GetField("categoryInt", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(__instance, ScenarioCategory.CustomLocal);
			return true;
		}
	}

	[HarmonyPatch(typeof(MapParent), "RecalculateHibernatableIncidentTargets")]
	public static class GiveMeRaidsPlease
	{
		[HarmonyPostfix]
		public static void RaidsAreFunISwear(MapParent __instance)
		{
			HashSet<IncidentTargetTagDef> hibernatableIncidentTargets =
				(HashSet<IncidentTargetTagDef>)typeof(MapParent)
					.GetField("hibernatableIncidentTargets", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(__instance);
			foreach (ThingWithComps current in __instance.Map.listerThings
				.ThingsOfDef(ThingDef.Named("JTDriveSalvage")).OfType<ThingWithComps>())
			{
				CompHibernatableSoS compHibernatable = current.TryGetComp<CompHibernatableSoS>();
				if (compHibernatable != null && compHibernatable.State == HibernatableStateDefOf.Starting &&
					compHibernatable.Props.incidentTargetWhileStarting != null)
				{
					if (hibernatableIncidentTargets == null)
					{
						hibernatableIncidentTargets = new HashSet<IncidentTargetTagDef>();
					}

					hibernatableIncidentTargets.Add(compHibernatable.Props.incidentTargetWhileStarting);
				}
			}

			typeof(MapParent)
				.GetField("hibernatableIncidentTargets", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(__instance, hibernatableIncidentTargets);
		}
	}

	[HarmonyPatch(typeof(Designator_Build)), HarmonyPatch("Visible", MethodType.Getter)]
	public static class UnlockBuildings
	{
		[HarmonyPostfix]
		public static void Unlock(ref bool __result, Designator_Build __instance)
		{
			if (__instance.PlacingDef is ThingDef && ((ThingDef)__instance.PlacingDef).HasComp(typeof(CompSoSUnlock)))
			{
				if (WorldSwitchUtility.PastWorldTracker.Unlocks.Contains(((ThingDef)__instance.PlacingDef).GetCompProperties<CompProperties_SoSUnlock>().unlock) || DebugSettings.godMode)
					__result = true;
				else
					__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(Page_SelectStartingSite), "CanDoNext")]
	public static class LetMeLandOnMyOwnBase
	{
		[HarmonyPrefix]
		public static bool Nope()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CanLandPlz(ref bool __result)
		{
			int selectedTile = Find.WorldInterface.SelectedTile;
			if (selectedTile < 0)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MustSelectLandingSite"), MessageTypeDefOf.RejectInput);
				__result = false;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (!TileFinder.IsValidTileForNewSettlement(selectedTile, stringBuilder) &&
					(Find.World.worldObjects.SettlementAt(selectedTile) == null ||
					 Find.World.worldObjects.SettlementAt(selectedTile).Faction != Faction.OfPlayer))
				{
					Messages.Message(stringBuilder.ToString(), MessageTypeDefOf.RejectInput);
					__result = false;
				}
				else
				{
					Tile tile = Find.WorldGrid[selectedTile];
					__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Page_SelectStartingSite), "ExtraOnGUI")]
	public class PatchStartingSite
	{
		public static void Postfix(Page_SelectStartingSite __instance)
		{
			if (Find.Scenario.AllParts.Any(part => part is ScenPart_StartInSpace))
			{
				Find.WorldInterface.SelectedTile = TileFinder.RandomStartingTile();
				typeof(Page_SelectStartingSite).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
			}
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_PsychicEmanation), "TryExecuteWorker")]
	public static class TogglePsychicAmplifierQuest
	{
		public static void Postfix(IncidentParms parms)
		{
			if (ShipInteriorMod2.ArchoStuffEnabled && !WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechSpore"))
			{
				Map spaceMap = null;
				foreach (Map map in Find.Maps)
				{
					if (map.IsSpace() && map.spawnedThings.Where(t => t.def == ThingDefOf.Ship_ComputerCore).Any())
						spaceMap = map;
				}
				if (spaceMap != null)
				{
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifier"), TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierDesc"), LetterDefOf.PositiveEvent);
					AttackableShip ship = new AttackableShip();
					ship.enemyShip = DefDatabase<EnemyShipDef>.GetNamed("PsychicAmplifier");
					spaceMap.passingShipManager.passingShips.Add(ship);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ResearchManager), "FinishProject")]
	public static class TriggerPillarMissions
	{
		public static void Postfix(ResearchProjectDef proj)
		{
			if (proj.defName.Equals("ArchotechPillarA"))
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarAMission"); //Handled in Building_ShipBridge
			else if (proj.defName.Equals("ArchotechPillarB"))
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarBMission"); //Handled in Building_ShipBridge
			else if (proj.defName.Equals("ArchotechPillarC"))
			{
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarCMission");
				ShipInteriorMod2.GenerateArchotechPillarCSite();
			}
			else if (proj.defName.Equals("ArchotechPillarD"))
			{
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarDMission");
				ShipInteriorMod2.GenerateArchotechPillarDSite();
			}
		}
	}

	[HarmonyPatch(typeof(Window), "PostClose")]
	public static class CreditsAreTheRealEnd
	{
		public static void Postfix(Window __instance)
		{
			if (__instance is Screen_Credits && ShipInteriorMod2.SoSWin)
			{
				ShipInteriorMod2.SoSWin = false;
				GenScene.GoToMainMenu();
			}
		}
	}

	//should be in vanilla RW section
	[HarmonyPatch(typeof(CompTempControl), "CompGetGizmosExtra")]
	public static class CannotControlEnemyRadiators
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "CompGetGizmosExtra")]
	public static class CannotControlEnemyPods
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompTransporter), "CompGetGizmosExtra")]
	public static class CannotControlEnemyPodsB
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompRefuelable), "CompGetGizmosExtra")]
	public static class CannotControlEnemyFuel
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

   //other
   [HarmonyPatch(typeof(Thing), "SmeltProducts")]
	public static class PerfectEfficiency
	{
		public static bool Prefix(float efficiency)
		{
			if (efficiency == 0)
				return false;
			return true;
		}

		public static void Postfix(float efficiency, ref IEnumerable<Thing> __result, Thing __instance)
		{
			if (efficiency == 0)
			{
				List<Thing> actualResult = new List<Thing>();
				List<ThingDefCountClass> costListAdj = __instance.def.CostListAdjusted(__instance.Stuff);
				for (int j = 0; j < costListAdj.Count; j++)
				{
					int num = GenMath.RoundRandom((float)costListAdj[j].count);
					if (num > 0)
					{
						Thing thing = ThingMaker.MakeThing(costListAdj[j].thingDef);
						thing.stackCount = num;
						actualResult.Add(thing);
					}
				}
				__result = actualResult;
			}
		}
	}

	[HarmonyPatch(typeof(DamageWorker))]
	[HarmonyPatch("ExplosionCellsToHit", new Type[] { typeof(IntVec3), typeof(Map), typeof(float), typeof(IntVec3), typeof(IntVec3) })]
	public static class FasterExplosions
	{
		public static bool Prefix(Map map, float radius)
		{
			return !map.GetComponent<ShipHeatMapComp>().InCombat || radius > 25; //Ludicrously large explosions cause a stack overflow
		}

		public static void Postfix(ref IEnumerable<IntVec3> __result, DamageWorker __instance, IntVec3 center, Map map, float radius)
		{
			if (map.GetComponent<ShipHeatMapComp>().InCombat && radius <= 25)
			{
				HashSet<IntVec3> cells = new HashSet<IntVec3>();
				List<ExplosionCell> cellsToRun = new List<ExplosionCell>();
				cellsToRun.Add(new ExplosionCell(center, new bool[4], 0));
				ExplosionCell curCell;
				while (cellsToRun.Count > 0)
				{
					curCell = cellsToRun.Pop();
					cells.Add(curCell.pos);
					if (curCell.dist <= radius)
					{
						Building edifice = null;
						if (curCell.pos.InBounds(map))
							edifice = curCell.pos.GetEdifice(map);
						if (edifice != null && edifice.HitPoints >= __instance.def.defaultDamage / 2)
							continue;
						if (!curCell.checkedDir[0]) //up
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[1] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, 1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[1]) //down
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[0] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, -1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[2]) //right
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[3] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(1, 0, 0), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[3]) //left
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[2] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(-1, 0, 0), newDir, curCell.dist + 1));
						}
					}
				}
				__result = cells;
			}
		}

		public struct ExplosionCell
		{
			public IntVec3 pos;
			public bool[] checkedDir;
			public int dist;

			public ExplosionCell(IntVec3 myPos, bool[] myCheckedDir, int myDist)
			{
				checkedDir = myCheckedDir;
				pos = myPos;
				dist = myDist;
			}
		}
	}

	[HarmonyPatch(typeof(MapPawns), "DeRegisterPawn")]
	public class MapPawnRegisterPatch //PsiTech "patch"
	{
		public static bool Prefix(Pawn p)
		{
			//This patch does literally nothing... and yet, somehow, it fixes a compatibility issue with PsiTech. Weird, huh?
			return true;
		}
	}

	//This is the most horrible hack that has ever been hacked, it *MUST* be removed before release
	[HarmonyPatch(typeof(District),"get_Map")]
	public static class FixMapIssue
    {
		public static bool Prefix(District __instance)
        {
			return Find.Maps.Where(map => Find.Maps.IndexOf(map)==__instance.mapIndex).Count()>0;
        }

		public static void Postfix(District __instance, ref Map __result)
        {
			if (Find.Maps.Where(map => Find.Maps.IndexOf(map) == __instance.mapIndex).Count() <= 0)
				__result = Find.Maps.FirstOrDefault();
		}
	}

	//pointless as the quest should not fire in space at all since it spawns enemy pawns
	[HarmonyPatch(typeof(QuestNode_Root_ShuttleCrash_Rescue), "TryFindShuttleCrashPosition")]
	public static class CrashOnShuttleBay
	{
		public static void Postfix(Map map, Faction faction, IntVec2 size, ref IntVec3 spot, QuestNode_Root_ShuttleCrash_Rescue __instance)
		{
			if (map.Biome == ShipInteriorMod2.OuterSpaceBiome)
			{
				foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBay")))
				{
					ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
					area.RecalculateBlockingThing();
					if (area.FirstBlockingThing == null)
					{
						spot = area.CenterCell;
						return;
					}
				}
				foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBayLarge")))
				{
					ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
					area.RecalculateBlockingThing();
					if (area.FirstBlockingThing == null)
					{
						spot = area.CenterCell;
						return;
					}
				}
				QuestPart raidPart = null;
				foreach (QuestPart part in QuestGen.quest.PartsListForReading)
				{
					if (part is QuestPart_PawnsArrive)
					{
						raidPart = part;
						break;
					}
				}
				if (raidPart != null)
					QuestGen.quest.RemovePart(raidPart);
			}
		}
	}

	/*[HarmonyPatch(typeof(CompShipPart),"PostSpawnSetup")]
	public static class RemoveVacuum{
		[HarmonyPostfix]
		public static void GetRidOfVacuum (CompShipPart __instance)
		{
			if (__instance.parent.Map.terrainGrid.TerrainAt (__instance.parent.Position).defName.Equals ("EmptySpace"))
				__instance.parent.Map.terrainGrid.SetTerrain (__instance.parent.Position,TerrainDef.Named("FakeFloorInsideShip"));
		}
	}*/
	/*[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	public static class HullTilesDontWipe
	{
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (constructible.def.defName.Contains("ShipHullTile") ^ t.def.defName.Contains("ShipHullTile"))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TravelingTransportPods))]
	[HarmonyPatch("TraveledPctStepPerTick", MethodType.Getter)]
	public static class InstantShuttleArrival
	{
		[HarmonyPostfix]
		public static void CloseRangeBoardingAction(int ___initialTile, TravelingTransportPods __instance, ref float __result)
		{
			if (Find.TickManager.TicksGame % 60 == 0)
			{
				var mapComp = Find.WorldObjects.MapParentAt(___initialTile).Map.GetComponent<ShipHeatMapComp>();
				if ((mapComp.InCombat && (__instance.destinationTile == mapComp.ShipCombatOriginMap.Tile ||
					__instance.destinationTile == mapComp.ShipCombatMasterMap.Tile)) || 
					__instance.arrivalAction is TransportPodsArrivalAction_MoonBase)
				{
					__result = 1f;
				}
			}

		}
	}*/
}