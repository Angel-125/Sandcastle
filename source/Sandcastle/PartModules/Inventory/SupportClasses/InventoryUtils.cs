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
        public static bool debugMode = false;
        #endregion

        #region Fields
        #endregion

        #region Housekeeping
        static List<string> thumbnailFilePaths = null;
        static Dictionary<string, Texture2D> thumbnails = null;
        #endregion

        #region API
        /// <summary>
        /// Retrieves an instantiated part from the supplied available part.
        /// </summary>
        /// <param name="availablePart">The AvailablePart</param>
        /// <returns></returns>
        public static Part GetPartFromAvailablePart(AvailablePart availablePart)
        {
            Part part = availablePart.partPrefab.protoPartSnapshot.CreatePart();
            part.ResumeState = PartStates.PLACEMENT;
            part.State = PartStates.PLACEMENT;
            part.name = availablePart.title;
            part.gameObject.SetActive(true);
            part.partInfo = availablePart;

            return part;
        }

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
        /// Searches the game folder for thumbnail images.
        /// </summary>
        public static void FindThumbnailPaths()
        {
            string gameDataPath = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"));
            string[] files = Directory.GetFiles(gameDataPath, "*_icon*.png", SearchOption.AllDirectories);

            thumbnailFilePaths = new List<string>();

            for (int index = 0; index < files.Length; index++)
            {
                if (files[index].Contains("@thumbs"))
                {
                    thumbnailFilePaths.Add(files[index]);
                }
            }

            // Don't forget the root path
            gameDataPath = Path.GetFullPath(KSPUtil.ApplicationRootPath);
            files = Directory.GetFiles(gameDataPath, "*_icon*.png", SearchOption.AllDirectories);
            for (int index = 0; index < files.Length; index++)
            {
                if (files[index].Contains("@thumbs"))
                {
                    thumbnailFilePaths.Add(files[index]);
                }
            }
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

        public static void SpawnPart(AvailablePart availablePart, Part parentPart, Transform dropTransform, Vector3 offsetAxis)
        {
            // Calculate craft size so we don't smack into the printer when we drop the part.
            Part part = availablePart.partPrefab;
            List<Part> parts = new List<Part>();
            parts.Add(part);

            Vector3 craftSize = ShipConstruction.CalculateCraftSize(parts, part);

            // Account for offset axis
            craftSize.x *= offsetAxis.x;
            craftSize.y *= offsetAxis.y;
            craftSize.z *= offsetAxis.z;

            // Offset the drop point so we don't smack into the printer when we drop the part.
            Vector3 dropPoint = dropTransform.TransformPoint(craftSize);
            Quaternion dropRotation = Quaternion.Inverse(FlightGlobals.ActiveVessel.mainBody.bodyTransform.rotation) * dropTransform.rotation;

            ConfigNode node = EVAConstructionModeController.Instance.evaEditor.GetProtoVesselNode(availablePart.title, dropPoint, dropRotation, FlightGlobals.ActiveVessel, part);
            ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(node);
            Vessel unloadedVessel = null;
            for (int index = 0; index < FlightGlobals.VesselsUnloaded.Count; ++index)
            {
                if (protoVessel.persistentId == FlightGlobals.VesselsUnloaded[index].persistentId)
                {
                    unloadedVessel = FlightGlobals.VesselsUnloaded[index];
                    unloadedVessel.SetPhysicsHoldExpiryOverride();
                    clearResources(unloadedVessel);
                    break;
                }
            }
        }

        public static void SpawnShip(ShipConstruct shipConstruct, Part parentPart, Transform dropTransform,
            Callback<DockedVesselInfo> onVesselCoupled, bool removeResources = true,
            bool repositionCraftBeforeSpawning = true, bool useProvidedPlacement = false,
            Vector3 providedRelativePosition = default(Vector3),
            Quaternion providedRelativeRotation = default(Quaternion))
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
            vessel.vesselType = VesselType.Probe;
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

            ShipConstruction.PutShipToGround(shipConstruct, dropTransform);

            Part rootPart = shipConstruct.parts[0].localRoot;
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

        /// <summary>
        /// Courtesy of MechJeb by Sarbian
        /// Licensed under GPLV3
        /// Computes the Bounds of the supplied part.
        /// This works both in the editor and flight.
        /// EX: Bounds partBounds = FlightGlobals.ActiveVessel.rootPart.GetBounds();
        /// </summary>
        /// <param name="part">A Part object to compute the Bounds for.</param>
        /// <returns>A Bounds object containing the part's bounds.</returns>
        internal static Bounds GetBounds(Part part)
        {
            Bounds partBounds = new Bounds();
            bool boundsInitialized = false;

            foreach (Transform t in part.FindModelComponents<Transform>())
            {
                // Check for inactive object. If inactive it's likely a part variant that's not active, so skip it.
                if (t.gameObject.activeSelf == false)
                    continue;

                // Check for disabled or non-existent colliders
                Collider collider = t.GetComponent<Collider>();
                if (collider == null || collider.enabled == false)
                    continue;

                // Check for disabled mesh renderers. Accounts for part variants.
                MeshRenderer renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.enabled == false)
                    continue;

                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf == null)
                    continue;

                Mesh m = mf.mesh;
                if (m == null)
                    continue;

                Matrix4x4 matrix = part.vessel.transform.worldToLocalMatrix * t.localToWorldMatrix;

                foreach (Vector3 vertex in m.vertices)
                {
                    Vector3 worldSpaceVertex = matrix.MultiplyPoint3x4(vertex);

                    if (!boundsInitialized)
                    {
                        partBounds = new Bounds(worldSpaceVertex, Vector3.zero);
                        boundsInitialized = true;
                    }
                    else
                    {
                        partBounds.Encapsulate(worldSpaceVertex);
                    }
                }
            }

            return partBounds;
        }
        /// <summary>
        /// Courtesy of MechJeb by Sarbian
        /// Licensed under GPLV3
        /// Computes the Bounds of the supplied vessel.
        /// This works both in the editor and in flight.
        /// EX: Bounds vesselBounds = FlightGlobals.ActiveVessel.GetBounds();
        /// </summary>
        /// <param name="vessel">A Vessel object to compute the bounds for.</param>
        /// <returns>A Bounds object containing the vessel's bounds.</returns>
        internal static Bounds GetBounds(Vessel vessel)
        {
            Bounds vesselBounds = new Bounds();
            bool boundsInitialized = false;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                Bounds partBounds = GetBounds(p);

                if (!boundsInitialized)
                {
                    vesselBounds = partBounds;
                    boundsInitialized = true;
                }
                else
                {
                    vesselBounds.Encapsulate(partBounds);
                }
            }

            return vesselBounds;
        }

        public static Bounds getBounds(Part rootPart, List<Part> parts)
        {
            if (rootPart == null || parts.Count == 0)
                return new Bounds();

            Bounds vesselBounds = new Bounds();
            bool boundsInitialized = false;

            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                Bounds partBounds = GetBounds(p);

                if (!boundsInitialized)
                {
                    vesselBounds = partBounds;
                    boundsInitialized = true;
                }
                else
                {
                    vesselBounds.Encapsulate(partBounds);
                }
            }

            return vesselBounds;
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
            // The craft begins above the surface and should enter physics rather
            // than being snapped back down by KSP's landed positioning.
            vessel.situation = Vessel.Situations.FLYING;
            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.GetHeightFromTerrain();
            vessel.loaded = wasLoaded;
            vessel.packed = wasPacked;

            FlightLogger.IgnoreGeeForces(20f);
            vessel.ignoreCollisionsFrames = 60;
            vessel.skipGroundPositioning = true;
            vessel.GoOffRails();
            yield return new WaitForFixedUpdate();
            vessel.skipGroundPositioning = false;
        }

        public static IEnumerator<YieldInstruction> decoupleVessel(Part rootPart, DockedVesselInfo dockedVesselInfo, bool switchToVessel = false)
        {
            rootPart.Undock(dockedVesselInfo);

            if (switchToVessel)
            {
                yield return new WaitForFixedUpdate();
                Vessel undockedVessel = FlightGlobals.VesselsLoaded[FlightGlobals.VesselsLoaded.Count - 1];
                yield return new WaitForFixedUpdate();
                FlightGlobals.ForceSetActiveVessel(undockedVessel);
            }

            yield return new WaitForFixedUpdate();
        }
        #endregion

        #region Helpers
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
                    for (int k = 0; k < variant.AttachNodes.Count; k++)
                    {
                        var vnode = variant.AttachNodes[k];
                        updateAttachNode(p, vnode);
                    }
                }
            }
        }

        internal static string saveCraftFile(AvailablePart availablePart)
        {
            ShipConstruct ship = new ShipConstruct(availablePart.title, "Sandcaster created part", availablePart.partPrefab);
            Quaternion rotation = ship.parts[0].transform.rotation;
            ship.parts[0].transform.rotation = Quaternion.identity;
            ConfigNode node = ship.SaveShip();
            ship.parts[0].transform.rotation = rotation;

            string dir = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}/Sandcaster/";
            string filePath = $"{dir}/temp.craft";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            node.Save(filePath);
            return filePath;
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
