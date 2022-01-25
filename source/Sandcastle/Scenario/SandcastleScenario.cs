using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using KSP.UI.Screens;
using Sandcastle.PrintShop;
using Sandcastle.Inventory;
using KSP.Localization;

namespace Sandcastle
{

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class SandcastleScenario: ScenarioModule
    {
        internal struct SnapshotRequest
        {
            public AvailablePart availablePart;
            public int variantId;
        }

        #region Housekeeping
        public static SandcastleScenario shared;
        static List<SnapshotRequest> snapshotRequests;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;

            if (HighLogic.LoadedSceneIsFlight)
            {
                MaterialsList.LoadLists();
                InventoryUtils.FindThumbnailPaths();
                GameEvents.onAttemptEva.Add(onAttemptEVA);
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                StartCoroutine(scanForThumbnails());
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
                GameEvents.onAttemptEva.Remove(onAttemptEVA);
        }
        #endregion

        #region Helpers
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
            FlightEVA.fetch.overrideEVA = crewMemeber.trait.Equals(KerbalRoster.touristTrait);
        }

        #endregion

    }
}