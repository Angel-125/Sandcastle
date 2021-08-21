using System;
using System.Collections.Generic;

namespace Sandcastle.PrintShop
{
    public struct PartRequiredComponent
    {
        public string name;
        public int amount;
    }

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
        const string kRequiredComponentNode = "REQUIRED_COMPONENT";
        const string kRequiredComponentName = "name";
        const string kRequriedComponentAmount = "amount";
        const string kIsBeingRecycled = "isBeingRecycled";
        const string kPrintResource = "PRINT_RESOURCE";
        const string kMinimumGravity = "minimumGravity";
        const string kMinimumPressure = "minimumPressure";
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
        public List<PartRequiredComponent> requiredComponents;

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

        /// <summary>
        /// The mininum gravity, in m/sec^2, that the part requires in order for the printer to print it.
        /// If set to 0, then the printer's vessel must be orbiting, sub-orbital, or on an escape trajectory, and not under acceleration.
        /// The default is -1, which ignores this requirement.
        /// </summary>
        public float minimumGravity = -1;

        /// <summary>
        /// The minimum pressure, in kPA, that the part required in order for the printer to print it.
        /// If set to > 1, then the printer's vessel must be in an atmosphere or submerged.
        /// If set to 0, then the printer's vessel must be in a vacuum.
        /// </summary>
        public float minimumPressure = -1;
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

            if (node.HasValue(kMinimumGravity))
                float.TryParse(node.GetValue(kMinimumGravity), out minimumGravity);

            if (node.HasValue(kMinimumPressure))
                float.TryParse(node.GetValue(kMinimumPressure), out minimumPressure);

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

            requiredComponents = new List<PartRequiredComponent>();
            if (node.HasNode(kRequiredComponentNode))
            {
                ConfigNode[] nodes = node.GetNodes(kRequiredComponentNode);
                ConfigNode componentNode;
                PartRequiredComponent component;
                for (int index = 0; index < nodes.Length; index++)
                {
                    componentNode = nodes[index];
                    if (!componentNode.HasValue(kRequiredComponentName) || !componentNode.HasValue(kRequriedComponentAmount))
                        continue;

                    component = new PartRequiredComponent();
                    component.name = componentNode.GetValue(kRequiredComponentName);
                    if (!int.TryParse(kRequriedComponentAmount, out component.amount))
                        component.amount = 1;

                    requiredComponents.Add(component);
                }
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

                        materials.Add(resource);
                    }
                }
            }

            // Tally the sum of all required resources' rates. Also grab the Ore resource if it's a requirement.
            int count = materials.Count;
            ModuleResource oreResource = null;
            double totalRate = 0;
            for (int index = 0; index < count; index++)
            {
                resource = materials[index];
                totalRate += resource.rate;

                if (resource.name == "Ore")
                    oreResource = resource;
            }

            // Sanity check: the sum of each material's rate must be equal to or greater than 1 to ensure conservation of mass.
            // If that isn't the case, then add/increase Ore until the sum equals 1.
            if (totalRate < 1)
            {
                if (oreResource != null)
                {
                    oreResource.rate += 1 - totalRate;
                }
                else
                {
                    oreResource = new ModuleResource();
                    oreResource.name = "Ore";
                    oreResource.rate = 1 - totalRate;
                    materials.Add(oreResource);
                }
            }

            // Now tally up the total units required and each indivdual resources required amounts.
            totalUnitsRequired = 0;
            count = materials.Count;
            for (int index = 0; index < count; index++)
            {
                resource = materials[index];
                resourceDef = definitions[resource.name];
                resource.amount = calculateRequiredAmount(availablePart.partPrefab.mass, resourceDef.density, resource.rate);
                totalUnitsRequired += resource.amount;
            }

            // If the part requires additional components, add them too.
            requiredComponents = new List<PartRequiredComponent>();
            if (availablePart.partConfig.HasNode(kRequiredComponentNode))
            {
                ConfigNode[] nodes = availablePart.partConfig.GetNodes(kRequiredComponentNode);
                ConfigNode componentNode;
                PartRequiredComponent component;
                for (int index = 0; index < nodes.Length; index++)
                {
                    componentNode = nodes[index];
                    if (!componentNode.HasValue(kRequiredComponentName) || !componentNode.HasValue(kRequriedComponentAmount))
                        continue;

                    component = new PartRequiredComponent();
                    component.name = componentNode.GetValue(kRequiredComponentName);
                    if (!int.TryParse(componentNode.GetValue(kRequriedComponentAmount), out component.amount))
                        component.amount = 1;

                    requiredComponents.Add(component);
                }
            }

            // Get minimum gravity requirements
            if (availablePart.partConfig.HasValue(kMinimumGravity))
                float.TryParse(availablePart.partConfig.GetValue(kMinimumGravity), out minimumGravity);

            // Minimum pressure requirements
            if (availablePart.partConfig.HasValue(kMinimumPressure))
                float.TryParse(availablePart.partConfig.GetValue(kMinimumPressure), out minimumPressure);

            // Finalize the new item
            totalUnitsPrinted = 0;
        }

        public BuildItem(BuildItem copyFrom)
        {
            materials = new List<ModuleResource>();
            requiredComponents = new List<PartRequiredComponent>();

            partName = copyFrom.partName;
            availablePart = copyFrom.availablePart;
            totalUnitsPrinted = copyFrom.totalUnitsPrinted;
            totalUnitsRequired = copyFrom.totalUnitsRequired;
            isBeingRecycled = copyFrom.isBeingRecycled;
            minimumGravity = copyFrom.minimumGravity;
            minimumPressure = copyFrom.minimumPressure;

            PartRequiredComponent component;
            int count = copyFrom.requiredComponents.Count;
            for (int index = 0; index < count; index++)
            {
                component = new PartRequiredComponent();
                component.name = copyFrom.requiredComponents[index].name;
                component.amount = copyFrom.requiredComponents[index].amount;
                requiredComponents.Add(component);
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
            ConfigNode componentNode;
            for (int index = 0; index < count; index++)
            {
                componentNode = new ConfigNode(kRequiredComponentNode);
                componentNode.AddValue(kRequiredComponentName, requiredComponents[index].name);
                componentNode.AddValue(kRequriedComponentAmount, requiredComponents[index].amount);
                node.AddNode(componentNode);
            }
            return node;
        }
        #endregion

        #region Helpers
        double calculateRequiredAmount(double partMass, double resourceDensity, double rate)
        {
            return (partMass / resourceDensity) * rate;
        }
        #endregion
    }
}
