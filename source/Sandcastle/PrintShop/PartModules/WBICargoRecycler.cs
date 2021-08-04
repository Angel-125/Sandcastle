using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Represents a shop that is capable of printing items and placing them in an available inventory.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_recyclerTitle")]
    public class WBICargoRecycler: PartModule
    {
        #region Constants
        const double kCatchupTime = 3600;
        const float kMsgDuration = 5;
        const float kInventoryRefreshDelay = 3;
        const string kRecycleState = "recycleState";
        const string kRecyclerGroup = "PrintShop";
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
        #endregion

        #region Housekeeping
        /// <summary>
        /// Represents the list of build items to recycle.
        /// </summary>
        public List<BuildItem> recycleQueue;

        /// <summary>
        /// Current state of the recycler.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_recycleState", guiActive = true, groupName = kRecyclerGroup, groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName")]
        public WBIPrintStates recycleState = WBIPrintStates.Idle;

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

        RecyclerUI recyclerUI = null;
        double inventoryRefreshTime = 0;
        bool missingRequirements = false;
        Dictionary<double, Part> unHighlightList = null;
        List<AvailablePart> partsToRecycle = null;
        #endregion

        #region FixedUpdate
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Handle unhighlight
            handleUnhighlightParts();

            // If the recycler is pased then we're done
            if (recycleState == WBIPrintStates.Paused)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recyclerUI.jobStatus = recycleState.ToString();
                part.Effect(runningEffect, 0);
                return;
            }

            // If there are no items in the queue then we're done.
            if (recycleQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleState = WBIPrintStates.Idle;
                recyclerUI.jobStatus = recycleState.ToString();
                part.Effect(runningEffect, 0);

                if (lastUpdateTime > inventoryRefreshTime)
                {
                    // Update the list of cargo parts
                    partsToRecycle = InventoryUtils.GetPartsToRecycle(part.vessel);
                    recyclerUI.partsList = partsToRecycle;
                    if (partsToRecycle.Count > 0)
                    {
                        recyclerUI.updateThumbnails();
                        recyclerUI.updatePartPreview(0);
                    }
                    inventoryRefreshTime = lastUpdateTime + kInventoryRefreshDelay;
                }
                return;
            }

            // Play effects
            part.Effect(runningEffect, 1);

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

            // Update the list of cargo parts
            partsToRecycle = InventoryUtils.GetPartsToRecycle(part.vessel);

            // Watch for game events
            GameEvents.onVesselChange.Add(onVesselChange);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            unHighlightList = new Dictionary<double, Part>();
            recycleQueue = new List<BuildItem>();
            recyclerUI = new RecyclerUI();
            recyclerUI.part = part;
            recyclerUI.recycleQueue = recycleQueue;
            recyclerUI.onRecycleStatus = onRecycleStatus;
            recyclerUI.recyclePercentage = recyclePercentage;
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (recyclerUI.IsVisible())
                recyclerUI.SetVisible(false);
        }

        public void Destroy()
        {
            if (recyclerUI.IsVisible())
                recyclerUI.SetVisible(false);
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        private void onVesselChange(Vessel newVessel)
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

            // Recycle Queue
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
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Recycle state
            node.AddValue(kRecycleState, recycleState.ToString());

            // Recycle queue
            ConfigNode buildItemNode;
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                buildItemNode = recycleQueue[index].Save();
                node.AddNode(buildItemNode);
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recyclerDesc"));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recycleSpeed", new string[1] { string.Format("{0:n1}", recycleSpeedUSec) }));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recyclePercent", new string[1] { string.Format("{0:n1}", recyclePercentage * 100f) }));
            info.Append(base.GetInfo());
            return info.ToString();
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, groupName = kRecyclerGroup, groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName", guiName = "#LOC_SANDCASTLE_openRecyclerGUI")]
        public void OpenGUI()
        {
            recyclerUI.partsList = partsToRecycle;
            recyclerUI.SetVisible(true);
        }
        #endregion

        #region API
        #endregion

        #region Helpers
        private void processRecycleQueue()
        {
            // Check the recycle queue again.
            if (recycleQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleState = WBIPrintStates.Idle;
                return;
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
                        recyclerUI.jobStatus = error;
                        return;
                    }
                }
            }
            if (resHandler.outputResources.Count > 0)
                resHandler.UpdateModuleResourceOutputs();

            // Handle the current print job.
            handleRecycleJob(TimeWarp.fixedDeltaTime);
        }

        private void handleCatchup()
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

        private void handleUnhighlightParts()
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

        private void handleRecycleJob(double elapsedTime)
        {
            // Update states
            missingRequirements = false;
            recyclerUI.isRecycling = true;

            // Get the build item
            BuildItem buildItem = recycleQueue[0];
            ModuleCargoPart cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

            // Remove item from inventory
            int count = 0;
            ModuleInventoryPart inventory = InventoryUtils.GetInventoryWithPart(part.vessel, buildItem.partName);
            if (inventory != null && !buildItem.isBeingRecycled)
            {
                // Empty the part of any resources
                StoredPart storedPart = null;
                count = inventory.storedParts.Keys.Count;
                for (int index = 0; index < count; index++)
                {
                    if (inventory.storedParts[index].partName == buildItem.partName)
                    {
                        storedPart = inventory.storedParts[index];
                        break;
                    }
                }
                if (storedPart != null)
                {
                    count = storedPart.snapshot.resources.Count;
                    ProtoPartResourceSnapshot resource;
                    for (int index = 0; index < count; index++)
                    {
                        resource = storedPart.snapshot.resources[index];
                        if (resource.amount > 0)
                        {
                            part.RequestResource(resource.definition.id, -resource.amount, resource.definition.resourceFlowMode);
                        }
                    }
                }

                // Take note that we're recycling a part that we've removed from the inventory.
                buildItem.isBeingRecycled = true;
                inventory.RemoveNPartsFromInventory(buildItem.partName, 1);
            }
            else if (!buildItem.isBeingRecycled)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                recycleQueue.RemoveAt(0);
                return;
            }

            // Recycle resources
            if (buildItem.totalUnitsPrinted < buildItem.totalUnitsRequired)
            {
                // Calculate recycleRate
                float recycleRate = recycleSpeedUSec * calculateSpecialistBonus() * (float)elapsedTime;

                ModuleResource material;
                count = buildItem.materials.Count;
                for (int index = 0; index < count; index++)
                {
                    material = buildItem.materials[index];

                    if (material.amount > 0)
                    {
                        // produce the resource
                        part.RequestResource(material.resourceDef.id, -recycleRate * recyclePercentage, material.resourceDef.resourceFlowMode);

                        material.amount -= recycleRate;
                        if (material.amount < 0)
                            material.amount = 0;

                        buildItem.totalUnitsPrinted += recycleRate;
                        if (buildItem.totalUnitsPrinted > buildItem.totalUnitsRequired)
                            buildItem.totalUnitsPrinted = buildItem.totalUnitsRequired;
                    }
                }
            }

            // Update progress
            double progress = (buildItem.totalUnitsPrinted / buildItem.totalUnitsRequired) * 100;
            recyclerUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_progress", new string[1] { string.Format("{0:n1}", progress) });
            if (progress < 100)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // Recycle components
            count = buildItem.requiredComponents.Count;
            string componentName;
            AvailablePart recycledComponent;
            List<string> doomed = new List<string>();
            for (int index = 0; index < count; index++)
            {
                componentName = buildItem.requiredComponents[index];
                recycledComponent = PartLoader.getPartInfoByName(componentName);
                cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

                // Make sure that the vessel has enough inventory space
                if (!InventoryUtils.HasEnoughSpace(part.vessel, recycledComponent))
                {
                    recyclerUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsSpace", new string[1] { string.Format("{0:n3}", cargoPart.packedVolume) });
                    missingRequirements = true;
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    return;
                }

                // Add the component to an inventory
                doomed.Add(componentName);
                Part inventoryPart = InventoryUtils.AddItem(part.vessel, recycledComponent, part.FindModuleImplementing<ModuleInventoryPart>());
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_recyclerStoredPart", new string[2] { recycledComponent.title, inventoryPart.partInfo.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                inventoryPart.Highlight(Color.cyan);
                unHighlightList.Add(lastUpdateTime + kMsgDuration, inventoryPart);
            }
            count = doomed.Count;
            for (int index = 0; index < count; index++)
                buildItem.requiredComponents.Remove(doomed[index]);

            // If we've finished recycling then update the queue and inform the user.
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (buildItem.requiredComponents.Count == 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_recycledPart", new string[1] { buildItem.availablePart.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                recycleQueue.RemoveAt(0);

                // Set timer to refresh our inventory
                if (recycleQueue.Count == 0)
                    inventoryRefreshTime = lastUpdateTime + kInventoryRefreshDelay;
            }
        }

        private void onRecycleStatus(bool isRecycling)
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

        private float calculateSpecialistBonus()
        {
            if (!UseSpecialistBonus || part.CrewCapacity == 0 || part.protoModuleCrew.Count == 0)
                return 1.0f;

            // Find crew with the required skill. They must be in the part.
            int count = part.protoModuleCrew.Count;
            ProtoCrewMember astronaut;
            int highestRank = 0;
            float bonus = 1.0f;

            for (int index = 0; index < count; index++)
            {
                astronaut = part.protoModuleCrew[index];
                if (astronaut.HasEffect(ExperienceEffect) && astronaut.experienceLevel > highestRank)
                    highestRank = astronaut.experienceLevel;
                if (highestRank >= 5)
                    break;
            }

            return bonus + (highestRank * SpecialistBonus);
        }
        #endregion
    }
}
