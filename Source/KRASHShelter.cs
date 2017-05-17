﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using KSP.UI.Screens;
using KSP.UI.Dialogs;
using KSPAchievements;
using System.Reflection;

//using System.Diagnostics;
using Upgradeables;


namespace KRASH
{
	[KSPScenario (ScenarioCreationOptions.AddToAllGames, new GameScenes[] {
		GameScenes.SPACECENTER,
		GameScenes.EDITOR,
		GameScenes.FLIGHT,
		GameScenes.TRACKSTATION,
		GameScenes.SPACECENTER
	})]
	public class KRASHPersistent : ScenarioModule
	{
		public static bool inited = false;
		[KSPField (isPersistant = true)]
		public bool shelterSimulationActive = false;

		[KSPField (isPersistant = true)]
		public string selectedCostsCfg = "";

        public static CelestialBodySubtree[] celestialBodyNodes;
        static bool suspendUpdate = false;

        public void SetSuspendUpdate(bool s)
        {
            suspendUpdate = s;
        }

        override public void  OnAwake()
		{
			Log.Info ("KRASHPersistent.Awake");
			KRASHShelter.persistent = this;
			inited = true;
		}

        public static void initialize()
        {
            Log.Info("KRASHPersistent.initialize");
            if (suspendUpdate)
                return;
            if (KRASHShelter.persistent != null)
                KRASHShelter.persistent.StartCoroutine(getCelestialBodyNodes());
            else
                Log.Info("KRASHPersistent.initialize KRASHShelter.persistent is null");
        }

       // private static bool loaded;
        private static IEnumerator getCelestialBodyNodes()
        {
           // loaded = false;

            int timer = 0;

            while (ProgressTracking.Instance == null && timer < 500)
            {
                timer++;
                yield return null;
            }

            if (timer >= 500)
            {
                Log.Error("[KRASHPersistent] Progress Parser Timed Out");
                //loaded = false;
                yield break;
            }

            while (timer < 10)
            {
                timer++;
                yield return null;
            }
            if (ProgressTracking.Instance != null)
            {
                Log.Info("ProgressTracking found");
                KRASHPersistent.celestialBodyNodes = ProgressTracking.Instance.celestialBodyNodes;
#if false
                foreach (var cbn in KRASHPersistent.celestialBodyNodes)
                {
                    Log.Info("body: " + cbn.Body.name + "  IsReached: " + cbn.IsReached.ToString());
                    foreach (var cbnchild in cbn.childTrees)
                    {
                        Log.Info("moon: " + cbnchild.Body.name + "  IsReached: " + cbnchild.IsReached.ToString());
                    }
                }
#endif
            }


            
            Log.Info("[KRASHPersistent] Progress Nodes Loaded...");
        }

        public void Start()
		{
			Log.Info ("KRASHPersistent.Start");
            //KRASHShelter.persistent = this;

            initialize();

        }
	}


	class PreSimStatus
	{
		public int	flightsGlobalIndex { get; set; }
		public bool scienceFromOrbit { get; set; }
		public bool landed { get; set; }
		public bool isReached { get; set; }
		public bool scienceFromAtmo { get; set; }
	}

    // This class is a place to stick data that will not be destroyed by KRASH.Deactivate(). 
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    class KRASHShelter : MonoBehaviour
    {
		public static KRASHPersistent persistent;

        // Editor stuff
        public static EditorFacility lastEditor = EditorFacility.None;
        public static ConfigNode lastShip = null;
		public static double simcost = 0.0f;
		public static double simCost {
			get {
				return simcost;
			}
			set {
				Log.Info ("Setting simCost: " + value.ToString ());
				simcost = value;
			}
		} // = 0.0F;
		public static double startingFunds { get; set; } = 0.0f;
        public static GameScenes lastScene;
		public static List<CelestialBody> bodiesListAtSimStart;
		public static List<PreSimStatus> preSimStatus;
		public static double LimitSimCost { get; set; }
		public static KRASH instance = null;
		public static float shipCost {get; set; }
		public static float simSetupCost { get; set; } = 0;
		public static UICLASS uiVisiblity;

        public static float dryMass { get; set; }
        public static float fuelMass { get; set; }

        public static bool TestFlightFailuresDisabled { get; set; }

        void Awake()
		{
			Log.Info ("KRASHShelter.Awake");
			uiVisiblity = new UICLASS ();
			uiVisiblity.Awake ();
		}

		void Start()
        {
			Log.Info ("KRASHShelter.Start");

            GameEvents.onLevelWasLoaded.Add(CallbackLevelWasLoaded);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
            DontDestroyOnLoad(this);
        }

		void OnDestroy()
		{

		}

		//void RotateGizmoSpawnedSpawned(AltimeterSliderButtons asb) {
		//	Log.Info ("RotateGizmoSpawnedSpawned");
		//}
        void CallbackLevelWasLoaded(GameScenes scene)
        {
            Log.Info("KRASHShelter CallbackLevelWasLoaded");
            //[KSPScenario(ScenarioCreationOptions.AddToNewGames, new[] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER })]
            if (scene == GameScenes.FLIGHT || scene == GameScenes.TRACKSTATION || scene == GameScenes.SPACECENTER)
            {
                KRASHPersistent.initialize();
                Log.Info("CallbackLevelWasLoaded loaded for " + scene.ToString());
            }
            else
            {
                Log.Info("No call at CallbackLevelWasLoaded for " + scene.ToString());
            }
        }
        // public static EventData<FromToAction<GameScenes, GameScenes>> onGameSceneSwitchRequested;
        void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromtoaction)
        {
            Log.Info("KRASHShelter OnSceneLoadRequested");
            //[KSPScenario(ScenarioCreationOptions.AddToNewGames, new[] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER })]
            if ((GameScenes)fromtoaction.from == GameScenes.FLIGHT || (GameScenes)fromtoaction.from == GameScenes.TRACKSTATION || (GameScenes)fromtoaction.from == GameScenes.SPACECENTER)
            {
                KRASHPersistent.initialize();
                Log.Info("OnSceneLoadRequested for " + ((GameScenes)fromtoaction.from).ToString());
            }
            else
            {
                Log.Info("No call at onGameSceneSwitchRequested for " + ((GameScenes)fromtoaction.from).ToString());
            }
        }

    }

#if false
	[KSPAddon(KSPAddon.Startup.Flight, true)]
	class GizmoEvents : MonoBehaviour
	{
		public static readonly EventData<AltimeterSliderButtons> onAltimeterSliderButtonsSpawned = new EventData<AltimeterSliderButtons>("onAltimeterSliderButtons");


		class GizmoCreationListener : MonoBehaviour
		{
			private void Start()
			// I use Start instead of Awake because whatever setup the editor does to the gizmo won't be done yet
			{
				AltimeterSliderButtons altimeterSliderButtons = null;


				if (gameObject.GetComponentCached(ref altimeterSliderButtons) != null)
				{
					onAltimeterSliderButtonsSpawned.Fire(altimeterSliderButtons);
				}
				else if (gameObject.GetComponentCached(ref altimeterSliderButtons) != null)
				{
					onAltimeterSliderButtonsSpawned.Fire(altimeterSliderButtons);
				}
				else Debug.LogError("Didn't find a gizmo on this GameObject -- something has broken");

				// could destroy this MB now, unless you wanted to use OnDestroy to sent an event
			}

			private void OnDestroy()
			{
				// could also send an event on despawn here
			}
		}


		private void Awake()
		{
			AddListenerToGizmo("AltimeterSliderButtons");

			Destroy(gameObject);
		}


		private static void AddListenerToGizmo(string prefabName)
		{
			var prefab = AssetBase.GetPrefab(prefabName);

			if (prefab == null)
			{
				Debug.LogError("Couldn't find Gizmo '" + prefabName + "'");
				return;
			}

			prefab.AddOrGetComponent<GizmoCreationListener>();

#if DEBUG
			Debug.Log("Added listener to " + prefabName);
#endif
		}
	}
#endif





}
