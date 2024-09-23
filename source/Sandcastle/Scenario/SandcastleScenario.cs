using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using KSP.UI.Screens;
using Sandcastle.PrintShop;
using Sandcastle.Inventory;
using KSP.Localization;
using Sandcastle.PrintShop;

namespace Sandcastle
{

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class SandcastleScenario: ScenarioModule
    {
        #region GameEvents
        /// <summary>
        /// A vessel printer has requested a list of parts to be printed.
        /// </summary>
        public static EventData<SCShipwright, List<BuildItem>> onSupportPrintingRequest = new EventData<SCShipwright, List<BuildItem>>("onSupportPrintingRequest");

        /// <summary>
        /// A part has been printed.
        /// </summary>
        public static EventData<SCBasePrinter, BuildItem> onPartPrinted = new EventData<SCBasePrinter, BuildItem>("onPartPrinted");

        /// <summary>
        /// A part has been recycled.
        /// </summary>
        public static EventData<SCShipbreaker, BuildItem> onPartRecycled = new EventData<SCShipbreaker, BuildItem>("onPartRecycled");
        #endregion

        internal struct SnapshotRequest
        {
            public AvailablePart availablePart;
            public int variantId;
        }

        #region Housekeeping
        public static SandcastleScenario shared;
        public static bool debugMode;
        public static bool checkForKerbals;

        static List<SnapshotRequest> snapshotRequests;
        bool worldStabilizerInstalled;
        Dictionary<Vessel, bool> spawnedVessels;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;
            InventoryUtils.debugMode = debugMode;

            debugMode = SandcastleSettings.DebugModeEnabled;
            checkForKerbals = SandcastleSettings.CheckForKerbals;
            worldStabilizerInstalled = AssemblyLoader.loadedAssemblies.Contains("WorldStabilizer");

            spawnedVessels = new Dictionary<Vessel, bool>();

            if (HighLogic.LoadedSceneIsFlight)
            {
                MaterialsList.LoadLists();
                InventoryUtils.FindThumbnailPaths();
                GameEvents.onAttemptEva.Add(onAttemptEVA);
                GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
                GameEvents.onVesselGoOffRails.Add(onVesselGoOffRails);
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                StartCoroutine(scanForThumbnails());
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onAttemptEva.Remove(onAttemptEVA);
                GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
                GameEvents.onVesselGoOffRails.Remove(onVesselGoOffRails);
            }
        }
        #endregion

        #region API
        public void addSpawnedVessel(Vessel vessel, bool keepLevel = false)
        {
            if (!spawnedVessels.ContainsKey(vessel))
                spawnedVessels.Add(vessel, keepLevel);
        }
        #endregion

        #region Helpers
        private void onGameSettingsApplied()
        {
            debugMode = SandcastleSettings.DebugModeEnabled;
            debugMode = SandcastleSettings.CheckForKerbals;
        }

        private void onVesselGoOffRails(Vessel vessel)
        {
            if (!vessel.Landed || vessel.Splashed)
                return;

            // Check if vessel was recently spawned
            if (!spawnedVessels.ContainsKey(vessel))
                return;

            // Remove the vessel from our list.
            spawnedVessels.Remove(vessel);

            // Safety check: Make sure the vessel is landed or splashed.
            if (!vessel.LandedOrSplashed)
                return;

            // Cleared to reposition the craft.
            if (debugMode)
                Debug.Log("[Sandcastle ] - onVesselGoOffRails called to reposition " + vessel.vesselName);

            FlightLogger.IgnoreGeeForces(20f);
            vessel.ignoreCollisionsFrames = 60;
            vessel.skipGroundPositioning = false;
            vessel.CheckGroundCollision();

            setupLaunchClamps(vessel);
        }

        private void setupLaunchClamps(Vessel vessel)
        {
            int count = vessel.parts.Count;
            Part part;
            PartModule partModule;
            for (int index = 0; index < count; index++)
            {
                part = vessel.parts[index];

                // Special case: handle Restock clamps
                partModule = part.Modules.GetModule("ModuleRestockLaunchClamp");
                if (partModule != null)
                {
                    part.SendMessage("RotateTower", SendMessageOptions.DontRequireReceiver);
                }

                // Special case: handle EL clamps
                partModule = part.Modules.GetModule("ELExtendingLaunchClamp");
                if (partModule != null)
                {
                    part.SendMessage("RotateTower", SendMessageOptions.DontRequireReceiver);
                }

                List<LaunchClamp> launchClamps = part.FindModulesImplementing<LaunchClamp>();
                if (launchClamps != null && launchClamps.Count > 0)
                {
                    int clampCount = launchClamps.Count;
                    for (int clampIndex = 0; clampIndex < clampCount; clampIndex++)
                    {
                        launchClamps[clampIndex].EnableExtension();
                    }
                }
            }
        }

        IEnumerator<YieldInstruction> scanForThumbnails()
        {
            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableCargoParts();
            AvailablePart availablePart;
            string filePath;
            string gameDataPath = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
            ModulePartVariants partVariant;
            int variantCount;
            string altFilePath;
            SnapshotRequest request;

            snapshotRequests = new List<SnapshotRequest>();

            // First, scan the list of cargo parts to see if we have any that need thumbnails.
            if (cargoParts != null && cargoParts.Count > 0)
            {
                int count = cargoParts.Count;

                for (int index = 0; index < count; index++)
                {
                    availablePart = cargoParts[index];
                    partVariant = availablePart.partPrefab.FindModuleImplementing<ModulePartVariants>();

                    // If the part has no variants, then check to see if its icon exists in the standard location and the default location.
                    if (partVariant == null)
                    {
                        filePath = Path.Combine(gameDataPath, InventoryUtils.GetFilePathForThumbnail(availablePart));
                        altFilePath = Path.Combine(gameDataPath, InventoryUtils.GetFilePathForThumbnail(availablePart, -1, true));
                        if (File.Exists(filePath) || File.Exists(altFilePath))
                            continue;

                        request = new SnapshotRequest();
                        request.availablePart = availablePart;
                        request.variantId = -1;
                        snapshotRequests.Add(request);
                    }

                    // Check the standard location and default location for all the part variants.
                    else
                    {
                        variantCount = partVariant.variantList.Count;
                        for (int variantIndex = 0; variantIndex < variantCount; variantIndex++)
                        {
                            filePath = Path.Combine(gameDataPath, InventoryUtils.GetFilePathForThumbnail(availablePart, variantIndex));
                            altFilePath = Path.Combine(gameDataPath, InventoryUtils.GetFilePathForThumbnail(availablePart, variantIndex, true));
                            if (File.Exists(filePath) || File.Exists(altFilePath))
                                continue;

                            request = new SnapshotRequest();
                            request.availablePart = availablePart;
                            request.variantId = variantIndex;
                            snapshotRequests.Add(request);
                        }
                    }
                }
            }

            // Next, if we have any snapshots to make, ask if the user wants to take the snapshots now.
            if (snapshotRequests.Count > 0)
            {
                int count = snapshotRequests.Count;
                int completed = 0;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_takingSnapshotsPleaseWait", new string[1] { count.ToString() }), 15f, ScreenMessageStyle.UPPER_CENTER);

                for (int index = 0; index < count; index++)
                {
                    request = snapshotRequests[index];
                    InventoryUtils.TakeSnapshot(request.availablePart, request.variantId);
                    completed += 1;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_snapshotsCompleted", new string[2] { completed.ToString(), count.ToString() }), 0.5f, ScreenMessageStyle.UPPER_LEFT);
                    yield return new WaitForFixedUpdate();
                }
            }
            yield return new WaitForFixedUpdate();
        }

        void onAttemptEVA(ProtoCrewMember crewMemeber, Part evaPart, Transform transform)
        {
            // Let tourists outside!
            if (crewMemeber.trait.Equals(KerbalRoster.touristTrait))
                FlightEVA.fetch.overrideEVA = false;
        }
        #endregion

    }
}