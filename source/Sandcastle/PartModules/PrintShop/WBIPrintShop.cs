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
    public class WBIPrintShop : SCBasePrinter
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
        /// Axis upon which to displace the part during spawn in. X, Y, Z
        /// </summary>
        [KSPField]
        public string offsetAxis = "0,1,1";

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
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Update the filtered list of cargo parts
            updateFilteredParts();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            printStateString = printState.ToString();
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
        #endregion

        #region Helpers
        protected override void onSupportPrintingRequest(SCShipwright sender, List<BuildItem> buildList)
        {
            if (sender.part.flightID == part.flightID)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle " + part.flightID + "] - " + " I've been asked by " + sender.part.flightID + " to print an item but I'm the same printer!");
                return;
            }

            // If this is a part printer, and there is a Shipwright in the part, then we defer to it.
            if (enablePartSpawn && part.FindModuleImplementing<SCShipwright>() != null)
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
            shopUI.partsList = filteredParts;
            shopUI.whitelistedCategories = whitelistedCategories;
            shopUI.SetVisible(true);

            SCShipbreaker shipbreaker = part.FindModuleImplementing<SCShipbreaker>();
            if (shipbreaker != null)
            {
                shipbreaker.DisableRecycler();
            }
        }
        #endregion

        #region Helpers
        private void onSpawnPrintedPart()
        {
            if (buildItemToSpawn == null || spawnTransform == null)
                return;

            shopUI.showPartSpawnButton = false;

            // Spawn the part.
            Vector3 axis = KSPUtil.ParseVector3(offsetAxis);
            if (!repositionCraftBeforeSpawning)
                axis = Vector3.zero;
            InventoryUtils.SpawnPart(buildItemToSpawn.availablePart, part, spawnTransform, axis);

            buildItemToSpawn = null;
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
