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
    /// Asks the delegate to spawn the current part that's just been printed.
    /// </summary>
    public delegate void SpawnPartDelegate();

    /// <summary>
    /// Represents the Print Shop UI
    /// </summary>
    public class PrintShopUI: Dialog<PrintShopUI>
    {
        #region Fields
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
        /// Callback to let the controller to spawn the printed part.
        /// </summary>
        public SpawnPartDelegate onSpawnPrintedPart;

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
        public List<string> whitelistedCategories;

        /// <summary>
        /// Flag to indicate whether or not to show the part spawn button.
        /// </summary>
        public bool showPartSpawnButton = false;

        #endregion

        #region Housekeeping
        List<AvailablePart> filteredParts;
        Dictionary<string, BuildItem> itemCache;
        AvailablePart previewPart;
        List<PartVariant> partVariants = new List<PartVariant>();
        Texture2D previewPartImage;
        string previewPartRequirements = string.Empty;
        string previewPartDescription = string.Empty;
        string previewPartMassVolume = string.Empty;
        Dictionary<string, Texture2D> iconSet;
        Dictionary<string, string> cckTags;
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
        GUILayoutOption[] previewVariantHeight = new GUILayoutOption[] { GUILayout.Height(75) };
        GUILayoutOption[] printQueueHeight = new GUILayoutOption[] { GUILayout.Height(115) };
        GUILayoutOption[] partInfoWidth = new GUILayoutOption[] { GUILayout.Width(325) };
        string currentCategory = PartCategories.Pods.ToString();
        string selectedCategory = PartCategories.Pods.ToString();
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
        public PrintShopUI(string windowTitle = "Print Shop", float defaultWidth = 635, float defaultHeight = 650) :
        base(windowTitle, defaultWidth, defaultHeight)
        {
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
            if (showPartSpawnButton)
                drawSpawnButton();
            else
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

        private void drawSpawnButton()
        {
            GUILayout.BeginHorizontal();
            if (showPartSpawnButton)
            {
                if (GUILayout.Button("Finalize Printing"))
                {
                    onSpawnPrintedPart();
                }
            }
            GUILayout.EndHorizontal();
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
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_jobStatus", new string[1] { jobStatus } ));
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

            // Part variant
            GUILayout.BeginScrollView(Vector2.zero, previewVariantHeight);
            if (previewPart != null)
            {
                BuildItem buildItem = itemCache[previewPart.name];

                GUILayout.BeginVertical();
                if (partVariants != null && partVariants.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", buttonDimensions))
                    {
                        buildItem.variantIndex -= 1;
                        if (buildItem.variantIndex < 0)
                            buildItem.variantIndex = 0;
                        updatePartPreview(currentIndex, buildItem.variantIndex);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("<color=white>" + partVariants[buildItem.variantIndex].DisplayName + "</color>");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(">", buttonDimensions))
                    {
                        buildItem.variantIndex = (buildItem.variantIndex + 1) % partVariants.Count;
                        updatePartPreview(currentIndex, buildItem.variantIndex);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("  ");
                }

                if (GUILayout.Button(Localizer.Format("#LOC_SANDCASTLE_addItemToPrintQueue")) && previewPart != null)
                {
                    buildItem = new BuildItem(itemCache[previewPart.name]);
                    printQueue.Add(buildItem);
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

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

                // Update preview item
                if (GUILayout.Button(partImages[index], selectorButtonDimensions))
                {
                    currentIndex = index;
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

            // Stock categories
            string stockCategory = PartCategories.Pods.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.FuelTank.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Engine.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Control.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Structural.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Robotics.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Coupling.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Payload.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Ground.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Thermal.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Electrical.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Communication.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Science.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Cargo.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.Utility.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            stockCategory = PartCategories.none.ToString();
            if (whitelistedCategories.Contains(stockCategory))
                drawCategoryButton(stockCategory);

            // CCK categories
            string[] keys = cckTags.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                drawCategoryButton(keys[index]);
            }

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

        private string getCategoryName(string categoryId)
        {
            return categoryId != PartCategories.none.ToString() ? categoryId : Localizer.Format("#LOC_SANDCASTLE_specialCategory");
        }
        
        private void drawCategoryButton(string categoryId)
        {
            GUI.backgroundColor = currentCategory == categoryId ? selectedColor : backgroundColor;
            if (GUILayout.Button(iconSet[categoryId], categoryButtonDimensions))
                selectedCategory = categoryId;
            if (isMouseOver())
            {
                categoryName = getCategoryName(categoryId);
                categoryMousedOver = true;
                categoryUpdateTime = Planetarium.GetUniversalTime() + 0.25;
            }
        }
        #endregion

        #region Helpers
        private void updatePartPreview(int partIndex, int variantIndex = 0)
        {
            previewPart = filteredParts[partIndex];
            partVariants = previewPart.Variants;

            // Get the build item
            BuildItem item;
            if (!itemCache.ContainsKey(previewPart.name))
            {
                item = new BuildItem(previewPart);
                itemCache.Add(previewPart.name, item);
            }
            item = itemCache[previewPart.name];
            item.UpdateResourceRequirements();

            // Part image
            previewPartImage = InventoryUtils.GetTexture(previewPart.name, item.variantIndex);

            // Part mass and volume
            ModuleCargoPart cargoPart = previewPart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart != null)
                previewPartMassVolume = Localizer.Format("#LOC_SANDCASTLE_partMassVolume", new string[2] { string.Format("{0:n3}", previewPart.partPrefab.mass), string.Format("{0:n3}", cargoPart.packedVolume) });
            else
                previewPartMassVolume = Localizer.Format("#LOC_SANDCASTLE_partMass", new string[1] { string.Format("{0:n3}", previewPart.partPrefab.mass) });

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
            string cckTag = string.Empty;

            if (cckTags.ContainsKey(currentCategory))
                cckTag = cckTags[currentCategory].ToLower();

            string tags;
            string partCategory;
            string title;
            for (int index = 0; index < count; index++)
            {
                title = partsList[index].title;
                partCategory = partsList[index].category.ToString();
                tags = partsList[index].tags;
                if (partCategory == currentCategory || (tags.Contains(cckTag) && !string.IsNullOrEmpty(cckTag)))
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
            cckTags = new Dictionary<string, string>();

            if (!iconSet.ContainsKey(PartCategories.Aero.ToString()))
                iconSet.Add(PartCategories.Aero.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advaerodynamics"));

            if (!iconSet.ContainsKey(PartCategories.Cargo.ToString()))
                iconSet.Add(PartCategories.Cargo.ToString(), loadTexture("Squad/PartList/SimpleIcons/deployed_science_part"));

            if (!iconSet.ContainsKey(PartCategories.Communication.ToString()))
                iconSet.Add(PartCategories.Communication.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advunmanned"));

            if (!iconSet.ContainsKey(PartCategories.Control.ToString()))
                iconSet.Add(PartCategories.Control.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_largecontrol"));

            if (!iconSet.ContainsKey(PartCategories.Coupling.ToString()))
                iconSet.Add(PartCategories.Coupling.ToString(), loadTexture("Squad/PartList/SimpleIcons/cs_size3"));

            if (!iconSet.ContainsKey(PartCategories.Electrical.ToString()))
                iconSet.Add(PartCategories.Electrical.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_electrics"));

            if (!iconSet.ContainsKey(PartCategories.Engine.ToString()))
                iconSet.Add(PartCategories.Engine.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_propulsionSystems"));

            if (!iconSet.ContainsKey(PartCategories.FuelTank.ToString()))
                iconSet.Add(PartCategories.FuelTank.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_fuelSystems-advanced"));

            if (!iconSet.ContainsKey(PartCategories.Ground.ToString()))
                iconSet.Add(PartCategories.Ground.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advancedmotors"));

            if (!iconSet.ContainsKey(PartCategories.Payload.ToString()))
                iconSet.Add(PartCategories.Payload.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_composites"));

            if (!iconSet.ContainsKey(PartCategories.Pods.ToString()))
                iconSet.Add(PartCategories.Pods.ToString(), loadTexture("Squad/PartList/SimpleIcons/RDicon_commandmodules"));

            if (!iconSet.ContainsKey(PartCategories.Robotics.ToString()))
                iconSet.Add(PartCategories.Robotics.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_robotics"));

            if (!iconSet.ContainsKey(PartCategories.Science.ToString()))
                iconSet.Add(PartCategories.Science.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advsciencetech"));

            if (!iconSet.ContainsKey(PartCategories.Structural.ToString()))
                iconSet.Add(PartCategories.Structural.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generalconstruction"));

            if (!iconSet.ContainsKey(PartCategories.Thermal.ToString()))
                iconSet.Add(PartCategories.Thermal.ToString(), loadTexture("Squad/PartList/SimpleIcons/fuels_monopropellant"));

            if (!iconSet.ContainsKey(PartCategories.Utility.ToString()))
                iconSet.Add(PartCategories.Utility.ToString(), loadTexture("Squad/PartList/SimpleIcons/R&D_node_icon_generic"));

            if (!iconSet.ContainsKey(PartCategories.none.ToString()))
                iconSet.Add(PartCategories.none.ToString(), loadTexture("Squad/PartList/SimpleIcons/deployable_part"));

            if (!iconSet.ContainsKey("Play"))
                iconSet.Add("Play", loadTexture("WildBlueIndustries/Sandcastle/Icons/Play"));

            if (!iconSet.ContainsKey("Pause"))
                iconSet.Add("Pause", loadTexture("WildBlueIndustries/Sandcastle/Icons/Pause"));

            if (!iconSet.ContainsKey("Trash"))
                iconSet.Add("Trash", loadTexture("WildBlueIndustries/Sandcastle/Icons/Trash"));

            if (!iconSet.ContainsKey("Blank"))
                iconSet.Add("Blank", loadTexture("WildBlueIndustries/Sandcastle/Icons/Box"));

            // Check for Community Category Kit items
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("CCKCommonFilterConfig");
            if (nodes.Length > 0)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    if (nodes[index].HasNode("Item"))
                        loadIcons(nodes[index].GetNodes("Item"));
                }
            }

            nodes = GameDatabase.Instance.GetConfigNodes("CCKExtraFilterConfig");
            if (nodes.Length > 0)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    if (nodes[index].HasNode("Item"))
                        loadIcons(nodes[index].GetNodes("Item"));
                }
            }
        }

        private void loadIcons(ConfigNode[] nodes)
        {
            ConfigNode node;
            string categoryName;
            string textureName;
            string tag;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name") && node.HasValue("normalIcon") && node.HasValue("tag"))
                {
                    categoryName = node.GetValue("name");
                    textureName = node.GetValue("normalIcon");
                    tag = node.GetValue("tag");

                    if (!iconSet.ContainsKey(categoryName))
                        iconSet.Add(categoryName, loadTexture(textureName));
                    if (!cckTags.ContainsKey(categoryName))
                        cckTags.Add(categoryName, tag);
                }
            }
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
