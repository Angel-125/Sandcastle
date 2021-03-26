using System;
using System.Collections.Generic;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Represents an item that needs to be built.
    /// </summary>
    public class BuildItem
    {
        #region Constants
        /// <summary>
        /// Build item node identifier
        /// </summary>
        public const string kBuildItemNode = "BUILD_ITEM";

        const string kPartName = "partName";
        const string kTotalUnitsRequired = "totalUnitsRequired";
        const string kTotalUnitsPrinted = "totalUnitsPrinted";
        const string kRequiredComponent = "requiredComponent";
        const string kIsBeingRecycled = "isBeingRecycled";
        const string kPrintResource = "PRINT_RESOURCE";
        #endregion

        #region Housekeeping
        /// <summary>
        /// Name of the part being built.
        /// </summary>
        public string partName = string.Empty;

        /// <summary>
        /// The Available part representing the build item.
        /// </summary>
        public AvailablePart availablePart;

        /// <summary>
        /// List of resource materials required. Rate in this context represents the amount of the resource required in order to complete the part.
        /// </summary>
        public List<ModuleResource> materials;

        /// <summary>
        /// List of parts required to complete the build item. The parts must be in the vessel inventory.
        /// </summary>
        public List<string> requiredComponents;

        /// <summary>
        /// Total units required to produce the item, determined from all required resources.
        /// </summary>
        public double totalUnitsRequired;

        /// <summary>
        /// Total units printed to date, determined from all required resources.
        /// </summary>
        public double totalUnitsPrinted;

        /// <summary>
        /// Flag indicating whether or not the part is being recycled.
        /// </summary>
        public bool isBeingRecycled;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new build item from the supplied config node.
        /// </summary>
        /// <param name="node">A ConfigNode containing data for the build item.</param>
        public BuildItem(ConfigNode node)
        {
            if (node.HasValue(kPartName))
                partName = node.GetValue(kPartName);

            if (node.HasValue(kTotalUnitsRequired))
                double.TryParse(node.GetValue(kTotalUnitsRequired), out totalUnitsRequired);

            if (node.HasValue(kTotalUnitsPrinted))
                double.TryParse(node.GetValue(kTotalUnitsPrinted), out totalUnitsPrinted);

            if (node.HasValue(kIsBeingRecycled))
                bool.TryParse(node.GetValue(kIsBeingRecycled), out isBeingRecycled);

            materials = new List<ModuleResource>();
            if (node.HasNode(MaterialsList.kResourceNode))
            {

                ConfigNode[] nodes = node.GetNodes(MaterialsList.kResourceNode);
                ModuleResource resource;
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    materials.Add(resource);
                }
            }

            requiredComponents = new List<string>();
            if (node.HasValue(kRequiredComponent))
            {
                string[] components = node.GetValues(kRequiredComponent);
                for (int index = 0; index < components.Length; index++)
                    requiredComponents.Add(components[index]);
            }

            availablePart = PartLoader.getPartInfoByName(partName);
        }

        /// <summary>
        /// Constructs a build item from the supplied available part.
        /// </summary>
        /// <param name="availablePart">The AvailablePart to base the build item on.</param>
        public BuildItem(AvailablePart availablePart)
        {
            partName = availablePart.name;
            this.availablePart = availablePart;

            // Get the materials list
            MaterialsList materialsList = MaterialsList.GetListForCategory(availablePart.category.ToString());
            ModuleResource[] resources = materialsList.materials.ToArray();
            ModuleResource resource;
            ConfigNode node;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;

            // Copy the required materials and tally the total units required.
            totalUnitsRequired = 0;
            materials = new List<ModuleResource>();
            for (int index = 0; index < resources.Length; index++)
            {
                resource = resources[index];
                if (definitions.Contains(resource.name))
                {
                    resourceDef = definitions[resource.name];
                    node = new ConfigNode(MaterialsList.kResourceNode);
                    resource.Save(node);

                    resource = new ModuleResource();
                    resource.Load(node);
                    resource.amount = calculateRequiredAmount(availablePart.partPrefab.mass, resourceDef.density, resource.rate);
                    totalUnitsRequired += resource.amount;

                    materials.Add(resource);
                }
            }

            // If the part requires special resources, add them too.
            if (availablePart.partConfig.HasNode(kPrintResource))
            {
                ConfigNode[] nodes = availablePart.partConfig.GetNodes(kPrintResource);
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    if (definitions.Contains(resource.name))
                    {
                        resourceDef = definitions[resource.name];
                        resource.amount = calculateRequiredAmount(availablePart.partPrefab.mass, resourceDef.density, resource.rate);
                        totalUnitsRequired += resource.amount;

                        materials.Add(resource);
                    }
                }
            }

            // If the part requires additional components, add them too.
            requiredComponents = new List<string>();
            if (availablePart.partConfig.HasValue(kRequiredComponent))
            {
                string[] components = availablePart.partConfig.GetValues(kRequiredComponent);
                for (int index = 0; index < components.Length; index++)
                    requiredComponents.Add(components[index]);
            }

            // Finalize the new item
            totalUnitsPrinted = 0;
        }

        public BuildItem(BuildItem copyFrom)
        {
            materials = new List<ModuleResource>();
            requiredComponents = new List<string>();

            partName = copyFrom.partName;
            availablePart = copyFrom.availablePart;
            totalUnitsPrinted = copyFrom.totalUnitsPrinted;
            totalUnitsRequired = copyFrom.totalUnitsRequired;
            isBeingRecycled = copyFrom.isBeingRecycled;

            int count = copyFrom.requiredComponents.Count;
            for (int index = 0; index < count; index++)
            {
                requiredComponents.Add(copyFrom.requiredComponents[index]);
            }

            ModuleResource resource;
            ConfigNode node;
            count = copyFrom.materials.Count;
            for (int index = 0; index < count; index++)
            {
                resource = copyFrom.materials[index];
                node = new ConfigNode(MaterialsList.kResourceNode);
                resource.Save(node);

                resource = new ModuleResource();
                resource.Load(node);
                materials.Add(resource);
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Saves the build item.
        /// </summary>
        /// <returns>A ConfigNode containing serialized data.</returns>
        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode(kBuildItemNode);

            node.AddValue(kPartName, partName);
            node.AddValue(kTotalUnitsRequired, totalUnitsRequired.ToString());
            node.AddValue(kTotalUnitsPrinted, totalUnitsPrinted.ToString());

            // Materials
            ModuleResource[] resources = materials.ToArray();
            ConfigNode resourceNode;
            for (int index = 0; index < resources.Length; index++)
            {
                resourceNode = new ConfigNode(MaterialsList.kResourceNode);
                resources[index].Save(resourceNode);
                node.AddNode(resourceNode);
            }

            // Required components
            int count = requiredComponents.Count;
            for (int index = 0; index < count; index++)
            {
                node.AddValue(kRequiredComponent, requiredComponents[index]);
            }
            return node;
        }
        #endregion

        #region Helpers
        double calculateRequiredAmount(double partMass, double resourceDensity, double rate)
        {
            double multiplier = rate;
            if (multiplier < 1)
                multiplier = 1;

            return (partMass / resourceDensity) * multiplier;
        }
        #endregion
    }
}
