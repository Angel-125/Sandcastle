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
    /// Represents the Print Shop UI
    /// </summary>
    public class ShipbreakerUI : Dialog<ShipbreakerUI>
    {
        #region Fields
        /// <summary>
        /// Represents the list of build items to recycle.
        /// </summary>
        public List<BuildItem> recycleQueue;

        /// <summary>
        /// Status of the current print job.
        /// </summary>
        public string jobStatus = string.Empty;

        /// <summary>
        /// Callback to tell the controller to cancel the build.
        /// </summary>
        public CancelBuildDelegate onCancelVesselBuild;

        /// <summary>
        /// Flag indicating that the printer is recycling
        /// </summary>
        public bool isRecycling;

        /// <summary>
        /// The Part associated with the UI.
        /// </summary>
        public Part part;

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

        /// <summary>
        /// Callback to let the controller know about the recycle state.
        /// </summary>
        public UpdatePrintStatusDelegate onRecycleStatusUpdate;

        /// <summary>
        /// Percentage of the resources that can be recycled.
        /// </summary>
        public double resourceRecylePercent = 1;

        /// <summary>
        /// List of support shipbreakers
        /// </summary>
        public List<SCShipbreaker> supportShipbreakers = null;
        #endregion

        #region Housekeeping
        bool canSelectVessel = true;
        AvailablePart previewPart;
        Texture2D previewPartImage;
        string previewPartRequirements = string.Empty;
        string previewPartDescription = string.Empty;
        string previewPartMassVolume = string.Empty;
        Dictionary<string, Texture2D> iconSet;
        Vector2 supportShipbreakersScrollPos;
        Vector2 partsScrollPos;
        Vector2 partInfoScrollPos;
        Vector2 partDescriptionScrollPos;
        GUILayoutOption[] buttonDimensions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        GUILayoutOption[] previewImageDimensions = new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(100) };
        GUILayoutOption[] previewImagePaneDimensions = new GUILayoutOption[] { GUILayout.Height(200), GUILayout.Width(115) };
        GUILayoutOption[] partRequirementsHeight = new GUILayoutOption[] { GUILayout.Height(200) };
        GUILayoutOption[] craftStatusHeight = new GUILayoutOption[] { GUILayout.Height(120) };
        GUILayoutOption[] recycleQueueHeight = new GUILayoutOption[] { GUILayout.Height(115) };
        GUILayoutOption[] partInfoWidth = new GUILayoutOption[] { GUILayout.Width(325)};
        GUILayoutOption[] partInfoHeight = new GUILayoutOption[] { GUILayout.Height(300) };
        int totalPartsToRecycle = 0;
        int totalPartsRecycled = 0;
        double craftBuildPercentage = 0;
        Dictionary<string, double> resourceAmounts = null;
        Dictionary<string, double> componentsRequired = null;
        BuildItem currentBuildItem = null;
        #endregion

        #region Constructors
        public ShipbreakerUI(string windowTitle = "Shipbreaker", float defaultWidth = 635, float defaultHeight = 650) :
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
        public void SetrecycleQueue(List<BuildItem> buildItems)
        {
            recycleQueue = buildItems;

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
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                buildItem = recycleQueue[index];

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

        public void SetPrintTotals(int totalPartsToRecycle, int totalPartsRecycled)
        {
            this.totalPartsToRecycle = totalPartsToRecycle;
            this.totalPartsRecycled = totalPartsRecycled;

            craftBuildPercentage = (double)totalPartsRecycled / (double)totalPartsToRecycle * 100f;
        }
        #endregion

        #region Window Drawing
        /// <summary>
        /// Draws the window
        /// </summary>
        /// <param name="windowId">An int representing the window ID.</param>
        protected override void DrawWindowContents(int windowId)
        {
            canSelectVessel = !isRecycling;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            drawUpperPanels();
            GUILayout.EndHorizontal();

            // Recycle status
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
            drawCraftStatus();

            // Part info
            drawPartInfo();

            GUILayout.EndScrollView();
        }

        private void drawLeftPanel()
        {
            GUILayout.BeginVertical();

            // Parts list
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_partsList"));

            partsScrollPos = GUILayout.BeginScrollView(partsScrollPos, partInfoHeight);

            // Show list of parts that will be printed/required in inventory.
            int count = recycleQueue.Count;
            BuildItem item = null;
            string itemTitle;
            for (int index = 0; index < count; index++)
            {
                item = recycleQueue[index];

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

            // Support Shipbreakers list
            if (supportShipbreakers != null)
            {
                GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_supportShipbreakers"));

                supportShipbreakersScrollPos = GUILayout.BeginScrollView(supportShipbreakersScrollPos);

                count = supportShipbreakers.Count;
                SCShipbreaker shipbreaker;
                StringBuilder builder;
                for (int index = 0; index < count; index++)
                {
                    shipbreaker = supportShipbreakers[index];
                    if (shipbreaker.recycleState != WBIPrintStates.Recycling && shipbreaker.recycleQueue.Count <= 0)
                        continue;
                    builder = new StringBuilder();

                    // Shipbreaker name
                    builder.AppendLine(Localizer.Format("#LOC_SANDCASTLE_supportBreakerName", new string[1] { shipbreaker.part.partInfo.title }));

                    // Part being recycled
                    builder.AppendLine(Localizer.Format("#LOC_SANDCASTLE_supportBreakerRecyclePart", new string[1] { shipbreaker.recycleQueue[0].availablePart.title }));

                    // Recycling status
                    builder.AppendLine(Localizer.Format("#LOC_SANDCASTLE_supportRecyclePartStatus", new string[1] { shipbreaker.recycleStatusText }));

                    // Output the listing
                    GUILayout.Label(builder.ToString());
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private void drawCraftStatus()
        {
            // Craft Status
            GUILayout.BeginScrollView(Vector2.zero, craftStatusHeight);
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_recyclingCraft", new string[1] { craftName }));
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_craftRecycleStatus", new string[1] { string.Format("{0:n0}", craftBuildPercentage) } ));
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_partsRecycled", new string[2] { totalPartsRecycled.ToString(), totalPartsToRecycle.ToString() }));
//            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_estimatedCompletion", new string[1] { estimatedCompletion }));
            GUILayout.EndScrollView();
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
            totalPartsToRecycle = 0;
            totalPartsRecycled = 0;
            craftBuildPercentage = 0;
            resourceAmounts.Clear();
            componentsRequired.Clear();
            recycleQueue.Clear();
            previewPartImage = iconSet["Blank"];
        }

        private void drawPrintStatus()
        {
            GUILayout.BeginHorizontal();
            // Pause/print button
            Texture2D buttonTexture = isRecycling ? iconSet["Pause"] : iconSet["Play"];
            if (GUILayout.Button(buttonTexture, buttonDimensions))
            {
                isRecycling = !isRecycling;
                onRecycleStatusUpdate(isRecycling);
            }

            // Cancel print job button
            if (GUILayout.Button(iconSet["Trash"], buttonDimensions))
            {
                clearUI();
                onCancelVesselBuild();
            }

            GUILayout.BeginVertical();
            // Current print job
            string jobTitle = recycleQueue.Count > 0 ? recycleQueue[0].availablePart.title : "None";
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
            if (recycleQueue == null || recycleQueue.Count <= 0)
                return;
            if (currentBuildItem == recycleQueue[0])
                return;

            // Get the build item
            currentBuildItem = recycleQueue[0];
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

            // Recycled resources
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            int count = currentBuildItem.materials.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_resourcesToRecycle"));
                for (int index = 0; index < count; index++)
                {
                    if (definitions.Contains(currentBuildItem.materials[index].name))
                    {
                        resourceDef = definitions[currentBuildItem.materials[index].name];
                        requirements.AppendLine(string.Format("{0:s}: {1:n3}u", resourceDef.displayName, currentBuildItem.materials[index].amount * resourceRecylePercent));
                    }
                }
            }

            // Recycled parts
            count = currentBuildItem.requiredComponents.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recycledParts"));
                AvailablePart requiredPart;
                int recycledPartCount;
                for (int index = 0; index < count; index++)
                {
                    requiredPart = PartLoader.getPartInfoByName(currentBuildItem.requiredComponents[index].name);
                    recycledPartCount = currentBuildItem.requiredComponents[index].amount;
                    requirements.AppendLine(requiredPart.title);
                    if (recycledPartCount == 1)
                    {
                        requirements.AppendLine(requiredPart.title);
                    }
                    else
                    {
                        requirements.AppendLine(requiredPart.title + ": " + recycledPartCount.ToString());
                    }
                }
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
