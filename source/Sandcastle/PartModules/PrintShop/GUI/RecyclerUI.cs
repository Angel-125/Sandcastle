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
    public class RecyclerUI: Dialog<RecyclerUI>
    {
        #region Fields
        /// <summary>
        /// Title of the selection dialog.
        /// </summary>
        public string titleText = Localizer.Format("#LOC_SANDCASTLE_recyclerTitle");

        /// <summary>
        /// Complete list of recyclable parts.
        /// </summary>
        public List<AvailablePart> partsList;

        /// <summary>
        /// Represents the list of build items to print.
        /// </summary>
        public List<BuildItem> recycleQueue;

        /// <summary>
        /// Status of the current print job.
        /// </summary>
        public string jobStatus = string.Empty;

        /// <summary>
        /// Callback to let the controller know about the print state.
        /// </summary>
        public UpdatePrintStatusDelegate onRecycleStatus;

        /// <summary>
        /// Flag indicating that the printer is printing
        /// </summary>
        public bool isRecycling;

        /// <summary>
        /// The Part associated with the UI.
        /// </summary>
        public Part part;

        /// <summary>
        /// How much of the part's resources are recycled.
        /// </summary>
        public double recyclePercentage = 1.0f;
        #endregion

        #region Housekeeping
        Dictionary<string, BuildItem> itemCache;
        AvailablePart previewPart;
        Texture2D previewPartImage;
        string previewPartRequirements = string.Empty;
        string previewPartDescription = string.Empty;
        string previewPartMassVolume = string.Empty;
        Dictionary<string, Texture2D> iconSet;
        Vector2 partsScrollPos;
        Vector2 partInfoScrollPos;
        Vector2 partQueueScrollPos;
        Vector2 partDescriptionScrollPos;
        GUILayoutOption[] buttonDimensions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        GUILayoutOption[] selectorPanelWidth = new GUILayoutOption[] { GUILayout.Width(235) };
        GUILayoutOption[] selectorButtonDimensions = new GUILayoutOption[] { GUILayout.MinWidth(64), GUILayout.MinHeight(64), GUILayout.MaxWidth(64), GUILayout.MaxHeight(64) };
        GUILayoutOption[] previewImageDimensions = new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(100) };
        GUILayoutOption[] previewImagePaneDimensions = new GUILayoutOption[] { GUILayout.Height(200), GUILayout.Width(115) };
        GUILayoutOption[] partRequirementsHeight = new GUILayoutOption[] { GUILayout.Height(200) };
        GUILayoutOption[] previewDescriptionHeight = new GUILayoutOption[] { GUILayout.Height(100) };
        GUILayoutOption[] recycleQueueHeight = new GUILayoutOption[] { GUILayout.Height(165) };
        GUILayoutOption[] partInfoWidth = new GUILayoutOption[] { GUILayout.Width(335) };
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
        public RecyclerUI() :
        base("Cargo Recycler", 590, 600)
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
            updateThumbnails();

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

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            // Draw parts list
            drawPartsList();
            GUILayout.EndHorizontal();

            // Print status
            drawPrintStatus();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
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
            drawRecycleQueue();

            GUILayout.EndScrollView();
        }

        private void drawPrintStatus()
        {
            GUILayout.BeginHorizontal();
            // Pause/print button
            Texture2D buttonTexture = isRecycling ? iconSet["Pause"] : iconSet["Play"];
            if (GUILayout.Button(buttonTexture, buttonDimensions))
            {
                isRecycling = !isRecycling;
                onRecycleStatus(isRecycling);
            }

            // Cancel print job button
            if (GUILayout.Button(iconSet["Trash"], buttonDimensions))
            {
                // We always work from the first item in the queue.
                recycleQueue.RemoveAt(0);
            }

            GUILayout.BeginVertical();
            // Current print job
            string jobTitle = recycleQueue.Count > 0 ? recycleQueue[0].availablePart.title : "None";
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_currentJob", new string[1] { jobTitle } ));

            // Job status
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_currentJob", new string[1] { jobStatus } ));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void drawRecycleQueue()
        {
            GUILayout.Label(Localizer.Format("#LOC_SANDCASTLE_recycleQueue"));

            List<BuildItem> doomed = new List<BuildItem>();
            partQueueScrollPos = GUILayout.BeginScrollView(partQueueScrollPos, recycleQueueHeight);
            int count = recycleQueue.Count;
            for (int index = 0; index < count; index++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(iconSet["Trash"], buttonDimensions))
                {
                    doomed.Add(recycleQueue[index]);
                }
                GUILayout.Label("<color=white>" + recycleQueue[index].availablePart.title + "</color>");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // Clear out any doomed items
            count = doomed.Count;
            for (int index = 0; index < count; index++)
            {
                recycleQueue.Remove(doomed[index]);
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
                    recycleQueue.Add(buildItem);
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

        #endregion

        #region Helpers
        /// <summary>
        /// Updates the part preview
        /// </summary>
        /// <param name="partIndex">An Int containing the index of the part to preview</param>
        public void updatePartPreview(int partIndex)
        {
            previewPart = partsList[partIndex];

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

            // Recycled resources
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            int count = item.materials.Count;
            double amount = 0;
            double maxAmount = 0;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recycledResources"));
                for (int index = 0; index < count; index++)
                {
                    if (definitions.Contains(item.materials[index].name))
                    {
                        resourceDef = definitions[item.materials[index].name];
                        part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                        requirements.AppendLine(string.Format("<color=white>{0:s}: {1:n3}u</color>", resourceDef.displayName, item.materials[index].amount * recyclePercentage));
                    }
                }
            }

            // Recycled parts
            count = item.requiredComponents.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_recycledParts"));
                AvailablePart requiredPart;
                int recycledPartCount = 0;
                for (int index = 0; index < count; index++)
                {
                    requiredPart = PartLoader.getPartInfoByName(item.requiredComponents[index].name);
                    recycledPartCount = item.requiredComponents[index].amount;
                    if (recycledPartCount <= 1)
                        requirements.AppendLine(requiredPart.title);
                    else
                        requirements.AppendLine(requiredPart.title + ": " + recycledPartCount.ToString());
                }
            }

            // Write out the part info
            previewPartRequirements = "<color=white>" + requirements.ToString() + "</color>";

            // Reset our scroll position
            partDescriptionScrollPos = Vector2.zero;
            partInfoScrollPos = Vector2.zero;
        }

        /// <summary>
        /// Updates the part thumbnails
        /// </summary>
        public void updateThumbnails()
        {
            // Filter parts for the current category.
            int count = partsList.Count;
            List<Texture2D> thumbnails = new List<Texture2D>();
            AvailablePart availablePart;
            for (int index = 0; index < count; index++)
            {
                availablePart = partsList[index];
                thumbnails.Add(InventoryUtils.GetTexture(availablePart.name));
            }

            partImages = thumbnails.ToArray();
            selectedIndex = 0;
            currentIndex = 0;
            partsScrollPos = Vector2.zero;
        }

        private void loadIcons()
        {
            iconSet = new Dictionary<string, Texture2D>();

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
