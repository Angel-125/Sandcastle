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
    [KSPModule("#LOC_SANDCASTLE_printShopTitle")]
    public class WBIPrintShop : WBIBasePrinter
    {
        #region Fields
        #endregion

        #region Housekeeping
        /// <summary>
        /// GUI name to use for the event that opens the printer GUI.
        /// </summary>
        [KSPField]
        public string printShopGUIName = "#LOC_SANDCASTLE_openGUI";

        /// <summary>
        /// Alternate group display name to use.
        /// </summary>
        [KSPField]
        public string printShopwGroupDisplayName;

        /// <summary>
        /// Title to use for the print shop dialog
        /// </summary>
        [KSPField]
        public string printShopDialogTitle;

        /// <summary>
        /// Current print state.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_printState", guiActive = true, groupName = "#LOC_SANDCASTLE_printShopGroupName", groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName")]
        public string printStateString;

        /// <summary>
        /// Flag indicating that part spawn is enabled. This lets the printer spawn parts into the world instead of putting them into an inventory.
        /// </summary>
        [KSPField]
        public bool enablePartSpawn = false;

        /// <summary>
        /// Maximum possible craft size that can be printed: Height (X) Width (Y) Length (Z).
        /// Leave empty for unlimited printing.
        /// </summary>
        [KSPField]
        public string maxPartDimensions;

        /// <summary>
        /// Flag to indicate if it should offset the printed vessel to avoid collisions. Recommended to set to FALSE for printers with enclosed printing spaces.
        /// </summary>
        [KSPField]
        public bool repositionCraftBeforeSpawning = true;

        List<AvailablePart> filteredParts = null;
        PrintShopUI shopUI = null;
        List<string> whitelistedCategories;
        BuildItem buildItemToSpawn = null;
        DockedVesselInfo dockedPartInfo = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Update the filtered list of cargo parts
            updateFilteredParts();
            updatePartPrintingAvailability();

            if (dockedPartInfo != null)
            {
                shopUI.showPartSpawnButton = false;
                shopUI.showPartDecoupleButton = true;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            updatePartPrintingAvailability();
            printStateString = printState.ToString();
        }

        public override void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight &&
                !isPartPrintingAvailable())
            {
                updatePartPrintingAvailability();
                return;
            }

            base.FixedUpdate();
        }

        public override void OnAwake()
        {
            base.OnAwake();

            string titleText = Localizer.Format("#LOC_SANDCASTLE_printShopTitle");
            if (!string.IsNullOrEmpty(printShopDialogTitle))
                titleText = Localizer.Format(printShopDialogTitle);

            shopUI = new PrintShopUI(titleText);
            shopUI.part = part;
            shopUI.printQueue = printQueue;
            shopUI.onPrintStatusUpdate = onPrintStatusUpdate;
            shopUI.gravityRequirementsMet = gravityRequirementMet;
            shopUI.pressureRequrementsMet = pressureRequrementsMet;
            shopUI.onSpawnPrintedPart = onSpawnPrintedPart;
            shopUI.onDecouplePrintedPart = onDecouplePrintedPart;

            if (!string.IsNullOrEmpty(printShopwGroupDisplayName))
                Fields["printStateString"].group.displayName = printShopwGroupDisplayName;

            if (!string.IsNullOrEmpty(printShopGUIName))
                Events["OpenGUI"].guiName = Localizer.Format(printShopGUIName);
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
        }

        public override void onVesselChange(Vessel newVessel)
        {
            base.onVesselChange(newVessel);

            if (shopUI.IsVisible())
                shopUI.SetVisible(false);
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_printerDesc"));
            if (maxPrintVolume > 0)
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxPrintVolume", new string[1] { string.Format("{0:n1}", maxPrintVolume) }));
            if (!string.IsNullOrEmpty(maxPartDimensions))
            {
                Vector3 maxDimensions = KSPUtil.ParseVector3(maxPartDimensions);
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsLength", new string[1] { string.Format("{0:n1}", maxDimensions.z) }));
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsWidth", new string[1] { string.Format("{0:n1}", maxDimensions.y) }));
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsHeight", new string[1] { string.Format("{0:n1}", maxDimensions.x) }));
            }
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_printSpeed", new string[1] { string.Format("{0:n1}", printSpeedUSec) }));
            info.Append(base.GetInfo());
            return info.ToString();
        }

        public override string GetModuleDisplayName()
        {
            if (!string.IsNullOrEmpty(printShopDialogTitle))
                return Localizer.Format(printShopDialogTitle);

            return base.GetModuleDisplayName();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // KSP can replace the public queue during module load after OnAwake.
            shopUI.printQueue = printQueue;

            if (node.HasNode("DOCKED_PART_INFO"))
            {
                dockedPartInfo = new DockedVesselInfo();
                dockedPartInfo.Load(node.GetNode("DOCKED_PART_INFO"));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (dockedPartInfo != null)
            {
                ConfigNode dockedPartNode = new ConfigNode("DOCKED_PART_INFO");
                dockedPartInfo.Save(dockedPartNode);
                node.AddNode(dockedPartNode);
            }
        }
        #endregion

        #region Helpers
        protected override void onSupportPrintingRequest(WBIShipwright sender, List<BuildItem> buildList)
        {
            if (!isPartPrintingAvailable())
                return;

            if (sender.part.flightID == part.flightID)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle " + part.flightID + "] - " + " I've been asked by " + sender.part.flightID + " to print an item but I'm the same printer!");
                return;
            }

            // If this is a part printer, and there is a Shipwright in the part, then we defer to it.
            if (enablePartSpawn && part.FindModuleImplementing<WBIShipwright>() != null)
                return;

            // Let the base class handle it.
            base.onSupportPrintingRequest(sender, buildList);
        }

        public override void buildItemCompleted(BuildItem buildItem)
        {
            base.buildItemCompleted(buildItem);

            // If we should spawn the item, then pause printing and enable the spawn item UI
            if (enablePartSpawn)
            {
                if (printQueue.Count > 1)
                    printState = WBIPrintStates.Paused;
                else
                    printState = WBIPrintStates.Idle;

                // If in timewarp and our queue is empty then kick out of timewarp
                if (TimeWarp.CurrentRateIndex > 0 && printQueue.Count <= 0)
                    TimeWarp.SetRate(0, true);

                // Update the GUI
                shopUI.isPrinting = false;
                shopUI.showPartSpawnButton = true;

                // Record the part to spawn
                buildItemToSpawn = buildItem;
            }
            else if (!buildItem.skipInventoryAdd)
            {
                // Add the item to an inventory
                Part inventoryPart = InventoryUtils.AddItem(part.vessel, buildItem.availablePart, buildItem.variantIndex, part.FindModuleImplementing<ModuleInventoryPart>(), buildItem.removeResources);
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_storedPart", new string[2] { buildItem.availablePart.title, inventoryPart.partInfo.title }), kMsgDuration, ScreenMessageStyle.UPPER_LEFT);
                inventoryPart.Highlight(Color.cyan);
                unHighlightList.Add(lastUpdateTime + kMsgDuration, inventoryPart);
            }
        }

        protected override void updateUIStatus(string statusUpdate)
        {
            shopUI.jobStatus = statusUpdate;
        }

        protected override void updateUIStatus(bool isPrinting)
        {
            shopUI.isPrinting = isPrinting;
        }

        protected override bool spaceRequirementsMet(BuildItem buildItem)
        {
            ModuleCargoPart cargoPart = buildItem.availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

            if (!InventoryUtils.HasEnoughSpace(part.vessel, buildItem.availablePart) && !enablePartSpawn)
            {
                shopUI.jobStatus = Localizer.Format("#LOC_SANDCASTLE_needsSpace", new string[1] { string.Format("{0:n3}", cargoPart.packedVolume) });
                return false;
            }

            return true;
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_printShopGroupName", groupDisplayName = "#LOC_SANDCASTLE_printShopGroupName", guiName = "#LOC_SANDCASTLE_openGUI")]
        public void OpenGUI()
        {
            if (!isPartPrintingAvailable())
                return;

            shopUI.partsList = filteredParts;
            shopUI.whitelistedCategories = whitelistedCategories;
            shopUI.SetVisible(true);

            WBIShipbreaker shipbreaker = part.FindModuleImplementing<WBIShipbreaker>();
            if (shipbreaker != null)
            {
                shipbreaker.DisableRecycler();
            }
        }
        #endregion

        #region Helpers
        private bool isPartPrintingAvailable()
        {
            // Inventory printing remains available in flight. Only printers
            // configured to spawn completed parts into the world are restricted
            // to landed or splashed vessels.
            return !enablePartSpawn ||
                (part != null && part.vessel != null &&
                part.vessel.LandedOrSplashed);
        }

        private void updatePartPrintingAvailability()
        {
            bool isAvailable = isPartPrintingAvailable();
            Events["OpenGUI"].active = isAvailable;

            if (isAvailable)
            {
                if (printState == WBIPrintStates.Unavailable)
                    printState = WBIPrintStates.Idle;
                return;
            }

            if (shopUI.IsVisible())
                shopUI.SetVisible(false);

            if (printQueue != null && printQueue.Count > 0)
            {
                Debug.Log(string.Format(
                    "[Sandcastle {0}] - Clearing {1} part-printing job(s) because the vessel is not landed or splashed.",
                    part.flightID, printQueue.Count));
                printQueue.Clear();
            }

            buildItemToSpawn = null;
            printState = WBIPrintStates.Unavailable;
            lastUpdateTime = Planetarium.GetUniversalTime();
            shopUI.isPrinting = false;
            shopUI.showPartSpawnButton = false;
            part.Effect(runningEffect, 0);
            if (animation != null)
            {
                animation[animationName].speed = 0f;
                animation.Stop();
            }
        }

        private void onSpawnPrintedPart()
        {
            if (!isPartPrintingAvailable() ||
                buildItemToSpawn == null || spawnTransform == null)
                return;

            shopUI.showPartSpawnButton = false;

            // Spawn the part at the configured boundary.
            InventoryUtils.SpawnPart(buildItemToSpawn.availablePart, part,
                spawnTransform, repositionCraftBeforeSpawning,
                new Callback<DockedVesselInfo>(onPrintedPartCoupled));

            buildItemToSpawn = null;
        }

        private void onPrintedPartCoupled(DockedVesselInfo dockedVesselInfo)
        {
            dockedPartInfo = dockedVesselInfo;
            shopUI.showPartSpawnButton = false;
            shopUI.showPartDecoupleButton = true;
        }

        private void onDecouplePrintedPart()
        {
            if (dockedPartInfo == null)
                return;

            Part dockedPart = part.vessel[dockedPartInfo.rootPartUId];
            if (dockedPart == null)
            {
                Debug.LogWarning("[Sandcastle] - Unable to find the coupled printed part.");
                return;
            }

            DockedVesselInfo partInfo = dockedPartInfo;
            dockedPartInfo = null;
            shopUI.showPartDecoupleButton = false;
            part.StartCoroutine(InventoryUtils.releaseOrbitalPrintedPart(
                dockedPart, partInfo, part, spawnTransform, true));
        }

        private void updateFilteredParts()
        {
            List<AvailablePart> availableParts = InventoryUtils.GetPrintableParts(maxPrintVolume, maxPartDimensions);
            ConfigNode node = getPartConfigNode();
            PartCategories category;
            whitelistedCategories = new List<string>();
            filteredParts = new List<AvailablePart>();

            // Get the whitelisted categories
            if (node != null && node.HasNode(kCategoryWhitelistNode))
            {
                ConfigNode categoryNode = node.GetNode(kCategoryWhitelistNode);
                string[] categories = categoryNode.GetValues(kWhitelistedCategory);
                if (categories.Length == 0)
                    categories = Enum.GetNames(typeof(PartCategories));
                for (int index = 0; index < categories.Length; index++)
                {
                    if (Enum.TryParse(categories[index], out category))
                    {
                        whitelistedCategories.Add(category.ToString());
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
                        whitelistedCategories.Add(category.ToString());
                    }
                }
            }

            // Get whitelisted parts. They can be printed regardless of whether or not the part is on the blacklist.
            string[] blacklistedParts = getBlacklistedParts(node);
            if (node != null && node.HasNode(kPartWhiteListNode))
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
                    if (whitelistedParts.Contains(availablePart.name) && whitelistedCategories.Contains(availablePart.category.ToString()))
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
        #endregion
    }
}
