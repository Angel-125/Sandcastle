using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        const string kPrintResourceAssembled = "PRINT_RESOURCE_ASSEMBLED";
        const string kMinimumGravity = "minimumGravity";
        const string kMinimumPressure = "minimumPressure";
        const string kRemoveResources = "removeResources";
        const string kVariantIndex = "variantIndex";
        const string kPackedVolume = "packedVolume";
        const string kBlacklisted = "isBlacklisted";
        const string kMass = "mass";
        const string kUnpackedInfo = "UNPACKED_INFO";
        const string kUnpackedVolume = "unpackedVolume";
        const string kIsUnpacked = "isUnpacked";
        const double kDefaultMass = 0.1;
        const double kDefaultMassResourcePercent = 0.05;
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

        /// <summary>
        /// Determines whether or not the printer should remove the part's resources before placing the printed part in an inventory.
        /// </summary>
        public bool removeResources = true;

        /// <summary>
        /// Index of the part variant to use (if any).
        /// </summary>
        public int variantIndex = 0;

        /// <summary>
        /// Volume of the item being printed.
        /// </summary>
        public float packedVolume = 0;

        /// <summary>
        /// Flag indicating if the part is blacklisted or not. If blacklisted then it can't be printed by a shipwright printer.
        /// </summary>
        public bool isBlacklisted;

        /// <summary>
        /// Mass of the part including variant.
        /// </summary>
        public double mass;

        /// <summary>
        /// Volume of the part when unpacked.
        /// </summary>
        public double unpackedVolume = -1;

        /// <summary>
        /// Flag to indicate whether or not the part is unpacked.
        /// </summary>
        public bool isUnpacked;

        /// <summary>
        /// ID of the part.
        /// </summary>
        public uint flightId;

        /// <summary>
        /// Flag to wait for a support unit to complete the job.
        /// </summary>
        public bool waitForSupportCompletion;

        /// <summary>
        /// Flag to indicate whether or not to add the item to the inventory when printing has completed.
        /// This is used by printers that are supporting a lead Shipwright. Instead of storing the part, they hand it over to the lead Shipwright for inclusion in a vessel.
        /// </summary>
        public bool skipInventoryAdd;
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

            if (node.HasValue(kRemoveResources))
                bool.TryParse(node.GetValue(kRemoveResources), out removeResources);

            if (node.HasValue(kVariantIndex))
                int.TryParse(node.GetValue(kVariantIndex), out variantIndex);

            if (node.HasValue(kPackedVolume))
                float.TryParse(node.GetValue(kPackedVolume), out packedVolume);

            if (node.HasValue(kBlacklisted))
                bool.TryParse(node.GetValue(kBlacklisted), out isBeingRecycled);

            if (node.HasValue(kMass))
                double.TryParse(node.GetValue(kMass), out mass);

            if (node.HasValue("flightId"))
                uint.TryParse(node.GetValue("flightId"), out flightId);

            if (node.HasValue("waitForSupportCompletion"))
                bool.TryParse(node.GetValue("waitForSupportCompletion"), out waitForSupportCompletion);

            if (node.HasValue("skipInventoryAdd"))
                bool.TryParse(node.GetValue("skipInventoryAdd"), out skipInventoryAdd);

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

            if (node.HasNode(kUnpackedInfo))
            {
                ConfigNode unpackedNode = node.GetNode(kUnpackedInfo);

                if (unpackedNode.HasValue(kUnpackedVolume))
                {
                    double.TryParse(unpackedNode.GetValue(kUnpackedVolume), out unpackedVolume);
                }
            }

            if (node.HasValue(kIsUnpacked))
            {
                bool.TryParse(node.GetValue(kIsUnpacked), out isUnpacked);
            }

            if (node.HasValue(kUnpackedVolume))
            {
                double.TryParse(node.GetValue(kUnpackedVolume), out unpackedVolume);
            }

            if (!string.IsNullOrEmpty(partName))
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
            mass = getPrefabPartMass();

            // Get the materials list
            string categoryName = getCategoryName(availablePart.category, availablePart.tags);
            MaterialsList materialsList = MaterialsList.GetListForCategory(categoryName);
            ModuleResource[] resources = materialsList.materials.ToArray();
            ModuleResource resource;
            ConfigNode node;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            // Copy the required materials and tally the total units required.
            Dictionary<string, ModuleResource> buildMaterials = new Dictionary<string, ModuleResource>();
            for (int index = 0; index < resources.Length; index++)
            {
                resource = resources[index];
                if (definitions.Contains(resource.name))
                {
                    node = new ConfigNode(MaterialsList.kResourceNode);
                    resource.Save(node);

                    resource = new ModuleResource();
                    resource.Load(node);

                    if (!buildMaterials.ContainsKey(resource.name))
                        buildMaterials.Add(resource.name, resource);
                }
            }

            // If the part requires special resources, then add them too.
            if (availablePart.partConfig.HasNode(kPrintResource))
            {
                ConfigNode[] nodes = availablePart.partConfig.GetNodes(kPrintResource);
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    if (definitions.Contains(resource.name))
                    {
                        if (!buildMaterials.ContainsKey(resource.name))
                            buildMaterials.Add(resource.name, resource);
                        else
                            buildMaterials[resource.name].rate = resource.rate;
                    }
                }
            }

            if (availablePart.partConfig.HasNode(kUnpackedInfo))
            {
                ConfigNode unpackedNode = availablePart.partConfig.GetNode(kUnpackedInfo);

                if (unpackedNode.HasValue(kUnpackedVolume))
                {
                    double.TryParse(unpackedNode.GetValue(kUnpackedVolume), out unpackedVolume);
                }
            }

            // Setup the materials list
            materials = new List<ModuleResource>();
            materials.AddRange(buildMaterials.Values);

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
            UpdateResourceRequirements();

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

            // Also add any components required by the materials list.
            if (materialsList.requiredComponents.Count > 0)
                requiredComponents.AddRange(materialsList.requiredComponents);

            // Get minimum gravity requirements
            if (availablePart.partConfig.HasValue(kMinimumGravity))
                float.TryParse(availablePart.partConfig.GetValue(kMinimumGravity), out minimumGravity);

            // Minimum pressure requirements
            if (availablePart.partConfig.HasValue(kMinimumPressure))
                float.TryParse(availablePart.partConfig.GetValue(kMinimumPressure), out minimumPressure);

            // Resource Removal
            if (availablePart.partConfig.HasValue(kRemoveResources))
                bool.TryParse(availablePart.partConfig.GetValue(kRemoveResources), out removeResources);

            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart != null)
                packedVolume = cargoPart.packedVolume;

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
            removeResources = copyFrom.removeResources;
            variantIndex = copyFrom.variantIndex;
            isBlacklisted = copyFrom.isBlacklisted;
            mass = copyFrom.mass;
            unpackedVolume = copyFrom.unpackedVolume;
            isUnpacked = copyFrom.isUnpacked;
            flightId = copyFrom.flightId;
            waitForSupportCompletion = copyFrom.waitForSupportCompletion;
            skipInventoryAdd = skipInventoryAdd;

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

            if (!string.IsNullOrEmpty(partName))
                node.AddValue(kPartName, partName);
            node.AddValue(kTotalUnitsRequired, totalUnitsRequired.ToString());
            node.AddValue(kTotalUnitsPrinted, totalUnitsPrinted.ToString());
            node.AddValue(kRemoveResources, removeResources);
            node.AddValue(kVariantIndex, variantIndex);
            node.AddValue(kBlacklisted, isBlacklisted);
            node.AddValue(kMass, mass);
            node.AddValue(kIsUnpacked, isUnpacked);
            node.AddValue(kUnpackedVolume, unpackedVolume);
            node.AddValue("flightId", flightId);
            node.AddValue("waitForSupportCompletion", waitForSupportCompletion);
            node.AddValue("skipInventoryAdd", skipInventoryAdd);

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

        public void UpdateResourceRequirements()
        {
            ModuleResource resource;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;

            // Get the adjusted part mass.
            double partMass = getPrefabPartMass();
            double variantMass;
            if (availablePart.Variants != null && availablePart.Variants.Count > 0 && variantIndex >= 0 && variantIndex <= availablePart.Variants.Count - 1)
            {
                variantMass = availablePart.Variants[variantIndex].Mass;
                if (partMass + variantMass > 0)
                    partMass += variantMass;
                else if (SandcastleScenario.debugMode)
                    Debug.Log("[Sandcastle] - UpdateResourceRequirements: For part " + availablePart.name + ", skipping variant part mass adjustment, partMass would be < 0! Variant: " + availablePart.Variants[variantIndex].DisplayName + ", partMass: " + partMass + ", variant mass: " + variantMass + ", partMass + mass: " + partMass + variantMass);
            }

            // Now tally up the total units required and each indivdual resources required amounts.
            totalUnitsRequired = 0;
            int count = materials.Count;
            for (int index = 0; index < count; index++)
            {
                resource = materials[index];
                resourceDef = definitions[resource.name];
                if (SandcastleScenario.debugMode)
                    Debug.Log("[Sandcastle] - Calculating required amount of " + resource.name + " for " + availablePart.name);
                resource.amount = calculateRequiredAmount(partMass, resourceDef.density, resource.rate);
                totalUnitsRequired += resource.amount;
            }
        }

        public void UpdateResourceRequirements(Part part)
        {
            double partMass = getPrefabPartMass();

            // Check for variant mass
            ModulePartVariants partVariant = part.FindModuleImplementing<ModulePartVariants>();
            if (partVariant != null && partVariant.useVariantMass)
            {
                PartVariant selectedVariant = partVariant.SelectedVariant;
                double variantMass;
                if (selectedVariant != null)
                {
                    variantMass = selectedVariant.Mass;

                    if (partMass + variantMass > 0)
                        partMass += variantMass;

                    else if (SandcastleScenario.debugMode)
                        Debug.Log("[Sandcastle] - UpdateResourceRequirements: For part " + availablePart.name + ", skipping variant part mass adjustment, partMass would be < 0! Variant: " + selectedVariant.DisplayName + ", partMass: " + partMass + ", variant mass: " + variantMass + ", partMass + mass: " + partMass + variantMass);
                }
            }

            // Check for unpacked mass
            int count = part.Modules.Count;
            PartModule partModule = null;
            for (int index = 0; index < count; index++)
            {
                partModule = part.Modules[index];
                if (partModule.moduleName == "WBIPackingBox" || partModule.moduleName == "WBIMultipurposeHab" || partModule.moduleName == "WBIMultipurposeLab")
                {
                    BaseField field = partModule.Fields["isDeployed"];
                    if (field != null)
                    {
                        bool isDeployed = (bool)field.GetValue(partModule);
                        if (!isDeployed)
                            return;
                    }

                    field = partModule.Fields["partMass"];
                    if (field != null)
                    {
                        float deployedMass = (float)field.GetValue(partModule);
                        partMass += deployedMass;
                        isUnpacked = true;
                        break;
                    }
                }
            }

            // Now update the resources
            // We have the adjusted part mass. Now add any resources that are required to build the part when it's pre-assembled.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            ModuleResource resource;

            // Create our building materials dictionary
            Dictionary<string, ModuleResource> buildMaterials = new Dictionary<string, ModuleResource>();
            count = materials.Count;
            for (int index = 0; index < count; index++)
            {
                buildMaterials.Add(materials[index].name, materials[index]);
            }

            // If the part requires special resources that only apply when the part has been pre-assembled, then add them too.
            if (availablePart.partConfig.HasNode(kPrintResourceAssembled))
            {
                ConfigNode[] nodes = availablePart.partConfig.GetNodes(kPrintResourceAssembled);
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    if (definitions.Contains(resource.name))
                    {
                        if (!buildMaterials.ContainsKey(resource.name))
                            buildMaterials.Add(resource.name, resource);
                        else
                            buildMaterials[resource.name].rate = resource.rate;
                    }
                }
            }

            // Setup the materials list
            materials = new List<ModuleResource>();
            materials.AddRange(buildMaterials.Values);

            // Now update resoure mass
            updateResourceMasses(partMass);
            mass = partMass;
        }

        public void UpdateResourceRequirements(ConfigNode partNode)
        {
            double partMass = getPrefabPartMass();

            ConfigNode[] moduleNodes = partNode.GetNodes("MODULE");
            ConfigNode moduleNode;
            string moduleName;
            totalUnitsRequired = 0;
            for (int moduleIndex = 0; moduleIndex < moduleNodes.Length; moduleIndex++)
            {
                moduleNode = moduleNodes[moduleIndex];
                moduleName = moduleNode.GetValue("name");
                switch (moduleName) {
                    case "ModulePartVariants":
                        partMass += updateModulePartVariantsMass(moduleNode);
                        break;

                    case "WBIPackingBox":
                    case "WBIMultipurposeHab":
                    case "WBIMultipurposeLab":
                        partMass += updateAssembledPartMass(moduleNode);
                        break;

                    default:
                        break;
                }
            }

            updateResourceMasses(partMass);

            mass = partMass;
        }
        #endregion

        #region Helpers
        double getPrefabPartMass()
        {
            double partMass = availablePart.partPrefab.mass;

            // Check for negative mass. If this happens, then somebody didn't do their math, as it's usually a problem between dry mass and resource mass.
            if (partMass <= 0)
            {
                // If the prefab has no resources then return a default amount.
                if (availablePart.partPrefab.resourceMass <= 0)
                {
                    if (SandcastleScenario.debugMode)
                        Debug.Log("[Sandcastle] - part " + partName + " mass is <0! Seting to default: " + kDefaultMass);
                    return kDefaultMass;
                }
                else
                {
                    double resourcePercentMass = availablePart.partPrefab.resourceMass * kDefaultMassResourcePercent;
                    if (SandcastleScenario.debugMode)
                        Debug.Log("[Sandcastle] - part " + partName + " mass is <0! Seting to resource %: " + kDefaultMassResourcePercent + " new mass: " + resourcePercentMass);
                    return resourcePercentMass;
                }
            }

            return partMass;
        }

        double updateAssembledPartMass(ConfigNode moduleNode)
        {
            if (!moduleNode.HasValue("isDeployed"))
                return 0;
            bool isDeployed;
            bool.TryParse(moduleNode.GetValue("isDeployed"), out isDeployed);
            if (!isDeployed)
                return 0;

            if (!moduleNode.HasValue("partMass"))
                return 0;
            double partMass;
            double.TryParse(moduleNode.GetValue("partMass"), out partMass);

            // We have the adjusted part mass. Now add any resources that are required to build the part when it's pre-assembled.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            ModuleResource resource;

            // Create our building materials dictionary
            Dictionary<string, ModuleResource> buildMaterials = new Dictionary<string, ModuleResource>();
            int count = materials.Count;
            for (int index = 0; index < count; index++)
            {
                buildMaterials.Add(materials[index].name, materials[index]);
            }

            // If the part requires special resources that only apply when the part has been pre-assembled, then add them too.
            if (availablePart.partConfig.HasNode(kPrintResourceAssembled))
            {
                ConfigNode[] nodes = availablePart.partConfig.GetNodes(kPrintResourceAssembled);
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    if (definitions.Contains(resource.name))
                    {
                        if (!buildMaterials.ContainsKey(resource.name))
                            buildMaterials.Add(resource.name, resource);
                        else
                            buildMaterials[resource.name].rate = resource.rate;
                    }
                }
            }

            // Setup the materials list
            materials = new List<ModuleResource>();
            materials.AddRange(buildMaterials.Values);

            return partMass;
        }

        double updateModulePartVariantsMass(ConfigNode moduleNode)
        {
            if (availablePart.Variants == null || availablePart.Variants.Count <= 0)
                return 0;

            if (!moduleNode.HasValue("isEnabled"))
                return 0;
            bool enabled;
            bool.TryParse(moduleNode.GetValue("isEnabled"), out enabled);
            if (!enabled)
                return 0;

            if (!moduleNode.HasValue("useVariantMass"))
                return 0;
            bool useVariantMass;
            bool.TryParse(moduleNode.GetValue("useVariantMass"), out useVariantMass);
            if (!useVariantMass)
                return 0;

            if (!moduleNode.HasValue("selectedVariant"))
                return 0;
            string selectedVariant = moduleNode.GetValue("selectedVariant");

            int count = availablePart.Variants.Count;
            for (int index = 0; index < count; index++)
            {
                if (availablePart.Variants[index].Name == selectedVariant)
                {
                    variantIndex = index;
                    return availablePart.Variants[index].Mass;
                }
            }

            return 0;
        }

        void updateResourceMasses(double adjustedPartMass)
        {
            ModuleResource resource;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;

            // Now tally up the total units required and each indivdual resources required amounts.
            totalUnitsRequired = 0;
            int count = materials.Count;
            for (int index = 0; index < count; index++)
            {
                resource = materials[index];
                resourceDef = definitions[resource.name];
                if (SandcastleScenario.debugMode)
                    Debug.Log("[Sandcastle] - Calculating required amount of " + resource.name + " for " + availablePart.name);
                resource.amount = calculateRequiredAmount(adjustedPartMass, resourceDef.density, resource.rate);
                totalUnitsRequired += resource.amount;
            }
        }

        double calculateRequiredAmount(double partMass, double resourceDensity, double rate)
        {
            double requiredAmount = (partMass / resourceDensity) * rate;

            if (SandcastleScenario.debugMode)
            {
                Debug.Log("[Sandcastle] - partMass: " + partMass + ", resourceDensity: " + resourceDensity + ", rate: " + rate + ", requiredAmount: " + requiredAmount);
            }

            return requiredAmount;
        }

        string getCategoryName(PartCategories category, string tags)
        {
            // If we have a community category tag then use the first one found as the category.
            if (category == PartCategories.none)
            {
                string[] tagsArray = tags.Split(new char[] { ' ' });
                for (int index = 0; index < tagsArray.Length; index++)
                {
                    if (tagsArray.Contains("cck-"))
                        return tagsArray[index];
                }
            }

            return category.ToString();
        }
        #endregion
    }
}
