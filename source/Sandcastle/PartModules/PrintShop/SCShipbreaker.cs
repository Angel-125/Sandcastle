using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Represents a shop that is capable of printing items and placing them in an available inventory.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_shipbreakerTitle")]
    public class SCShipbreaker: BasePartModule
    {
        #region Constants
        const double kCatchupTime = 3600;
        const float kMsgDuration = 5;
        const float kInventoryRefreshDelay = 3;
        const string kRecycleState = "recycleState";
        const string kFlightID = "docketFlightID";
        #endregion

        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// The number of resource units per second that the recycler can recycle.
        /// </summary>
        [KSPField]
        public float recycleSpeedUSec = 1f;

        /// <summary>
        /// Flag to indicate whether or not to allow specialists to improve the recycle speed. Exactly how the specialist(s) does that is a trade secret.
        /// </summary>
        [KSPField]
        public bool UseSpecialistBonus = true;

        /// <summary>
        /// Per experience rating, how much to improve the recycle speed by.
        /// The print shop part must have crew capacity.
        /// </summary>
        [KSPField]
        public float SpecialistBonus = 0.05f;

        /// <summary>
        /// The skill required to improve the recycle speed.
        /// </summary>
        [KSPField]
        public string ExperienceEffect = "ConverterSkill";

        /// <summary>
        /// Name of the effect to play from the part's EFFECTS node when the printer is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// What percentage of resources will be recycled.
        /// </summary>
        [KSPField]
        public double recyclePercentage = 0.45;

        /// <summary>
        /// Name of the animation to play during printing.
        /// </summary>
        [KSPField]
        public string animationName = string.Empty;

        /// <summary>
        /// Flag to indicate if vessel capture is enabled.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool vesselCaptureEnabled;

        /// <summary>
        /// Maximum distance allowed for other shipbreakers to help break up a vessel.
        /// </summary>
        [KSPField]
        public float maxBuildingDistance = 50f;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Represents the list of build items to recycle.
        /// </summary>
        public List<BuildItem> recycleQueue;

        /// <summary>
        /// Current state of the recycler.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_recycleState", guiActive = true, groupName = "#LOC_SANDCASTLE_shipbreakerGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipbreakerGroupName")]
        public WBIPrintStates recycleState = WBIPrintStates.Idle;

        /// <summary>
        /// status text.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_recycleStatus", guiActive = true, groupName = "#LOC_SANDCASTLE_shipbreakerGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipbreakerGroupName")]
        public string recycleStatusText;

        /// <summary>
        /// Describes when the recycler was last updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastUpdateTime;

        /// <summary>
        /// Current job being recycled.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string currentJob = string.Empty;

        ShipbreakerUI recyclerUI = null;
        bool missingRequirements = false;
        Dictionary<double, Part> unHighlightList = null;
        public Animation animation = null;
        protected AnimationState animationState;
        string shipName;
        double shipTotalUnitsToRecycle;
        double shipTotalUnitsRecycled;
        int totalPartsToRecycle;
        int totalPartsRecycled;
        Vessel vesselToRecycle = null;
        DockedVesselInfo dockedVesselInfo = null;
        bool tryToCoupleVessel = false;
        List<BuildItem> partsNeedingRecycling = null;
        List<SCShipbreaker> supportShipbreakers = null;
        #endregion

        #region FixedUpdate
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Try to dock the vessel that we'll recycle
            if (vesselToRecycle != null && dockedVesselInfo == null && tryToCoupleVessel)
            {
                tryToCoupleVessel = false;
                Debug.Log(formatPartID() + " - Calling coupleVessel");
                part.StartCoroutine(InventoryUtils.coupleVessel(vesselToRecycle, part, onVesselCoupled));
                return;
            }

            // Handle unhighlight
            handleUnhighlightParts();

            // If the recycler is pased then we're done
            if (recycleState == WBIPrintStates.Paused)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleStatusText = recycleState.ToString();
                part.Effect(runningEffect, 0);
                if (animation != null)
                {
                    animation[animationName].speed = 0f;
                    animation.Stop();
                }
                return;
            }

            // If there are no items in the queue then we're done.
            if (recycleQueue.Count == 0 && dockedVesselInfo == null)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleState = WBIPrintStates.Idle;
                recycleStatusText = recycleState.ToString();
                part.Effect(runningEffect, 0);
                if (animation != null)
                {
                    animation[animationName].speed = 0f;
                    animation.Stop();
                }
                return;
            }

            // Play effects
            part.Effect(runningEffect, 1);
            if (animation != null && animation[animationName].time <= 0)
            {
                animation.Play(animationName);
                animation[animationName].time = 0f;
                animation[animationName].speed = 1.0f;
            }

            // Handle catchup
            handleCatchup();

            // Process the queue
            processRecycleQueue();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Game settings
            debugMode = SandcastleScenario.debugMode;

            // Watch for game events
            GameEvents.onVesselChange.Add(onVesselChange);
            SandcastleScenario.onPartRecycled.Add(onPartRecycled);

            // Setup animation
            setupAnimation();

            // Setup vessel capture toggle button
            if (vesselCaptureEnabled)
                Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOff");
            else
                Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOn");
        }

        public override void OnAwake()
        {
            base.OnAwake();
            unHighlightList = new Dictionary<double, Part>();
            recycleQueue = new List<BuildItem>();
            supportShipbreakers = new List<SCShipbreaker>();
            recyclerUI = new ShipbreakerUI();
            recyclerUI.part = part;
            recyclerUI.onRecycleStatusUpdate = onRecycleStatusUpdate;
            recyclerUI.recycleQueue = new List<BuildItem>();
            recyclerUI.resourceRecylePercent = recyclePercentage;
            recyclerUI.onCancelVesselBuild = onCancelVesselBuild;
            recyclerUI.supportShipbreakers = supportShipbreakers;
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (recyclerUI.IsVisible())
                recyclerUI.SetVisible(false);
        }

        public virtual void OnDestroy()
        {
            if (recyclerUI.IsVisible())
                recyclerUI.SetVisible(false);
            GameEvents.onVesselChange.Remove(onVesselChange);
            SandcastleScenario.onPartRecycled.Remove(onPartRecycled);
        }

        public virtual void onVesselChange(Vessel newVessel)
        {
            if (recyclerUI.IsVisible())
                recyclerUI.SetVisible(false);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Recycle state
            if (node.HasValue(kRecycleState))
                recycleState = (WBIPrintStates)Enum.Parse(typeof(WBIPrintStates), node.GetValue(kRecycleState));

            if (node.HasValue("shipName"))
                shipName = node.GetValue("shipName");

            if (node.HasValue("shipTotalUnitsToRecycle"))
                double.TryParse(node.GetValue("shipTotalUnitsToRecycle"), out shipTotalUnitsToRecycle);

            if (node.HasValue("shipTotalUnitsRecycled"))
                double.TryParse(node.GetValue("shipTotalUnitsRecycled"), out shipTotalUnitsRecycled);

            if (node.HasValue("totalPartsToRecycle"))
                int.TryParse(node.GetValue("totalPartsToRecycle"), out totalPartsToRecycle);

            if (node.HasValue("totalPartsRecycled"))
                int.TryParse(node.GetValue("totalPartsRecycled"), out totalPartsRecycled);

            // Recycle Queue
            recycleQueue = new List<BuildItem>();
            if (node.HasNode(BuildItem.kBuildItemNode))
            {
                BuildItem buildItem;
                ConfigNode[] nodes = node.GetNodes(BuildItem.kBuildItemNode);
                for (int index = 0; index < nodes.Length; index++)
                {
                    buildItem = new BuildItem(nodes[index]);
                    recycleQueue.Add(buildItem);
                }
            }

            // Parts needing recycling
            partsNeedingRecycling = new List<BuildItem>();
            if (node.HasNode("BUILDITEM_TODO"))
            {
                BuildItem buildItem;
                ConfigNode[] nodes = node.GetNodes("BUILDITEM_TODO");
                for (int index = 0; index < nodes.Length; index++)
                {
                    buildItem = new BuildItem(nodes[index]);
                    recycleQueue.Add(buildItem);
                }
            }

            // Docked vessel info
            if (node.HasNode("DOCKED_VESSEL_INFO"))
            {
                ConfigNode dockedVesselNode = node.GetNode("DOCKED_VESSEL_INFO");
                dockedVesselInfo = new DockedVesselInfo();
                dockedVesselInfo.Load(dockedVesselNode);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Recycle state
            node.AddValue(kRecycleState, recycleState.ToString());

            if (!string.IsNullOrEmpty(shipName))
                node.AddValue("shipName", shipName);
            node.AddValue("shipTotalUnitsToRecycle", shipTotalUnitsToRecycle);
            node.AddValue("shipTotalUnitsRecycled", shipTotalUnitsRecycled);
            node.AddValue("totalPartsToRecycle", totalPartsToRecycle);
            node.AddValue("totalPartsRecycled", totalPartsRecycled);

            // Recycle queue
            ConfigNode buildItemNode;
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                buildItemNode = recycleQueue[index].Save();
                node.AddNode(buildItemNode);
            }

            // Parts needing recycling
            if (partsNeedingRecycling == null)
                partsNeedingRecycling = new List<BuildItem>();
            count = partsNeedingRecycling.Count;
            for (int index = 0; index < count; index++)
            {
                buildItemNode = recycleQueue[index].Save();
                buildItemNode.name = "BUILDITEM_TODO";
                node.AddNode(buildItemNode);
            }

            // Docked vessel info
            if (dockedVesselInfo != null)
            {
                ConfigNode dockedVesselNode = new ConfigNode("DOCKED_VESSEL_INFO");
                dockedVesselInfo.Save(dockedVesselNode);
                node.AddNode(dockedVesselNode);
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_shipbreakerDesc"));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recycleSpeed", new string[1] { string.Format("{0:n1}", recycleSpeedUSec) }));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recyclePercent", new string[1] { string.Format("{0:n1}", recyclePercentage * 100f) }));
            info.Append(base.GetInfo());
            return info.ToString();
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipbreakerGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipbreakerGroupName", guiName = "#LOC_SANDCASTLE_openShpbreaker")]
        public void OpenGUI()
        {
            recyclerUI.SetVisible(true);
        }

        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipbreakerGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipbreakerGroupName", guiName = "#LOC_SANDCASTLE_vesselCaptureOn")]
        public void ToggleActiveState()
        {
            vesselCaptureEnabled = !vesselCaptureEnabled;

            if (vesselCaptureEnabled)
                Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOff");
            else
                Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOn");
        }
        #endregion

        #region API

        public void DisableRecycler()
        {
            vesselCaptureEnabled = false;
            recycleState = WBIPrintStates.Idle;
            Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOn");
        }

        protected void updateUIStatus()
        {
            recyclerUI.isRecycling = recycleState == WBIPrintStates.Recycling;
            recyclerUI.UpdateResourceRequirements();
            recyclerUI.SetPrintTotals(totalPartsToRecycle, totalPartsRecycled);
            recyclerUI.jobStatus = recycleStatusText;
            recyclerUI.craftName = shipName;
        }
        #endregion

        #region Helpers
        private string formatPartID()
        {
            return "[Shipbreaker " + part.flightID + "]";
        }

        public bool IsRecycling(BuildItem buildItem)
        {
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                if (recycleQueue[index].flightId == buildItem.flightId)
                    return true;
            }

            return false;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (dockedVesselInfo != null)
            {
                return;
            }
            if (collider.attachedRigidbody == null || !collider.CompareTag("Untagged"))
            {
                return;
            }
            if (!vesselCaptureEnabled)
            {
                return;
            }

            //Get the part that collided with the trigger
            Part collidedPart = collider.attachedRigidbody.GetComponent<Part>();
            if (collidedPart == null)
            {
                vesselToRecycle = null;
                return;
            }

            // Check for asteroids and comets
            if (collidedPart.vessel.FindPartModuleImplementing<ModuleAsteroid>() != null)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_alertNoAsteroidAllowed"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                vesselToRecycle = null;
                return;
            }
            if (collidedPart.vessel.FindPartModuleImplementing<ModuleComet>() != null)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_alertNoCometAllowed"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                vesselToRecycle = null;
                return;
            }

            // Check for kerbals
            if (debugMode)
                Debug.Log(formatPartID() + " - Checking for kerbals");

            if (SandcastleScenario.checkForKerbals && collidedPart.vessel.GetCrewCount() > 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_removeCrew"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                vesselToRecycle = null;
                return;
            }

            // Check to make sure we're not already recycling the vessel.
            if (collidedPart.vessel == vesselToRecycle)
            {
                Debug.Log(formatPartID() + " - Exiting OnTriggerEnter, we already have a vessel to recycle.");
                return;
            }

            // Set vessel to recycle
            vesselToRecycle = collidedPart.vessel;
            if (debugMode)
                Debug.Log(formatPartID() + " - Found vessel to recycle");

            // Get parts to recycle
            int count = vesselToRecycle.Parts.Count;
            Part partToRecycle;
            BuildItem recycleItem;
            shipTotalUnitsToRecycle = 0;
            for (int index = 0; index < count; index++)
            {
                partToRecycle = vesselToRecycle.Parts[index];

                recycleItem = new BuildItem(partToRecycle.partInfo);
                recycleItem.flightId = partToRecycle.flightID;
                recycleItem.UpdateResourceRequirements(partToRecycle);
                shipTotalUnitsToRecycle += recycleItem.totalUnitsRequired;
                partsNeedingRecycling.Add(recycleItem);

                if (debugMode)
                {
                    Debug.Log(formatPartID() + " - found part: " + recycleItem.partName);
                }

            }

            // Setup stats
            totalPartsToRecycle = partsNeedingRecycling.Count;
            totalPartsRecycled = 0;
            shipTotalUnitsRecycled = 0;
            shipName = vesselToRecycle.vesselName;

            // Set flag to couple the vessel that we're about to recycle
            tryToCoupleVessel = true;

            if (debugMode)
            {
                Debug.Log(formatPartID() + " - Recycling " + shipName);
                Debug.Log(formatPartID() + " - " + partsNeedingRecycling.Count + " parts to recycle");
                Debug.Log(formatPartID() + " - shipTotalUnitsToRecycle: " + shipTotalUnitsToRecycle);
            }
        }

        void onVesselCoupled(DockedVesselInfo dockedVessel)
        {
            dockedVesselInfo = dockedVessel;
            Debug.Log(formatPartID() + " - onVesselCoupled");

            // Disable vessel capture.
            vesselCaptureEnabled = false;
            Events["ToggleActiveState"].guiName = Localizer.Format("#LOC_SANDCASTLE_vesselCaptureOn");

            if (recycleQueue.Count <= 0)
                processVesselToRecycle();
        }

        protected virtual void processVesselToRecycle()
        {
            List<BuildItem> doomed = new List<BuildItem>();
            int count = partsNeedingRecycling.Count;

            // Find parts that have no child parts and add them to the build queue.
            Part partToRecycle;
            BuildItem recycleItem;
            for (int index = 0; index < count; index++)
            {
                recycleItem = partsNeedingRecycling[index];
                partToRecycle = part.vessel[recycleItem.flightId];
                if (partToRecycle == null)
                    continue;

                // If the part has no children then add it to the recycle queue.
                if (partToRecycle.children == null || partToRecycle.children.Count <= 0)
                {
                    doomed.Add(recycleItem);
                    recycleQueue.Add(recycleItem);
                }
            }

            // Clear the parts needing recycling of those that were added to the queue
            count = doomed.Count;
            for (int index = 0; index < count; index++)
            {
                partsNeedingRecycling.Remove(doomed[index]);
            }

            // Update the UI's recycle queue.
            recyclerUI.recycleQueue = new List<BuildItem>();
            recyclerUI.recycleQueue.AddRange(recycleQueue);
            recyclerUI.recycleQueue.AddRange(partsNeedingRecycling);

            if (recycleQueue.Count <= 1)
                return;

            // Find support shipbreakers and ask them if they can help out.
            findShipbreakers();
            if (debugMode)
                Debug.Log(formatPartID() + " - Support breakers found: " + supportShipbreakers.Count);

            // If we have support breakers then give them work to do.
            count = supportShipbreakers.Count;
            if (count > 0)
            {
                List<BuildItem> recycleItems = new List<BuildItem>();
                recycleItems.AddRange(recycleQueue);
                recycleItems.Reverse();

                int recycleItemIndex = 0;
                int recycleItemCount = recycleItems.Count;
                for (int index = 0; index < count; index++)
                {
                    if (recycleItemIndex <= recycleItemCount - 1)
                    {
                        recycleItems[recycleItemIndex].waitForSupportCompletion = true;
                        supportShipbreakers[index].recycleQueue.Add(recycleItems[recycleItemIndex]);
                        if (debugMode)
                            Debug.Log(formatPartID() + " - Added " + recycleItems[recycleItemIndex].partName + " to support shipbreaker " + supportShipbreakers[index].part.flightID);
                        recycleItemIndex += 1;
                    }
                }
            }

            recyclerUI.supportShipbreakers = supportShipbreakers;
        }

        private void findShipbreakers()
        {
            int vesselCount = FlightGlobals.VesselsLoaded.Count;
            Vessel loadedVessel;
            int count;
            supportShipbreakers = new List<SCShipbreaker>();
            List<SCShipbreaker> shipbreakers;
            for (int vesselIndex = 0; vesselIndex < vesselCount; vesselIndex++)
            {
                loadedVessel = FlightGlobals.VesselsLoaded[vesselIndex];

                // Skip if out of range
                float distance = Vector3.Distance(part.vessel.transform.position, loadedVessel.transform.position);
                if (distance > maxBuildingDistance)
                    continue;

                // Now find shipbreaker modules
                shipbreakers = loadedVessel.FindPartModulesImplementing<SCShipbreaker>();
                if (shipbreakers != null)
                {
                    count = shipbreakers.Count;

                    for (int index = 0; index < count; index++)
                    {
                        // Ignore ourself
                        if (shipbreakers[index] == this)
                            continue;

                        // If the shipbreaker is alredy breaking a ship then disqualify it
                        if (shipbreakers[index].dockedVesselInfo != null || shipbreakers[index].partsNeedingRecycling.Count > 0)
                            continue;

                        // The shipbreaker can help
                        supportShipbreakers.Add(shipbreakers[index]);
                    }
                }
                if (debugMode)
                    Debug.Log(formatPartID() + " - Support breakers found: " + supportShipbreakers.Count);
            }
        }

        protected virtual void processRecycleQueue()
        {
            // Check the recycle queue again.
            int partsToRecycleCount = partsNeedingRecycling.Count;
            int queueCount = recycleQueue.Count;
            if (queueCount == 0 && partsToRecycleCount == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleState = WBIPrintStates.Idle;
                recycleStatusText = "";
                recyclerUI.clearUI();
                return;
            }

            // If the recycle queue is empty but we have more parts to process then 
            // process the vessel.
            else if (partsToRecycleCount > 0 && queueCount == 0)
            {
                processVesselToRecycle();
            }

            // Continue with the recycling
            recycleState = WBIPrintStates.Recycling;

            // Consume any resources that we require to operate.
            if (resHandler.inputResources.Count > 0)
            {
                string error = string.Empty;
                resHandler.UpdateModuleResourceInputs(ref error, 1.0f, 0.1f, true);
                int count = resHandler.inputResources.Count;
                for (int index = 0; index < count; index++)
                {
                    if (!resHandler.inputResources[index].available)
                    {
                        lastUpdateTime = Planetarium.GetUniversalTime();
                        recycleStatusText = error;
                        return;
                    }
                }
            }
            if (resHandler.outputResources.Count > 0)
                resHandler.UpdateModuleResourceOutputs();

            // Handle the current print job.
            handleRecycleJob(TimeWarp.fixedDeltaTime);
        }

        protected virtual void handleCatchup()
        {
            BuildItem buildItem;
            double printTimeRemaining = 0;
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdateTime;
            while (elapsedTime > TimeWarp.fixedDeltaTime * 2 && recycleQueue.Count > 0)
            {
                // We always work with the first item in the queue.
                buildItem = recycleQueue[0];

                // Update recycle state
                recycleState = WBIPrintStates.Recycling;

                // Calculate print time remaining
                printTimeRemaining = (buildItem.totalUnitsRequired - buildItem.totalUnitsPrinted) * recycleSpeedUSec;
                if (printTimeRemaining > elapsedTime)
                    printTimeRemaining = elapsedTime;

                // Handle recycle job
                handleRecycleJob(printTimeRemaining);

                // Update elapsedTime
                elapsedTime -= printTimeRemaining;

                if (recycleQueue.Count == 0 || missingRequirements)
                {
                    elapsedTime = TimeWarp.fixedDeltaTime;
                    break;
                }
            }
        }

        protected virtual void handleUnhighlightParts()
        {
            double[] unHighlightTimes = unHighlightList.Keys.ToArray();
            double currentTime = Planetarium.GetUniversalTime();
            List<double> doomed = new List<double>();

            for (int index = 0; index < unHighlightTimes.Length; index++)
            {
                if (currentTime >= unHighlightTimes[index])
                {
                    doomed.Add(unHighlightTimes[index]);
                    unHighlightList[unHighlightTimes[index]].Highlight(false);
                }
            }
            int count = doomed.Count;
            for (int index = 0; index < count; index++)
                unHighlightList.Remove(doomed[index]);
        }

        private bool supportUnitStillProcessing(BuildItem buildItem)
        {
            if (supportShipbreakers == null)
                supportShipbreakers = new List<SCShipbreaker>();

            int count = supportShipbreakers.Count;
            if (supportShipbreakers == null || count <= 0)
                return false;

            for (int index = 0; index < count; index++)
            {
                if (supportShipbreakers[index].IsRecycling(buildItem))
                    return true;
            }

            return false;
        }

        private void scrapPart(BuildItem buildItem)
        {
            Part partToRecycle = vessel[buildItem.flightId];
            if (!buildItem.isBeingRecycled && partToRecycle != null)
            {
                // Remove part from its parent.
                if (partToRecycle.parent != null)
                    partToRecycle.parent.removeChild(partToRecycle);

                // If it is the root part then we need to undock it first.
                if (dockedVesselInfo != null && partToRecycle.flightID == dockedVesselInfo.rootPartUId)
                {
                    InventoryUtils.decoupleVessel(partToRecycle, dockedVesselInfo);
                    dockedVesselInfo = null;
                }

                // Dispose of the part.
                partToRecycle.explosionPotential = 0.01f;
                partToRecycle.explode();
            }
        }

        private void onPartRecycled(SCShipbreaker shipbreaker, BuildItem buildItem)
        {
            // If we're the one who fired the event or we're not the Lead Shipbreaker (the one doing the breaking), then we're done.
            if (shipbreaker == this || dockedVesselInfo == null)
                return;

            if (debugMode)
                Debug.Log(formatPartID() + " - Support Shipbreaker " + shipbreaker.part.flightID + " recycled " + buildItem.partName);

            // Update stats
            shipTotalUnitsRecycled += buildItem.totalUnitsPrinted;
            totalPartsRecycled += 1;
            updateUIStatus();

            // Remove the part from our queue
            BuildItem doomed = null;
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                if (recycleQueue[index].flightId == buildItem.flightId)
                {
                    doomed = recycleQueue[index];
                    break;
                }
            }
            if (doomed != null)
                recycleQueue.Remove(doomed);

            // Setup our recycle queue if we're out of parts to recycle and we still have more of the vessel to break up.
            if (recycleQueue.Count <= 0 && partsNeedingRecycling.Count > 0)
                processVesselToRecycle();

            if (debugMode)
                Debug.Log(formatPartID() + " - Current recycleQueue count: " + recycleQueue.Count);

            // Give the support breaker another item to process if we have more to process.
            if (recycleQueue.Count > 1)
            {
                BuildItem recycleItem = recycleQueue[recycleQueue.Count - 1];
                if (recycleItem.flightId != dockedVesselInfo.rootPartUId)
                {
                    recycleQueue.Remove(recycleItem);
                    shipbreaker.recycleQueue.Add(recycleItem);
                    if (debugMode)
                        Debug.Log(formatPartID() + " - Support Shipbreaker " + shipbreaker.part.flightID + " asked to recycle " + recycleItem.partName);
                }
            }

            // Update recycler UI's queue
            recyclerUI.recycleQueue.Clear();
            recyclerUI.recycleQueue.AddRange(recycleQueue);
            recyclerUI.recycleQueue.AddRange(partsNeedingRecycling);
        }

        private void handleRecycleJob(double elapsedTime)
        {
            if (recycleQueue == null || recycleQueue.Count <= 0)
                return;

            // Update states
            missingRequirements = false;

            // Get the build item
            BuildItem buildItem = recycleQueue[0];

            // If a support unit is recycling the part then wait for it to finish.
            if (buildItem.waitForSupportCompletion && dockedVesselInfo != null)
            {
                // Failsafe: If no support recycler has this build item in its queue, then switch off the flag.
                if (supportUnitStillProcessing(buildItem))
                    buildItem.waitForSupportCompletion = false;

                recycleStatusText = Localizer.Format("#LOC_SANDCASTLE_waitingForCompletion");
                updateUIStatus();
                return;
            }

            // Check for crew
            Part partToRecycle = part.vessel[buildItem.flightId];
            if (SandcastleScenario.checkForKerbals && partToRecycle != null  && partToRecycle.protoModuleCrew.Count > 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_removeCrew"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                vesselToRecycle = null;
                return;
            }

            // Drain resources if any
            PartResourceDefinition resourceDefinition;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            double vesselAmount, vesselTotalAmount;
            if (partToRecycle != null)
            {
                PartResource resource;
                int resourceCount = partToRecycle.Resources.Count;
                bool hadResourcesToRecycle = false;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    resource = partToRecycle.Resources[resourceIndex];
                    resourceDefinition = definitions[resource.resourceName];

                    // If the resource is electric charge then skip it.
                    if (resource.resourceName == "ElectricCharge")
                        continue;

                    // Get total vessel amounts
                    part.vessel.GetConnectedResourceTotals(resourceDefinition.id, out vesselAmount, out vesselTotalAmount);
                    Debug.Log(formatPartID() + " - vesselAmount: " + vesselAmount + " vesselTotalAmount: " + vesselTotalAmount);

                    // If the resource is nearly full then skip it.
                    if (vesselAmount / vesselTotalAmount >= 0.97)
                        continue;

                    if (debugMode && dockedVesselInfo == null)
                        Debug.Log(formatPartID() + " - draining " + resource.resourceName + " amount remaining: " + resource.amount);

                    // Now drain the resource.
                    if (resource.amount > 0)
                    {
                        hadResourcesToRecycle = true;
                        part.RequestResource(resourceDefinition.id, -recycleSpeedUSec, ResourceFlowMode.ALL_VESSEL);
                        resource.amount -= recycleSpeedUSec;
                        if (resource.amount < 0)
                            resource.amount = 0;
                    }
                }

                if (hadResourcesToRecycle)
                {
                    recycleStatusText = Localizer.Format("#LOC_SANDCASTLE_drainingResources");
                    updateUIStatus();
                    return;
                }
            }

            // Remove the part from the vessel
            scrapPart(buildItem);

            // See if we can find an inventory that has space for the part. If we do, then add the part to the inventory.
            ModuleCargoPart cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            float partVolume = buildItem.isUnpacked ? (float)buildItem.unpackedVolume : -1f;
            if (!buildItem.isBeingRecycled && cargoPart != null && InventoryUtils.HasEnoughSpace(part.vessel, buildItem.availablePart, 1, buildItem.mass, partVolume))
            {
                InventoryUtils.AddItem(part.vessel, buildItem.availablePart, buildItem.variantIndex);
                if (debugMode)
                    Debug.Log(formatPartID() + " - " + buildItem.availablePart.title + " added to inventory");

                // Remove the item from the queue
                recycleQueue.RemoveAt(0);

                // Let interested parties know that we've stored the part.
                SandcastleScenario.onPartRecycled.Fire(this, buildItem);

                // Update our stats
                shipTotalUnitsRecycled += buildItem.totalUnitsRequired;
                totalPartsRecycled += 1;
                recyclerUI.recycleQueue.Clear();
                recyclerUI.recycleQueue.AddRange(recycleQueue);
                recyclerUI.recycleQueue.AddRange(partsNeedingRecycling);
                updateUIStatus();
                return;
            }

            // Record our recycling state.
            buildItem.isBeingRecycled = true;

            // Recycle resources that comprise the part
            int count = buildItem.materials.Count;
            int resourceID;
            ResourceFlowMode flowMode;
            if (buildItem.totalUnitsPrinted < buildItem.totalUnitsRequired)
            {
                // Calculate recycleRate
                float recycleRate = recycleSpeedUSec * calculateSpecialistBonus() * (float)elapsedTime;

                ModuleResource material;
                for (int index = 0; index < count; index++)
                {
                    material = buildItem.materials[index];
                    if (material.resourceDef == null)
                    {
                        resourceID = definitions[material.name].id;
                        flowMode = definitions[material.name].resourceFlowMode;
                    }
                    else
                    {
                        resourceID = material.resourceDef.id;
                        flowMode = material.resourceDef.resourceFlowMode;
                    }

                    if (material.amount > 0)
                    {
                        // produce the resource
                        part.RequestResource(resourceID, -recycleRate * recyclePercentage, flowMode);

                        material.amount -= recycleRate;
                        if (material.amount < 0)
                            material.amount = 0;

                        buildItem.totalUnitsPrinted += recycleRate;
                        if (buildItem.totalUnitsPrinted > buildItem.totalUnitsRequired)
                            buildItem.totalUnitsPrinted = buildItem.totalUnitsRequired;

                        shipTotalUnitsRecycled += recycleRate;
                        if (shipTotalUnitsRecycled > shipTotalUnitsToRecycle)
                            shipTotalUnitsRecycled = shipTotalUnitsToRecycle;
                    }
                }
            }

            // Update progress
            double progress = buildItem.totalUnitsPrinted / buildItem.totalUnitsRequired * 100;
            recycleStatusText = Localizer.Format("#LOC_SANDCASTLE_progress", new string[1] { string.Format("{0:n1}", progress) });
            updateUIStatus();
            if (progress < 100)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // Recycle components
            count = buildItem.requiredComponents.Count;
            string componentName;
            int recycledComponentCount;
            AvailablePart recycledComponent;
            List<PartRequiredComponent> doomed = new List<PartRequiredComponent>();
            PartRequiredComponent component;
            for (int index = 0; index < count; index++)
            {
                component = buildItem.requiredComponents[index];
                componentName = component.name;
                recycledComponentCount = component.amount;
                recycledComponent = PartLoader.getPartInfoByName(componentName);
                cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

                // Make sure that the vessel has enough inventory space
                if (!InventoryUtils.HasEnoughSpace(part.vessel, recycledComponent, recycledComponentCount))
                {
                    recyclerUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsSpace", new string[1] { string.Format("{0:n3}", cargoPart.packedVolume) });
                    updateUIStatus();
                    missingRequirements = true;
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    return;
                }

                // Add the components to an inventory
                doomed.Add(buildItem.requiredComponents[index]);
                for (int recycledIndex = 0; recycledIndex < recycledComponentCount; recycledIndex++)
                {
                    Part inventoryPart = InventoryUtils.AddItem(part.vessel, recycledComponent, 0, part.FindModuleImplementing<ModuleInventoryPart>());
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_recyclerStoredPart", new string[2] { recycledComponent.title, inventoryPart.partInfo.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                    inventoryPart.Highlight(Color.cyan);
                    unHighlightList.Add(lastUpdateTime + kMsgDuration, inventoryPart);
                }
            }
            count = doomed.Count;
            for (int index = 0; index < count; index++)
                buildItem.requiredComponents.Remove(doomed[index]);

            // If we've finished recycling then update the queue and update the UI.
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (buildItem.requiredComponents.Count == 0)
            {
                //ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_recycledPart", new string[1] { buildItem.availablePart.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                if (debugMode)
                    Debug.Log(formatPartID() + " - Finished recycling " + buildItem.availablePart.title);
                recycleQueue.RemoveAt(0);
                totalPartsRecycled += 1;

                SandcastleScenario.onPartRecycled.Fire(this, buildItem);
            }

            if (recycleQueue.Count == 0)
            {
                recycleState = WBIPrintStates.Idle;
            }
            updateUIStatus();
        }

        private void onRecycleStatusUpdate(bool isRecycling)
        {
            if (isRecycling)
            {
                recycleState = recycleQueue.Count > 0 ? WBIPrintStates.Recycling : WBIPrintStates.Idle;
            }
            else
            {
                recycleState = WBIPrintStates.Paused;
            }
        }

        private void onCancelVesselBuild()
        {
            if (recycleState != WBIPrintStates.Recycling)
                return;
            Debug.Log(formatPartID() + " - onCancelVesselBuild");

            // Disable recycling
            DisableRecycler();

            // Clear the UI
            recyclerUI.clearUI();
            shipTotalUnitsRecycled = 0;
            shipTotalUnitsToRecycle = 0;
            totalPartsRecycled = 0;
            totalPartsToRecycle = 0;
            shipName = string.Empty;
            recycleQueue.Clear();
            partsNeedingRecycling.Clear();

            // Decouple what's left of the vessel
            if (dockedVesselInfo == null)
            {
                ScreenMessages.PostScreenMessage("dockedVesselInfo == null", kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // Get the docked vessel's root part.
            Part dockedVesselRootPart = part.vessel[dockedVesselInfo.rootPartUId];
            if (dockedVesselRootPart == null)
            {
                ScreenMessages.PostScreenMessage("dockedVesselRootPart == null", kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            Debug.Log(formatPartID() + " - Calling decoupleVessel");
            part.StartCoroutine(InventoryUtils.decoupleVessel(dockedVesselRootPart, dockedVesselInfo, false));

            dockedVesselInfo = null;
            vesselToRecycle = null;
        }

        private float calculateSpecialistBonus()
        {
            if (!UseSpecialistBonus || part.CrewCapacity == 0 || part.protoModuleCrew.Count == 0)
                return 1.0f;

            // Find crew with the required skill. They must be in the part.
            int count = part.protoModuleCrew.Count;
            ProtoCrewMember astronaut;
            int totalRanks = 0;
            float bonus = 1.0f;

            for (int index = 0; index < count; index++)
            {
                astronaut = part.protoModuleCrew[index];
                if (astronaut.HasEffect(ExperienceEffect))
                    totalRanks += astronaut.experienceLevel;
            }

            return bonus + (totalRanks * SpecialistBonus);
        }

        private void setupAnimation()
        {
            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null || animations.Length == 0)
                return;

            animation = animations[0];
            if (animation == null)
                return;

            animationState = animation[animationName];
            animationState.wrapMode = WrapMode.Loop;
            animation.Stop();
        }
        #endregion
    }
}
