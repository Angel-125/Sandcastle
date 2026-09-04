using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace Sandcastle.Inventory
{
    /// <summary>
    /// An inventory helper class
    /// </summary>
    public class InventoryUtils
    {
        #region Constants
        const int kTextureSize = 64;
        const float kLandedSpawnClearance = 1f;
        #endregion

        #region Fields
        #endregion

        #region Housekeeping
        static Dictionary<string, Texture2D> thumbnails = null;
        #endregion

        #region API
        /// <summary>
        /// Gets an inventory with enough storage space and storage mass for the desired part.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="availablePart">The AvailablePart to check for space.</param>
        /// <returns>A ModuleInventoryPart if space can be found or null if not.</returns>
        public static ModuleInventoryPart GetInventoryWithCargoSpace(Vessel vessel, AvailablePart availablePart)
        {
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
                return null;

            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory;
            int count = inventories.Count;
            bool massRequirementMet = false;
            bool volRequirementMet = false;
            double partMass = availablePart.partPrefab.mass + availablePart.partPrefab.resourceMass;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];

                if (!inventory.isEnabled || inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
                    continue;

                // Check mass
                if (inventory.HasMassLimit)
                {
                    float massAvailable = inventory.massLimit - inventory.massCapacity;
                    if (massAvailable < partMass)
                        continue;
                    else
                        massRequirementMet = true;
                }
                else
                {
                    massRequirementMet = true;
                }

                // Check volume
                if (inventory.HasPackedVolumeLimit)
                {
                    float volumeAvailable = inventory.packedVolumeLimit - inventory.volumeCapacity;

                    if (volumeAvailable < cargoPart.packedVolume)
                        continue;
                    else
                        volRequirementMet = true;
                }
                else
                {
                    volRequirementMet = true;
                }

                // If we've met all requirements then we found an inventory that has enough space.
                if (massRequirementMet && volRequirementMet)
                    return inventory;

                // Reset for next inventory
                volRequirementMet = false;
                massRequirementMet = false;
            }

            // No space available.
            return null;
        }

        /// <summary>
        /// Returns a list of inventory parts that can be recycled.
        /// </summary>
        /// <param name="vessel">The Vessel to search for parts to recycle.</param>
        /// <returns>A List of AvailablePart objects.</returns>
        public static List<AvailablePart> GetPartsToRecycle(Vessel vessel)
        {
            List<AvailablePart> partsToRecycle = new List<AvailablePart>();
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory;
            int count = inventories.Count;
            StoredPart storedPart;
            int[] keys = null;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];
                if (inventory.InventoryIsEmpty)
                    continue;

                keys = inventory.storedParts.Keys.ToArray();
                for (int storedPartIndex = 0; storedPartIndex < keys.Length; storedPartIndex++)
                {
                    storedPart = inventory.storedParts[keys[storedPartIndex]];
                    for (int stackIndex = 0; stackIndex < storedPart.quantity; stackIndex++)
                    {
                        partsToRecycle.Add(PartLoader.getPartInfoByName(storedPart.partName));
                    }
                }
            }

            return partsToRecycle;
        }

        /// <summary>
        /// Determines whether or not the supplied inventory has space for the desired part.
        /// </summary>
        /// <param name="inventory">A ModuleInventoryPart to check for space.</param>
        /// <param name="availablePart">An AvailablePart to check to see if it fits.</param>
        /// <returns>true if the inventory has space for the part, false if not.</returns>
        public static bool InventoryHasSpace(ModuleInventoryPart inventory, AvailablePart availablePart)
        {
            if (inventory == null)
                return false;

            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
                return false;

            bool massRequirementMet = false;
            bool volRequirementMet = false;
            double partMass = availablePart.partPrefab.mass + availablePart.partPrefab.resourceMass;

            if (!inventory.isEnabled || inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
                return false;

            // Check mass
            if (inventory.HasMassLimit)
            {
                float massAvailable = inventory.massLimit - inventory.massCapacity;
                if (massAvailable < partMass)
                    return false;
                else
                    massRequirementMet = true;
            }
            else
            {
                massRequirementMet = true;
            }

            // Check volume
            if (inventory.HasPackedVolumeLimit)
            {
                float volumeAvailable = inventory.packedVolumeLimit - inventory.volumeCapacity;

                if (volumeAvailable < cargoPart.packedVolume)
                    return false;
                else
                    volRequirementMet = true;
            }
            else
            {
                volRequirementMet = true;
            }

            return massRequirementMet && volRequirementMet;
        }

        /// <summary>
        /// Determines whether or not the vessel has enough storage space.
        /// </summary>
        /// <param name="vessel">The vessel to query</param>
        /// <param name="availablePart">The AvailablePart to check for space.</param>
        /// <param name="amount">The number of parts that need space. Default is 1.</param>
        /// <param name="partMassOverride">Optional mass, in metric tons, to use instead of the part's configured mass.</param>
        /// <param name="volumeOverride">Optional packed volume, in liters, to use instead of the part's configured volume.</param>
        /// <returns>true if there is enough space, false if not.</returns>
        public static bool HasEnoughSpace(Vessel vessel, AvailablePart availablePart, int amount = 1, double partMassOverride = -1, float volumeOverride = -1)
        {
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
            {
                return false;
            }

            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory;
            int count = inventories.Count;
            bool massRequirementMet = false;
            bool volRequirementMet = false;

            double partMass = availablePart.partPrefab.mass + availablePart.partPrefab.resourceMass;
            if (partMassOverride > 0)
                partMass = partMassOverride;

            double totalMassNeeded = partMass * amount;
            float totalVolumeNeeded = cargoPart.packedVolume * amount;
            if (volumeOverride > 0)
                totalVolumeNeeded = volumeOverride * amount;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];

                if (!inventory.isEnabled || inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
                    continue;

                // Check mass
                if (inventory.HasMassLimit)
                {
                    float massAvailable = inventory.massLimit - inventory.massCapacity;
                    if (massAvailable < partMass)
                    {
                        continue;
                    }
                    else
                    {
                        totalMassNeeded -= partMass;
                        if (totalMassNeeded <= 0.00001)
                            massRequirementMet = true;
                    }
                }
                else
                {
                    massRequirementMet = true;
                }

                // Check volume
                if (inventory.HasPackedVolumeLimit)
                {
                    float volumeAvailable = inventory.packedVolumeLimit - inventory.volumeCapacity;
                    if (volumeAvailable < cargoPart.packedVolume)
                    {
                        continue;
                    }
                    else
                    {
                        totalVolumeNeeded -= cargoPart.packedVolume;
                        if (totalVolumeNeeded <= 0.00001)
                            volRequirementMet = true;
                    }
                }
                else
                {
                    volRequirementMet = true;
                }

                // If we've met all requirements then we found an inventory that has enough space.
                if (massRequirementMet && volRequirementMet)
                    return true;

                // Reset for next inventory
                volRequirementMet = false;
                massRequirementMet = false;
            }

            // No space available.
            return false;
        }

        /// <summary>
        /// Determines whether or not the vessel has the item in question.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="partName">The name of the part to look for</param>
        /// <returns>true if the vessel has the part, false if not.</returns>
        public static bool HasItem(Vessel vessel, string partName)
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            int count = inventories.Count;

            for (int index = 0; index < count; index++)
            {
                if (inventories[index].ContainsPart(partName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the number of parts in the vessel's inventory, if it has the part.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="partName">The name of the part to look for.</param>
        /// <returns>An Int containing the number of parts in the vessel's inventory.</returns>
        public static int GetInventoryItemCount(Vessel vessel, string partName)
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            int count = inventories.Count;
            int foundParts = 0;
            int storedParts = 0;

            for (int index = 0; index < count; index++)
            {
                storedParts = inventories[index].TotalAmountOfPartStored(partName);
                if (storedParts > 0)
                    foundParts += storedParts;
            }

            return foundParts;
        }

        /// <summary>
        /// Determines whether or not the vessel has the item in question.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="partName">The name of the part to look for</param>
        /// <returns>the ModuleInventoryPart if the vessel has the part, null if not.</returns>
        public static ModuleInventoryPart GetInventoryWithPart(Vessel vessel, string partName)
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            int count = inventories.Count;

            for (int index = 0; index < count; index++)
            {
                if (inventories[index].ContainsPart(partName))
                    return inventories[index];
            }

            return null;
        }

        /// <summary>
        /// Removes the item from the vessel if it exists.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="partName">The name of the part to remove.</param>
        /// <param name="partCount">The number parts to remove. Default is 1.</param>
        public static void RemoveItem(Vessel vessel, string partName, int partCount = 1)
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory = null;
            int count = inventories.Count;
            int storedPartsAmount = 0;
            int currentPartCount = partCount;
            int partsToRemove = 0;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];
                storedPartsAmount = inventory.TotalAmountOfPartStored(partName);
                if (storedPartsAmount > 0 && currentPartCount > 0)
                {
                    if (storedPartsAmount >= currentPartCount)
                    {
                        inventory.RemoveNPartsFromInventory(partName, currentPartCount);
                        return;
                    }
                    else
                    {
                        partsToRemove = storedPartsAmount;
                        currentPartCount -= storedPartsAmount;
                        inventory.RemoveNPartsFromInventory(partName, partsToRemove);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the item to the vessel inventory if there is enough room.
        /// </summary>
        /// <param name="vessel">The vessel to query.</param>
        /// <param name="availablePart">The part to add to the inventory</param>
        /// <param name="variantIndex">An int containing the index of the part variant to store.</param>
        /// <param name="preferredInventory">The preferred inventory to store the part in.</param>
        /// <param name="removeResources">A bool indicating whether or not to remove resources when storing the part. Default is true.</param>
        /// <returns>The Part that the item was stored in, or null if no place could be found for the part.</returns>
        public static Part AddItem(Vessel vessel, AvailablePart availablePart, int variantIndex, ModuleInventoryPart preferredInventory = null, bool removeResources = true)
        {
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
            {

                return null;
            }

            normalizePersistentStringFields(availablePart.partPrefab);

            PartVariant partVariant = null;
            PartVariant prevVariant = null;
            string variantName = string.Empty;
            ModulePartVariants moduleVariants = availablePart.partPrefab.FindModuleImplementing<ModulePartVariants>();
            if (availablePart.Variants != null && availablePart.Variants.Count > 0 && variantIndex >= 0 && variantIndex <= availablePart.Variants.Count - 1)
            {
                // Get part variant and the name of the variant that we want to use.
                partVariant = availablePart.Variants[variantIndex];
                variantName = partVariant.Name;

                // Record current variant and name
                prevVariant = availablePart.variant;

                // Set new variant for storage purposes
                availablePart.variant = partVariant;
                if (moduleVariants != null)
                    moduleVariants.SetVariant(variantName);
            }

            // Fix for science lab
            ModuleScienceLab lab = availablePart.partPrefab.FindModuleImplementing<ModuleScienceLab>();
            if (lab != null)
            {
                lab.ExperimentData = new List<string>();
            }

            ModuleInventoryPart inventory = null;
            if (InventoryHasSpace(preferredInventory, availablePart))
                inventory = preferredInventory;
            else
                inventory = GetInventoryWithCargoSpace(vessel, availablePart);
            if (inventory == null)
                return null;

            bool partAddedToInventory = false;
            int storedPartIndex = -1;
            bool canBeStacked = cargoPart.stackableQuantity > 1;
            bool inventoryContainsPart = inventory.ContainsPart(availablePart.name);
            StoredPart storedPart;
            bool addToEmptySpace = false;
            for (int index = 0; index < inventory.InventorySlots; index++)
            {
                // If the part can't be stacked then find an empty inventory slot.
                if (!canBeStacked && inventory.IsSlotEmpty(index))
                {
                    storedPartIndex = index;
                    partAddedToInventory = inventory.StoreCargoPartAtSlot(availablePart.partPrefab, storedPartIndex);
                    break;
                }

                // Part can be stacked. If the inventory doesn't contain the part, then find an empty slot and add it.
                else if (!inventoryContainsPart && inventory.IsSlotEmpty(index))
                {
                    storedPartIndex = index;
                    partAddedToInventory = inventory.StoreCargoPartAtSlot(availablePart.partPrefab, storedPartIndex);
                    break;
                }

                // Part can be stacked, but we need an empty slot to store it.
                else if (inventory.IsSlotEmpty(index) && addToEmptySpace)
                {
                    storedPartIndex = index;
                    partAddedToInventory = inventory.StoreCargoPartAtSlot(availablePart.partPrefab, storedPartIndex);
                    break;
                }

                // Inventory contains the part. Find the slot that it is in and add it there. If the stack is full then we need to find an empty slot.
                else if (inventory.storedParts[index].partName == availablePart.name)
                {
                    storedPartIndex = index;
                    storedPart = inventory.storedParts[index];
                    if (inventory.CanStackInSlot(availablePart, variantName, storedPartIndex))
                    {
                        partAddedToInventory = inventory.UpdateStackAmountAtSlot(index, storedPart.quantity + 1, variantName);
                        break;
                    }
                    else
                    {
                        addToEmptySpace = true;
                    }
                }
            }

            // Remove resources from the stored part
            if (partAddedToInventory)
            {
                storedPart = inventory.storedParts[storedPartIndex];
                UI_Grid grid = inventory.Fields["InventorySlots"].uiControlFlight as UI_Grid;
                if (grid != null && grid.pawInventory != null)
                {
                    List<EditorPartIcon> partIcons = grid.pawInventory.slotPartIcon;
                    EditorPartIcon partIcon = null;
                    for (int index = 0; index < partIcons.Count; index++)
                    {
                        if (partIcons[index].AvailPart == availablePart)
                        {
                            partIcon = partIcons[index];
                            break;
                        }
                    }
                    if (partIcon != null && partIcon.inventoryItemThumbnail != null && partIcon.inventoryItemThumbnail.texture == null)
                    {
                        Texture2D texture = GetTexture(availablePart.name, variantIndex);
                        partIcon.inventoryItemThumbnail.texture = texture;
                        partIcon.inventoryItemThumbnail.SetNativeSize();
                        MonoUtilities.RefreshContextWindows(inventory.part);
                    }
                }

                if (removeResources)
                {
                    int count = storedPart.snapshot.resources.Count;
                    for (int resourceIndex = 0; resourceIndex < count; resourceIndex++)
                    {
                        if (storedPart.snapshot.resources[resourceIndex].resourceName == "ElectricCharge" ||
                            storedPart.snapshot.resources[resourceIndex].resourceName == "Ablator")
                            continue;
                        storedPart.snapshot.resources[resourceIndex].amount = 0;
                    }
                }
            }

            // Cleanup
            if (prevVariant != null)
            {
                availablePart.variant = prevVariant;
                if (moduleVariants != null)
                    moduleVariants.SetVariant(prevVariant.Name);
            }

            // No place to store the part.
            return partAddedToInventory ? inventory.part : null;
        }

        /// <summary>
        /// Retrieves a list of parts that can be printed by the specified max print volume.
        /// </summary>
        /// <param name="maxPrintVolume">A float containing the max possible print volume.</param>
        /// <param name="maxPartDimensions">An optional string containing the max possible print dimensions.</param>
        /// <returns>A List of AvailablePart objects that can be printed.</returns>
        public static List<AvailablePart> GetPrintableParts(float maxPrintVolume, string maxPartDimensions = null)
        {
            List<AvailablePart> filteredParts = new List<AvailablePart>();
            Vector3 maxDimensions = Vector3.zero;
            Vector3 craftSize;
            List<Part> parts;
            Part part;

            if (!string.IsNullOrEmpty(maxPartDimensions))
                maxDimensions = KSPUtil.ParseVector3(maxPartDimensions);

            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableCargoParts();
            if (cargoParts != null && cargoParts.Count > 0)
            {
                int count = cargoParts.Count;
                ModuleCargoPart cargoPart;
                float maxPrintableVolume = maxPrintVolume > 0 ? maxPrintVolume : float.MaxValue;
                AvailablePart availablePart;
                for (int index = 0; index < count; index++)
                {
                    availablePart = cargoParts[index];
                    if (availablePart.partPrefab == null)
                        continue;
                    cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();

                    // Check volume and dimensions
                    if (cargoPart.packedVolume > 0 && cargoPart.packedVolume <= maxPrintableVolume)
                    {
                        // Check dimensions
                        if (maxDimensions != Vector3.zero)
                        {
                            // Calculate craft size so we don't smack into the printer when we drop the part.
                            part = availablePart.partPrefab;
                            parts = new List<Part>();
                            parts.Add(part);

                            craftSize = ShipConstruction.CalculateCraftSize(parts, part);
                            if (craftSize.x > maxDimensions.x || craftSize.y > maxDimensions.y || craftSize.z > maxDimensions.z)
                                continue;
                        }

                        // Check tech hidden
                        if (availablePart.TechHidden == false || canPrintHiddenPart(availablePart))
                        {
                            // For some reason, flat-packed and boxed Pathfinder parts list a negative prefab mass. We need to fix that.
                            if (availablePart.partPrefab.mass < 0 && availablePart.partConfig != null && availablePart.partConfig.HasValue("mass"))
                            {
                                float.TryParse(availablePart.partConfig.GetValue("mass"), out availablePart.partPrefab.mass);
                            }
                            filteredParts.Add(availablePart);
                        }
                    }
                }
            }

            return filteredParts;
        }

        /// <summary>
        /// Retrieves parts that can be printed directly into the world. Unlike
        /// <see cref="GetPrintableParts"/>, this includes cargo parts whose
        /// packed volume is negative because world-spawned parts do not need to
        /// fit in a stock inventory.
        /// </summary>
        /// <param name="maxPrintVolume">
        /// Maximum bounding-box volume in liters, or a non-positive value for
        /// no volume limit.
        /// </param>
        /// <param name="maxPartDimensions">
        /// Optional maximum part dimensions in meters.
        /// </param>
        /// <returns>A list of parts eligible for direct world spawning.</returns>
        public static List<AvailablePart> GetWorldSpawnPrintableParts(
            float maxPrintVolume, string maxPartDimensions = null)
        {
            List<AvailablePart> filteredParts = new List<AvailablePart>();
            Vector3 maxDimensions = Vector3.zero;
            if (!string.IsNullOrEmpty(maxPartDimensions))
                maxDimensions = KSPUtil.ParseVector3(maxPartDimensions);

            bool checkDimensions = maxDimensions != Vector3.zero;
            bool checkVolume = maxPrintVolume > 0f;
            List<AvailablePart> cargoParts =
                PartLoader.Instance.GetAvailableCargoParts();
            if (cargoParts == null)
                return filteredParts;

            for (int index = 0; index < cargoParts.Count; index++)
            {
                AvailablePart availablePart = cargoParts[index];
                if (availablePart == null || availablePart.partPrefab == null)
                    continue;

                ModuleCargoPart cargoPart = availablePart.partPrefab
                    .FindModuleImplementing<ModuleCargoPart>();
                if (cargoPart == null || cargoPart.packedVolume == 0f)
                    continue;

                if (checkDimensions || checkVolume)
                {
                    Bounds partBounds;
                    if (!TryGetPartBounds(availablePart, out partBounds))
                    {
                        if (SandcastleScenario.debugMode)
                            Debug.LogWarning("[Sandcastle] - Cannot evaluate world-spawn limits for "
                                + availablePart.name + ": model bounds are unavailable.");
                        continue;
                    }

                    Vector3 dimensions = partBounds.size;
                    if (checkDimensions &&
                        (dimensions.x > maxDimensions.x ||
                        dimensions.y > maxDimensions.y ||
                        dimensions.z > maxDimensions.z))
                    {
                        continue;
                    }

                    // Unity/KSP model dimensions are meters. Convert the
                    // bounding-box volume from cubic meters to liters so it is
                    // comparable to maxPrintVolume and ModuleCargoPart volume.
                    float partVolume = dimensions.x * dimensions.y *
                        dimensions.z * 1000f;
                    if (checkVolume && partVolume > maxPrintVolume)
                        continue;
                }

                if (availablePart.TechHidden && !canPrintHiddenPart(availablePart))
                    continue;

                // Some legacy boxed parts expose a transient negative prefab
                // mass; restore the configured dry mass before creating jobs.
                if (availablePart.partPrefab.mass < 0f &&
                    availablePart.partConfig != null &&
                    availablePart.partConfig.HasValue("mass"))
                {
                    float.TryParse(availablePart.partConfig.GetValue("mass"),
                        out availablePart.partPrefab.mass);
                }

                filteredParts.Add(availablePart);
            }

            return filteredParts;
        }

        /// <summary>
        /// Calculates a part prefab's active model bounds in part-local space.
        /// </summary>
        /// <param name="availablePart">The part definition to measure.</param>
        /// <param name="partBounds">The calculated local bounds.</param>
        /// <returns>True when at least one active model mesh was measured.</returns>
        public static bool TryGetPartBounds(AvailablePart availablePart,
            out Bounds partBounds)
        {
            partBounds = new Bounds();
            if (availablePart == null || availablePart.partPrefab == null)
                return false;

            Part prefab = availablePart.partPrefab;
            return TryGetPartLocalBounds(prefab, prefab.transform,
                out partBounds);
        }

        /// <summary>
        /// Retrieves the thumbnail texture that depicts the specified part name.
        /// </summary>
        /// <param name="partName">A string containing the name of the part.</param>
        /// <returns>A Texture2D if the texture exists, or a blank texture if not.</returns>
        public static Texture2D GetTexture(string partName)
        {
            Texture2D texture = GetTexture(partName, 0);

            return texture;
        }

        /// <summary>
        /// Retrieves the thumbnail texture that depicts the specified part name.
        /// </summary>
        /// <param name="partName">A string containing the name of the part.</param>
        /// <param name="variantIndex">An int containing the index of the desired part variant image.</param>
        /// <returns>A Texture2D if the texture exists, or a blank texture if not.</returns>
        public static Texture2D GetTexture(string partName, int variantIndex)
        {
            if (thumbnails == null)
                thumbnails = new Dictionary<string, Texture2D>();

            AvailablePart availablePart = PartLoader.getPartInfoByName(partName);
            if (availablePart == null)
                return null;

            string partVariantName = partName + "_icon" + variantIndex.ToString();
            if (availablePart.Variants == null || availablePart.Variants.Count == 0)
                partVariantName = partName;

            if (!thumbnails.ContainsKey(partVariantName))
            {
                Texture2D texture = new Texture2D(kTextureSize, kTextureSize, TextureFormat.RGBA32, false);
                string gameDataPath = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
                string filePath = Path.Combine(gameDataPath, GetFilePathForThumbnail(availablePart, variantIndex));
                string altFilePath = Path.Combine(gameDataPath, GetFilePathForThumbnail(availablePart, variantIndex, true));

                // If we can find the thumbnail file then load it and add it to the thumbnails map.
                if (File.Exists(filePath))
                {
                    texture.LoadImage(File.ReadAllBytes(filePath));
                    thumbnails.Add(partVariantName, texture);
                }
                else if (File.Exists(altFilePath))
                {
                    texture.LoadImage(File.ReadAllBytes(altFilePath));
                    thumbnails.Add(partVariantName, texture);
                }

                // Use the default image.
                else
                {
                    Texture2D snapshot = GameDatabase.Instance.GetTexture("WildBlueIndustries/Sandcastle/Icons/Box", false);
                    thumbnails.Add(partVariantName, snapshot);
                }
            }

            return thumbnails[partVariantName];
        }

        /// <summary>
        /// Returns the full path to the part's thumbnail image.
        /// </summary>
        /// <param name="availablePart">An AvailablePart to check for images.</param>
        /// <param name="variantIndex">An int containing the variant index to check for. Default is -1.</param>
        /// <param name="useDefaultPath">A bool indicating whether or not to use the default thumbnails path.</param>
        /// <returns></returns>
        public static string GetFilePathForThumbnail(AvailablePart availablePart, int variantIndex = -1, bool useDefaultPath = false)
        {
            if (availablePart == null)
                return string.Empty;

            ModulePartVariants partVariants = availablePart.partPrefab.FindModuleImplementing<ModulePartVariants>();
            string variantId = (partVariants != null && variantIndex >= 0) ? variantIndex.ToString() : "";

            string filePath;
            if (availablePart.partUrl.LastIndexOf("Parts/") > 0 && !useDefaultPath)
                filePath = availablePart.partUrl.Substring(0, availablePart.partUrl.LastIndexOf("Parts/") + 6) + "@thumbs/" + availablePart.name + "_icon" + variantId;
            else
                filePath = KSPUtil.ApplicationRootPath + "@thumbs/Parts/" + availablePart.name + "_icon" + variantId;

            filePath += ".png";

            return filePath;
        }

        public static Texture2D TakeSnapshot(AvailablePart availablePart, int variantIndex = -1)
        {
            ProtoPartSnapshot protoPart = availablePart.partPrefab.protoPartSnapshot;
            string partName = availablePart.name;

            // Snapshots go in the default folder.
            string snapshotPath = KSPUtil.ApplicationRootPath + "@thumbs/Parts/"; ;
            if (availablePart.partUrl.LastIndexOf("Parts/") > 0)
                snapshotPath = availablePart.partUrl.Substring(0, availablePart.partUrl.LastIndexOf("Parts/") + 6) + "@thumbs/";
            string gameDataPath = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
            snapshotPath = Path.Combine(gameDataPath, snapshotPath);
            Debug.Log("[Sandcastle] - Trying to save a thumbnale for " + partName + " at location " + snapshotPath);
            string fullFileName = "";

            // Setup camera
            int resolution = 256;
            float elevation = 15f;
            float azimuth = 25f;
            float pitch = 15f;
            float hdg = 25f;
            float fovFactor = 18f;
            GameObject goSnapshotCamera = new GameObject("SnapshotCamera");
            Camera snapshotCamera = goSnapshotCamera.AddComponent<Camera>();
            float camFov = 30f;
            float camDist = 0.0f;
            snapshotCamera.clearFlags = CameraClearFlags.Color;
            snapshotCamera.backgroundColor = Color.clear;
            snapshotCamera.fieldOfView = camFov;
            snapshotCamera.cullingMask = 1;
            snapshotCamera.enabled = false;
            snapshotCamera.orthographic = true;
            snapshotCamera.orthographicSize = 0.75f;
            snapshotCamera.allowHDR = false;

            Light light = goSnapshotCamera.AddComponent<Light>();
            light.renderingLayerMask = 1;
            light.type = LightType.Spot;
            light.range = 100f;
            light.intensity = 1.25f;

            GameObject goIconPrefab = UnityEngine.Object.Instantiate<GameObject>(availablePart.iconPrefab);
            goIconPrefab.SetActive(true);

            // Setup variant, if any
            Material[] materialArray = EditorPartIcon.CreateMaterialArray(goIconPrefab, true);
            if (variantIndex > -1)
                ModulePartVariants.ApplyVariant(null, goIconPrefab.transform, availablePart.Variants[variantIndex], materialArray, false, variantIndex);
            IThumbnailSetup thumbNailSetupIface = CraftThumbnail.GetThumbNailSetupIface(availablePart);
            int length = materialArray.Length;
            while (length-- > 0)
            {
                if (!materialArray[length].shader.name.Contains("ScreenSpaceMask"))
                {
                    if (materialArray[length].shader.name == "KSP/Bumped Specular (Mapped)")
                        materialArray[length].shader = Shader.Find("KSP/ScreenSpaceMaskSpecular");
                    else if (materialArray[length].shader.name.Contains("Bumped"))
                        materialArray[length].shader = Shader.Find("KSP/ScreenSpaceMaskBumped");
                    else if (materialArray[length].shader.name.Contains("KSP/Alpha/CutoffBackground"))
                        materialArray[length].shader = Shader.Find("KSP/ScreenSpaceMaskAlphaCutoffBackground");
                    else if (materialArray[length].shader.name == "KSP/Unlit")
                        materialArray[length].shader = Shader.Find("KSP/ScreenSpaceMaskUnlit");
                    else
                        materialArray[length].shader = Shader.Find("KSP/ScreenSpaceMask");
                }
                materialArray[length].enableInstancing = false;
            }

            if (thumbNailSetupIface != null)
                thumbNailSetupIface.AssumeSnapshotPosition(goIconPrefab, protoPart);
            Vector3 size = PartGeometryUtil.MergeBounds(PartGeometryUtil.GetPartRendererBounds(availablePart.partPrefab), availablePart.partPrefab.transform.root).size;
            camDist = KSPCameraUtil.GetDistanceToFit(Mathf.Max(Mathf.Max(size.x, size.y), size.z), camFov * fovFactor, resolution);
            snapshotCamera.transform.position = Quaternion.AngleAxis(azimuth, Vector3.up) * Quaternion.AngleAxis(elevation, Vector3.right) * (Vector3.back * camDist);
            snapshotCamera.transform.rotation = Quaternion.AngleAxis(hdg, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            goIconPrefab.transform.SetParent(snapshotCamera.transform);
            snapshotCamera.transform.Translate(0.0f, -1000f, -250f);

            // Render the image
            Texture2D thumbTexture = renderCamera(snapshotCamera, resolution, resolution, 24, RenderTextureReadWrite.Default);
            byte[] png = thumbTexture.EncodeToPNG();
            string variantId = "";
            if (variantIndex > -1)
                variantId = variantIndex.ToString();
            if (!Directory.Exists(snapshotPath))
                Directory.CreateDirectory(snapshotPath);
            fullFileName = snapshotPath + availablePart.name + "_icon" + variantId;
            try
            {
                File.WriteAllBytes(fullFileName + ".png", png);
            }
            catch (Exception ex)
            {
                Debug.LogError(("[Sandcastle]: Error writing thumbnail: " + fullFileName + " Message: " + ex));
            }

            // Cleanup
            UnityEngine.Object.DestroyImmediate(goSnapshotCamera);
            UnityEngine.Object.DestroyImmediate(goIconPrefab);
            return thumbTexture;
        }

        /// <summary>
        /// Drops a completed print onto the ground using KSP's stock EVA construction
        /// proto-vessel path. This API intentionally does not support orbital spawning;
        /// use <see cref="SpawnOrbitalPart"/> for that case.
        /// </summary>
        /// <param name="availablePart">The part definition to place into the world.</param>
        /// <param name="parentPart">The printer part whose vessel supplies the spawn environment.</param>
        /// <param name="dropTransform">The printer transform that defines position and orientation.</param>
        /// <param name="repositionPart">Whether to move the print beyond the spawn boundary.</param>
        /// <returns>True if the ground-drop request was accepted.</returns>
        public static bool SpawnGroundPart(AvailablePart availablePart, Part parentPart,
            Transform dropTransform, bool repositionPart)
        {
            if (availablePart == null || availablePart.partPrefab == null ||
                parentPart == null || parentPart.vessel == null ||
                dropTransform == null || !parentPart.vessel.LandedOrSplashed)
            {
                Debug.LogWarning("[Sandcastle] - Cannot drop printed part: ground-spawn data is invalid or the printer is not landed or splashed.");
                return false;
            }

            Part part = availablePart.partPrefab;
            Vector3 dropPoint = dropTransform.position;
            Bounds localBounds;
            bool hasLocalBounds = TryGetPartLocalBounds(part, part.transform,
                out localBounds);
            float boundaryOffset = 0f;

            if (repositionPart && hasLocalBounds)
            {
                // LaunchPos local +Z points away from the print head. Move
                // the part only far enough that none of its geometry crosses
                // the virtual boundary at the transform origin.
                boundaryOffset = Mathf.Max(0f, -localBounds.min.z);
                dropPoint += dropTransform.forward * boundaryOffset;
            }

            if (!hasLocalBounds)
            {
                Debug.LogWarning("[Sandcastle] - Unable to calculate bounds for "
                    + availablePart.name + "; spawning at the LaunchPos origin.");
            }
            else
            {
                movePartAboveTerrain(ref dropPoint, dropTransform.rotation,
                    localBounds, parentPart.vessel.mainBody);
            }

            Quaternion dropRotation = Quaternion.Inverse(parentPart.vessel.mainBody.bodyTransform.rotation) * dropTransform.rotation;

            if (SandcastleScenario.debugMode)
            {
                Debug.Log("[Sandcastle] - SpawnGroundPart placement diagnostics for "
                    + availablePart.name
                    + "\n  parent vessel: " + parentPart.vessel.vesselName
                    + " (" + parentPart.vessel.id + "), situation: "
                    + parentPart.vessel.situation
                    + "\n  LaunchPos world position: "
                    + dropTransform.position.ToString("F4")
                    + ", parent-relative position: "
                    + parentPart.transform.InverseTransformPoint(
                        dropTransform.position).ToString("F4")
                    + "\n  LaunchPos forward/up/right: "
                    + dropTransform.forward.ToString("F4") + " / "
                    + dropTransform.up.ToString("F4") + " / "
                    + dropTransform.right.ToString("F4")
                    + "\n  LaunchPos world rotation: "
                    + dropTransform.rotation.eulerAngles.ToString("F4")
                    + ", body-relative rotation: "
                    + dropRotation.eulerAngles.ToString("F4")
                    + "\n  part local bounds center/extents/min/max: "
                    + (hasLocalBounds
                        ? localBounds.center.ToString("F4") + " / "
                            + localBounds.extents.ToString("F4") + " / "
                            + localBounds.min.ToString("F4") + " / "
                            + localBounds.max.ToString("F4")
                        : "<unavailable>")
                    + "\n  reposition: " + repositionPart
                    + ", boundary offset: " + boundaryOffset.ToString("F4")
                    + "\n  final drop point: " + dropPoint.ToString("F4"));
            }

            // The stock proto-vessel helper remains appropriate for landed and
            // splashed drops, where its latitude/longitude placement is stable.
            ConfigNode node = EVAConstructionModeController.Instance.evaEditor.GetProtoVesselNode(availablePart.title, dropPoint, dropRotation, parentPart.vessel, part);
            // This direct application keeps printer spawning correct even if another mod
            // changes the stock GetProtoVesselNode Harmony patch ordering.
            global::Sandcastle.UnderwaterSpawnUtils.ApplyToProtoVessel(
                node, parentPart.vessel, "WBIPrintShop");
            if (SandcastleScenario.debugMode)
            {
                ConfigNode orbitNode = node.GetNode("ORBIT");
                Debug.Log("[Sandcastle] - SpawnGroundPart proto-vessel diagnostics for "
                    + availablePart.name
                    + "\n  situation/landed/lat/lon/alt: "
                    + node.GetValue("sit") + " / " + node.GetValue("landed")
                    + " / " + node.GetValue("lat") + " / "
                    + node.GetValue("lon") + " / " + node.GetValue("alt")
                    + "\n  rotation: " + node.GetValue("rot")
                    + "\n  orbit: "
                    + (orbitNode != null
                        ? orbitNode.ToString()
                        : "<none>"));
            }

            ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(node);
            for (int index = 0; index < FlightGlobals.VesselsUnloaded.Count; ++index)
            {
                if (protoVessel.persistentId == FlightGlobals.VesselsUnloaded[index].persistentId)
                {
                    Vessel unloadedVessel = FlightGlobals.VesselsUnloaded[index];
                    unloadedVessel.SetPhysicsHoldExpiryOverride();
                    unloadedVessel.ignoreCollisionsFrames = 60;
                    clearResources(unloadedVessel);
                    if (SandcastleScenario.debugMode)
                    {
                        Debug.Log("[Sandcastle] - SpawnPart created vessel "
                            + unloadedVessel.vesselName + " ("
                            + unloadedVessel.id + ")"
                            + "\n  situation: " + unloadedVessel.situation
                            + ", world position: "
                            + unloadedVessel.transform.position.ToString("F4")
                            + "\n  orbit position/velocity: "
                            + unloadedVessel.orbit.pos.ToString("F4") + " / "
                            + unloadedVessel.orbit.vel.ToString("F4"));
                    }
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Spawns a one-part vessel in a stable orbit and couples it to its printer.
        /// The part is wrapped in a <see cref="ShipConstruct"/> so it uses the same
        /// launch, orbit synchronization, and coupling path as <c>WBIShipwright</c>.
        /// </summary>
        /// <param name="availablePart">The part definition to place into the world.</param>
        /// <param name="variantIndex">The selected part variant index.</param>
        /// <param name="parentPart">The printer part whose vessel supplies the orbit.</param>
        /// <param name="dropTransform">The printer transform that defines position and orientation.</param>
        /// <param name="repositionPart">Whether to move the print beyond the spawn boundary.</param>
        /// <param name="removeResources">Whether printable resources should be emptied.</param>
        /// <param name="onPartCoupled">Callback invoked after the part is coupled.</param>
        /// <returns>True if orbital construction and spawning started.</returns>
        public static bool SpawnOrbitalPart(AvailablePart availablePart,
            int variantIndex, Part parentPart, Transform dropTransform,
            bool repositionPart, bool removeResources,
            Callback<DockedVesselInfo> onPartCoupled)
        {
            if (availablePart == null || availablePart.partPrefab == null ||
                parentPart == null || parentPart.vessel == null ||
                parentPart.vessel.situation != Vessel.Situations.ORBITING ||
                dropTransform == null || onPartCoupled == null)
            {
                Debug.LogWarning("[Sandcastle] - Cannot spawn orbital printed part: spawn data is invalid or the printer is not orbiting.");
                return false;
            }

            ShipConstruct shipConstruct = CreateSinglePartConstruct(
                availablePart, variantIndex);
            if (shipConstruct == null || shipConstruct.parts == null ||
                shipConstruct.parts.Count == 0)
            {
                Debug.LogError("[Sandcastle] - Unable to create a one-part ShipConstruct for "
                    + availablePart.name + ".");
                return false;
            }

            Part rootPart = shipConstruct.parts[0].localRoot;
            Bounds localBounds;
            Vector3 relativePosition = Vector3.zero;
            if (repositionPart && TryGetPartLocalBounds(rootPart,
                rootPart.transform, out localBounds))
            {
                relativePosition.z = Mathf.Max(0f, -localBounds.min.z);
            }

            if (SandcastleScenario.debugMode)
                Debug.Log("[Sandcastle] - Spawning one-part ShipConstruct "
                    + availablePart.name + " at LaunchPos-relative position "
                    + relativePosition.ToString("F4"));

            SpawnShip(shipConstruct, parentPart, dropTransform, onPartCoupled,
                removeResources, repositionPart, true, relativePosition,
                Quaternion.identity, VesselType.DroppedPart);
            return true;
        }

        /// <summary>
        /// Calculates the bounds of a part in the coordinate system of the
        /// supplied reference transform.
        /// </summary>
        static bool TryGetPartLocalBounds(Part part, Transform referenceTransform,
            out Bounds localBounds)
        {
            localBounds = new Bounds();
            bool boundsInitialized = false;

            if (part == null || referenceTransform == null)
                return false;

            Matrix4x4 worldToReference = referenceTransform.worldToLocalMatrix;
            foreach (Transform modelTransform in part.FindModelComponents<Transform>())
            {
                if (!modelTransform.gameObject.activeSelf)
                    continue;

                MeshRenderer renderer = modelTransform.GetComponent<MeshRenderer>();
                if (renderer != null && !renderer.enabled)
                    continue;

                MeshFilter meshFilter = modelTransform.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                Matrix4x4 meshToReference = worldToReference *
                    modelTransform.localToWorldMatrix;
                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    Vector3 point = meshToReference.MultiplyPoint3x4(vertices[vertexIndex]);
                    if (!boundsInitialized)
                    {
                        localBounds = new Bounds(point, Vector3.zero);
                        boundsInitialized = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(point);
                    }
                }
            }

            return boundsInitialized;
        }

        /// <summary>
        /// Raises a prospective part placement until its lowest bounds point is
        /// one meter above the local terrain.
        /// </summary>
        static void movePartAboveTerrain(ref Vector3 dropPoint, Quaternion partRotation,
            Bounds localBounds, CelestialBody body)
        {
            if (body == null)
                return;

            Vector3 upAxis = ((Vector3d)dropPoint - body.position).normalized;
            if (upAxis == Vector3.zero)
                return;

            double latitude = body.GetLatitude(dropPoint);
            double longitude = body.GetLongitude(dropPoint);
            double terrainAltitude = body.TerrainAltitude(latitude, longitude);
            Vector3 terrainPoint = body.GetWorldSurfacePosition(
                latitude, longitude, terrainAltitude);

            Vector3 localUp = Quaternion.Inverse(partRotation) * upAxis;
            Vector3 extents = localBounds.extents;
            float supportRadius =
                Mathf.Abs(localUp.x) * extents.x +
                Mathf.Abs(localUp.y) * extents.y +
                Mathf.Abs(localUp.z) * extents.z;
            float lowestPointOffset =
                Vector3.Dot(partRotation * localBounds.center, upAxis) -
                supportRadius;
            float currentClearance =
                Vector3.Dot(dropPoint - terrainPoint, upAxis) +
                lowestPointOffset;
            float terrainOffset = kLandedSpawnClearance - currentClearance;
            if (terrainOffset <= 0f)
                return;

            dropPoint += upAxis * terrainOffset;
            if (SandcastleScenario.debugMode)
                Debug.Log("[Sandcastle] - Raised printed part by " + terrainOffset
                    + "m for terrain clearance.");
        }

        public static void SpawnShip(ShipConstruct shipConstruct, Part parentPart, Transform dropTransform,
            Callback<DockedVesselInfo> onVesselCoupled, bool removeResources = true,
            bool repositionCraftBeforeSpawning = true, bool useProvidedPlacement = false,
            Vector3 providedRelativePosition = default(Vector3),
            Quaternion providedRelativeRotation = default(Quaternion),
            VesselType spawnedVesselType = VesselType.Probe)
        {
            Debug.Log("[Sandcastle] - SpawnShip called for " + shipConstruct.shipName);
            shipConstruct.missionFlag = parentPart.flagURL;

            Part rootPart = shipConstruct.parts[0].localRoot;

            // Setup launch clamps
            setupLaunchClamps(shipConstruct);

            if (useProvidedPlacement)
            {
                rootPart.transform.rotation = dropTransform.rotation * providedRelativeRotation;
                rootPart.transform.position = dropTransform.TransformPoint(providedRelativePosition);
            }
            else if (!parentPart.vessel.LandedOrSplashed)
            {
                Bounds craftBounds;
                if (!TryPositionShipConstruct(shipConstruct, parentPart, dropTransform,
                    repositionCraftBeforeSpawning, out providedRelativePosition,
                    out providedRelativeRotation, out craftBounds))
                {
                    Debug.LogError("[Sandcastle] - Unable to calculate a boundary-safe placement for "
                        + shipConstruct.shipName + ". Vessel spawn aborted.");
                    return;
                }
            }
            else
            {
                Bounds craftBounds;
                if (!TryPositionLandedShipConstruct(shipConstruct, parentPart, dropTransform,
                    repositionCraftBeforeSpawning, out providedRelativePosition,
                    out providedRelativeRotation, out craftBounds))
                {
                    Debug.LogError("[Sandcastle] - Unable to calculate landed placement for "
                        + shipConstruct.shipName + ". Vessel spawn aborted.");
                    return;
                }
            }

            // Preserve the craft's placement relative to the printer. KSP can move
            // the flight scene's reference frame while the new vessel initializes,
            // so world-space coordinates captured here cannot be treated as stable.
            Vector3 relativePosition = dropTransform.InverseTransformPoint(rootPart.transform.position);
            Quaternion relativeRotation = Quaternion.Inverse(dropTransform.rotation) * rootPart.transform.rotation;

            // Spawn the vessel into the game.
            ShipConstruction.AssembleForLaunch(shipConstruct, "", "", parentPart.flagURL, FlightDriver.FlightStateCache, new VesselCrewManifest());
            Vessel vessel = shipConstruct.parts[0].localRoot.GetComponent<Vessel>();
            vessel.launchedFrom = parentPart.vessel.launchedFrom;
            vessel.vesselType = spawnedVesselType;
            vessel.ignoreCollisionsFrames = 60;

            // Update highlighters
            rootPart.highlighter.UpdateHighlighting(true);
            parentPart.highlighter.UpdateHighlighting(true);

            // Now update orbit.
            FlightGlobals.ForceSetActiveVessel(vessel);
            setCraftOrbit(vessel, OrbitDriver.UpdateMode.IDLE, parentPart);

            // Clear resources
            if (removeResources)
                clearResources(vessel);

            // Set the situation to match the dispenser part's parent vessel.
            vessel.situation = parentPart.vessel.situation;
            Debug.Log("[Sandcastle] - crafVessel.situation: " + vessel.situation);

            // We're landed, check for ground collisions and such
            if (parentPart.vessel.LandedOrSplashed)
            {
                // Keep the craft at the previewed ground placement until all parts
                // initialize, then explicitly finalize its landed state.
                parentPart.StartCoroutine(finalizeLandedVessel(vessel, parentPart,
                    dropTransform, relativePosition, relativeRotation));
            }

            // We're flying, orbiting, suborbital, or escaping. Couple the new craft to the printer.
            else
            {
                // Keep the vessel anchored to the live spawn transform while its
                // parts initialize, then couple it to the printer.
                parentPart.StartCoroutine(coupleVessel(vessel, parentPart, onVesselCoupled,
                    dropTransform, relativePosition, relativeRotation));
            }

            // Go for launch!
            StageManager.BeginFlight();
        }

        /// <summary>
        /// Creates an independent one-part construct from a part-loader prefab.
        /// Saving and reloading the temporary construct clones the prefab into the
        /// initialized form expected by <see cref="ShipConstruction.AssembleForLaunch"/>.
        /// </summary>
        /// <param name="availablePart">The part definition to clone.</param>
        /// <param name="variantIndex">The selected variant index.</param>
        /// <returns>A launchable one-part construct, or null on failure.</returns>
        internal static ShipConstruct CreateSinglePartConstruct(
            AvailablePart availablePart, int variantIndex = 0)
        {
            if (availablePart == null || availablePart.partPrefab == null)
                return null;

            normalizePersistentStringFields(availablePart.partPrefab);

            ShipConstruct temporaryConstruct = new ShipConstruct(
                availablePart.title, "Sandcastle printed part",
                availablePart.partPrefab);
            Part temporaryRoot = temporaryConstruct.parts[0];
            Quaternion prefabRotation = temporaryRoot.transform.rotation;
            ConfigNode constructNode;
            try
            {
                temporaryRoot.transform.rotation = Quaternion.identity;
                constructNode = temporaryConstruct.SaveShip();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Sandcastle] - Unable to serialize a one-part construct for "
                    + availablePart.name + ": " + ex);
                return null;
            }
            finally
            {
                temporaryRoot.transform.rotation = prefabRotation;
            }

            ShipConstruct shipConstruct = new ShipConstruct();
            try
            {
                if (!shipConstruct.LoadShip(constructNode) ||
                    shipConstruct.parts == null || shipConstruct.parts.Count == 0)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Sandcastle] - Unable to load a one-part construct for "
                    + availablePart.name + ": " + ex);
                return null;
            }

            if (availablePart.Variants != null &&
                variantIndex >= 0 && variantIndex < availablePart.Variants.Count)
            {
                ModulePartVariants variants = shipConstruct.parts[0]
                    .FindModuleImplementing<ModulePartVariants>();
                if (variants != null)
                    variants.SetVariant(availablePart.Variants[variantIndex].Name);
            }

            applyNodeVariants(shipConstruct);
            return shipConstruct;
        }

        /// <summary>
        /// Positions an unassembled craft relative to a printer and optionally
        /// keeps its complete bounds beyond the spawn transform's virtual boundary.
        /// </summary>
        internal static bool TryPositionShipConstruct(ShipConstruct shipConstruct, Part parentPart,
            Transform dropTransform, bool enforceBoundary, out Vector3 relativePosition,
            out Quaternion relativeRotation, out Bounds craftBounds)
        {
            relativePosition = Vector3.zero;
            relativeRotation = Quaternion.identity;
            craftBounds = new Bounds();

            if (shipConstruct == null || shipConstruct.parts == null ||
                shipConstruct.parts.Count == 0 || parentPart == null || dropTransform == null)
                return false;

            Part rootPart = shipConstruct.parts[0].localRoot;

            // Vessel's front will be pointing towards the printhead.
            Quaternion baseRotation = new Quaternion(0, 1, 0, 0);
            relativeRotation = baseRotation * rootPart.transform.rotation;
            rootPart.transform.rotation = dropTransform.rotation * relativeRotation;
            rootPart.transform.position = dropTransform.position;

            if (!TryGetConstructBounds(shipConstruct, out craftBounds))
                return false;

            if (enforceBoundary)
            {
                Vector3 boundaryNormal = getPlacementBoundaryNormal(parentPart, dropTransform, false);
                moveBoundsBeyondBoundary(rootPart.transform, ref craftBounds,
                    dropTransform.position, boundaryNormal);
            }

            relativePosition = dropTransform.InverseTransformPoint(rootPart.transform.position);
            relativeRotation = Quaternion.Inverse(dropTransform.rotation) * rootPart.transform.rotation;

            if (SandcastleScenario.debugMode)
            {
                Debug.Log("[Sandcastle] - Craft Bounds: " + craftBounds);
                Debug.Log("[Sandcastle] - Craft placement relative position: " + relativePosition);
                Debug.Log("[Sandcastle] - Craft placement relative rotation: " + relativeRotation.eulerAngles);
            }

            return true;
        }

        /// <summary>
        /// Uses KSP's ground-placement logic on an unassembled craft and captures
        /// the resulting transform relative to the printer's spawn transform.
        /// </summary>
        internal static bool TryPositionLandedShipConstruct(ShipConstruct shipConstruct,
            Part parentPart, Transform dropTransform, bool enforceBoundary,
            out Vector3 relativePosition, out Quaternion relativeRotation,
            out Bounds craftBounds)
        {
            relativePosition = Vector3.zero;
            relativeRotation = Quaternion.identity;
            craftBounds = new Bounds();

            if (shipConstruct == null || shipConstruct.parts == null ||
                shipConstruct.parts.Count == 0 || parentPart == null || dropTransform == null)
                return false;

            Part rootPart = shipConstruct.parts[0].localRoot;

            // Apply the same construct-space orientation used for orbital
            // spawning before asking KSP to place the craft on the ground.
            // PutShipToGround can then perform the final alignment with the
            // actual local terrain slope for both SPH and VAB craft.
            Quaternion headingCorrection = new Quaternion(0, 1, 0, 0);
            rootPart.transform.rotation = headingCorrection *
                rootPart.transform.rotation;

            ShipConstruction.PutShipToGround(shipConstruct, dropTransform);

            rootPart.transform.position += parentPart.vessel.upAxis.normalized *
                kLandedSpawnClearance;
            if (!TryGetConstructBounds(shipConstruct, out craftBounds))
                return false;

            if (enforceBoundary)
            {
                Vector3 boundaryNormal = getPlacementBoundaryNormal(parentPart, dropTransform, true);
                moveBoundsBeyondBoundary(rootPart.transform, ref craftBounds,
                    dropTransform.position, boundaryNormal);
            }

            relativePosition = dropTransform.InverseTransformPoint(rootPart.transform.position);
            relativeRotation = Quaternion.Inverse(dropTransform.rotation) * rootPart.transform.rotation;

            if (SandcastleScenario.debugMode)
            {
                Debug.Log("[Sandcastle] - Landed craft bounds: " + craftBounds);
                Debug.Log("[Sandcastle] - Landed placement relative position: " + relativePosition);
                Debug.Log("[Sandcastle] - Landed placement relative rotation: " + relativeRotation.eulerAngles);
            }

            return true;
        }

        static Vector3 getPlacementBoundaryNormal(Part parentPart, Transform boundaryTransform,
            bool projectOntoGround)
        {
            Vector3 normal = boundaryTransform.position - parentPart.transform.position;
            if (projectOntoGround)
                normal = Vector3.ProjectOnPlane(normal, parentPart.vessel.upAxis.normalized);
            return normal.normalized;
        }

        static void moveBoundsBeyondBoundary(Transform rootTransform, ref Bounds craftBounds,
            Vector3 boundaryPoint, Vector3 boundaryNormal)
        {
            if (boundaryNormal == Vector3.zero)
                return;

            Vector3 extents = craftBounds.extents;
            float supportRadius =
                Mathf.Abs(boundaryNormal.x) * extents.x +
                Mathf.Abs(boundaryNormal.y) * extents.y +
                Mathf.Abs(boundaryNormal.z) * extents.z;
            float centerDistance = Vector3.Dot(
                craftBounds.center - boundaryPoint, boundaryNormal);

            if (centerDistance < supportRadius)
            {
                Vector3 displacement = boundaryNormal * (supportRadius - centerDistance);
                rootTransform.position += displacement;
                craftBounds.center += displacement;

                if (SandcastleScenario.debugMode)
                    Debug.Log("[Sandcastle] - Moved vessel beyond virtual printer boundary by "
                        + displacement.magnitude + "m.");
            }
        }

        /// <summary>
        /// Calculates construct bounds in the coordinate system of a supplied
        /// reference transform without requiring an assembled Vessel.
        /// </summary>
        internal static bool TryGetConstructLocalBounds(ShipConstruct shipConstruct,
            Transform referenceTransform, out Bounds localBounds)
        {
            localBounds = new Bounds();
            bool boundsInitialized = false;

            if (shipConstruct == null || shipConstruct.parts == null ||
                shipConstruct.parts.Count == 0 || referenceTransform == null)
                return false;

            Matrix4x4 worldToReference = referenceTransform.worldToLocalMatrix;
            for (int partIndex = 0; partIndex < shipConstruct.parts.Count; partIndex++)
            {
                Part craftPart = shipConstruct.parts[partIndex];
                foreach (Transform modelTransform in craftPart.FindModelComponents<Transform>())
                {
                    if (!modelTransform.gameObject.activeInHierarchy)
                        continue;

                    MeshRenderer renderer = modelTransform.GetComponent<MeshRenderer>();
                    if (renderer != null && !renderer.enabled)
                        continue;

                    MeshFilter meshFilter = modelTransform.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    Matrix4x4 meshToReference = worldToReference *
                        modelTransform.localToWorldMatrix;
                    Vector3[] vertices = meshFilter.sharedMesh.vertices;
                    for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        Vector3 point = meshToReference.MultiplyPoint3x4(vertices[vertexIndex]);
                        if (!boundsInitialized)
                        {
                            localBounds = new Bounds(point, Vector3.zero);
                            boundsInitialized = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(point);
                        }
                    }
                }
            }

            return boundsInitialized;
        }

        /// <summary>
        /// Calculates world-space bounds for a ShipConstruct before it has been
        /// assembled into a Vessel. Collider bounds are preferred because they
        /// match the coordinate space used by the spawn collision checks.
        /// </summary>
        internal static bool TryGetConstructBounds(ShipConstruct shipConstruct, out Bounds constructBounds)
        {
            constructBounds = new Bounds();
            bool boundsInitialized = false;

            if (shipConstruct == null || shipConstruct.parts == null || shipConstruct.parts.Count == 0)
                return false;

            int partCount = shipConstruct.parts.Count;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                Part craftPart = shipConstruct.parts[partIndex];
                Transform modelTransform = craftPart.transform.Find("model");
                if (modelTransform == null)
                    continue;

                Collider[] colliders = modelTransform.GetComponentsInChildren<Collider>();
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    Collider collider = colliders[colliderIndex];
                    if (collider == null || !collider.enabled || collider.isTrigger || collider.gameObject.layer == 21)
                        continue;

                    if (!boundsInitialized)
                    {
                        constructBounds = collider.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        constructBounds.Encapsulate(collider.bounds);
                    }
                }
            }

            // Some parts have no usable colliders. Fall back to visible renderers,
            // whose bounds are also expressed in world space and require no Vessel.
            if (!boundsInitialized)
            {
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    Renderer[] renderers = shipConstruct.parts[partIndex].GetComponentsInChildren<Renderer>();
                    for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        Renderer renderer = renderers[rendererIndex];
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                            continue;

                        if (!boundsInitialized)
                        {
                            constructBounds = renderer.bounds;
                            boundsInitialized = true;
                        }
                        else
                        {
                            constructBounds.Encapsulate(renderer.bounds);
                        }
                    }
                }
            }

            return boundsInitialized;
        }

        public static void setupLaunchClamps(ShipConstruct ship)
        {
            int count = ship.parts.Count;
            Part part;
            PartModule partModule;
            for (int index = 0; index < count; index++)
            {
                part = ship.parts[index];

                // Special case: handle Restock clamps
                partModule = part.Modules.GetModule("ModuleRestockLaunchClamp");
                if (partModule != null)
                {
                    part.SendMessage("RotateTower", SendMessageOptions.DontRequireReceiver);
                }

                // Special case: handle EL clamps
                partModule = part.Modules.GetModule("ELExtendingLaunchClamp");
                if (partModule != null)
                {
                    part.SendMessage("RotateTower", SendMessageOptions.DontRequireReceiver);
                }

                List<LaunchClamp> launchClamps = part.FindModulesImplementing<LaunchClamp>();
                if (launchClamps != null && launchClamps.Count > 0)
                {
                    int clampCount = launchClamps.Count;
                    for (int clampIndex = 0; clampIndex < clampCount; clampIndex++)
                    {
                        launchClamps[clampIndex].EnableExtension();
                    }
                }
            }
        }

        public static bool allPartsStarted(Vessel vessel)
        {
            int count = vessel.Parts.Count;
            Part part;
            bool allPartsStarted = false;
            while (!allPartsStarted)
            {
                allPartsStarted = true;
                for (int index = 0; index < count; index++)
                {
                    part = vessel.Parts[index];
                    if (!part.started)
                    {
                        return false;
                    }
                }

                OrbitPhysicsManager.HoldVesselUnpack(2);
            }

            return true;
        }

        public static IEnumerator<YieldInstruction> coupleVessel(Vessel vessel, Part parentPart,
            Callback<DockedVesselInfo> onVesselCoupled, Transform anchorTransform = null,
            Vector3 relativePosition = default(Vector3), Quaternion relativeRotation = default(Quaternion))
        {
            // Wait for all part to be initialized.
            Debug.Log("[Sandcastle] - coupleVessel called");
            Debug.Log("[Sandcastle] - vessel part count: " + vessel.Parts.Count);
            int count = vessel.Parts.Count;
            Part part;
            bool allPartsStarted = false;
            bool anchorVessel = anchorTransform != null;
            if (anchorVessel)
                FlightGlobals.overrideOrbit = true;

            while (!allPartsStarted)
            {
                allPartsStarted = true;
                for (int index = 0; index < count; index++)
                {
                    part = vessel.Parts[index];
                    if (!part.started)
                    {
                        allPartsStarted = false;
                        break;
                    }
                }

                if (anchorVessel)
                {
                    repositionVessel(vessel, anchorTransform, relativePosition, relativeRotation);
                    setCraftOrbit(vessel, OrbitDriver.UpdateMode.UPDATE, parentPart);
                }

                if (allPartsStarted)
                    break;

                OrbitPhysicsManager.HoldVesselUnpack(2);
                yield return new WaitForFixedUpdate();
            }

            // Perform one last synchronization using the current printer transform
            // and orbit before switching the craft to physics.
            if (anchorVessel)
            {
                FlightGlobals.overrideOrbit = false;
                repositionVessel(vessel, anchorTransform, relativePosition, relativeRotation);
                setCraftOrbit(vessel, OrbitDriver.UpdateMode.UPDATE, parentPart);
            }

            // Create docked vessel info
            Debug.Log("[Sandcastle] - " + vessel.vesselName + " going off rails.");
            vessel.GoOffRails();
            DockedVesselInfo dockedVesselInfo = new DockedVesselInfo();
            dockedVesselInfo.name = vessel.name;
            dockedVesselInfo.vesselType = vessel.vesselType;
            dockedVesselInfo.rootPartUId = vessel.rootPart.flightID;

            // Couple the vessel to the printer.
            // NOTE: Doing this will cause the vessel object to be destroyed and it will become null.
            // But you can get the docked root part via its flightID (rootPartUId in docked vessel info).
            vessel.rootPart.Couple(parentPart);
            Debug.Log("[Sandcastle] - " + vessel.vesselName + " root part" + vessel.rootPart.partInfo.name + " coupled to " + parentPart.partInfo.name);

            // Reset active vessel to the printer.
            if (parentPart.vessel != FlightGlobals.ActiveVessel)
                FlightGlobals.SetActiveVessel(parentPart.vessel);

            // Signal that we're done.
            Debug.Log("[Sandcastle] - calling onVesselCoupled");
            onVesselCoupled(dockedVesselInfo);

            yield return null;
        }

        static void repositionVessel(Vessel vessel, Transform anchorTransform,
            Vector3 relativePosition, Quaternion relativeRotation)
        {
            Quaternion rotation = anchorTransform.rotation * relativeRotation;
            Vector3 position = anchorTransform.TransformPoint(relativePosition);
            vessel.SetRotation(rotation, false);
            vessel.SetPosition(position, true);
        }

        static IEnumerator<YieldInstruction> finalizeLandedVessel(Vessel vessel, Part parentPart,
            Transform anchorTransform, Vector3 relativePosition, Quaternion relativeRotation)
        {
            FlightGlobals.overrideOrbit = true;

            while (!allPartsStarted(vessel))
            {
                repositionVessel(vessel, anchorTransform, relativePosition, relativeRotation);
                setCraftOrbit(vessel, OrbitDriver.UpdateMode.UPDATE, parentPart);
                OrbitPhysicsManager.HoldVesselUnpack(2);
                yield return new WaitForFixedUpdate();
            }

            FlightGlobals.overrideOrbit = false;
            repositionVessel(vessel, anchorTransform, relativePosition, relativeRotation);
            setCraftOrbit(vessel, OrbitDriver.UpdateMode.UPDATE, parentPart);

            bool wasLoaded = vessel.loaded;
            bool wasPacked = vessel.packed;
            vessel.loaded = true;
            vessel.packed = false;

            // Keep the craft flying so KSP does not snap the user-selected
            // preview placement back to its stock landed position.
            vessel.situation = Vessel.Situations.FLYING;
            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.GetHeightFromTerrain();
            vessel.loaded = wasLoaded;
            vessel.packed = wasPacked;

            FlightLogger.IgnoreGeeForces(20f);
            vessel.ignoreCollisionsFrames = 60;
            vessel.skipGroundPositioning = true;

            // Stock surface-positioning uses gravity easing for this same kind
            // of transition. It preserves the flying state and custom placement
            // while allowing an unclamped ground craft to settle without a
            // destructive one-meter drop. Launch clamps already stabilize their
            // vessel, and easing could otherwise remain active after launch.
            List<LaunchClamp> launchClamps =
                vessel.FindPartModulesImplementing<LaunchClamp>();
            if (!parentPart.vessel.Splashed &&
                (launchClamps == null || launchClamps.Count == 0))
                FlightGlobals.fetch.ToggleVesselEaseIn(vessel, true, 0.1);

            vessel.GoOffRails();
            yield return new WaitForFixedUpdate();
            vessel.skipGroundPositioning = false;

            // FLYING is intentional: marking the vessel LANDED lets KSP restore
            // the craft-file launch position and discards the player's preview
            // placement. It also means SandcastleScenario's landed off-rails
            // handler will not run, so invoke KSP's stock surface raycast here.
            // Unlike PQS terrain altitude, CheckGroundCollision includes facility
            // colliders such as the KSC runway and raises the vessel onto them.
            vessel.CheckGroundCollision();
        }

        public static IEnumerator<YieldInstruction> decoupleVessel(Part rootPart, DockedVesselInfo dockedVesselInfo, bool switchToVessel = false)
        {
            if (rootPart == null || dockedVesselInfo == null)
                yield break;

            Vessel parentVessel = rootPart.vessel;
            rootPart.Undock(dockedVesselInfo);

            if (switchToVessel)
            {
                // Follow the released root part instead of assuming that KSP
                // appends the new vessel to the end of VesselsLoaded. Other mods
                // can create or reorder vessels during the undock callbacks.
                Vessel undockedVessel = rootPart.vessel;
                while (undockedVessel == null || undockedVessel == parentVessel)
                {
                    yield return new WaitForFixedUpdate();
                    undockedVessel = rootPart.vessel;
                }

                yield return new WaitForFixedUpdate();
                refreshVesselControl(undockedVessel);
                yield return new WaitForFixedUpdate();
                FlightGlobals.ForceSetActiveVessel(undockedVessel);
            }

            yield return new WaitForFixedUpdate();
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Rebuilds command-source registration after KSP creates a vessel by
        /// undocking already-started parts. ModuleCommand.Start does not run a
        /// second time, so its CommNet registration can otherwise remain tied
        /// to the vessel that temporarily contained the printed craft.
        /// </summary>
        /// <param name="vessel">The vessel created by the undock operation.</param>
        private static void refreshVesselControl(Vessel vessel)
        {
            if (vessel == null)
                return;

            if (vessel.Connection != null)
                vessel.Connection.FindCommandSources();

            List<ModuleCommand> commandModules =
                vessel.FindPartModulesImplementing<ModuleCommand>();
            for (int index = 0; index < commandModules.Count; index++)
                commandModules[index].UpdateControlState();

            GameEvents.onVesselWasModified.Fire(vessel);

            if (SandcastleScenario.debugMode)
                Debug.Log("[Sandcastle] - Refreshed command control for "
                    + vessel.vesselName + "; command modules: "
                    + commandModules.Count + ", control level: "
                    + vessel.CurrentControlLevel + ", controllable: "
                    + vessel.IsControllable);
        }

        /// <summary>
        /// Replaces null persistent string fields with empty strings before KSP serializes a part
        /// prefab into an inventory snapshot. ConfigNode cannot store null strings, and part modules
        /// may legitimately leave an optional persistent string unset until it is first used.
        /// </summary>
        /// <param name="partPrefab">The part prefab that KSP is about to store in an inventory.</param>
        private static void normalizePersistentStringFields(Part partPrefab)
        {
            if (partPrefab == null || partPrefab.Modules == null)
                return;

            for (int moduleIndex = 0; moduleIndex < partPrefab.Modules.Count; moduleIndex++)
            {
                PartModule partModule = partPrefab.Modules[moduleIndex];
                if (partModule == null || partModule.Fields == null)
                    continue;

                for (int fieldIndex = 0; fieldIndex < partModule.Fields.Count; fieldIndex++)
                {
                    BaseField field = partModule.Fields[fieldIndex];
                    if (!field.isPersistant || field.FieldInfo == null ||
                        field.FieldInfo.FieldType != typeof(string))
                        continue;

                    if (field.FieldInfo.GetValue(partModule) == null)
                        field.FieldInfo.SetValue(partModule, string.Empty);
                }
            }
        }

        internal static void clearResources(Vessel vessel)
        {
            if (vessel.loaded)
            {
                int partCount = vessel.Parts.Count;
                Part part;
                PartResource resource;
                int resourceCount;
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    part = vessel.Parts[partIndex];
                    resourceCount = part.Resources.Count;
                    for (int index = 0; index < resourceCount; index++)
                    {
                        resource = part.Resources[index];
                        if (resource.resourceName != "ElectricCharge" && resource.resourceName != "Ablator")
                            resource.amount = 0f;
                    }
                }
            }
            else
            {
                int partCount = vessel.protoVessel.protoPartSnapshots.Count;
                ProtoPartSnapshot protoPart;
                int resourceCount;
                ProtoPartResourceSnapshot resourceSnapshot;
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    protoPart = vessel.protoVessel.protoPartSnapshots[partIndex];
                    resourceCount = protoPart.resources.Count;
                    for (int index = 0; index < resourceCount; index++)
                    {
                        resourceSnapshot = protoPart.resources[index];
                        if (resourceSnapshot.resourceName != "ElectricCharge" && resourceSnapshot.resourceName != "Ablator")
                            resourceSnapshot.amount = 0;
                    }
                }
            }
        }

        internal static void updateAttachNode(Part p, AttachNode vnode)
        {
            var pnode = p.FindAttachNode(vnode.id);
            if (pnode != null)
            {
                pnode.originalPosition = vnode.originalPosition;
                pnode.position = vnode.position;
                pnode.size = vnode.size;
            }
        }

        internal static Vector3 getVesselWorldCoM(Vessel v)
        {
            Vector3 com = v.localCoM;
            return v.rootPart.partTransform.TransformPoint(com);
        }

        internal static void applyNodeVariants(ShipConstruct ship)
        {
            for (int i = 0; i < ship.parts.Count; i++)
            {
                var p = ship.parts[i];
                var pv = p.FindModulesImplementing<ModulePartVariants>();
                for (int j = 0; j < pv.Count; j++)
                {
                    var variant = pv[j].SelectedVariant;
                    if (variant == null)
                        continue;

                    for (int k = 0; k < variant.AttachNodes.Count; k++)
                    {
                        var vnode = variant.AttachNodes[k];
                        updateAttachNode(p, vnode);
                    }
                }
            }
        }

        internal static void setCraftOrbit(Vessel craftVessel, OrbitDriver.UpdateMode mode, Part parentPart)
        {
            craftVessel.orbitDriver.SetOrbitMode(mode);

            var craftCoM = getVesselWorldCoM(craftVessel);
            var vesselCoM = getVesselWorldCoM(parentPart.vessel);
            var offset = (Vector3d.zero + craftCoM - vesselCoM).xzy;

            var corb = craftVessel.orbit;
            var orb = parentPart.vessel.orbit;
            var UT = Planetarium.GetUniversalTime();
            var body = orb.referenceBody;
            corb.UpdateFromStateVectors(orb.pos + offset, orb.vel, body, UT);
        }

        internal static Texture2D renderCamera(Camera cam, int width, int height, int depth, RenderTextureReadWrite rtReadWrite)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32, rtReadWrite);
            renderTexture.Create();
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = renderTexture;
            cam.targetTexture = renderTexture;
            cam.Render();
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, true);
            texture2D.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0, false);
            texture2D.Apply();
            RenderTexture.active = active;
            cam.targetTexture = null;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            return texture2D;
        }

        private static bool canPrintHiddenPart(AvailablePart availablePart)
        {
            if (availablePart.TechHidden && availablePart.category == PartCategories.none && availablePart.partConfig.HasValue("canPrintHiddenPart"))
            {
                // Check the part config
                bool canPrintHiddenPart = false;
                bool.TryParse(availablePart.partConfig.GetValue("canPrintHiddenPart"), out canPrintHiddenPart);

                return canPrintHiddenPart;
            }

            return false;
        }
        #endregion
    }
}
