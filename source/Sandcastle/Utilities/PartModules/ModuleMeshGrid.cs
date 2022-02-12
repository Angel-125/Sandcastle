using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace Sandcastle.Utilities.PartModules
{
    /// <summary>
    /// This class provides a dynamically configurable mesh grid based on a single mesh transform. It clones the mesh and its colliders and places them in a grid based on the mesh's dimensions.
    /// </summary>
    public class ModuleMeshGrid: WBIPartModule, IPartCostModifier, IPartMassModifier
    {
        #region Constants
        const string kVariantName = "meshGrid";
        const string kVariantDisplayName = "Mesh Grid";
        const string kRows = "<<Rows>>";
        const string kColumns = "<<Columns>>";
        const string kStacks = "<<Stacks>>";
        const string kRowsAndColumns = "<<Rows*Columns>>";
        const string kRowsColumnsAndStacks = "<<Rows*Columns*Stacks>>";
        #endregion

        #region Fields
        /// <summary>
        /// Name of the model transform that has the mesh to duplicate. The mesh must have a box collider for the grid to work properly.
        /// </summary>
        [KSPField]
        public string meshTransformName = string.Empty;

        /// <summary>
        /// How many rows in the grid
        /// </summary>
        [KSPField(isPersistant = true)]
        public int rowCount = 1;

        /// <summary>
        /// How many columns in the grid.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int columnCount = 1;

        /// <summary>
        /// How many vertical stacks in the grid.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int stackCount = 1;
        #endregion

        #region Housekeeping
        float elementLength = 0;
        float elementWidth = 0;
        float elementHeight = 0;
        Transform meshTransform = null;
        Collider[] colliders = null;
        List<GameObject> gridElements = new List<GameObject>();
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
            if (string.IsNullOrEmpty(meshTransformName))
                return;

            setupPartNodes();

            meshTransform = part.FindModelTransform(meshTransformName);
            if (meshTransform != null)
            {
                Bounds rendererBounds = PartGeometryUtil.GetRendererBounds(meshTransform.gameObject);
                elementWidth = rendererBounds.size.x;
                elementLength = rendererBounds.size.z;
                elementHeight = rendererBounds.size.y;

                // Get the colliders
                colliders = meshTransform.GetComponents<Collider>();

                // Hide the mesh
                Renderer renderer = meshTransform.gameObject.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;

                // Update the grid.
                UpdateGrid();
            }
            else
            {
                Events["AddRow"].guiActiveEditor = false;
                Events["AddColumn"].guiActiveEditor = false;
                Events["RemoveRow"].guiActiveEditor = false;
                Events["RemoveColumn"].guiActiveEditor = false;
            }

            if (rowCount == 1)
            {
                Events["RemoveRow"].guiActiveEditor = false;
            }
            if (columnCount == 1)
            {
                Events["RemoveColumn"].guiActiveEditor = false;
            }

        }

        public override void OnWasCopied(PartModule copyPartModule, bool asSymCounterpart)
        {
            base.OnWasCopied(copyPartModule, asSymCounterpart);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            ModuleMeshGrid copyGrid = (ModuleMeshGrid)copyPartModule;
            copyGrid.copyOriginalNodes(originalNodes);
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

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SANDCASTLE_meshGridModuleInfo");
        }
        #endregion

        #region IPartCostModifier and IPartMassModifier
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.FIXED : ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            int numberOfElements = rowCount * columnCount;
            return part.partInfo.cost * (numberOfElements - 1);
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.FIXED : ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            int numberOfElements = rowCount * columnCount;
            return part.partInfo.partPrefab.mass * (numberOfElements - 1);
        }
        #endregion

        #region Inventory and construction
        public override void OnPartCreatedFomInventory(ModuleInventoryPart moduleInventoryPart)
        {
            base.OnPartCreatedFomInventory(moduleInventoryPart);
            rowCount = 1;
            columnCount = 1
;
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

        #region Events
        /// <summary>
        /// Adds a single row to the mesh grid.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiName = "#LOC_SANDCASTLE_meshGridAddRow", groupName = "MeshGrid", groupDisplayName = "#LOC_SANDCASTLE_meshGridGroup", groupStartCollapsed = true)]
        public void AddRow()
        {
            if (meshTransform == null)
                return;

            rowCount += 1;
            Events["RemoveRow"].guiActiveEditor = true;

            UpdateGrid();

            updateSymmetryParts();
        }

        /// <summary>
        /// Removes a single row to the mesh grid.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiName = "#LOC_SANDCASTLE_meshGridRemoveRow", groupName = "MeshGrid", groupDisplayName = "#LOC_SANDCASTLE_meshGridGroup", groupStartCollapsed = true)]
        public void RemoveRow()
        {
            if (meshTransform == null)
                return;

            rowCount -= 1;
            if (rowCount <= 1)
            {
                rowCount = 1;
                Events["RemoveRow"].guiActiveEditor = false;
            }

            UpdateGrid();

            updateSymmetryParts();
        }

        /// <summary>
        /// Adds a single column to the mesh grid.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiName = "#LOC_SANDCASTLE_meshGridAddColumn", groupName = "MeshGrid", groupDisplayName = "#LOC_SANDCASTLE_meshGridGroup", groupStartCollapsed = true)]
        public void AddColumn()
        {
            if (meshTransform == null)
                return;

            columnCount += 1;
            Events["RemoveColumn"].guiActiveEditor = true;

            UpdateGrid();

            updateSymmetryParts();
        }

        /// <summary>
        /// Removes a single column to the mesh grid.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiName = "#LOC_SANDCASTLE_meshGridRemoveColumn", groupName = "MeshGrid", groupDisplayName = "#LOC_SANDCASTLE_meshGridGroup", groupStartCollapsed = true)]
        public void RemoveColumn()
        {
            if (meshTransform == null)
                return;

            columnCount -= 1;
            if (columnCount <= 1)
            {
                Events["RemoveColumn"].guiActiveEditor = false;
                columnCount = 1;
            }

            UpdateGrid();

            updateSymmetryParts();
        }
        #endregion

        #region Mesh Grid
        private GameObject cloneMesh()
        {
            if (meshTransform == null)
                return null;

            // Clone the mesh
            GameObject goMesh = Instantiate(meshTransform.gameObject, part.transform);

            // Enable the renderer
            Renderer renderer = goMesh.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = true;

            // Setup the colliders
            if (colliders != null && colliders.Length > 0)
            {
                BoxCollider boxCollider;
                BoxCollider sourceBoxCollider;
                CapsuleCollider capsuleCollider;
                CapsuleCollider sourceCapsuleCollider;
                MeshCollider meshCollider;
                MeshCollider sourceMeshCollider;
                Mesh mesh;

                for (int index = 0; index < colliders.Length; index++)
                {
                    if (colliders[index] is BoxCollider)
                    {
                        sourceBoxCollider = (BoxCollider)colliders[index];
                        boxCollider = goMesh.AddComponent<BoxCollider>();

                        boxCollider.center = new Vector3(sourceBoxCollider.center.x, sourceBoxCollider.center.y, sourceBoxCollider.center.z);
                        boxCollider.size = new Vector3(sourceBoxCollider.size.x, sourceBoxCollider.size.y, sourceBoxCollider.size.z);
                        boxCollider.enabled = true;
                    }
                    else if (colliders[index] is CapsuleCollider)
                    {
                        sourceCapsuleCollider = (CapsuleCollider)colliders[index];
                        capsuleCollider = goMesh.AddComponent<CapsuleCollider>();

                        capsuleCollider.center = new Vector3(sourceCapsuleCollider.center.x, sourceCapsuleCollider.center.y, sourceCapsuleCollider.center.z);
                        capsuleCollider.height = sourceCapsuleCollider.height;
                        capsuleCollider.radius = sourceCapsuleCollider.radius;
                        capsuleCollider.enabled = true;
                    }
                    else if (colliders[index] is MeshCollider)
                    {
                        sourceMeshCollider = (MeshCollider)colliders[index];

                        mesh = new Mesh();
                        mesh.name = sourceMeshCollider.sharedMesh.name;
                        mesh.vertices = sourceMeshCollider.sharedMesh.vertices;
                        mesh.triangles = sourceMeshCollider.sharedMesh.triangles;
                        mesh.normals = sourceMeshCollider.sharedMesh.normals;
                        mesh.uv = sourceMeshCollider.sharedMesh.uv;

                        meshCollider = goMesh.AddComponent<MeshCollider>();
                        meshCollider.convex = sourceMeshCollider.convex;
                        meshCollider.isTrigger = sourceMeshCollider.isTrigger;
                        meshCollider.sharedMesh = mesh;
                    }
                }
            }

            return goMesh;
        }

        /// <summary>
        /// Updates the grid.
        /// </summary>
        public void UpdateGrid()
        {
            // Clear the list of every element.
            int count = gridElements.Count;
            for (int index = 0; index < count; index++)
            {
                DestroyImmediate(gridElements[index]);
            }
            gridElements.Clear();

            // Reposition the elements.
            repositionElements();

            // Move the attachment nodes.
            if (updateNodePositions)
                moveAttachmentNodes();

            // Update drag cubes
            part.DragCubes.ForceUpdate(true, true, true);

            // Refresh the part highlighter
            part.RefreshHighlighter();

            // Let interested parties know that we've updated the grid.
            PartVariant variant = createGridPartVariant();
            if (variant != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                    GameEvents.onEditorVariantApplied.Fire(part, variant);
                }
                GameEvents.onVariantApplied.Fire(part, variant);
            }
        }

        private void repositionElements()
        {
            if (rowCount == 1 && columnCount == 1)
            {
                if (gridElements.Count == 0)
                {
                    gridElements.Add(cloneMesh());
                }
                return;
            }

            // Adjusted values to account for a single element in the grid.
            int adjustedRowCount = rowCount - 1;
            int adjustedColumCount = columnCount - 1;

            // Calculate the translation vector for the elements
            float translateLength = adjustedRowCount / 2 * elementLength;
            if (adjustedRowCount % 2 > 0)
                translateLength += elementLength / 2;
            float translateWidth = adjustedColumCount / 2 * elementWidth;
            if (adjustedColumCount % 2 > 0)
                translateWidth += elementWidth / 2;
            Vector3 translationVector = new Vector3(-translateWidth, -translateLength, 0);

            // Clone and position the grid elements
            GameObject gridElement = null;
            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    gridElement = cloneMesh();
                    gridElements.Add(gridElement);

                    // Set initial position and then translate it.
                    gridElement.transform.localPosition = new Vector3(elementLength * column, elementWidth * row, 0);
                    gridElement.transform.localPosition += translationVector;
                }
            }
        }
        #endregion

        #region Part positioning
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
            UpdateGrid();
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

        private void setupPartNodes()
        {
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
        }

        private void moveAttachmentNodes()
        {
            // Determine how far to move
            float translateLength = elementLength / 2 * (rowCount - 1);
            float translateWidth = elementWidth / 2 * (columnCount - 1);

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
            if (node.orientation.y != 0 && rowCount > 1)
            {
                if (node.orientation.y > 0)
                    translationVector = new Vector3(0, translateLength, 0);
                else
                    translationVector = new Vector3(0, -translateLength, 0);

                repositioningNode.originalPosition += translationVector;
                repositioningNode.position = repositioningNode.originalPosition;
            }
            else if (node.orientation.x != 0 && columnCount > 1)
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
        #endregion

        #region Helpers
        void updateSymmetryParts()
        {
            ModuleMeshGrid grid;
            for (int index = 0; index < part.symmetryCounterparts.Count; index++)
            {
                grid = part.symmetryCounterparts[index].FindModuleImplementing<ModuleMeshGrid>();
                grid.rowCount = rowCount;
                grid.columnCount = columnCount;
                grid.stackCount = stackCount;
                grid.UpdateGrid();
            }
        }

        PartVariant createGridPartVariant()
        {
            ConfigNode node = getPartConfigNode();
            if (node == null || !node.HasNode("VARIANT"))
                return null;
            node = node.GetNode("VARIANT");
            if (!node.HasNode("EXTRA_INFO"))
                return null;

            // Make a copy of the node
            ConfigNode nodeCopy = new ConfigNode("VARIANT");
            ConfigNode.Merge(nodeCopy, node);
            node = nodeCopy;

            // Set name & display name
            StringBuilder builder = new StringBuilder();
            builder.Append(rowCount);
            builder.Append("x");
            builder.Append(columnCount);
            builder.Append("x");
            builder.Append(stackCount);
            string variantName = kVariantName + builder.ToString();
            string variantDisplayName = kVariantName + " " + builder.ToString();
            node.SetValue("name", variantName, true);
            node.SetValue("displayName", variantDisplayName, true);

            // Set mass & cost
            float mass = GetModuleMass(part.partInfo.partPrefab.mass, ModifierStagingSituation.CURRENT);
            float cost = GetModuleCost(part.partInfo.cost, ModifierStagingSituation.CURRENT);
            node.SetValue("cost", cost, true);
            node.SetValue("mass", mass, true);

            // Setup extra info
            ConfigNode extraInfoNode = node.GetNode("EXTRA_INFO");
            foreach (ConfigNode.Value valueItem in extraInfoNode.values)
            {
                if (valueItem.value.Contains(kRows))
                    valueItem.value = rowCount.ToString();

                else if (valueItem.value.Contains(kColumns))
                    valueItem.value = columnCount.ToString();

                else if (valueItem.value.Contains(kStacks))
                    valueItem.value = stackCount.ToString();

                else if (valueItem.value.Contains(kRowsAndColumns))
                    valueItem.value = (rowCount * columnCount).ToString();

                else if (valueItem.value.Contains(kRowsColumnsAndStacks))
                    valueItem.value = (rowCount * columnCount * stackCount).ToString();
            }
            
            // Generate the part variant
            PartVariant variant = new PartVariant(variantName, variantDisplayName, null);
            variant.Load(node);

            return variant;
        }
        #endregion
    }
}