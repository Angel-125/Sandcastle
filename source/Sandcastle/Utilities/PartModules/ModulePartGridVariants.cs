using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace Sandcastle
{
    /// <summary>
    /// This is a specialized class that creates a two-dimensional grid of meshes from a collection of meshes provided by the model.
    /// While it is possible to duplicate multiple copies of a single transform, research shows that the part's radial attachment
    /// system gets messed up when you do that. So for now, we have a grid that is limited by the total number of meshes in the model.
    /// </summary>
    public class ModulePartGridVariants : WBIPartModule, IPartCostModifier, IPartMassModifier, IModuleInfo
    {
        #region Constants
        const float previewOpacity = 0.3f;
        const float messageDuration = 5f;
        #endregion

        #region Fields
        /// <summary>
        /// Base name of the meshes found in the part's model object.
        /// All model transforms start with this prefix. Individual elements in the mesh should have " (n)" appended to them.
        /// NOTE: Be sure to have a total number of elements equal to totalRows * totalColumns and be sure to label them from (0) to (totalElements - 1)
        /// Example: yardFrameFlat37 (0), yardFrameFlat37 (1) ... yardFrameFlat37 (35)
        /// Note that there is a space between the prefix and the element id.
        /// </summary>
        [KSPField]
        public string elementTransformName = string.Empty;

        /// <summary>
        /// Length of a single element, in meters.
        /// </summary>
        [KSPField]
        public float elementLength = 0;

        /// <summary>
        /// Width of a single element, in meters.
        /// </summary>
        [KSPField]
        public float elementWidth = 0;

        /// <summary>
        /// Height of a single element, in meters.
        /// </summary>
        [KSPField]
        public float elementHeight = 0;

        /// <summary>
        /// Total number of rows that are possible in the grid.
        /// </summary>
        [KSPField]
        public int totalRows = 1;

        /// <summary>
        /// Total number of columns that are possible in the grid.
        /// </summary>
        [KSPField]
        public int totalColumns = 1;

        /// <summary>
        /// Current selected row variant.
        /// </summary>
        [KSPField(isPersistant = true)]
        [UI_VariantSelector(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public int rowIndex = 0;

        /// <summary>
        /// Current selected column variant.
        /// </summary>
        [KSPField(isPersistant = true)]
        [UI_VariantSelector(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public int columnIndex = 0;
        #endregion

        #region Housekeeping
        List<Transform> gridElements = new List<Transform>();
        int totalElements = 0;
        List<AttachNode> originalNodes = new List<AttachNode>();
        List<Vector3> originalNodePositions = new List<Vector3>();
        bool updateNodePositions = true;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            // Calculate total elements and find the meshes.
            totalElements = totalRows * totalColumns;
            findGridElements();

            // Setup variants
            setupVariantSelector("rowIndex", "#LOC_SANDCASTLE_rowsVariant", totalRows, "#000000", "#ffffff");
            setupVariantSelector("columnIndex", "#LOC_SANDCASTLE_columnsVariant", totalColumns, "#ffffff", "#000000");

            // Create the list of original nodes.
            int count = part.attachNodes.Count;
            if (originalNodes.Count < count)
            {
                originalNodes.Clear();
                for (int index = 0; index < count; index++)
                    originalNodes.Add(AttachNode.Clone(part.attachNodes[index]));
                originalNodes.Add(AttachNode.Clone(part.srfAttachNode));
            }

            // If the grid was set up previously, then when we start, the attachNodes will NOT reflect their original predefined locations (as defined in the config file).
            // Since we cloned them into originalNodes, we need to redo the originalNodes so that they reflect their original predefined locations.
            // We can do that if originalNodePositions was loaded during OnLoad.
            if (originalNodePositions.Count > 0)
            {
                count = originalNodePositions.Count;
                for (int index = 0; index < count; index++)
                {
                    originalNodes[index].position = originalNodePositions[index];
                    originalNodes[index].originalPosition = originalNodePositions[index];
                }

                // We also need to make sure that we don't move the attach nodes since they're already in place.
                updateNodePositions = false;
            }

            // If originalNodePositions is empty, then it means that this is a new instance of a part (OnLoad isn't called when a new part is created in the editor), 
            // and we can trust that attachNodes are in their original predefined locations.
            else
            {
                count = part.attachNodes.Count;
                for (int index = 0; index < count; index++)
                    originalNodePositions.Add(part.attachNodes[index].position);
                originalNodePositions.Add(part.srfAttachNode.position);
            }

            // Update the grid.
            updateModelGrid();

            // Allow in-space construction?
            if (HighLogic.LoadedSceneIsFlight && rowIndex <= 0 && columnIndex <= 0)
            {
                Events["EnableConstructionMode"].guiActiveUnfocused = true;
            }
        }

        public override void OnWasCopied(PartModule copyPartModule, bool asSymCounterpart)
        {
            base.OnWasCopied(copyPartModule, asSymCounterpart);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            ModulePartGridVariants copyGrid = (ModulePartGridVariants)copyPartModule;
            copyGrid.copyOriginalNodes(originalNodes);
        }

        /// <summary>
        /// Called when the part was copied in the editor.
        /// </summary>
        /// <param name="copyNodes">The list of AttachNode objects to copy into our originalNodes field.</param>
        public void copyOriginalNodes(List<AttachNode> copyNodes)
        {
            // Copy the original nodes
            originalNodes.Clear();
            int count = copyNodes.Count;
            for (int index = 0; index < count; index++)
            {
                originalNodes.Add(AttachNode.Clone(copyNodes[index]));
            }

            // Reset our attachment nodes. We do this because when the original was copied, the duplicate will have
            // repositioned attachment nodes instead of their original positions.
            resetAttachNodes();

            // Update the grid
            updateModelGrid();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Save original node positions if we have them.
            int count;
            string nodePosition;
            Vector3 position;
            if (originalNodePositions.Count > 0)
            {
                count = originalNodePositions.Count;
                for (int index = 0; index < count; index++)
                {
                    position = originalNodePositions[index];
                    nodePosition = string.Format("{0:s},{1:s},{2:s}", position.x.ToString(), position.y.ToString(), position.z.ToString());
                    node.AddValue("originalNodePosition", nodePosition);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            string[] originalPositions = node.GetValues("originalNodePosition");
            if (originalPositions.Length <= 0)
                return;

            string[] vectorValues;
            char[] delimiter = new char[] { ',' };
            Vector3 position = Vector3.zero;

            originalNodePositions.Clear();
            for (int index = 0; index < originalPositions.Length; index++)
            {
                vectorValues = originalPositions[index].Split(delimiter);

                position = Vector3.zero;
                float.TryParse(vectorValues[0], out position.x);
                float.TryParse(vectorValues[1], out position.y);
                float.TryParse(vectorValues[2], out position.z);

                originalNodePositions.Add(position);
            }
        }

        public void Destroy()
        {

        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SANDCASTLE_moduleInfo", new string[2] { totalRows.ToString(), totalColumns.ToString() });
        }

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SANDCASTLE_moduleName");
        }
        #endregion

        #region Inventory and construction
        public override void OnPartCreatedFomInventory(ModuleInventoryPart moduleInventoryPart)
        {
            base.OnPartCreatedFomInventory(moduleInventoryPart);
            rowIndex = 0;
            columnIndex = 0;
        }

        public override void OnInventoryModeDisable()
        {
            base.OnInventoryModeDisable();
        }

        public override void OnInventoryModeEnable()
        {
            base.OnInventoryModeEnable();
        }
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return GetModuleDisplayName();
        }

        public string GetPrimaryField()
        {
            return Localizer.Format("#LOC_SANDCASTLE_modulePrimaryInfo", new string[2] { totalRows.ToString(), totalColumns.ToString() });
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }
        #endregion

        #region IPartCostModifier and IPartMassModifier
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            int numberOfElements = (rowIndex + 1) * (columnIndex + 1);
            return part.partInfo.cost * (numberOfElements -1);
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            int numberOfElements = (rowIndex + 1) * (columnIndex + 1);
            return part.partInfo.partPrefab.mass * (numberOfElements - 1);
        }
        #endregion

        #region Helpers
        private void updateModelGrid()
        {
            if (gridElements.Count < ((rowIndex + 1) * (columnIndex + 1)))
                return;

            // Reposition the elements
            repositionElements();

            // Move the attachment nodes.
            if (updateNodePositions)
                moveAttachmentNodes();

            // Update drag cubes
            part.DragCubes.ForceUpdate(true, true, true);

            // Finally, refresh the highlighter
            part.RefreshHighlighter();
        }

        private void moveAttachmentNodes()
        {
            // Determine how far to move
            float translateLength = elementLength / 2 * rowIndex;
            float translateWidth = elementWidth / 2 * columnIndex;

            // Go through all the stack nodes and move them.
            Vector3 translationVector = Vector3.zero;
            AttachNode node = null;
            int count = part.attachNodes.Count;
            for (int index = 0; index < count; index++)
            {
                node = part.attachNodes[index];
                if (node.orientation.y != 0 || node.orientation.x != 0)
                    repositionNode(node, AttachNode.Clone(originalNodes[index]), translateLength, translateWidth);
            }

            // Update surface attachment node
            if (part.srfAttachNode != null)
                repositionNode(part.srfAttachNode, AttachNode.Clone(originalNodes[originalNodes.Count - 1]), translateLength, translateWidth);
        }

        private void repositionNode(AttachNode node, AttachNode repositioningNode, float translateLength, float translateWidth)
        {
            Vector3 translationVector = Vector3.zero;

            // If orientation is on the y axis, then modify based on length. Otherwise, if orientation is on the x axis then modify based on width.
            if (node.orientation.y != 0 && rowIndex > 0)
            {
                if (node.orientation.y > 0)
                    translationVector = new Vector3(0, translateLength, 0);
                else
                    translationVector = new Vector3(0, -translateLength, 0);

                repositioningNode.originalPosition += translationVector;
                repositioningNode.position = repositioningNode.originalPosition;
            }
            else if (node.orientation.x != 0 && columnIndex > 0)
            {
                if (node.orientation.x > 0)
                    translationVector = new Vector3(translateWidth, 0, 0);
                else
                    translationVector = new Vector3(-translateWidth, 0, 0);

                repositioningNode.originalPosition += translationVector;
                repositioningNode.position = repositioningNode.originalPosition;
            }

            // Update part position
            if (HighLogic.LoadedSceneIsEditor)
                ModulePartVariants.UpdatePartPosition(node, repositioningNode);
            else if (HighLogic.LoadedSceneIsFlight)
                updatePartPosition(node, repositioningNode);

            // Update the node.
            node.originalPosition = repositioningNode.originalPosition;
            node.position = repositioningNode.position;
        }

        private void updatePartPosition(AttachNode currentNode, AttachNode newNode)
        {
            if (currentNode.attachedPart != null && currentNode.attachedPart != part.vessel.rootPart)
            {
                Vector3 translation = Vector3.zero;
                if (currentNode.attachedPart != null)
                {
                    if (currentNode.owner == null)
                        return;
                    translation = currentNode.owner.transform.TransformPoint(newNode.originalPosition) - currentNode.owner.transform.TransformPoint(currentNode.originalPosition);
                    currentNode.attachedPart.transform.Translate(translation, Space.World);
                }

                if (currentNode.owner.potentialParent == null)
                    return;

                Vector3 vector3 = currentNode.owner.transform.TransformPoint(newNode.originalPosition);
                translation = currentNode.owner.transform.TransformPoint(currentNode.originalPosition) - vector3;
                currentNode.owner.transform.Translate(translation, Space.World);
            }
        }

        private void repositionElements()
        {
            int rowCount = rowIndex + 1;
            int columnCount = columnIndex + 1;

            // Calculate the translation vector for the elements
            float translateLength = rowIndex / 2 * elementLength;
            if (rowIndex % 2 > 0)
                translateLength += elementLength / 2;
            float translateWidth = columnIndex / 2 * elementWidth;
            if (columnIndex % 2 > 0)
                translateWidth += elementWidth / 2;
            Vector3 translationVector = new Vector3(-translateWidth, -translateLength, 0);

            // Enable and position the elements
            int lastUsedIndex = 0;
            Transform gridElement;
            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    gridElement = gridElements[lastUsedIndex];
                    lastUsedIndex += 1;
                    setEnabled(gridElement, true);

                    // Set initial position and then translate it.
                    gridElement.localPosition = new Vector3(elementLength * column, elementWidth * row, 0);
                    gridElement.localPosition += translationVector;
                }
            }

            // Hide the remaining elements
            for (int index = lastUsedIndex; index < totalElements; index++)
            {
                gridElement = gridElements[index];
                setEnabled(gridElement, false);
                gridElement.localPosition = Vector3.zero;
            }
        }

        private string getElementIndexTransformName(int index)
        {
            string transformName = elementTransformName + string.Format(" ({0:D})", index);
            return transformName;
        }

        private void findGridElements()
        {
            if (string.IsNullOrEmpty(elementTransformName))
                return;

            Transform gridElement;
            for (int index = 0; index < totalElements; index++)
            {
                gridElement = part.FindModelTransform(getElementIndexTransformName(index));
                if (gridElement != null)
                {
                    gridElements.Add(gridElement);
                    setEnabled(gridElement);
                }
            }

            if (gridElements.Count > 0)
                setEnabled(gridElements[0], true);
        }

        private void setEnabled(Transform transform, bool isEnabled = false)
        {
            transform.gameObject.SetActive(isEnabled);
            Collider collider = transform.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isEnabled;
        }

        private void setupVariantSelector(string fieldName, string displayName, int totalVariants, string primaryColor, string secondaryColor)
        {
            UI_VariantSelector variantSelector = null;
            PartVariant variant;
            string variantName;

            if (HighLogic.LoadedSceneIsEditor)
                variantSelector = this.Fields[fieldName].uiControlEditor as UI_VariantSelector;
            else
                variantSelector = this.Fields[fieldName].uiControlFlight as UI_VariantSelector;
            variantSelector.onFieldChanged += new Callback<BaseField, object>(this.onVariantChanged);
            variantSelector.onSymmetryFieldChanged += new Callback<BaseField, object>(this.onVariantChanged);

            // Setup variant list
            variantSelector.variants = new List<PartVariant>();

            for (int index = 0; index < totalVariants; index++)
            {
                variantName = fieldName + index.ToString();
                variant = new PartVariant(variantName, Localizer.Format(displayName, new string[1] { (index + 1).ToString() }), null);
                variant.PrimaryColor = primaryColor;
                variant.SecondaryColor = secondaryColor;
                variantSelector.variants.Add(variant);
            }
        }

        private void resetAttachNodes()
        {
            int count = part.attachNodes.Count;
            for (int index = 0; index < count; index++)
            {
                ModulePartVariants.UpdatePartPosition(part.attachNodes[index], originalNodes[index]);
                part.attachNodes[index].position = originalNodes[index].position;
                part.attachNodes[index].originalPosition = originalNodes[index].originalPosition;
            }
            if (part.srfAttachNode != null)
            {
                ModulePartVariants.UpdatePartPosition(part.srfAttachNode, originalNodes[originalNodes.Count - 1]);
                part.srfAttachNode.position = originalNodes[originalNodes.Count - 1].position;
                part.srfAttachNode.originalPosition = originalNodes[originalNodes.Count - 1].originalPosition;
            }
        }

        void onVariantChanged(BaseField baseField, object obj)
        {
            updateNodePositions = true;
            updateModelGrid();
        }
        #endregion
    }
}
