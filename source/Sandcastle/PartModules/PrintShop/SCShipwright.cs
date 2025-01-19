using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;
using KSP.UI.Screens;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Prints entire vessels
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_shipwrightTitle")]
    public class SCShipwright: SCBasePrinter
    {
        #region Fields
        /// <summary>
        /// Alternate transform to use for VAB craft.
        /// </summary>
        [KSPField]
        public string spawnTransformVABName;

        /// <summary>
        /// Alternate transform to use for SPH craft.
        /// </summary>
        [KSPField]
        public string spawnTransformSPHName;

        /// <summary>
        /// Flag to indicate if it should offset the printed vessel to avoid collisions. Recommended to set to FALSE for printers with enclosed printing spaces.
        /// </summary>
        [KSPField]
        public bool repositionCraftBeforeSpawning = true;

        #endregion

        #region Housekeeping
        /// <summary>
        /// Current printer state.
        /// </summary>
        [KSPField(guiName = "#LOC_SANDCASTLE_printState", guiActive = true, groupName = "#LOC_SANDCASTLE_shipwrightGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipwrightGroupName")]
        public string printStateString;

        /// <summary>
        /// Maximum possible craft size that can be printed: Height (X) Width (Y) Length (Z).
        /// Leave empty for unlimited printing.
        /// </summary>
        [KSPField]
        public string maxCraftDimensions;

        CraftBrowserDialog craftBrowserDialog = null;
        string editorFacility;
        string craftFilePath;
        string shipName;
        double shipTotalUnitsRequired = 0;
        double shipTotalUnitsPrinted = 0;
        int totalPartsToPrint = 0;
        int totalPartsPrinted = 0;
        bool finalizeVesselAtStartup = false;
        ShipTemplate shipTemplate = null;
        DockedVesselInfo dockedVesselInfo = null;
        ShipwrightUI shipwrightUI = null;
        bool spawnCraftAfterLoading;
        uint alarmID;
        Vector3 shipSize;
        SCModuleBoundingBox moduleBoundingBox;
        #endregion

        #region Events
        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipwrightGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipwrightGroupName", guiName = "#LOC_SANDCASTLE_openShipwright")]
        public void ShowPrinterDialog()
        {
            shipwrightUI.SetVisible(true);
            SCShipbreaker shipbreaker = part.FindModuleImplementing<SCShipbreaker>();
            if (shipbreaker != null)
            {
                shipbreaker.DisableRecycler();
            }
        }

        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipwrightGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipwrightGroupName", guiName = "(Debug) Load Craft")]
        public void LoadCraft()
        {
            spawnCraftAfterLoading = true;
            onOpenCraftBrowser();
        }

        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipwrightGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipwrightGroupName", guiName = "(Debug) Decouple Craft")]
        public void DecoupleCraft()
        {
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

            part.StartCoroutine(InventoryUtils.decoupleVessel(dockedVesselRootPart, dockedVesselInfo, true));
        }

        [KSPEvent(guiActive = true, groupName = "#LOC_SANDCASTLE_shipwrightGroupName", groupDisplayName = "#LOC_SANDCASTLE_shipwrightGroupName", guiName = "(Debug) Spawn Vessel")]
        public void SpawnVessel()
        {
            onSpawnShip();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            moduleBoundingBox = part.FindModuleImplementing<SCModuleBoundingBox>();

            Events["LoadCraft"].active = debugMode;
            Events["DecoupleCraft"].active = debugMode;
            Events["SpawnVessel"].active = false; // Only needed once we load a vessel.

            if (!string.IsNullOrEmpty(spawnTransformName))
                spawnTransform = part.FindModelTransform(spawnTransformName);

            if (HighLogic.LoadedSceneIsFlight)
                SandcastleScenario.onPartPrinted.Add(onPartPrinted);

            if (HighLogic.LoadedSceneIsFlight && printQueue != null && printQueue.Count > 0)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle] - Setting up shipwrightUI OnStart");
                shipwrightUI.UpdateResourceRequirements();
                shipwrightUI.SetPrintTotals(totalPartsToPrint, totalPartsPrinted, shipTotalUnitsRequired, shipTotalUnitsPrinted);
                shipwrightUI.craftName = shipName;
            }

            if (finalizeVesselAtStartup)
            {
                // If craft was coupled, show decouple button. Otherwise show finalize button.
                shipwrightUI.showSpawnButton = true;

                // Show bounding box
                if (part.vessel.LandedOrSplashed)
                    showVesselBoundingBox();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            printStateString = printState.ToString();

            // Get estimated time to completion
            if (printState != WBIPrintStates.Idle)
            {
                double unitsRemaining = shipTotalUnitsRequired - shipTotalUnitsPrinted;
                double timeRemaining = unitsRemaining / printSpeedUSec;
                string estimatedCompletion = KSPUtil.PrintDateDeltaCompact(timeRemaining, true, true, true);
                shipwrightUI.estimatedCompletion = estimatedCompletion;
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_shipwrightDesc"));
            if (!string.IsNullOrEmpty(maxCraftDimensions))
            {
                Vector3 maxDimensions = KSPUtil.ParseVector3(maxCraftDimensions);
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsLength", new string[1] { string.Format("{0:n1}", maxDimensions.z) }));
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsWidth", new string[1] { string.Format("{0:n1}", maxDimensions.y) }));
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_maxDimensionsHeight", new string[1] { string.Format("{0:n1}", maxDimensions.x) }));
            }
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_printSpeed", new string[1] { string.Format("{0:n1}", printSpeedUSec) }));
            info.Append(base.GetInfo());
            return info.ToString();
        }

        public override void buildItemCompleted(BuildItem buildItem)
        {
            base.buildItemCompleted(buildItem);

            // Update total units printed.
            updateCompletedBuildTotals(buildItem);
        }

        protected override void printJobsCompleted()
        {
            base.printJobsCompleted();
            if (string.IsNullOrEmpty(craftFilePath))
            {
                clearStats();
                return;
            }

            // Show bounding box
            showVesselBoundingBox();

            // Show spawn button
            shipwrightUI.showSpawnButton = true;

            // Edge case: user could go away from the vessel and come back to it. Make sure that we know to show the finalize button
            finalizeVesselAtStartup = true;
        }

        protected override void handlePrintJob(double elapsedTime)
        {
            // Get the build item
            BuildItem buildItem = printQueue[0];

            // If we have the item in the inventory and we can use pre-existing items in the inventory,
            // then pull it from the inventory, consume it, and move to the next item.
            if (InventoryUtils.HasItem(part.vessel, buildItem.partName))
            {
                // Remove the item from inventory
                InventoryUtils.RemoveItem(part.vessel, buildItem.partName);

                // Remove the item from the print queue
                printQueue.RemoveAt(0);

                // Update total units printed
                shipTotalUnitsPrinted += buildItem.totalUnitsPrinted;
                updateCompletedBuildTotals(buildItem);

                // Wait until the next frame.
                if (debugMode)
                    Debug.Log("[Sandcastle] - Pulled " + buildItem.partName + " from inventory and updated print queue.");
                return;
            }

            // If the part is blacklisted and we didn't find it in the inventory then we're stuck and can't print.
            if (buildItem.isBlacklisted)
            {
                updateUIStatus(Localizer.Format("#LOC_SANDCASTLE_needsPart", new string[1] { buildItem.availablePart.title }));
                lastUpdateTime = Planetarium.GetUniversalTime();
                missingRequirements = true;
                return;
            }

            // If we need to wait for a support printer to finish the job, then do so.
            if (buildItem.waitForSupportCompletion && supportPrinterIsPrinting(buildItem))
            {
                updateUIStatus(Localizer.Format("#LOC_SANDCASTLE_waitingForCompletion"));
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            base.handlePrintJob(elapsedTime);
        }

        void showVesselBoundingBox()
        {
            if (moduleBoundingBox != null && spawnTransform != null)
            {
                ShipConstruct shipConstruct = ShipConstruction.LoadShip(craftFilePath);

                // Get the vessel bounds. The center will be relative to the root part.
                Bounds bounds = InventoryUtils.getBounds(shipConstruct.parts[0].localRoot, shipConstruct.parts);
                Vector3 offset = shipConstruct.shipFacility == EditorFacility.VAB ? new Vector3(0, bounds.center.y - spawnTransform.position.y, 0) : new Vector3(0, 0, bounds.center.z - spawnTransform.position.z);

                // Reset the bounds so that it is centered on the spawnTransform. We'll apply the offset shortly.
                bounds = new Bounds(spawnTransform.position, shipSize);

                // Now set up the bounding box.
                moduleBoundingBox.originTransform = spawnTransform;
                moduleBoundingBox.SetupWireframe(spawnTransform, bounds, offset);
                moduleBoundingBox.wireframeIsVisible = true;
                moduleBoundingBox.moveGizmoIsVisible = true;

                // Delete the parts
                // This will get rid of the unusable craft parts that we currently don't need.
                List<Part> doomed = new List<Part>();
                for (int index = 0; index < shipConstruct.parts.Count; index++)
                {
                    if (shipConstruct.parts[index].gameObject != null)
                        doomed.Add(shipConstruct.parts[index]);
                }
                for (int index = 0; index < doomed.Count; index++)
                {
                    shipConstruct.Remove(doomed[index]);
                    doomed[index].OnDelete();
                    Destroy(doomed[index].gameObject);
                }
                FlightGlobals.PersistentVesselIds.Remove(shipConstruct.persistentId);
            }
        }

        void hideVesselBoundingBox()
        {
            if (moduleBoundingBox != null)
            {
                moduleBoundingBox.wireframeIsVisible = false;
                moduleBoundingBox.moveGizmoIsVisible = false;
            }
        }

        bool supportPrinterIsPrinting(BuildItem buildItem)
        {
            // Find all loaded vessels with printers and see if any of them are printing the build item.
            int count = FlightGlobals.VesselsLoaded.Count;
            Vessel loadedVessel;
            List<SCBasePrinter> printers;
            SCBasePrinter printer;
            BuildItem doomed = null;
            for (int index = 0; index < count; index++)
            {
                loadedVessel = FlightGlobals.VesselsLoaded[index];

                printers = loadedVessel.FindPartModulesImplementing<SCBasePrinter>();
                if (printers == null || printers.Count == 0)
                    continue;

                int printerCount = printers.Count;
                for (int printerIndex = 0; printerIndex < printerCount; printerIndex++)
                {
                    printer = printers[printerIndex];
                    if (printer == this)
                        continue;

                    int queueCount = printer.printQueue.Count;
                    for (int queueIndex = 0; queueIndex < queueCount; queueIndex++)
                    {
                        if (printer.printQueue[queueIndex].flightId == buildItem.flightId)
                        {
                            if (debugMode)
                                Debug.Log("[Sandcastle " + part.flightID + "] - Checking if support printer " + printer.part.flightID + " is still printing " + buildItem.partName);

                            // We found the item, now make sure that the printer is still good to go.
                            if (printer.printState != WBIPrintStates.Printing || printer.missingRequirements)
                            {
                                if (debugMode)
                                {
                                    Debug.Log("[Sandcastle " + part.flightID + "] - Support printer " + printer.part.flightID + " is unable to print " + buildItem.partName);
                                    if (printer.printState != WBIPrintStates.Printing)
                                        Debug.Log("[Sandcastle " + part.flightID + "] - Support printer.printState: " + printer.printState);
                                    if (printer.missingRequirements)
                                        Debug.Log("[Sandcastle " + part.flightID + "] - Support printer.missingRequirements: " + printer.missingRequirements);
                                }

                                // Don't wait on the support printer, it's having problems.
                                buildItem.waitForSupportCompletion = false;
                                doomed = printer.printQueue[queueIndex];
                                break;
                            }

                            // Printer is good to go, keep waiting for the job to complete.
                            else
                            {
                                if (debugMode)
                                    Debug.Log("[Sandcastle " + part.flightID + "] - Support printer " + printer.part.flightID + " is still printing " + buildItem.partName);
                                return true;
                            }
                        }
                    }

                    // Remove the item from the support printer; we'll take care of it instead.
                    if (doomed != null && printer.printQueue.Contains(doomed))
                    {
                        printer.printQueue.Remove(doomed);
                        buildItem.waitForSupportCompletion = false;
                        return false;
                    }
                }
            }

            // Couldn't find the build item in any of the printers, or there was a problem with one of them. Make sure we don't wait for a support printer to print it.
            buildItem.waitForSupportCompletion = false;
            return false;
        }

        protected override void updateUnitsPrinted(BuildItem buildItem, double unitsPrinted)
        {
            base.updateUnitsPrinted(buildItem, unitsPrinted);

            // Update overall progress
            shipTotalUnitsPrinted += unitsPrinted;
            shipwrightUI.SetPrintTotals(totalPartsToPrint, totalPartsPrinted, shipTotalUnitsRequired, shipTotalUnitsPrinted);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (shipwrightUI.IsVisible())
                shipwrightUI.SetVisible(false);

            if (HighLogic.LoadedSceneIsFlight)
                SandcastleScenario.onPartPrinted.Remove(onPartPrinted);
        }

        public override void OnInactive()
        {
            base.OnInactive();

            if (shipwrightUI.IsVisible())
                shipwrightUI.SetVisible(false);
        }

        public override void onVesselChange(Vessel newVessel)
        {
            base.onVesselChange(newVessel);

            if (shipwrightUI.IsVisible())
                shipwrightUI.SetVisible(false);
        }

        public override void OnAwake()
        {
            base.OnAwake();

            string titleText = Localizer.Format("#LOC_SANDCASTLE_shipwrightTitle");

            shipwrightUI = new ShipwrightUI(titleText);
            shipwrightUI.part = part;
            shipwrightUI.printQueue = printQueue;
            shipwrightUI.onPrintStatusUpdate = onPrintStatusUpdate;
            shipwrightUI.gravityRequirementsMet = gravityRequirementMet;
            shipwrightUI.pressureRequrementsMet = pressureRequrementsMet;
            shipwrightUI.onOpenCraftBrowser = onOpenCraftBrowser;
            shipwrightUI.onSpawnShip = onSpawnShip;
            shipwrightUI.onDecoupleShip = DecoupleCraft;
            shipwrightUI.onCancelVesselBuild = onCancelVesselBuild;
        }

        protected override void updateUIStatus(string statusUpdate)
        {
            shipwrightUI.jobStatus = statusUpdate;
        }

        protected override void updateUIStatus(bool isPrinting)
        {
            shipwrightUI.isPrinting = isPrinting;
        }

        protected override bool spaceRequirementsMet(BuildItem buildItem)
        {
            // We're printing a vessel, not a part. Nothing to see here...
            return true;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasValue("shipName"))
                shipName = node.GetValue("shipName");

            if (node.HasValue("editorFacility"))
                editorFacility = node.GetValue("editorFacility");

            if (node.HasValue("craftFilePath"))
                craftFilePath = node.GetValue("craftFilePath");

            if (node.HasValue("shipTotalUnitsRequired"))
                double.TryParse(node.GetValue("shipTotalUnitsRequired"), out shipTotalUnitsRequired);

            if (node.HasValue("shipTotalUnitsPrinted"))
                double.TryParse(node.GetValue("shipTotalUnitsPrinted"), out shipTotalUnitsPrinted);

            if (node.HasValue("totalPartsToPrint"))
                int.TryParse(node.GetValue("totalPartsToPrint"), out totalPartsToPrint);

            if (node.HasValue("totalPartsPrinted"))
                int.TryParse(node.GetValue("totalPartsPrinted"), out totalPartsPrinted);

            if (node.HasValue("finalizeVesselAtStartup"))
                bool.TryParse(node.GetValue("finalizeVesselAtStartup"), out finalizeVesselAtStartup);

            if (node.HasValue("alarmID"))
                uint.TryParse(node.GetValue("alarmID"), out alarmID);

            if (node.HasValue("shipSize"))
                shipSize = KSPUtil.ParseVector3(node.GetValue("shipSize"));

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

            if (!string.IsNullOrEmpty(shipName))
                node.AddValue("shipName", shipName);

            if (!string.IsNullOrEmpty(editorFacility))
                node.AddValue("editorFacility", editorFacility);

            if (!string.IsNullOrEmpty(craftFilePath))
                node.AddValue("craftFilePath", craftFilePath);

            node.AddValue("shipTotalUnitsRequired", shipTotalUnitsRequired);

            node.AddValue("shipTotalUnitsPrinted", shipTotalUnitsPrinted);

            node.AddValue("totalPartsToPrint", totalPartsToPrint);

            node.AddValue("totalPartsPrinted", totalPartsPrinted);

            node.AddValue("alarmID", alarmID);

            node.AddValue("shipSize", string.Format("{0},{1},{2}", shipSize.x, shipSize.y, shipSize.z));

            if (finalizeVesselAtStartup)
                node.AddValue("finalizeVesselAtStartup", finalizeVesselAtStartup);

            if (dockedVesselInfo != null)
            {
                ConfigNode dockedVesselNode = new ConfigNode("DOCKED_VESSEL_INFO");
                dockedVesselInfo.Save(dockedVesselNode);
                node.AddNode(dockedVesselNode);
            }
        }

        public void WaitForCompletion(uint buildItemID)
        {
            int count = printQueue.Count;
            for (int index = 0; index < count; index++)
            {
                if (printQueue[index].flightId == buildItemID)
                {
                    printQueue[index].waitForSupportCompletion = true;
                    return;
                }
            }
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

            // If there's only one item in the list then ignore the request. We let the sender of this event handle it.
            int count = buildList.Count;
            if (count == 1)
                return;

            // Add the first part that we're capable of printing to our print queue.
            BuildItem buildItem = buildList[0];
            buildList.RemoveAt(0);

            printQueue.Add(buildItem);

            // Auto-start the printer.
            if (printQueue.Count > 0)
                printState = WBIPrintStates.Printing;

            if (debugMode)
                Debug.Log("[Sandcastle " + part.flightID + "] - " + " I've been asked by " + sender.part.flightID + " to print " + buildItem.partName);
        }

        private void updateCompletedBuildTotals(BuildItem buildItem)
        {
            totalPartsPrinted += 1;

            shipwrightUI.UpdateResourceRequirements();
            shipwrightUI.SetPrintTotals(totalPartsToPrint, totalPartsPrinted, shipTotalUnitsRequired, shipTotalUnitsPrinted);
        }

        private void onOpenCraftBrowser()
        {
            if (craftBrowserDialog != null)
                craftBrowserDialog.Dismiss();

            craftBrowserDialog = CraftBrowserDialog.Spawn(EditorFacility.VAB, HighLogic.SaveFolder, new CraftBrowserDialog.SelectFileCallback(onShipSelected), new CraftBrowserDialog.CancelledCallback(cancelLoad), false);
        }

        private void onShipSelected(string filePath, CraftBrowserDialog.LoadType loadType)
        {
            craftFilePath = filePath;
            if (debugMode)
                Debug.Log("[Sandcastle] - Ship file selected: " + filePath);
            ConfigNode shipNode = ConfigNode.Load(filePath);

            if (shipNode.HasValue("ship"))
            {
                shipName = shipNode.GetValue("ship");
                if (debugMode)
                    Debug.Log("[Sandcastle] - Ship Name: " + shipName);
            }

            if (shipNode.HasValue("type"))
            {
                editorFacility = shipNode.GetValue("type");
                if (debugMode)
                    Debug.Log("[Sandcastle] - Editor Facility: " + editorFacility);
            }

            if (shipNode.HasValue("size"))
            {
                shipSize = KSPUtil.ParseVector3(shipNode.GetValue("size"));
                if (debugMode)
                    Debug.Log("[Sandcastle] - Size (Width, Height, Length): " + shipSize.ToString());

                if (!string.IsNullOrEmpty(maxCraftDimensions))
                {
                    Vector3 maxDimensions = KSPUtil.ParseVector3(maxCraftDimensions);
                    if (shipSize.x > maxDimensions.x || shipSize.y > maxDimensions.y || shipSize.z > maxDimensions.z)
                    {
                        if (debugMode)
                            Debug.Log("[Sandcastle] - " + shipName + " cannot be printed, it exceeds the printer's maximum print dimensions.");
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_printDimensionsExceeded", new string[1] { shipName }), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                }
            }

            if (debugMode)
                Debug.Log("[Sandcastle] - Part Count: " + shipNode.nodes.Count);

            // Find the list of parts and get their construction details.
            int count = shipNode.nodes.Count;
            ConfigNode partNode;
            string partName;
            AvailablePart availablePart;
            int valueCount;
            List<BuildItem> buildItems = new List<BuildItem>();
            BuildItem buildItem;
            shipTotalUnitsRequired = 0;
            shipTotalUnitsPrinted = 0;
            for (int index = 0; index < count; index++)
            {
                partNode = shipNode.nodes[index];

                // KLUDGE! For some reason I can't just use partNode.GetValue("part"). Doing so returns null.
                valueCount = partNode.values.Count;
                partName = "";
                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    if (partNode.values[valueIndex].name == "part")
                    {
                        partName = KSPUtil.GetPartName(partNode.values[valueIndex].value);
                        break;
                    }
                }

                availablePart = PartLoader.getPartInfoByName(partName);
                if (availablePart == null)
                    continue;

                if (debugMode)
                    Debug.Log("[Sandcastle] - Part to print: " + availablePart.title);

                // Create a build item
                buildItem = new BuildItem(availablePart);
                buildItem.flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                buildItem.UpdateResourceRequirements(partNode);

                // A blacklisted part cannot be printed and must be in the printer vessel's inventory in order for the vessel to be printed.
                buildItem.isBlacklisted = isBlacklistedPart(availablePart);

                // Add to the list
                buildItems.Add(buildItem);

                shipTotalUnitsRequired += buildItem.totalUnitsRequired;
            }

            if (spawnCraftAfterLoading)
            {
                spawnCraftAfterLoading = false;
                if (moduleBoundingBox != null && part.vessel.LandedOrSplashed)
                {
                    // Show vessel bounding box.
                    showVesselBoundingBox();

                    // Enable finalize button.
                    shipwrightUI.showSpawnButton = true;

                    Events["SpawnVessel"].active = true;
                }
                else
                {
                    onSpawnShip();
                }
                return;
            }

            // We now have the complete list of parts that are required to build the ship.
            // Add them to the print queue.
            printQueue = buildItems;
            totalPartsToPrint = buildItems.Count;
            totalPartsPrinted = 0;
            shipwrightUI.SetPrintQueue(printQueue);
            shipwrightUI.SetPrintTotals(totalPartsToPrint, totalPartsPrinted, shipTotalUnitsRequired, shipTotalUnitsPrinted);
            shipwrightUI.craftName = shipName;

            // Create alarm
            if (shipwrightUI.createAlarm)
            {
                double unitsRemaining = shipTotalUnitsRequired - shipTotalUnitsPrinted;
                double timeRemaining = unitsRemaining / printSpeedUSec;
                if (AlarmClockScenario.AlarmExists(alarmID))
                    AlarmClockScenario.DeleteAlarm(alarmID);
                AlarmTypeRaw alarm = new AlarmTypeRaw();
                alarm.ut = Planetarium.GetUniversalTime() + timeRemaining;
                alarm.vesselId = FlightGlobals.ActiveVessel.persistentId;
                alarm.vesselName = part.vessel.vesselName;
                string titleDesc = Localizer.Format("#LOC_SANDCASTLE_alarmTitleDesc", new string[1] { shipName });
                alarm.title = titleDesc;
                alarm.description = titleDesc;
                if (AlarmClockScenario.AddAlarm(alarm))
                {
                    alarmID = alarm.Id;
                }
            }

            // Ask other printers to help print the parts.
            List<BuildItem> reversedPrintQueue = new List<BuildItem>();
            count = printQueue.Count;
            for (int index = 0; index < count; index++)
                reversedPrintQueue.Add(new BuildItem(printQueue[index]));
            reversedPrintQueue.Reverse();
            SandcastleScenario.onSupportPrintingRequest.Fire(this, reversedPrintQueue);
            printState = WBIPrintStates.Printing;
        }

        private void onPartPrinted(SCBasePrinter sender, BuildItem buildItem)
        {
            if (sender == this || printState != WBIPrintStates.Printing)
                return;

            if (debugMode)
                Debug.Log("[SCShipwright " + part.flightID + "] - " + "Support printer " + sender.part.flightID + " has completed printing " + buildItem.partName);

            // Find the item in our print queue
            int count = printQueue.Count;
            BuildItem doomed = null;
            for (int index = 0; index < count; index++)
            {
                if (printQueue[index].flightId == buildItem.flightId)
                {
                    doomed = printQueue[index];
                    break;
                }
            }
            if (doomed == null)
                return;

            // Pull the item from our print queue
            printQueue.Remove(doomed);

            // Update total units printed.
            shipTotalUnitsPrinted += buildItem.totalUnitsPrinted;
            updateCompletedBuildTotals(buildItem);

            // See if we can give the printer something else to do.
            if (printQueue.Count > 1)
            {
                BuildItem item = printQueue[printQueue.Count - 1];
                sender.printQueue.Add(new BuildItem(item));

                // Yes we're doing this deliberately. We want the support printer to print the item but we need to wait for that print job to be completed.
                item.waitForSupportCompletion = true;
                item.skipInventoryAdd = true;

                if (debugMode)
                    Debug.Log("[SCShipwright " + part.flightID + "] - " + "Asking support printer " + sender.part.flightID + " to print " + item.partName);
            }

            // If queue is empty, kick out of timewarp and signal that we've completed our print jobs.
            if (printQueue.Count <= 0 && !string.IsNullOrEmpty(craftFilePath))
            {
                TimeWarp.SetRate(0, false);
                printJobsCompleted();
            }
        }

        private void cancelLoad()
        {

        }

        private void onVesselCoupled(DockedVesselInfo dockedVesselInfo)
        {
            this.dockedVesselInfo = dockedVesselInfo;

            // Show the button to release the vessel.
            shipwrightUI.showSpawnButton = false;
            shipwrightUI.showDecoupleButton = true;
        }

        private void onCancelVesselBuild()
        {
            clearStats();
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SANDCASTLE_printJobCanceled"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        private void clearStats()
        {
            craftFilePath = string.Empty;
            shipName = string.Empty;
            shipTotalUnitsRequired = 0;
            shipTotalUnitsPrinted = 0;
            totalPartsToPrint = 0;
            totalPartsPrinted = 0;
            shipTemplate = null;
            dockedVesselInfo = null;
            printState = WBIPrintStates.Idle;
            finalizeVesselAtStartup = false;
            printQueue.Clear();
            shipwrightUI.clearUI();
            if (AlarmClockScenario.AlarmExists(alarmID))
                AlarmClockScenario.DeleteAlarm(alarmID);
            if (moduleBoundingBox != null)
            {
                hideVesselBoundingBox();
                Events["SpawnVessel"].active = false;
            }
        }

        private void onSpawnShip()
        {
            if (string.IsNullOrEmpty(craftFilePath))
            {
                if (debugMode)
                    Debug.Log("[Sandcastle] - craftFilePath is null. Exiting onSpawnShip.");
                return;
            }

            ConfigNode shipNode = ConfigNode.Load(craftFilePath);

            shipTemplate = new ShipTemplate();
            shipTemplate.LoadShip(shipNode);
            if (debugMode)
                Debug.Log("[Sandcastle] - Spawning ship " + shipTemplate.shipName);
            if (debugMode)
                Debug.Log("[Sandcastle] - size: " + shipTemplate.GetShipSize().ToString());

            ShipConstruction.CreateConstructFromTemplate(shipTemplate, new Callback<ShipConstruct>(onShipConstructCompleted));
        }

        private void onShipConstructCompleted(ShipConstruct shipConstruct)
        {
            if (debugMode)
                Debug.Log("[Sandcastle] - onShipConstructCompleted called.");

            // Use the extra spawn transforms?
            Transform dropTransform = null;
            if (shipConstruct.shipFacility == EditorFacility.VAB && !string.IsNullOrEmpty(spawnTransformVABName))
            {
                dropTransform = part.FindModelTransform(spawnTransformVABName);

            }
            else if (shipConstruct.shipFacility == EditorFacility.SPH && !string.IsNullOrEmpty(spawnTransformSPHName))
            {
                dropTransform = part.FindModelTransform(spawnTransformSPHName);
            }

            else
            {
                dropTransform = spawnTransform;
            }

            if (dropTransform == null)
            {
                if (debugMode)
                    Debug.Log("[Sandcastle] - dropTransform is null! Exiting.");
                return;
            }

            clearStats();

            InventoryUtils.SpawnShip(shipConstruct, part, dropTransform, new Callback<DockedVesselInfo>(onVesselCoupled), true, repositionCraftBeforeSpawning);
        }
        #endregion
    }
}
