using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sandcastle.Inventory;
using System.Text;
using KSP.Localization;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Callback to let the controller know about the print state.
    /// </summary>
    public delegate void UpdatePrintStatusDelegate(bool isPrinting);

    /// <summary>
    /// Asks the delegate if the minimum gravity requirements are met.
    /// </summary>
    /// <param name="minimumGravity">A float containing the minimum required gravity.</param>
    /// <returns>true if the requirement can be met, false if not.</returns>
    public delegate bool GravityRequirementsMetDelegate(float minimumGravity);

    /// <summary>
    /// Asks the delegate if the minimum pressure requirements are met.
    /// </summary>
    /// <param name="minimumPressure">A float containing the minimum required pressure.</param>
    /// <returns>true if the requirement can be met, false if not.</returns>
    public delegate bool PressureRequirementMetDelegate(float minimumPressure);

    /// <summary>
    /// Represents the Print Shop UI
    /// </summary>
    public class PrintShopUI: Dialog<PrintShopUI>
    {
        #region Fields
        /// <summary>
        /// Title of the selection dialog.
        /// </summary>
        public string titleText = Localizer.Format("#LOC_SANDCASTLE_printShopTitle");

        /// <summary>
        /// Complete list of printable parts.
        /// </summary>
        public List<AvailablePart> partsList;

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
        /// Flag indicating that the printer is printing
        /// </summary>
        public bool isPrinting;

        /// <summary>
        /// The Part associated with the UI.
        /// </summary>
        public Part part;

        /// <summary>
        /// Whitelisted categories that the printer can print from.
        /// </summary>
        public List<PartCategories> whitelistedCategories;

        #endregion

        #region Housekeeping
        List<AvailablePart> filteredParts;
        Dictionary<string, BuildItem> itemCache;
        AvailablePart previewPart;
        Texture2D previewPartImage;
        string previewPartRequirements = string.Empty;
        string previewPartDescription = string.Empty;
        string previewPartMassVolume = string.Empty;
        Dictionary<string, Texture2D> iconSet;
        Vector2 categoryScrollPos;
        Vector2 partsScrollPos;
        Vector2 partInfoScrollPos;
        Vector2 partQueueScrollPos;
        Vector2 partDescriptionScrollPos;
        GUILayoutOption[] categoryPanelWidth = new GUILayoutOption[] { GUILayout.Width(75) };
        GUILayoutOption[] categoryButtonDimensions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        GUILayoutOption[] buttonDimensions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        GUILayoutOption[] selectorPanelWidth = new GUILayoutOption[] { GUILayout.Width(235) };
        GUILayoutOption[] selectorButtonDimensions = new GUILayoutOption[] { GUILayout.MinWidth(64), GUILayout.MinHeight(64), GUILayout.MaxWidth(64), GUILayout.MaxHeight(64) };
        GUILayoutOption[] previewImageDimensions = new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(100) };
        GUILayoutOption[] previewImagePaneDimensions = new GUILayoutOption[] { GUILayout.Height(200), GUILayout.Width(115) };
        GUILayoutOption[] partRequirementsHeight = new GUILayoutOption[] { GUILayout.Height(200) };
        GUILayoutOption[] previewDescriptionHeight = new GUILayoutOption[] { GUILayout.Height(100) };
        GUILayoutOption[] printQueueHeight = new GUILayoutOption[] { GUILayout.Height(190) };
        GUILayoutOption[] partInfoWidth = new GUILayoutOption[] { GUILayout.Width(325) };
        PartCategories currentCategory = PartCategories.Pods;
        PartCategories selectedCategory = PartCategories.Pods;
        Texture2D[] partImages;
        int selectedIndex;
        int currentIndex;
        Color selectedColor = Color.yellow;
        Color backgroundColor;
        string categoryName = PartCategories.Pods.ToString();
        bool categoryMousedOver;
        double categoryUpdateTime;
        #endregion

        #region Constructors
        public PrintShopUI() :
        base("Print Shop", 635, 650)
        {
            WindowTitle = titleText;
            Resizable = false;

            // Load category icons
            loadIcons();
            itemCache = new Dictionary<string, BuildItem>();
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
            updateCategoryParts();

            if (newValue)
            {
                previewPartImage = iconSet["Blank"];
            }
        }
        #endregion

        #region Window Drawing
        /// <summary>
        /// Draws the window
        /// </summary>
        /// <param name="windowId">An int representing the window ID.</param>
        protected override void DrawWindowContents(int windowId)
        {
            backgroundColor = GUI.backgroundColor;

            GUILayout.BeginVertical();

            GUILayout.Label("<color=white><b>" + categoryName + "</b></color>");

            GUILayout.BeginHorizontal();

            // Draw category selector
            drawCategorySelector();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            // Draw parts list
            drawPartsList();
            GUILayout.EndHorizontal();

            // Print status
            drawPrintStatus();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void drawPartsList()
        {
            // Part buttons
            drawPartButtons();

            // Part info and construction
            GUILayout.BeginScrollView(Vector2.zero, partInfoWidth);

            // Part preview info
            drawPreviewPartInfo();

            // Print queue
            drawPrintQueue();

            GUILayout.EndScrollView();
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
                // We always work from the first item in the queue.
                printQueue.RemoveAt(0);
            }

            GUILayout.BeginVertical();
            // Current print job
            string jobTitle = printQueue.Count > 0 ? printQueue[0].availablePart.title : "None";
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_currentJob", new string[1] { jobTitle } ));

            // Job status
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_currentJob", new string[1] { jobStatus } ));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void drawPrintQueue()
        {
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_printQueue"));

            List<BuildItem> doomed = new List<BuildItem>();
            partQueueScrollPos = GUILayout.BeginScrollView(partQueueScrollPos, printQueueHeight);
            int count = printQueue.Count;
            for (int index = 0; index < count; index++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(iconSet["Trash"], buttonDimensions))
                {
                    doomed.Add(printQueue[index]);
                }
                GUILayout.Label("<color=white>" + printQueue[index].availablePart.title + "</color>");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // Clear out any doomed items
            count = doomed.Count;
            for (int index = 0; index < count; index++)
            {
                printQueue.Remove(doomed[index]);
            }
        }

        private void drawPreviewPartInfo()
        {
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
            partDescriptionScrollPos = GUILayout.BeginScrollView(partDescriptionScrollPos, previewDescriptionHeight);
            GUILayout.Label(previewPartDescription);
            GUILayout.EndScrollView();
        }

        private void drawPartButtons()
        {
            partsScrollPos = GUILayout.BeginScrollView(partsScrollPos, selectorPanelWidth);
            int column = -1;
            for (int index = 0; index < partImages.Length; index++)
            {
                // Begin a new row
                if (column == -1)
                {
                    GUILayout.BeginHorizontal();
                }

                // Increment column
                column += 1;

                // Add item to print queue
                if (GUILayout.Button(partImages[index], selectorButtonDimensions))
                {
                    updatePartPreview(index);
                    BuildItem buildItem = new BuildItem(itemCache[previewPart.name]);
                    printQueue.Add(buildItem);
                }

                // Update part preview
                else if (isMouseOver())
                {
                    updatePartPreview(index);
                }

                // Close out the column
                if (column == 2)
                {
                    column = -1;
                    GUILayout.EndHorizontal();
                }
            }

            // Make sure we match all our begin and end horizontals.
            if (column != -1)
                GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void drawCategorySelector()
        {
            GUILayout.BeginVertical();

            selectedCategory = currentCategory;

            categoryScrollPos = GUILayout.BeginScrollView(categoryScrollPos, categoryPanelWidth);

            if (whitelistedCategories.Contains(PartCategories.Pods))
                drawCategoryButton(PartCategories.Pods);
            if (whitelistedCategories.Contains(PartCategories.FuelTank))
                drawCategoryButton(PartCategories.FuelTank);
            if (whitelistedCategories.Contains(PartCategories.Engine))
                drawCategoryButton(PartCategories.Engine);
            if (whitelistedCategories.Contains(PartCategories.Control))
                drawCategoryButton(PartCategories.Control);
            if (whitelistedCategories.Contains(PartCategories.Structural))
                drawCategoryButton(PartCategories.Structural);
            if (whitelistedCategories.Contains(PartCategories.Robotics))
                drawCategoryButton(PartCategories.Robotics);
            if (whitelistedCategories.Contains(PartCategories.Coupling))
                drawCategoryButton(PartCategories.Coupling);
            if (whitelistedCategories.Contains(PartCategories.Payload))
                drawCategoryButton(PartCategories.Payload);
            if (whitelistedCategories.Contains(PartCategories.Ground))
                drawCategoryButton(PartCategories.Ground);
            if (whitelistedCategories.Contains(PartCategories.Thermal))
                drawCategoryButton(PartCategories.Thermal);
            if (whitelistedCategories.Contains(PartCategories.Electrical))
                drawCategoryButton(PartCategories.Electrical);
            if (whitelistedCategories.Contains(PartCategories.Communication))
                drawCategoryButton(PartCategories.Communication);
            if (whitelistedCategories.Contains(PartCategories.Science))
                drawCategoryButton(PartCategories.Science);
            if (whitelistedCategories.Contains(PartCategories.Cargo))
                drawCategoryButton(PartCategories.Cargo);
            if (whitelistedCategories.Contains(PartCategories.Utility))
                drawCategoryButton(PartCategories.Utility);
            if (whitelistedCategories.Contains(PartCategories.none))
                drawCategoryButton(PartCategories.none);

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            // Reset background color
            GUI.backgroundColor = backgroundColor;

            // Change categories
            if (selectedCategory != currentCategory)
            {
                currentCategory = selectedCategory;
                categoryName = getCategoryName(currentCategory);
                updateCategoryParts();
            }

            // Reset category
            if (categoryMousedOver && Planetarium.GetUniversalTime() > categoryUpdateTime)
            {
                categoryMousedOver = false;
                categoryName = getCategoryName(currentCategory);
            }
        }

        private string getCategoryName(PartCategories category)
        {
            return category != PartCategories.none ? category.ToString() : Localizer.Format("#LOC_SANDCASTLE_specialCategory");
        }
        
        private void drawCategoryButton(PartCategories category)
        {
            GUI.backgroundColor = currentCategory == category ? selectedColor : backgroundColor;
            if (GUILayout.Button(iconSet[category.ToString()], categoryButtonDimensions))
                selectedCategory = category;
            if (isMouseOver())
            {
                categoryName = getCategoryName(category);
                categoryMousedOver = true;
                categoryUpdateTime = Planetarium.GetUniversalTime() + 0.25;
            }
        }
        #endregion

        #region Helpers
        private void updatePartPreview(int partIndex)
        {
            previewPart = filteredParts[partIndex];

            // Get the build item
            BuildItem item;
            if (!itemCache.ContainsKey(previewPart.name))
            {
                item = new BuildItem(previewPart);
                itemCache.Add(previewPart.name, item);
            }
            item = itemCache[previewPart.name];

            // Part image
            previewPartImage = InventoryUtils.GetTexture(previewPart.name);

            // Part mass and volume
            ModuleCargoPart cargoPart = previewPart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            previewPartMassVolume = Localizer.Format("#LOC_SANDCASTLE_partMassVolume", 
                new string[2] { string.Format("{0:n3}", previewPart.partPrefab.mass), string.Format("{0:n3}", cargoPart.packedVolume) });

            // Part description
            previewPartDescription = "<color=white>" + previewPart.description + "</color>";

            // Title
            StringBuilder requirements = new StringBuilder();
            requirements.AppendLine("<b>" + previewPart.title + "</b>");

            // Required resources
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            int count = item.materials.Count;
            double amount = 0;
            double maxAmount = 0;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredResources"));
                for (int index = 0; index < count; index++)
                {
                    if (definitions.Contains(item.materials[index].name))
                    {
                        resourceDef = definitions[item.materials[index].name];
                        part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                        if (amount < item.materials[index].amount)
                            requirements.AppendLine(string.Format("<color=red>{0:s}: {1:n3}u</color>", resourceDef.displayName, item.materials[index].amount));
                        else
                            requirements.AppendLine(string.Format("{0:s}: {1:n3}u", resourceDef.displayName, item.materials[index].amount));
                    }
                }
            }

            // Required parts
            count = item.requiredComponents.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredParts"));
                AvailablePart requiredPart;
                int requiredPartCount = 0;
                for (int index = 0; index < count; index++)
                {
                    requiredPart = PartLoader.getPartInfoByName(item.requiredComponents[index].name);
                    requiredPartCount = item.requiredComponents[index].amount;
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
            if (item.minimumGravity > -1)
            {
                bool meetsMinGravity = gravityRequirementsMet(item.minimumGravity);
                string gravityRequirement = string.Empty;

                if (item.minimumGravity < 0.00001)
                    gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredMicrogravity");
                else
                    gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredGravity", new string[1] { string.Format("{0:n2}", item.minimumGravity)});

                if (meetsMinGravity)
                    requirements.AppendLine("<color=white>" + gravityRequirement + "</color>");
                else
                    requirements.AppendLine("<color=red>" + gravityRequirement + "</color>");
            }

            // Minimum pressure
            if (item.minimumPressure > -1)
            {
                bool meetsMinPressure = pressureRequrementsMet(item.minimumPressure);
                string pressureRequirement = string.Empty;

                if (item.minimumPressure < 0.001)
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredVacuum");
                else
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredPressure", new string[1] { string.Format("{0:n2}", item.minimumPressure) });

                if (meetsMinPressure)
                    requirements.AppendLine("<color=white>" + pressureRequirement + "</color>");
                else
                    requirements.AppendLine("<color=red>" + pressureRequirement + "</color>");
            }

            // Write out the part info
            previewPartRequirements = "<color=white>" + requirements.ToString() + "</color>";

            // Reset our scroll position
            partDescriptionScrollPos = Vector2.zero;
            partInfoScrollPos = Vector2.zero;
        }

        private void updateCategoryParts()
        {
            // Filter parts for the current category.
            filteredParts = new List<AvailablePart>();
            int count = partsList.Count;
            List<Texture2D> thumbnails = new List<Texture2D>();
            AvailablePart availablePart;
            for (int index = 0; index < count; index++)
            {
                if (partsList[index].category == currentCategory)
                {
                    availablePart = partsList[index];
                    filteredParts.Add(availablePart);
                    thumbnails.Add(InventoryUtils.GetTexture(availablePart.name));
                }
            }

            partImages = thumbnails.ToArray();
            selectedIndex = 0;
            currentIndex = 0;
            partsScrollPos = Vector2.zero;
        }

        private void loadIcons()
        {
            iconSet = new Dictionary<string, Texture2D>();

            iconSet.Add(PartCategories.Aero.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advaerodynamics"));
            iconSet.Add(PartCategories.Cargo.ToString(), loadTexture("Squad/PartList/SimpleIcons/deployed_science_part"));
            iconSet.Add(PartCategories.Communication.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advunmanned"));
            iconSet.Add(PartCategories.Control.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_largecontrol"));
            iconSet.Add(PartCategories.Coupling.ToString(), loadTexture("Squad/PartList/SimpleIcons/cs_size3"));
            iconSet.Add(PartCategories.Electrical.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_electrics"));
            iconSet.Add(PartCategories.Engine.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_propulsionSystems"));
            iconSet.Add(PartCategories.FuelTank.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_fuelSystems-advanced"));
            iconSet.Add(PartCategories.Ground.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advancedmotors"));
            iconSet.Add(PartCategories.Payload.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_composites"));
            iconSet.Add(PartCategories.Pods.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_commandmodules"));
            iconSet.Add(PartCategories.Robotics.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_robotics"));
            iconSet.Add(PartCategories.Science.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advsciencetech"));
            iconSet.Add(PartCategories.Structural.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generalconstruction"));
            iconSet.Add(PartCategories.Thermal.ToString(), loadTexture("Squad/PartList/SimpleIcons/fuels_monopropellant"));
            iconSet.Add(PartCategories.Utility.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generic"));
            iconSet.Add(PartCategories.none.ToString(), loadTexture("Squad/PartList/SimpleIcons/deployable_part"));

            iconSet.Add("Play", loadTexture("WildBlueIndustries/Sandcastle/Icons/Play"));
            iconSet.Add("Pause", loadTexture("WildBlueIndustries/Sandcastle/Icons/Pause"));
            iconSet.Add("Trash", loadTexture("WildBlueIndustries/Sandcastle/Icons/Trash"));
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
