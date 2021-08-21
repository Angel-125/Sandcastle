using System;
using System.Collections.Generic;
using System.IO;
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
        #endregion

        #region Fields
        #endregion

        #region Housekeeping
        static List<string> thumbnailFilePaths = null;
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

                if (inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
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
            int storedPartCount;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];
                if (inventory.InventoryIsEmpty)
                    continue;

                storedPartCount = inventory.storedParts.Keys.Count;
                for (int storedPartIndex = 0; storedPartIndex < storedPartCount; storedPartIndex++)
                {
                    storedPart = inventory.storedParts[storedPartIndex];
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

            if (inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
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
        /// <returns>true if there is enough space, false if not.</returns>
        public static bool HasEnoughSpace(Vessel vessel, AvailablePart availablePart, int amount = 1)
        {
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
                return false;

            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory;
            int count = inventories.Count;
            bool massRequirementMet = false;
            bool volRequirementMet = false;
            double partMass = availablePart.partPrefab.mass + availablePart.partPrefab.resourceMass;
            double totalMassNeeded = partMass * amount;
            float totalVolumeNeeded = cargoPart.packedVolume * amount;

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];

                if (inventory.InventoryIsFull || inventory.massCapacityReached || inventory.volumeCapacityReached)
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
        /// <param name="preferredInventory">The preferred inventory to store the part in.</param>
        /// <param name="removeResources">A bool indicating whether or not to remove resources when storing the part. Default is true.</param>
        /// <returns>The Part that the item was stored in, or null if no place could be found for the part.</returns>
        public static Part AddItem(Vessel vessel, AvailablePart availablePart, ModuleInventoryPart preferredInventory = null, bool removeResources = true)
        {
            ModuleInventoryPart inventory = null;
            if (InventoryHasSpace(preferredInventory, availablePart))
                inventory = preferredInventory;
            else
                inventory = GetInventoryWithCargoSpace(vessel, availablePart);
            if (inventory == null)
                return null;

            for (int index = 0; index < inventory.InventorySlots; index++)
            {
                if (inventory.IsSlotEmpty(index))
                {
                    inventory.StoreCargoPartAtSlot(availablePart.partPrefab, index);
                    if (removeResources)
                    {
                        StoredPart storedPart = inventory.storedParts[index];
                        int count = storedPart.snapshot.resources.Count;
                        for (int resourceIndex = 0; resourceIndex < count; resourceIndex++)
                            storedPart.snapshot.resources[resourceIndex].amount = 0;
                    }
                    return inventory.part;
                }
            }

            // No place to store the part.
            return null;
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
        }

        /// <summary>
        /// Retrieves the thumbnail texture that depicts the specified part name.
        /// </summary>
        /// <param name="partName">A string containing the name of the part.</param>
        /// <returns>A Texture2D if the texture exists, or a blank texture if not.</returns>
        public static Texture2D GetTexture(string partName)
        {
            if (thumbnails == null)
                thumbnails = new Dictionary<string, Texture2D>();

            if (!thumbnails.ContainsKey(partName))
                thumbnails.Add(partName, loadTexture(partName));

            return thumbnails[partName];
        }

        /// <summary>
        /// Retrieves a list of parts that can be printed by the specified max print volume.
        /// </summary>
        /// <param name="maxPrintVolume">A float containing the max possible print volume.</param>
        /// <returns>A List of AvailablePart objects that can be printed.</returns>
        public static List<AvailablePart> GetPrintableParts(float maxPrintVolume)
        {
            List<AvailablePart>  filteredParts = new List<AvailablePart>();

            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableCargoParts();
            if (cargoParts != null && cargoParts.Count > 0)
            {
                int count = cargoParts.Count;
                ModuleCargoPart cargoPart;
                float maxPrintableVolume = maxPrintVolume > 0 ? maxPrintVolume : float.MaxValue;
                for (int index = 0; index < count; index++)
                {
                    cargoPart = cargoParts[index].partPrefab.FindModuleImplementing<ModuleCargoPart>();

                    if (cargoPart.packedVolume > 0 && cargoPart.packedVolume <= maxPrintableVolume)
                    {
                        if (cargoParts[index].TechHidden == false || canPrintHiddenPart(cargoParts[index]))
                            filteredParts.Add(cargoParts[index]);
                    }
                }
            }

            return filteredParts;
        }
        #endregion

        #region Helpers
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

        /// <summary>
        /// Retrieves the thumbnail texture that depicts the specified part name.
        /// </summary>
        /// <param name="partName">A string containing the name of the part.</param>
        /// <returns>A Texture2D if the texture exists, or a blank texture if not.</returns>
        public static Texture2D loadTexture(string partName)
        {
            Texture2D texture = new Texture2D(kTextureSize, kTextureSize, TextureFormat.RGBA32, false);
            Texture2D defaultTexture = GameDatabase.Instance.GetTexture("WildBlueIndustries/Sandcastle/Icons/Box", false);

            // Find the file path
            string filePath;
            int count = thumbnailFilePaths.Count;
            for (int index = 0; index < count; index++)
            {
                filePath = thumbnailFilePaths[index];
                if (filePath.Contains(partName))
                {
                    if (File.Exists(filePath))
                    {
                        texture.LoadImage(File.ReadAllBytes(filePath));
                        return texture;
                    }
                }
            }

            return defaultTexture;
        }
        #endregion
    }
}
