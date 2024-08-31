using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sandcastle.Inventory;
using System.Text;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Asks the delegate to spawn the ship that's just been printed.
    /// </summary>
    public delegate void SpawnShipDelegate();

    /// <summary>
    /// Asks the delegate to decouple the ship that's just been printed.
    /// </summary>
    public delegate void DecoupleShipDelegate();

    /// <summary>
    /// Delegate to get the ship to print.
    /// </summary>
    public delegate void SelectShipDelegate();

    /// <summary>
    /// Delegate to cancel the build.
    /// </summary>
    public delegate void CancelBuildDelegate();

    /// <summary>
    /// Represents the Print Shop UI
    /// </summary>
    public class ShipwrightUI: Dialog<ShipwrightUI>
    {
        #region Fields
        /// <summary>
        /// Represents the list of build items to print.
        /// </summary>
        public List<BuildItem> printQueue;

        /// <summary>
        /// Status of the current print job.
        /// </summary>
        public string jobStatus = string.Empty;

        /// <summary>
        /// Callback to let the controller know about the print state.
        /// </summary>
        public UpdatePrintStatusDelegate onPrintStatusUpdate;

        /// <summary>
        /// Callback to see if the part's gravity requirements are met.
        /// </summary>
        public GravityRequirementsMetDelegate gravityRequirementsMet;

        /// <summary>
        /// Callback to see if the part's pressure requirements are met.
        /// </summary>
        public PressureRequirementMetDelegate pressureRequrementsMet;

        /// <summary>
        /// Callback to let the controller to spawn the printed ship.
        /// </summary>
        public SpawnShipDelegate onSpawnShip;

        /// <summary>
        /// Callback to let the controller to decouple the printed ship.
        /// </summary>
        public DecoupleShipDelegate onDecoupleShip;

        /// <summary>
        /// Callback to select a ship to print.
        /// </summary>
        public SelectShipDelegate onOpenCraftBrowser;

        /// <summary>
        /// Callback to tell the controller to cancel the build.
        /// </summary>
        public CancelBuildDelegate onCancelVesselBuild;

        /// <summary>
        /// Flag indicating that the printer is printing
        /// </summary>
        public bool isPrinting;

        /// <summary>
        /// The Part associated with the UI.
        /// </summary>
        public Part part;

        /// <summary>
        /// Flag to indicate whether or not to show the spawn button.
        /// </summary>
        public bool showSpawnButton = false;

        /// <summary>
        /// Flag to indicate whether or not to show the decouple button.
        /// </summary>
        public bool showDecoupleButton = false;

        /// <summary>
        /// Name of the craft being printed.
        /// </summary>
        public string craftName = "";

        /// <summary>
        /// Estimated time to completion of the vessel.
        /// </summary>
        public string estimatedCompletion = "";

        /// <summary>
        /// Flag to indicate if an alarm shoudl be created for print job completion.
        /// </summary>
        public bool createAlarm = true;
        #endregion

        #region Housekeeping
        bool canSelectVessel = true;
        AvailablePart previewPart;
        Texture2D previewPartImage;
        string previewPartRequirements = string.Empty;
        string previewPartDescription = string.Empty;
        string previewPartMassVolume = string.Empty;
        Dictionary<string, Texture2D> iconSet;
        Vector2 resourcesScrollPos;
        Vector2 partsScrollPos;
        Vector2 partInfoScrollPos;
        Vector2 partQueueScrollPos;
        Vector2 partDescriptionScrollPos;
        GUILayoutOption[] buttonDimensions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        GUILayoutOption[] previewImageDimensions = new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(100) };
        GUILayoutOption[] previewImagePaneDimensions = new GUILayoutOption[] { GUILayout.Height(200), GUILayout.Width(115) };
        GUILayoutOption[] partRequirementsHeight = new GUILayoutOption[] { GUILayout.Height(200) };
        GUILayoutOption[] craftStatusHeight = new GUILayoutOption[] { GUILayout.Height(120) };
        GUILayoutOption[] printQueueHeight = new GUILayoutOption[] { GUILayout.Height(115) };
        GUILayoutOption[] partInfoWidth = new GUILayoutOption[] { GUILayout.Width(325)};
        GUILayoutOption[] partInfoHeight = new GUILayoutOption[] { GUILayout.Height(300) };
        int totalPartCount = 0;
        int totalPartsPrinted = 0;
        double totalUnitsRequired = 0;
        double totalUnitsPrinted = 0;
        double craftBuildPercentage = 0;
        Dictionary<string, double> resourceAmounts = null;
        Dictionary<string, double> componentsRequired = null;
        BuildItem currentBuildItem = null;
        #endregion

        #region Constructors
        public ShipwrightUI(string windowTitle = "Shipwright", float defaultWidth = 635, float defaultHeight = 650) :
        base(windowTitle, defaultWidth, defaultHeight)
        {
            Resizable = false;

            loadIcons();

            resourceAmounts = new Dictionary<string, double>();
            componentsRequired = new Dictionary<string, double>();
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Toggles window visibility
        /// </summary>
        /// <param name="newValue">A flag indicating whether the window shoudld be visible or not.</param>
        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                previewPartImage = iconSet["Blank"];
                canSelectVessel = true;
            }
        }
        #endregion

        #region API
        public void SetPrintQueue(List<BuildItem> buildItems)
        {
            printQueue = buildItems;

            // Get total list of resources required and their amounts. Also list required components.
            UpdateResourceRequirements();

            base.SetVisible(true);
        }

        public void UpdateResourceRequirements()
        {
            resourceAmounts = new Dictionary<string, double>();
            componentsRequired = new Dictionary<string, double>();

            BuildItem buildItem;
            List<ModuleResource> resources;
            ModuleResource resource;
            int resourceCount;
            string displayName;
            double amount;
            List<PartRequiredComponent> requiredComponents;
            PartRequiredComponent requiredComponent;
            int count = printQueue.Count;
            for (int index = 0; index < count; index++)
            {
                buildItem = printQueue[index];

                // Get the resources and their amounts.
                resources = buildItem.materials;
                resourceCount = resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    resource = resources[resourceIndex];
                    displayName = resource.resourceDef.displayName;
                    amount = resource.amount;

                    if (!resourceAmounts.ContainsKey(displayName))
                        resourceAmounts.Add(displayName, 0);

                    resourceAmounts[displayName] += amount;
                }

                // Get list of required components
                requiredComponents = buildItem.requiredComponents;
                if (requiredComponents != null && requiredComponents.Count > 0)
                {
                    int componentCount = requiredComponents.Count;
                    for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
                    {
                        requiredComponent = requiredComponents[componentIndex];

                        if (!componentsRequired.ContainsKey(requiredComponent.name))
                            componentsRequired.Add(requiredComponent.name, 0);

                        componentsRequired[requiredComponent.name] += requiredComponent.amount;
                    }
                }
            }
        }

        public void SetPrintTotals(int totalPartCount, int totalPartsPrinted, double totalUnitsRequired, double totalUnitsPrinted)
        {
            this.totalPartCount = totalPartCount;
            this.totalPartsPrinted = totalPartsPrinted;
            this.totalUnitsRequired = totalUnitsRequired;
            this.totalUnitsPrinted = totalUnitsPrinted;

            craftBuildPercentage = totalUnitsPrinted / totalUnitsRequired * 100f;
        }
        #endregion

        #region Window Drawing
        /// <summary>
        /// Draws the window
        /// </summary>
        /// <param name="windowId">An int representing the window ID.</param>
        protected override void DrawWindowContents(int windowId)
        {
            canSelectVessel = !isPrinting;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            drawUpperPanels();
            GUILayout.EndHorizontal();

            // Print status or spawn button
            if (showSpawnButton)
                drawSpawnButton();
            else if (showDecoupleButton)
                drawDecoupleButton();
            else
                drawPrintStatus();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void drawUpperPanels()
        {
            drawLeftPanel();

            // Right panel: part info and construction
            GUILayout.BeginScrollView(Vector2.zero, partInfoWidth);

            // Draw craft status
            if (!string.IsNullOrEmpty(craftName))
                drawCraftStatus();

            // Part info
            drawPartInfo();

            GUILayout.EndScrollView();
        }

        private void drawLeftPanel()
        {
            GUILayout.BeginVertical();

            // Left panel: Load vessel button & list of parts to print.
            if (GUILayout.Button(Localizer.Format("#LOC_SANDCASTLE_selectVessel")))
            {
                if (canSelectVessel)
                {
                    base.SetVisible(false);
                    onOpenCraftBrowser();
                }
            }

            // Parts list
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_partsList"));

            partsScrollPos = GUILayout.BeginScrollView(partsScrollPos, partInfoHeight);

            // Show list of parts that will be printed/required in inventory.
            int count = printQueue.Count;
            BuildItem item = null;
            string itemTitle;
            for (int index = 0; index < count; index++)
            {
                item = printQueue[index];

                // First item is bold. It's the one being printed.
                if (index == 0)
                    itemTitle = "<color=white><b>" + item.availablePart.title + "</b></color>";
                else
                    itemTitle = "<color=white>" + item.availablePart.title + "</color>";

                // Blacklisted notice
                if (item.isBlacklisted)
                    itemTitle = "<color=orange><b>" + item.availablePart.title + "</b></color>";

                GUILayout.Label(itemTitle);
            }

            GUILayout.EndScrollView();

            // Resource list
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_resourcesList"));

            resourcesScrollPos = GUILayout.BeginScrollView(resourcesScrollPos);

            // Required resources
            count = resourceAmounts.Keys.Count;
            string key;
            string[] keys;
            double amount;
            if (count > 0)
            {
                keys = resourceAmounts.Keys.ToArray();

                for (int index = 0; index < count; index++)
                {
                    key = keys[index];
                    amount = resourceAmounts[key];
                    GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_requiredItemResource", new string[2] { key, string.Format("{0:n1}", amount) }));
                }
            }

            // Required parts
            count = componentsRequired.Count;
            if (count > 0)
            {
                keys = componentsRequired.Keys.ToArray();

                for (int index = 0; index < count; index++)
                {
                    key = keys[index];
                    amount = componentsRequired[key];
                    GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_requiredItemResource", new string[2] { key, string.Format("{0:n1}", amount) }));
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void drawCraftStatus()
        {
            // Craft Status
            GUILayout.BeginScrollView(Vector2.zero, craftStatusHeight);

            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_printingCraft", new string[1] { craftName }));

            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_craftJobStatus", new string[1] { string.Format("{0:n0}", craftBuildPercentage) } ));

            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_partsPrinted", new string[2] { totalPartsPrinted.ToString(), totalPartCount.ToString() }));

            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_estimatedCompletion", new string[1] { estimatedCompletion }));

            GUILayout.EndScrollView();

            // Alarm creation
            if (!string.IsNullOrEmpty(craftName))
                createAlarm = GUILayout.Toggle(createAlarm, Localizer.Format("#LOC_SANDCASTLE_createAlarm"));
        }

        private void drawSpawnButton()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_SANDCASTLE_finalizePrinting")))
            {
                base.SetVisible(false);
                clearUI();
                onSpawnShip();
            }
            GUILayout.EndHorizontal();
        }

        private void drawDecoupleButton()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_SANDCASTLE_decoupleShip")))
            {
                base.SetVisible(false);
                clearUI();
                onDecoupleShip();
            }
            GUILayout.EndHorizontal();
        }

        public void clearUI()
        {
            currentBuildItem = null;
            previewPart = null;
            previewPartDescription = null;
            estimatedCompletion = null;
            craftName = null;
            craftBuildPercentage = 0;
            previewPartRequirements = string.Empty;
            previewPartDescription = string.Empty;
            previewPartMassVolume = string.Empty;
            totalPartCount = 0;
            totalPartsPrinted = 0;
            totalUnitsRequired = 0;
            totalUnitsPrinted = 0;
            craftBuildPercentage = 0;
            resourceAmounts.Clear();
            componentsRequired.Clear();
            canSelectVessel = true;
            isPrinting = false;
            showDecoupleButton = false;
            showSpawnButton = false;
            previewPartImage = iconSet["Blank"];
        }

        private void drawPrintStatus()
        {
            GUILayout.BeginHorizontal();
            // Pause/print button
            Texture2D buttonTexture = isPrinting ? iconSet["Pause"] : iconSet["Play"];
            if (GUILayout.Button(buttonTexture, buttonDimensions))
            {
                isPrinting = !isPrinting;
                onPrintStatusUpdate(isPrinting);
            }

            // Cancel print job button
            if (GUILayout.Button(iconSet["Trash"], buttonDimensions))
            {
                clearUI();
                onCancelVesselBuild();
            }

            GUILayout.BeginVertical();
            // Current print job
            string jobTitle = printQueue.Count > 0 ? printQueue[0].availablePart.title : "None";
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_currentJob", new string[1] { jobTitle } ));

            // Job status
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_jobStatus", new string[1] { jobStatus } ));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void drawPartInfo()
        {
            updatePartInfo();

            // Part image and print requirements
            GUILayout.BeginHorizontal(partRequirementsHeight);

            // Preview image
            GUILayout.BeginScrollView(Vector2.zero, previewImagePaneDimensions);
            GUILayout.Label(previewPartImage, previewImageDimensions);
            GUILayout.Label(previewPartMassVolume);
            GUILayout.EndScrollView();

            // Name and requirements
            partInfoScrollPos = GUILayout.BeginScrollView(partInfoScrollPos);
            GUILayout.Label(previewPartRequirements);
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();

            // Part description
            partDescriptionScrollPos = GUILayout.BeginScrollView(partDescriptionScrollPos);
            GUILayout.Label(previewPartDescription);
            GUILayout.EndScrollView();
        }
        #endregion

        #region Helpers
        private void updatePartInfo()
        {
            if (printQueue == null || printQueue.Count <= 0)
                return;
            if (currentBuildItem == printQueue[0])
                return;

            // Get the build item
            currentBuildItem = printQueue[0];
            previewPart = currentBuildItem.availablePart;

            // Part image
            previewPartImage = InventoryUtils.GetTexture(previewPart.name, currentBuildItem.variantIndex);

            // Part mass and volume
            ModuleCargoPart cargoPart = previewPart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart != null)
                previewPartMassVolume = Localizer.Format("#LOC_SANDCASTLE_partMassVolume", new string[2] { string.Format("{0:n3}", currentBuildItem.mass), string.Format("{0:n3}", cargoPart.packedVolume) });
            else
                previewPartMassVolume = Localizer.Format("#LOC_SANDCASTLE_partMass", new string[1] { string.Format("{0:n3}", currentBuildItem.mass) });

            // Part description
            previewPartDescription = "<color=white>" + previewPart.description + "</color>";

            // Title
            StringBuilder requirements = new StringBuilder();
            requirements.AppendLine("<b>" + previewPart.title + "</b>");

            // Required resources
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            int count = currentBuildItem.materials.Count;
            double amount = 0;
            double maxAmount = 0;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredResources"));
                for (int index = 0; index < count; index++)
                {
                    if (definitions.Contains(currentBuildItem.materials[index].name))
                    {
                        resourceDef = definitions[currentBuildItem.materials[index].name];
                        part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                        if (amount < currentBuildItem.materials[index].amount)
                            requirements.AppendLine(string.Format("<color=red>{0:s}: {1:n3}u</color>", resourceDef.displayName, currentBuildItem.materials[index].amount));
                        else
                            requirements.AppendLine(string.Format("{0:s}: {1:n3}u", resourceDef.displayName, currentBuildItem.materials[index].amount));
                    }
                }
            }

            // Required parts
            count = currentBuildItem.requiredComponents.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredParts"));
                AvailablePart requiredPart;
                int requiredPartCount = 0;
                for (int index = 0; index < count; index++)
                {
                    requiredPart = PartLoader.getPartInfoByName(currentBuildItem.requiredComponents[index].name);
                    requiredPartCount = currentBuildItem.requiredComponents[index].amount;
                    if (requiredPartCount == 1)
                    {
                        if (InventoryUtils.HasItem(part.vessel, requiredPart.name))
                            requirements.AppendLine(requiredPart.title);
                        else
                            requirements.AppendLine("<color=red>" + requiredPart.title + "</color>");
                    }
                    else
                    {
                        int inventoryPartCount = InventoryUtils.GetInventoryItemCount(part.vessel, requiredPart.name);
                        if (inventoryPartCount >= requiredPartCount)
                            requirements.AppendLine(requiredPart.title + ": " + requiredPartCount.ToString());
                        else
                            requirements.AppendLine("<color=red>" + requiredPart.title + ": " + requiredPartCount.ToString() + "</color>");
                    }
                }
            }

            // Minimum gravity
            if (currentBuildItem.minimumGravity > -1)
            {
                bool meetsMinGravity = gravityRequirementsMet(currentBuildItem.minimumGravity);
                string gravityRequirement = string.Empty;

                if (currentBuildItem.minimumGravity < 0.00001)
                    gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredMicrogravity");
                else
                    gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredGravity", new string[1] { string.Format("{0:n2}", currentBuildItem.minimumGravity)});

                if (meetsMinGravity)
                    requirements.AppendLine("<color=white>" + gravityRequirement + "</color>");
                else
                    requirements.AppendLine("<color=red>" + gravityRequirement + "</color>");
            }

            // Minimum pressure
            if (currentBuildItem.minimumPressure > -1)
            {
                bool meetsMinPressure = pressureRequrementsMet(currentBuildItem.minimumPressure);
                string pressureRequirement = string.Empty;

                if (currentBuildItem.minimumPressure < 0.001)
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredVacuum");
                else
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredPressure", new string[1] { string.Format("{0:n2}", currentBuildItem.minimumPressure) });

                if (meetsMinPressure)
                    requirements.AppendLine("<color=white>" + pressureRequirement + "</color>");
                else
                    requirements.AppendLine("<color=red>" + pressureRequirement + "</color>");
            }

            // Is blacklisted
            if (currentBuildItem.isBlacklisted)
            {
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_blacklistedNotice"));
            }

            // Write out the part info
            previewPartRequirements = "<color=white>" + requirements.ToString() + "</color>";

            // Reset our scroll position
            partDescriptionScrollPos = Vector2.zero;
            partInfoScrollPos = Vector2.zero;
        }

        private void loadIcons()
        {
            iconSet = new Dictionary<string, Texture2D>();

            if (!iconSet.ContainsKey("Play"))
                iconSet.Add("Play", loadTexture("WildBlueIndustries/Sandcastle/Icons/Play"));

            if (!iconSet.ContainsKey("Pause"))
                iconSet.Add("Pause", loadTexture("WildBlueIndustries/Sandcastle/Icons/Pause"));

            if (!iconSet.ContainsKey("Trash"))
                iconSet.Add("Trash", loadTexture("WildBlueIndustries/Sandcastle/Icons/Trash"));

            if (!iconSet.ContainsKey("Blank"))
                iconSet.Add("Blank", loadTexture("WildBlueIndustries/Sandcastle/Icons/Box"));
        }

        private Texture2D loadTexture(string path)
        {
            Texture2D texture = GameDatabase.Instance.GetTexture(path, false);
            return texture != null ? texture : new Texture2D(32, 32);
        }

        private bool isMouseOver()
        {
            return (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition));
        }
        #endregion
    }
}
