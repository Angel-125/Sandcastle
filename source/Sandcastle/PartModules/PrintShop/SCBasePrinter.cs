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
    #region Print states enum
    /// <summary>
    /// Lists the different printer states
    /// </summary>
    public enum WBIPrintStates
    {
        /// <summary>
        /// Printer is idle, nothing to print.
        /// </summary>
        Idle,

        /// <summary>
        /// Printer has an item to print but is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Printer is printing something.
        /// </summary>
        Printing,

        /// <summary>
        /// The recycler is recycling something.
        /// </summary>
        Recycling
    }
    #endregion

    public class SCBasePrinter: BasePartModule
    {
        #region Constants
        public const double kCatchupTime = 3600;
        public const float kMsgDuration = 5;
        public const string kPrintState = "printState";
        public const string kPartWhiteListNode = "PARTS_WHITELIST";
        public const string kWhitelistedPart = "whitelistedPart";
        public const string kPartBlackListNode = "PARTS_BLACKLIST";
        public const string kBlacklistedPart = "blacklistedPart";
        public const string kCategoryWhitelistNode = "CATEGORY_WHITELIST";
        public const string kWhitelistedCategory = "whitelistedCategory";
        #endregion

        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// The maximum volume that the printer can print, in liters. Set to less than 0 for no restrictions.
        /// </summary>
        [KSPField]
        public float maxPrintVolume = 500f;

        /// <summary>
        /// The number of resource units per second that the printer can print.
        /// </summary>
        [KSPField]
        public float printSpeedUSec = 1f;

        /// <summary>
        /// Flag to indicate whether or not to allow specialists to improve the print speed. Exactly how the specialist(s) does that is a trade secret.
        /// </summary>
        [KSPField]
        public bool UseSpecialistBonus = true;

        /// <summary>
        /// Per experience rating, how much to improve the print speed by.
        /// The print shop part must have crew capacity.
        /// </summary>
        [KSPField]
        public float SpecialistBonus = 0.05f;

        /// <summary>
        /// The skill required to improve the print speed.
        /// </summary>
        [KSPField]
        public string ExperienceEffect = "ConverterSkill";

        /// <summary>
        /// Name of the effect to play from the part's EFFECTS node when the printer is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// Where to spawn the printed part.
        /// </summary>
        [KSPField]
        public string spawnTransformName;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Represents the list of build items to print.
        /// </summary>
        public List<BuildItem> printQueue;

        /// <summary>
        /// Current state of the printer.
        /// </summary>
        public WBIPrintStates printState = WBIPrintStates.Idle;

        /// <summary>
        /// Describes when the printer was last updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastUpdateTime;

        /// <summary>
        /// Current job being printed.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string currentJob = string.Empty;

        /// <summary>
        /// Name of the animation to play during printing.
        /// </summary>
        [KSPField]
        public string animationName = string.Empty;

        public Animation animation = null;
        protected double printResumeTime = 0;
        public bool missingRequirements = false;
        protected Dictionary<double, Part> unHighlightList = null;
        protected AnimationState animationState;
        protected Transform spawnTransform = null;
        string partsBlacklisted = string.Empty;
        string partsWhitelisted = string.Empty;
        #endregion

        #region FixedUpdate
        public virtual void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Handle unhighlight
            handleUnhighlightParts();

            // If the printer is paused then we're done
            if (printState == WBIPrintStates.Paused)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                updateUIStatus(printState.ToString());
                part.Effect(runningEffect, 0);
                if (animation != null)
                {
                    animation[animationName].speed = 0f;
                    animation.Stop();
                }
                if (debugMode)
                    Debug.Log("[Sandcastle] - Printer paused");
                return;
            }

            // If there are no items in the print queue then we're done.
            if (printQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                printState = WBIPrintStates.Idle;
                updateUIStatus(printState.ToString());
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

            // Process the print queue
            processPrintQueue();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            printResumeTime = Planetarium.GetUniversalTime() + 5;

            // Game settings
            debugMode = SandcastleScenario.debugMode;

            // Watch for game events
            GameEvents.onVesselChange.Add(onVesselChange);
            SandcastleScenario.onSupportPrintingRequest.Add(onSupportPrintingRequest);

            // Setup animations
            setupAnimation();

            // Setup spawn transform
            if (!string.IsNullOrEmpty(spawnTransformName))
                spawnTransform = part.FindModelTransform(spawnTransformName);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            unHighlightList = new Dictionary<double, Part>();
            printQueue = new List<BuildItem>();
        }

        public virtual void onVesselChange(Vessel newVessel)
        {
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Print state
            if (node.HasValue(kPrintState))
                printState = (WBIPrintStates)Enum.Parse(typeof(WBIPrintStates), node.GetValue(kPrintState));

            // Print Queue
            if (node.HasNode(BuildItem.kBuildItemNode))
            {
                BuildItem buildItem;
                ConfigNode[] nodes = node.GetNodes(BuildItem.kBuildItemNode);
                for (int index = 0; index < nodes.Length; index++)
                {
                    buildItem = new BuildItem(nodes[index]);
                    printQueue.Add(buildItem);
                }
                if (debugMode)
                    Debug.Log("[Sandcastle] - Print Queue has " + printQueue.Count + " items OnLoad");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Print state
            node.AddValue(kPrintState, printState.ToString());

            // Print queue
            ConfigNode buildItemNode;
            int count = printQueue.Count;
            for (int index = 0; index < count; index++)
            {
                buildItemNode = printQueue[index].Save();
                node.AddNode(buildItemNode);
            }
        }

        public virtual void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(onVesselChange);
            SandcastleScenario.onSupportPrintingRequest.Remove(onSupportPrintingRequest);
        }
        #endregion

        #region Helpers
        protected virtual void onSupportPrintingRequest(SCShipwright sender, List<BuildItem> buildList)
        {
            if (sender.part.flightID == part.flightID)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle " + part.flightID + "] - " + " I've been asked by " + sender.part.flightID + " to print an item but I'm the same printer!");
                return;
            }

            // If there's only one item in the list then ignore the request. We let the sender of this event handle it.
            int count = buildList.Count;
            if (count == 1)
                return;

            // Add the first part that we're capable of printing to our print queue.
            BuildItem buildItem;
            BuildItem doomed = null;
            for (int index = 0; index < count; index++)
            {
                buildItem = buildList[index];

                if (buildItem.packedVolume <= maxPrintVolume)
                {
                    // When we get done, don't add the item to the inventory.
                    buildItem.skipInventoryAdd = true;

                    // Tell the caller to wait for the print job to be completed.
                    sender.WaitForCompletion(buildItem.flightId);

                    // Add the item to our build queue
                    printQueue.Add(buildItem);
                    doomed = buildItem;
                    break;
                }
            }

            // If we've added an item to our print queue then remove the corresponding item from the build list.
            if (doomed != null)
                buildList.Remove(doomed);

            // Auto-start the printer.
            if (printQueue.Count > 0)
                printState = WBIPrintStates.Printing;

            if (debugMode && doomed != null)
                Debug.Log("[Sandcastle " + part.flightID + "] - " + " I've been asked by " + sender.part.flightID + " to print " + doomed.partName);
        }

        public virtual void buildItemCompleted(BuildItem buildItem)
        {
            if (debugMode)
            {
                Debug.Log("[Sandcastle] - build item completed for: " + buildItem.partName);
                Debug.Log("[Sandcastle] - Print Queue - Items remaining: " + printQueue.Count);
                Debug.Log("[Sandcastle] - Print State: " + printState);
            }
            SandcastleScenario.onPartPrinted.Fire(this, buildItem);
        }

        protected virtual void updateUIStatus(string statusUpdate)
        {

        }

        protected virtual void updateUIStatus(bool isPrinting)
        {

        }

        protected virtual bool spaceRequirementsMet(BuildItem buildItem)
        {
            return false;
        }

        protected virtual void processPrintQueue()
        {
            // Check the print queue again.
            if (printQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                printState = WBIPrintStates.Idle;
                if (debugMode)
                    Debug.Log("[Sandcastle] - Nothing to print!");
                return;
            }

            // Continue with the printing
            printState = WBIPrintStates.Printing;

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
                        updateUIStatus(error);
                        if (debugMode)
                        {
                            Debug.Log("[Sandcastle] - Cannot print, out of resources to run printer");
                            Debug.Log("[Sandcastle] - Reported error: " + error);
                        }
                        return;
                    }
                }
            }
            if (resHandler.outputResources.Count > 0)
                resHandler.UpdateModuleResourceOutputs();

            // Handle the current print job.
            handlePrintJob(TimeWarp.fixedDeltaTime);
        }

        protected virtual void handleCatchup()
        {
            BuildItem buildItem;
            double printTimeRemaining = 0;
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdateTime;
            while (elapsedTime > TimeWarp.fixedDeltaTime * 2 && printQueue.Count > 0)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle] - Handling catchup");

                // We always work with the first item in the queue.
                buildItem = printQueue[0];

                // Update print state
                printState = WBIPrintStates.Printing;

                // Calculate print time remaining
                printTimeRemaining = (buildItem.totalUnitsRequired - buildItem.totalUnitsPrinted) * printSpeedUSec;
                if (printTimeRemaining > elapsedTime)
                    printTimeRemaining = elapsedTime;

                // Handle print job
                handlePrintJob(printTimeRemaining);

                // Update elapsedTime
                elapsedTime -= printTimeRemaining;

                if (printQueue.Count == 0 || missingRequirements)
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

        protected virtual bool pressureRequrementsMet(float minimumPressure)
        {
            if (minimumPressure < 0)
                return true;

            if (minimumPressure < 0.001)
                return part.vessel.staticPressurekPa < 0.001;
            else
                return part.vessel.staticPressurekPa < minimumPressure;
        }

        protected virtual bool gravityRequirementMet(float minimumGravity)
        {
            // If we have no requirements then we're good
            if (minimumGravity < 0)
                return true;

            // Check for microgravity requirements
            if (minimumGravity < 0.00001)
            {
                // Vessel must be orbiting, sub-orbital, or escaping.
                if (part.vessel.situation != Vessel.Situations.ORBITING && part.vessel.situation != Vessel.Situations.SUB_ORBITAL && part.vessel.situation != Vessel.Situations.ESCAPING)
                    return false;

                // Vessel must not be under acceleration
                if (part.vessel.geeForce > 0.001)
                    return false;
            }

            // Check for heavy gravity requirements
            else
            {
                if (part.vessel.LandedOrSplashed || part.vessel.situation == Vessel.Situations.FLYING)
                {
                    // Vessel's gravity at current altitude must meet or exceed the minimum requirement.
                    if (part.vessel.graviticAcceleration.magnitude < minimumGravity)
                        return false;
                }

                // Check vessel acceleration
                else
                {
                    if (part.vessel.geeForce < minimumGravity)
                        return false;
                }
            }

            // All good
            return true;
        }

        protected virtual void handlePrintJob(double elapsedTime)
        {
            // Update states
            missingRequirements = false;
            updateUIStatus(true);

            // Get the build item
            BuildItem buildItem = printQueue[0];

            // If we have a gravity requirement, make sure that the requiement is met.
            string requirementsStatus = string.Empty;
            if (!gravityRequirementMet(buildItem.minimumGravity))
            {
                if (buildItem.minimumGravity > 0)
                    requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_needsGravity", new string[1] { string.Format("{0:n3}", buildItem.minimumGravity) });
                else
                    requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_needsZeroGravity");
                updateUIStatus(requirementsStatus);
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                if (debugMode)
                    Debug.Log("[Sandcastle] - " + requirementsStatus);
                return;
            }

            // If we have pressure requirements, make sure that the requirement is met.
            if (!pressureRequrementsMet(buildItem.minimumPressure))
            {
                if (buildItem.minimumPressure > 0)
                    requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_needsPressure", new string[1] { string.Format("{0:n3}", buildItem.minimumGravity) });
                else
                    requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_needsZeroPressure");
                updateUIStatus(requirementsStatus);
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                if (debugMode)
                    Debug.Log("[Sandcastle] - " + requirementsStatus);
                return;
            }

            // Make sure that the vessel has enough inventory space
            if (!spaceRequirementsMet(buildItem))
            {
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                if (debugMode)
                    Debug.Log("[Sandcastle] - Space requirements not met");
                return;
            }

            // Consume resources
            int count = 0;
            if (buildItem.totalUnitsPrinted < buildItem.totalUnitsRequired)
            {
                // Calculate consumptionRate
                double consumptionRate = printSpeedUSec * calculateSpecialistBonus() * (float)elapsedTime;

                double amount = 0;
                double maxAmount = 0;
                ModuleResource material;
                count = buildItem.materials.Count;
                bool allMaterialsPrinted = true;
                for (int index = 0; index < count; index++)
                {
                    material = buildItem.materials[index];

                    if (material.amount > 0)
                    {
                        allMaterialsPrinted = false;

                        // Make sure that we have enough of the resource
                        part.GetConnectedResourceTotals(material.resourceDef.id, out amount, out maxAmount);
                        if (amount < consumptionRate)
                        {
                            requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_needsResource", new string[1] { material.resourceDef.displayName });
                            updateUIStatus(requirementsStatus);
                            lastUpdateTime = Planetarium.GetUniversalTime();
                            missingRequirements = true;
                            if (debugMode)
                                Debug.Log("[Sandcastle] - " + requirementsStatus);
                            return;
                        }

                        // Adjust consumption rate if needed.
                        if (buildItem.totalUnitsPrinted + consumptionRate > buildItem.totalUnitsRequired)
                        {
                            consumptionRate = buildItem.totalUnitsPrinted + consumptionRate - buildItem.totalUnitsRequired;
                        }
                        buildItem.totalUnitsPrinted += consumptionRate;

                        // Update material amount.
                        material.amount -= consumptionRate;
                        if (material.amount < 0)
                            material.amount = 0;

                        // Consume the resource
                        part.RequestResource(material.resourceDef.id, consumptionRate, ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE);

                        // Update units printed.
                        updateUnitsPrinted(buildItem, consumptionRate);
                    }

                    // Edge case: we might not have printed all the units yet but we have printed all the materials. In this case, update our total units printed.
                    else if (allMaterialsPrinted && buildItem.totalUnitsPrinted < buildItem.totalUnitsRequired)
                    {
                        buildItem.totalUnitsPrinted = buildItem.totalUnitsRequired;
                    }
                }
            }

            // Update progress
            double progress = (buildItem.totalUnitsPrinted / buildItem.totalUnitsRequired) * 100;
            requirementsStatus = Localizer.Format("#LOC_SANDCASTLE_progress", new string[1] { string.Format("{0:n1}", progress) });
            updateUIStatus(requirementsStatus);
            if (progress < 100)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // Consume required components
            count = buildItem.requiredComponents.Count;
            PartRequiredComponent requiredPart;
            List<PartRequiredComponent> doomed = new List<PartRequiredComponent>();
            int partsFound = 0;
            for (int index = 0; index < count; index++)
            {
                requiredPart = buildItem.requiredComponents[index];
                partsFound = InventoryUtils.GetInventoryItemCount(part.vessel, requiredPart.name);
                if (partsFound >= requiredPart.amount)
                {
                    InventoryUtils.RemoveItem(part.vessel, requiredPart.name, requiredPart.amount);
                    doomed.Add(requiredPart);
                }
                else
                {
                    AvailablePart availablePart = PartLoader.getPartInfoByName(requiredPart.name);
                    updateUIStatus(Localizer.Format("#LOC_SANDCASTLE_needsPart", new string[1] { availablePart.title }));
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    missingRequirements = true;
                    return;
                }
            }
            count = doomed.Count;
            for (int index = 0; index < count; index++)
                buildItem.requiredComponents.Remove(doomed[index]);

            // If we've finished printing then signal completion and remove the build item from the print queue.
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (buildItem.requiredComponents.Count == 0)
            {
                // Remove the item from the print queue
                printQueue.RemoveAt(0);
                if (debugMode)
                    Debug.Log("[Sandcastle] - Removed top item from print queue.");

                // Signal build item completed.
                buildItemCompleted(buildItem);

                // If queue is empty, kick out of timewarp and signal that we've completed our print jobs.
                if (printQueue.Count <= 0)
                {
                    TimeWarp.SetRate(0, false);
                    printJobsCompleted();
                }
            }
        }

        protected virtual void updateUnitsPrinted(BuildItem buildItem, double unitsPrinted)
        {

        }

        protected virtual void onPrintStatusUpdate(bool isPrinting)
        {
            if (isPrinting)
            {
                printState = printQueue.Count > 0 ? WBIPrintStates.Printing : WBIPrintStates.Idle;
            }
            else
            {
                printState = WBIPrintStates.Paused;
            }

            updateUIStatus(isPrinting);
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


        protected virtual void printJobsCompleted()
        {
            if (debugMode)
                Debug.Log("[Sandcastle] - printJobsCompleted");
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

        protected bool isBlacklistedPart(AvailablePart availablePart)
        {
            ConfigNode node = getPartConfigNode();

            // Build blacklisted parts list.
            if (string.IsNullOrEmpty(partsBlacklisted) && node != null)
            {
                string[] blacklistedParts = getBlacklistedParts(node);
                StringBuilder builder = new StringBuilder();

                for (int index = 0; index < blacklistedParts.Length; index++)
                    builder.Append(blacklistedParts[index]);

                partsBlacklisted = builder.ToString();
            }

            // Build whitelisted parts list
            if (string.IsNullOrEmpty(partsWhitelisted) && node != null && node.HasNode(kPartWhiteListNode))
            {
                ConfigNode partsNode = node.GetNode(kPartWhiteListNode);
                string[] whitelistedParts = partsNode.GetValues(kWhitelistedPart);
                StringBuilder builder = new StringBuilder();

                for (int index = 0; index < whitelistedParts.Length; index++)
                    builder.Append(whitelistedParts[index]);

                partsWhitelisted = builder.ToString();
            }

            // If a part is on the whitelist then it's automatically NOT blacklisted.
            if (partsWhitelisted.Contains(availablePart.name))
                return false;

            return partsBlacklisted.Contains(availablePart.name);
        }

        protected string[] getBlacklistedParts(ConfigNode node)
        {
            List<string> blacklistedParts = new List<string>();
            ConfigNode[] nodes = null;
            ConfigNode blacklistNode;
            string[] values = null;

            if (node == null)
                return blacklistedParts.ToArray();

            // Handle local blacklist
            if (node.HasNode(kPartBlackListNode))
            {
                blacklistNode = node.GetNode(kPartBlackListNode);
                if (blacklistNode.HasValue(kBlacklistedPart))
                {
                    values = blacklistNode.GetValues(kBlacklistedPart);
                    for (int index = 0; index < values.Length; index++)
                    {
                        if (!blacklistedParts.Contains(values[index]))
                            blacklistedParts.Add(values[index]);
                    }
                }
            }

            // Handle global blacklist
            nodes = GameDatabase.Instance.GetConfigNodes(kPartBlackListNode);
            for (int index = 0; index < nodes.Length; index++)
            {
                blacklistNode = nodes[index];
                if (!blacklistNode.HasValue(kBlacklistedPart))
                    continue;

                values = blacklistNode.GetValues(kBlacklistedPart);
                for (int listIndex = 0; listIndex < values.Length; listIndex++)
                {
                    if (!blacklistedParts.Contains(values[listIndex]))
                        blacklistedParts.Add(values[listIndex]);
                }
            }

            return blacklistedParts.ToArray();
        }

        #endregion

    }
}
