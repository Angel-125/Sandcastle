using System;
using System.Collections.Generic;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Represents a list of resources needed to build an item of a particular part category.
    /// </summary>
    public class MaterialsList
    {
        #region constants
        /// <summary>
        /// Node ID for a materials list.
        /// </summary>
        public const string kMaterialsListNode = "MATERIALS_LIST";

        /// <summary>
        /// Node ID for tech node materials. Parts in a specific tech node can require additional materials.
        /// </summary>
        public const string kTechNodeMaterials = "TECH_NODE_MATERIALS";

        /// <summary>
        /// Name of the default materials list.
        /// </summary>
        public const string kDefaultMaterialsListName = "Default";

        /// <summary>
        /// Represents a resource node.
        /// </summary>
        public const string kResourceNode = "RESOURCE";

        const string kListName = "name";
        const string kDefaultResource = "Ore";
        const string kDefaultRate = "5";
        const string kDefaultFlowMode = "STAGE_PRIORITY_FLOW";
        const string kRequiredComponentNode = "REQUIRED_COMPONENT";
        const string kRequiredComponentName = "name";
        const string kRequriedComponentAmount = "amount";
        #endregion

        #region Housekeeping
        /// <summary>
        /// Name of the materials list. This should correspond to one of the part categories.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// List of resource materials required.
        /// </summary>
        public List<ModuleResource> materials;

        /// <summary>
        /// List of components required by the materials list.
        /// </summary>
        public List<PartRequiredComponent> requiredComponents;
        #endregion

        #region Constructors
        /// <summary>
        /// Loads the materials lists that specify what materials are required to produce an item from a particular category.
        /// </summary>
        /// <returns>A Dictionary containing the list names as keys and MaterialList objects as values.</returns>
        public static Dictionary<string, MaterialsList> LoadLists()
        {
            materialsLists = new Dictionary<string, MaterialsList>();

            // Load the materials list for part categories.
            loadListsWithID(kMaterialsListNode);

            // Load the optional additional materials lists for tech nodes.
            loadListsWithID(kTechNodeMaterials);

            // Make sure we at least have the default list.
            if (!materialsLists.ContainsKey(kDefaultMaterialsListName))
            {
                materialsLists.Add(kDefaultMaterialsListName, GetDefaultList());
            }

            return materialsLists;
        }

        MaterialsList()
        {
            materials = new List<ModuleResource>();
            requiredComponents = new List<PartRequiredComponent>();
        }
        #endregion

        #region statics
        /// <summary>
        /// A map of all materials lists, keyed by part category name.
        /// </summary>
        public static Dictionary<string, MaterialsList> materialsLists;

        /// <summary>
        /// Returns the materials list for the requested category, or the default list if the list for the requested category doesn't exist.
        /// </summary>
        /// <param name="categoryName">A string containing the desired category.</param>
        /// <returns>A MaterialsList if one exists for the desired category, or the default list.</returns>
        public static MaterialsList GetListForCategory(string categoryName)
        {
            if (materialsLists.ContainsKey(categoryName))
                return materialsLists[categoryName];
            else if (materialsLists.ContainsKey(kDefaultMaterialsListName))
                return materialsLists[kDefaultMaterialsListName];
            else
            {
                materialsLists.Add(kDefaultMaterialsListName, GetDefaultList());
                return materialsLists[kDefaultMaterialsListName];
            }
        }

        /// <summary>
        /// Creates the default materials list.
        /// </summary>
        /// <returns>A MaterialsList containing the default materials.</returns>
        public static MaterialsList GetDefaultList()
        {
            MaterialsList materialsList = new MaterialsList();
            materialsList.name = kDefaultMaterialsListName;

            ConfigNode node = new ConfigNode(kResourceNode);
            node.AddValue("name", kDefaultResource);
            node.AddValue("rate", kDefaultRate);

            ModuleResource resource = new ModuleResource();
            resource.Load(node);

            materialsList.materials = new List<ModuleResource>();
            materialsList.materials.Add(resource);

            return materialsList;
        }
        #endregion

        #region Helpers
        private static void loadListsWithID(string nodeID)
        {
            if (GameDatabase.Instance == null)
                return;
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(nodeID);
            if (nodes.Length == 0)
                return;

            ConfigNode[] resourceNodes;
            ConfigNode node;
            ModuleResource resource;
            MaterialsList materialsList;

            // Load the config nodes.
            for (int index = 0; index < nodes.Length; index++)
            {
                materialsList = new MaterialsList();

                node = nodes[index];

                // Load the resources
                if (node.HasValue(kListName) && node.HasNode(kResourceNode))
                {
                    materialsList.name = node.GetValue(kListName);

                    resourceNodes = node.GetNodes(kResourceNode);
                    for (int nodeIndex = 0; nodeIndex < resourceNodes.Length; nodeIndex++)
                    {
                        resource = new ModuleResource();
                        resource.Load(resourceNodes[nodeIndex]);
                        materialsList.materials.Add(resource);
                    }

                    materialsLists.Add(materialsList.name, materialsList);
                }

                // Now load the requrired components, if any.
                if (node.HasNode(kRequiredComponentNode))
                {
                    PartRequiredComponent component;
                    ConfigNode[] componentNodes = node.GetNodes(kRequiredComponentNode);
                    ConfigNode componentNode;

                    for (int nodeIndex = 0; nodeIndex < componentNodes.Length; nodeIndex++)
                    {
                        componentNode = componentNodes[index];
                        if (!componentNode.HasValue(kRequiredComponentName) || !componentNode.HasValue(kRequriedComponentAmount))
                            continue;

                        component = new PartRequiredComponent();
                        component.name = componentNode.GetValue(kRequiredComponentName);
                        if (!int.TryParse(componentNode.GetValue(kRequriedComponentAmount), out component.amount))
                            component.amount = 1;

                        materialsList.requiredComponents.Add(component);
                    }
                }
            }
        }
        #endregion

    }
}
