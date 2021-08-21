using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;

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

    /// <summary>
    /// Represents a shop that is capable of printing items and placing them in an available inventory.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_printShopTitle")]
    public class WBIPrintShop : WBIPartModule
    {
        #region Constants
        const double kCatchupTime = 3600;
        const float kMsgDuration = 5;
        const string kPrintState = "printState";
        const string kPrintShopGroup = "PrintShop";
        const string kPartWhiteListNode = "PARTS_WHITELIST";
        const string kWhitelistedPart = "whitelistedPart";
        const string kPartBlackListNode = "PARTS_BLACKLIST";
        const string kBlacklistedPart = "blacklistedPart";
        const string kCategoryWhitelistNode = "CATEGORY_WHITELIST";
        const string kWhitelistedCategory = "whitelistedCategory";
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
        #endregion

        #region Housekeeping
        /// <summary>
        /// Represents the list of build items to print.
        /// </summary>
        public List<BuildItem> printQueue;

        /// <summary>
        /// Current state of the printer.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_printState", guiActive = true, groupName = kPrintShopGroup, groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName")]
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

        List<AvailablePart> filteredParts = null;
        PrintShopUI shopUI = null;
        double printResumeTime = 0;
        bool missingRequirements = false;
        Dictionary<double, Part> unHighlightList = null;
        List<PartCategories> whitelistedCategories;
        #endregion

        #region FixedUpdate
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Handle unhighlight
            handleUnhighlightParts();

            // If the printer is pased then we're done
            if (printState == WBIPrintStates.Paused)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                shopUI.jobStatus = printState.ToString();
                part.Effect(runningEffect, 0);
                return;
            }

            // If there are no items in the print queue then we're done.
            if (printQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                printState = WBIPrintStates.Idle;
                shopUI.jobStatus = printState.ToString();
                part.Effect(runningEffect, 0);
                return;
            }

            // Play effects
            part.Effect(runningEffect, 1);

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

            // Update the filtered list of cargo parts
            updateFilteredParts();

            // Watch for game events
            GameEvents.onVesselChange.Add(onVesselChange);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            unHighlightList = new Dictionary<double, Part>();
            printQueue = new List<BuildItem>();
            shopUI = new PrintShopUI();
            shopUI.part = part;
            shopUI.printQueue = printQueue;
            shopUI.onPrintStatusUpdate = onPrintStatusUpdate;
            shopUI.gravityRequirementsMet = gravityRequirementMet;
            shopUI.pressureRequrementsMet = pressureRequrementsMet;
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
        }

        public void Destroy()
        {
            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        private void onVesselChange(Vessel newVessel)
        {
            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
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

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_printerDesc"));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxPrintVolume", new string[1] { string.Format("{0:n1}", maxPrintVolume) }));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_printSpeed", new string[1] { string.Format("{0:n1}", printSpeedUSec) }));
            info.Append(base.GetInfo());
            return info.ToString();
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, groupName = kPrintShopGroup, groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName", guiName = "#LOC_SANDCASTLE_openGUI")]
        public void OpenGUI()
        {
            shopUI.partsList = filteredParts;
            shopUI.whitelistedCategories = whitelistedCategories;
            shopUI.SetVisible(true);
        }
        #endregion

        #region API
        #endregion

        #region Helpers
        private void processPrintQueue()
        {
            // Check the print queue again.
            if (printQueue.Count == 0)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();
                printState = WBIPrintStates.Idle;
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
                        shopUI.jobStatus = error;
                        return;
                    }
                }
            }
            if (resHandler.outputResources.Count > 0)
                resHandler.UpdateModuleResourceOutputs();

            // Handle the current print job.
            handlePrintJob(TimeWarp.fixedDeltaTime);
        }

        private void handleCatchup()
        {
            BuildItem buildItem;
            double printTimeRemaining = 0;
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdateTime;
            while (elapsedTime > TimeWarp.fixedDeltaTime * 2 && printQueue.Count > 0)
            {
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

        private bool pressureRequrementsMet(float minimumPressure)
        {
            if (minimumPressure < 0)
                return true;

            if (minimumPressure < 0.001)
                return part.vessel.staticPressurekPa < 0.001;
            else
                return part.vessel.staticPressurekPa < minimumPressure;
        }

        private bool gravityRequirementMet(float minimumGravity)
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

        private void handlePrintJob(double elapsedTime)
        {
            // Update states
            missingRequirements = false;
            shopUI.isPrinting = true;

            // Get the build item
            BuildItem buildItem = printQueue[0];
            ModuleCargoPart cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

            // If we have a gravity requirement, make sure that the requiement is met.
            if (!gravityRequirementMet(buildItem.minimumGravity))
            {
                if (buildItem.minimumGravity > 0)
                    shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsGravity", new string[1] { string.Format("{0:n3}", buildItem.minimumGravity) });
                else
                    shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsZeroGravity");
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // If we have pressure requirements, make sure that the requirement is met.
            if (!pressureRequrementsMet(buildItem.minimumPressure))
            {
                if (buildItem.minimumGravity > 0)
                    shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsPressure", new string[1] { string.Format("{0:n3}", buildItem.minimumGravity) });
                else
                    shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsZeroPressure");
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // Make sure that the vessel has enough inventory space
            if (!InventoryUtils.HasEnoughSpace(part.vessel, buildItem.availablePart))
            {
                shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsSpace", new string[1] { string.Format("{0:n3}", cargoPart.packedVolume) });
                missingRequirements = true;
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            // Consume resources
            int count = 0;
            if (buildItem.totalUnitsPrinted < buildItem.totalUnitsRequired)
            {
                // Calculate consumptionRate
                float consumptionRate = printSpeedUSec * calculateSpecialistBonus() * (float)elapsedTime;

                double amount = 0;
                double maxAmount = 0;
                ModuleResource material;
                count = buildItem.materials.Count;
                for (int index = 0; index < count; index++)
                {
                    material = buildItem.materials[index];

                    if (material.amount > 0)
                    {
                        // Make sure that we have enough of the resource
                        part.GetConnectedResourceTotals(material.resourceDef.id, out amount, out maxAmount);
                        if (amount < consumptionRate)
                        {
                            shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsResource", new string[1] { material.resourceDef.displayName });
                            lastUpdateTime = Planetarium.GetUniversalTime();
                            missingRequirements = true;
                            return;
                        }

                        // Consume the resource
                        part.RequestResource(material.resourceDef.id, consumptionRate, material.resourceDef.resourceFlowMode);

                        material.amount -= consumptionRate;
                        if (material.amount < 0)
                            material.amount = 0;

                        buildItem.totalUnitsPrinted += consumptionRate;
                        if (buildItem.totalUnitsPrinted > buildItem.totalUnitsRequired)
                            buildItem.totalUnitsPrinted = buildItem.totalUnitsRequired;
                    }
                }
            }

            // Update progress
            double progress = (buildItem.totalUnitsPrinted / buildItem.totalUnitsRequired) * 100;
            shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_progress", new string[1] { string.Format("{0:n1}", progress) });
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
                    shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsPart", new string[1] { availablePart.title });
                    lastUpdateTime = Planetarium.GetUniversalTime();
                    missingRequirements = true;
                    return;
                }
            }
            count = doomed.Count;
            for (int index = 0; index < count; index++)
                buildItem.requiredComponents.Remove(doomed[index]);

            // If we've finished printing then add the item to an inventory.
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (buildItem.requiredComponents.Count == 0)
            {
                // Add the item to an inventory
                Part inventoryPart = InventoryUtils.AddItem(part.vessel, buildItem.availablePart, part.FindModuleImplementing<ModuleInventoryPart>());
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_storedPart", new string[2] { buildItem.availablePart.title, inventoryPart.partInfo.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                inventoryPart.Highlight(Color.cyan);
                unHighlightList.Add(lastUpdateTime + kMsgDuration, inventoryPart);

                // Remove the item from the print queue
                printQueue.RemoveAt(0);
            }
        }

        private void onPrintStatusUpdate(bool isPrinting)
        {
            if (isPrinting)
            {
                printState = printQueue.Count > 0 ? WBIPrintStates.Printing : WBIPrintStates.Idle;
            }
            else
            {
                printState = WBIPrintStates.Paused;
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

        private void updateFilteredParts()
        {
            List<AvailablePart> availableParts = InventoryUtils.GetPrintableParts(maxPrintVolume);
            ConfigNode node = getPartConfigNode();
            PartCategories category;
            whitelistedCategories = new List<PartCategories>();
            filteredParts = new List<AvailablePart>();

            // Get the whitelisted categories
            if (node.HasNode(kCategoryWhitelistNode))
            {
                ConfigNode categoryNode = node.GetNode(kCategoryWhitelistNode);
                string[] categories = categoryNode.GetValues(kWhitelistedCategory);
                if (categories.Length == 0)
                    categories = Enum.GetNames(typeof(PartCategories));
                for (int index = 0; index < categories.Length; index++)
                {
                    if (Enum.TryParse(categories[index], out category))
                    {
                        whitelistedCategories.Add(category);
                    }
                }
            }

            // Add all the categories
            else
            {
                string[] categoryNames = Enum.GetNames(typeof(PartCategories));
                for (int index = 0; index < categoryNames.Length; index++)
                {
                    if (Enum.TryParse(categoryNames[index], out category))
                    {
                        whitelistedCategories.Add(category);
                    }
                }
            }

            // Get whitelisted parts. They can be printed regardless of whether or not the part is on the blacklist.
            string[] blacklistedParts = blacklistedParts = getBlacklistedParts(node);
            if (node.HasNode(kPartWhiteListNode))
            {
                ConfigNode partsNode = node.GetNode(kPartWhiteListNode);
                string[] whitelistedParts = partsNode.GetValues(kWhitelistedPart);
                if (whitelistedParts.Length == 0)
                {
                    filteredParts = availableParts;
                    return;
                }
                int count = availableParts.Count;
                AvailablePart availablePart;
                for (int index = 0; index < count; index++)
                {
                    availablePart = availableParts[index];

                    // If the part is on our whitelist then we can print it regardless of black lists.
                    if (whitelistedParts.Contains(availablePart.name) && whitelistedCategories.Contains(availablePart.category))
                        filteredParts.Add(availablePart);
                }
            }

            // We don't have a whitelist so add parts that aren't on our blacklist.
            else
            {
                int count = availableParts.Count;
                AvailablePart availablePart;
                for (int index = 0; index < count; index++)
                {
                    availablePart = availableParts[index];

                    if (!blacklistedParts.Contains(availablePart.name))
                        filteredParts.Add(availablePart);
                }
            }
        }

        private string[] getBlacklistedParts(ConfigNode node)
        {
            List<string> blacklistedParts = new List<string>();
            ConfigNode[] nodes = null;
            ConfigNode blacklistNode;
            string[] values = null;

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
