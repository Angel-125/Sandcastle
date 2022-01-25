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
        const bool debugMode = false;
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
        /// <param name="variantIndex">An int containing the index of the part variant to store.</param>
        /// <param name="preferredInventory">The preferred inventory to store the part in.</param>
        /// <param name="removeResources">A bool indicating whether or not to remove resources when storing the part. Default is true.</param>
        /// <returns>The Part that the item was stored in, or null if no place could be found for the part.</returns>
        public static Part AddItem(Vessel vessel, AvailablePart availablePart, int variantIndex, ModuleInventoryPart preferredInventory = null, bool removeResources = true)
        {
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart == null)
                return null;

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
                if (partIcon != null && partIcon.inventoryItemThumbnail.texture == null)
                {
                    Texture2D texture = GetTexture(availablePart.name, variantIndex);
                    partIcon.inventoryItemThumbnail.texture = texture;
                    partIcon.inventoryItemThumbnail.SetNativeSize();
                    MonoUtilities.RefreshContextWindows(inventory.part);
                }

                if (removeResources)
                {
                    int count = storedPart.snapshot.resources.Count;
                    for (int resourceIndex = 0; resourceIndex < count; resourceIndex++)
                        storedPart.snapshot.resources[resourceIndex].amount = 0;
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
        /// <returns>A List of AvailablePart objects that can be printed.</returns>
        public static List<AvailablePart> GetPrintableParts(float maxPrintVolume)
        {
            List<AvailablePart> filteredParts = new List<AvailablePart>();

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
        /// <param name="variantIndex">An int containing the index of the desired part variant image.
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
        #endregion

        #region Helpers
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
