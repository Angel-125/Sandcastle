using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using Sandcastle.Inventory;
using UnityEngine;
using WildBlueCore.KerbalGear;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Provides a KerbalGear-activated print shop that consumes resources exposed on an EVA Kerbal
    /// and stores completed cargo parts in the Kerbal's inventory.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_evaPrintShopTitle")]
    public class WBIModuleEVAPrintShop : WBIBasePrinter, IKerbalGearInventoryListener
    {
        private const string KerbalEVAModulesNode = "KERBAL_EVA_MODULES";
        private const string ModuleNode = "MODULE";

        /// <summary>
        /// Localized label used by the event that opens the EVA print-shop window.
        /// </summary>
        [KSPField]
        public string printShopGUIName = "#LOC_SANDCASTLE_openEVAPrintShop";

        /// <summary>
        /// Localized title used by the EVA print-shop window.
        /// </summary>
        [KSPField]
        public string printShopDialogTitle = "#LOC_SANDCASTLE_evaPrintShopTitle";

        /// <summary>
        /// Maximum printable-part dimensions expressed as Height (X), Width (Y), Length (Z).
        /// Leave empty to restrict printing by volume only.
        /// </summary>
        [KSPField]
        public string maxPartDimensions;

        /// <summary>
        /// Current state displayed in the EVA Kerbal's part action window.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_printState", guiActive = true,
            groupName = "#LOC_SANDCASTLE_printShopGroupName",
            groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName")]
        public string printStateString;

        private PrintShopUI shopUI;
        private List<AvailablePart> filteredParts = new List<AvailablePart>();
        private List<string> whitelistedCategories = new List<string>();
        private ModuleInventoryPart inventory;
        private bool evaPrinterIsActive;
        private bool initialized;

        /// <summary>
        /// Creates the reusable print-shop UI and connects it to this printer's queue.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            shopUI = new PrintShopUI(Localizer.Format(printShopDialogTitle));
            configureUI();
        }

        /// <summary>
        /// Initializes the EVA printer after KSP has loaded its configured fields.
        /// </summary>
        /// <param name="state">KSP's current part-module startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            ensureInitialized();
            configureUI();
            updateEventAvailability();
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
        }

        /// <summary>
        /// Keeps the UI attached to the queue instance restored by KSP.
        /// </summary>
        /// <param name="node">The module's saved or prefab configuration.</param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (shopUI != null)
                shopUI.printQueue = printQueue;
        }

        /// <summary>
        /// Activates the printer when its KerbalGear item is present in the EVA inventory.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();

            evaPrinterIsActive = true;
            ensureInitialized();
            refreshPrintableParts();
            updateEventAvailability();
        }

        /// <summary>
        /// Deactivates the printer and cancels its queue when the enabling gear is removed.
        /// Retained gear no longer receives lifecycle callbacks during ordinary inventory refreshes.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();

            evaPrinterIsActive = false;
            closeUI();
            stopPrinterEffects();
            cancelPrintQueue();
            updateEventAvailability();
        }

        /// <summary>
        /// Refreshes the printer's inventory reference and UI wiring after a retained gear item changes.
        /// </summary>
        /// <param name="changedInventory">The EVA inventory whose contents changed.</param>
        public void OnKerbalGearInventoryChanged(ModuleInventoryPart changedInventory)
        {
            if (!evaPrinterIsActive || changedInventory == null)
                return;

            ensureInitialized();
            if (!initialized || changedInventory != inventory)
                return;

            configureUI();
            updateEventAvailability();
        }

        /// <summary>
        /// Updates PAW state only while the EVA printer gear is active.
        /// </summary>
        public override void OnUpdate()
        {
            if (!evaPrinterIsActive)
                return;

            // KSP can populate KerbalEVA.ModuleInventoryPartReference after the wearable
            // controller activates this module, so keep trying until the inventory is ready.
            if (!initialized)
            {
                ensureInitialized();
                updateEventAvailability();
            }

            base.OnUpdate();
            printStateString = printState.ToString();
        }

        /// <summary>
        /// Advances printing only while the EVA printer gear is active.
        /// </summary>
        public override void FixedUpdate()
        {
            if (!evaPrinterIsActive)
                return;

            // FixedUpdate provides a second initialization opportunity in case Unity has not
            // begun calling OnUpdate yet for this newly activated KerbalGear module.
            if (!initialized)
            {
                ensureInitialized();
                updateEventAvailability();
            }

            if (!initialized)
                return;

            base.FixedUpdate();
        }

        /// <summary>
        /// Closes the printer window when focus changes to another vessel.
        /// </summary>
        /// <param name="newVessel">The newly active vessel.</param>
        public override void onVesselChange(Vessel newVessel)
        {
            base.onVesselChange(newVessel);
            closeUI();
        }

        /// <summary>
        /// Cancels pending jobs when the EVA vessel is destroyed, including when the Kerbal boards.
        /// </summary>
        public override void OnDestroy()
        {
            evaPrinterIsActive = false;
            cancelPrintQueue();
            closeUI();
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            base.OnDestroy();
        }

        /// <summary>
        /// Returns the localized module title shown by KSP.
        /// </summary>
        /// <returns>The EVA print-shop title.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format(printShopDialogTitle);
        }

        /// <summary>
        /// Adds a completed cargo part to the EVA Kerbal's inventory.
        /// </summary>
        /// <param name="buildItem">The completed print job.</param>
        public override void buildItemCompleted(BuildItem buildItem)
        {
            base.buildItemCompleted(buildItem);
            if (buildItem == null || buildItem.skipInventoryAdd || part == null || part.vessel == null)
                return;

            Part inventoryPart = InventoryUtils.AddItem(part.vessel, buildItem.availablePart,
                buildItem.variantIndex, inventory, buildItem.removeResources);
            if (inventoryPart == null)
            {
                Debug.LogError("[Sandcastle] EVA Print Shop could not store completed part " +
                    buildItem.partName + " in the Kerbal's inventory.");
                return;
            }

            ScreenMessages.PostScreenMessage(
                Localizer.Format("#LOC_SANDCASTLE_storedPart", new string[2]
                {
                    buildItem.availablePart.title,
                    inventoryPart.partInfo.title
                }),
                kMsgDuration,
                ScreenMessageStyle.UPPER_LEFT);

            inventoryPart.Highlight(Color.cyan);
            unHighlightList[lastUpdateTime + kMsgDuration] = inventoryPart;
        }

        /// <summary>
        /// Toggles the EVA print-shop window while its printer gear is active.
        /// </summary>
        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_printShopGroupName",
            groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName",
            guiName = "#LOC_SANDCASTLE_openEVAPrintShop")]
        public void OpenGUI()
        {
            if (!evaPrinterIsActive || !initialized || shopUI == null)
                return;

            if (shopUI.IsVisible())
            {
                shopUI.SetVisible(false);
                return;
            }

            refreshPrintableParts();
            shopUI.partsList = filteredParts;
            shopUI.whitelistedCategories = whitelistedCategories;
            shopUI.SetVisible(true);
        }

        /// <summary>
        /// Updates the print-job status shown in the shared print-shop UI.
        /// </summary>
        /// <param name="statusUpdate">The new localized or formatted status.</param>
        protected override void updateUIStatus(string statusUpdate)
        {
            if (shopUI != null)
                shopUI.jobStatus = statusUpdate;
        }

        /// <summary>
        /// Updates the running state shown in the shared print-shop UI.
        /// </summary>
        /// <param name="isPrinting">Whether the current queue is printing.</param>
        protected override void updateUIStatus(bool isPrinting)
        {
            if (shopUI != null)
                shopUI.isPrinting = isPrinting;
        }

        /// <summary>
        /// Verifies that the EVA inventory has room for the completed cargo part.
        /// </summary>
        /// <param name="buildItem">The print job whose output needs inventory space.</param>
        /// <returns>True when an inventory can accept the completed part.</returns>
        protected override bool spaceRequirementsMet(BuildItem buildItem)
        {
            if (buildItem == null || part == null || part.vessel == null || inventory == null)
                return false;

            if (InventoryUtils.HasEnoughSpace(part.vessel, buildItem.availablePart))
                return true;

            ModuleCargoPart cargoPart =
                buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            float requiredVolume = cargoPart != null ? cargoPart.packedVolume : buildItem.packedVolume;
            updateUIStatus(Localizer.Format("#LOC_SANDCASTLE_needsSpace", new string[1]
            {
                string.Format("{0:n3}", requiredVolume)
            }));
            return false;
        }

        /// <summary>
        /// Calculates the EVA Kerbal's specialist bonus without relying on part CrewCapacity.
        /// </summary>
        /// <returns>The multiplier applied to the EVA printer's base speed.</returns>
        protected override float calculateSpecialistBonus()
        {
            if (!UseSpecialistBonus || part == null || part.protoModuleCrew == null ||
                part.protoModuleCrew.Count <= 0)
            {
                return 1.0f;
            }

            ProtoCrewMember astronaut = part.protoModuleCrew[0];
            if (astronaut == null || !astronaut.HasEffect(ExperienceEffect))
                return 1.0f;

            return 1.0f + astronaut.experienceLevel * SpecialistBonus;
        }

        /// <summary>
        /// Prevents a personal EVA printer from accepting distributed shipwright jobs.
        /// </summary>
        /// <param name="sender">The shipwright requesting printer support.</param>
        /// <param name="buildList">The shipwright's remaining build items.</param>
        protected override void onSupportPrintingRequest(WBIShipwright sender, List<BuildItem> buildList)
        {
        }

        /// <summary>
        /// Resolves the EVA inventory and finishes wiring the shared UI.
        /// </summary>
        private void ensureInitialized()
        {
            if (initialized || part == null)
                return;

            KerbalEVA kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA == null)
                return;

            // The stock EVA module owns the authoritative inventory reference. It is assigned
            // during EVA setup and can become available later than this module's OnStart call.
            inventory = kerbalEVA.ModuleInventoryPartReference;
            if (inventory == null)
                inventory = part.FindModuleImplementing<ModuleInventoryPart>();

            if (inventory == null)
                return;

            configureUI();
            initialized = true;
        }

        /// <summary>
        /// Connects the reusable print-shop window to this EVA printer instance.
        /// </summary>
        private void configureUI()
        {
            if (shopUI == null)
                return;

            shopUI.WindowTitle = Localizer.Format(printShopDialogTitle);
            shopUI.part = part;
            shopUI.printQueue = printQueue;
            shopUI.onPrintStatusUpdate = onPrintStatusUpdate;
            shopUI.gravityRequirementsMet = gravityRequirementMet;
            shopUI.pressureRequrementsMet = pressureRequrementsMet;
            shopUI.showPartSpawnButton = false;
            shopUI.showPartDecoupleButton = false;

            if (Events != null && Events.Contains("OpenGUI"))
                Events["OpenGUI"].guiName = Localizer.Format(printShopGUIName);
        }

        /// <summary>
        /// Builds the list of cargo parts this EVA printer can produce.
        /// </summary>
        private void refreshPrintableParts()
        {
            if (!initialized)
                return;

            List<AvailablePart> availableParts =
                InventoryUtils.GetPrintableParts(maxPrintVolume, maxPartDimensions);
            ConfigNode printerNode = getEVAPrinterConfigNode() ?? new ConfigNode(ModuleNode);
            whitelistedCategories = getWhitelistedCategories(printerNode);
            filteredParts = new List<AvailablePart>();

            string[] blacklistedParts = getBlacklistedParts(printerNode);
            string[] whitelistedParts = new string[0];
            if (printerNode.HasNode(kPartWhiteListNode))
                whitelistedParts = printerNode.GetNode(kPartWhiteListNode).GetValues(kWhitelistedPart);

            for (int index = 0; index < availableParts.Count; index++)
            {
                AvailablePart availablePart = availableParts[index];
                if (!whitelistedCategories.Contains(availablePart.category.ToString()))
                    continue;

                if (whitelistedParts.Length > 0)
                {
                    if (whitelistedParts.Contains(availablePart.name))
                        filteredParts.Add(availablePart);
                }
                else if (!blacklistedParts.Contains(availablePart.name))
                {
                    filteredParts.Add(availablePart);
                }
            }
        }

        /// <summary>
        /// Gets the configured category whitelist or all stock categories when none is supplied.
        /// </summary>
        /// <param name="printerNode">The EVA printer's injected module configuration.</param>
        /// <returns>Category names accepted by the printer UI.</returns>
        private List<string> getWhitelistedCategories(ConfigNode printerNode)
        {
            List<string> categories = new List<string>();
            string[] categoryNames = Enum.GetNames(typeof(PartCategories));
            if (printerNode != null && printerNode.HasNode(kCategoryWhitelistNode))
            {
                string[] configuredCategories = printerNode.GetNode(kCategoryWhitelistNode)
                    .GetValues(kWhitelistedCategory);
                if (configuredCategories.Length > 0)
                    categoryNames = configuredCategories;
            }

            for (int index = 0; index < categoryNames.Length; index++)
            {
                PartCategories category;
                if (Enum.TryParse(categoryNames[index], out category))
                    categories.Add(category.ToString());
            }
            return categories;
        }

        /// <summary>
        /// Finds this dynamically injected module's original KERBAL_EVA_MODULES configuration.
        /// </summary>
        /// <returns>The matching MODULE node, or null when none can be found.</returns>
        private ConfigNode getEVAPrinterConfigNode()
        {
            ConfigNode[] evaNodes = GameDatabase.Instance.GetConfigNodes(KerbalEVAModulesNode);
            for (int nodeIndex = 0; nodeIndex < evaNodes.Length; nodeIndex++)
            {
                ConfigNode[] moduleNodes = evaNodes[nodeIndex].GetNodes(ModuleNode);
                for (int moduleIndex = 0; moduleIndex < moduleNodes.Length; moduleIndex++)
                {
                    ConfigNode moduleNode = moduleNodes[moduleIndex];
                    if (!moduleNode.HasValue("name") || moduleNode.GetValue("name") != ClassName)
                        continue;

                    if (!string.IsNullOrEmpty(moduleID) && moduleNode.HasValue("moduleID") &&
                        moduleNode.GetValue("moduleID") != moduleID)
                    {
                        continue;
                    }

                    return moduleNode;
                }
            }
            return null;
        }

        /// <summary>
        /// Cancels all jobs immediately when this EVA Kerbal boards another part.
        /// </summary>
        /// <param name="data">The EVA part being boarded from and the destination part.</param>
        private void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            if (data.from != part)
                return;

            evaPrinterIsActive = false;
            cancelPrintQueue();
            closeUI();
            stopPrinterEffects();
            updateEventAvailability();
        }

        /// <summary>
        /// Permanently discards all pending EVA print jobs and resets printer state.
        /// </summary>
        private void cancelPrintQueue()
        {
            if (printQueue != null)
                printQueue.Clear();

            printState = WBIPrintStates.Idle;
            currentJob = string.Empty;
            missingRequirements = false;
            lastUpdateTime = HighLogic.LoadedSceneIsFlight
                ? Planetarium.GetUniversalTime()
                : 0.0;

            if (shopUI != null)
            {
                shopUI.isPrinting = false;
                shopUI.jobStatus = string.Empty;
            }
        }

        /// <summary>
        /// Hides the reusable print-shop window if it is currently open.
        /// </summary>
        private void closeUI()
        {
            if (shopUI != null && shopUI.IsVisible())
                shopUI.SetVisible(false);
        }

        /// <summary>
        /// Stops optional part effects and animations when EVA printing is disabled.
        /// </summary>
        private void stopPrinterEffects()
        {
            if (part != null && !string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, 0);

            if (animation == null)
                return;

            animation[animationName].speed = 0f;
            animation.Stop();
        }

        /// <summary>
        /// Shows the PAW event only while the KerbalGear printer is active and initialized.
        /// </summary>
        private void updateEventAvailability()
        {
            if (Events != null && Events.Contains("OpenGUI"))
                Events["OpenGUI"].active = evaPrinterIsActive && initialized;
        }
    }
}
