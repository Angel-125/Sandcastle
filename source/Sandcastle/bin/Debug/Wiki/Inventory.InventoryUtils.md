            
An inventory helper class
        
## Methods


### GetInventoryWithCargoSpace(Vessel,AvailablePart)
Gets an inventory with enough storage space and storage mass for the desired part.
> #### Parameters
> **vessel:** The vessel to query.

> **availablePart:** The AvailablePart to check for space.

> #### Return value
> A ModuleInventoryPart if space can be found or null if not.

### GetPartsToRecycle(Vessel)
Returns a list of inventory parts that can be recycled.
> #### Parameters
> **vessel:** The Vessel to search for parts to recycle.

> #### Return value
> A List of AvailablePart objects.

### InventoryHasSpace(ModuleInventoryPart,AvailablePart)
Determines whether or not the supplied inventory has space for the desired part.
> #### Parameters
> **inventory:** A ModuleInventoryPart to check for space.

> **availablePart:** An AvailablePart to check to see if it fits.

> #### Return value
> true if the inventory has space for the part, false if not.

### HasEnoughSpace(Vessel,AvailablePart,System.Int32,System.Double,System.Single)
Determines whether or not the vessel has enough storage space.
> #### Parameters
> **vessel:** The vessel to query

> **availablePart:** The AvailablePart to check for space.

> **amount:** The number of parts that need space. Default is 1.

> **partMassOverride:** Optional mass, in metric tons, to use instead of the part's configured mass.

> **volumeOverride:** Optional packed volume, in liters, to use instead of the part's configured volume.

> #### Return value
> true if there is enough space, false if not.

### HasItem(Vessel,System.String)
Determines whether or not the vessel has the item in question.
> #### Parameters
> **vessel:** The vessel to query.

> **partName:** The name of the part to look for

> #### Return value
> true if the vessel has the part, false if not.

### GetInventoryItemCount(Vessel,System.String)
Returns the number of parts in the vessel's inventory, if it has the part.
> #### Parameters
> **vessel:** The vessel to query.

> **partName:** The name of the part to look for.

> #### Return value
> An Int containing the number of parts in the vessel's inventory.

### GetInventoryWithPart(Vessel,System.String)
Determines whether or not the vessel has the item in question.
> #### Parameters
> **vessel:** The vessel to query.

> **partName:** The name of the part to look for

> #### Return value
> the ModuleInventoryPart if the vessel has the part, null if not.

### RemoveItem(Vessel,System.String,System.Int32)
Removes the item from the vessel if it exists.
> #### Parameters
> **vessel:** The vessel to query.

> **partName:** The name of the part to remove.

> **partCount:** The number parts to remove. Default is 1.


### AddItem(Vessel,AvailablePart,System.Int32,ModuleInventoryPart,System.Boolean)
Adds the item to the vessel inventory if there is enough room.
> #### Parameters
> **vessel:** The vessel to query.

> **availablePart:** The part to add to the inventory

> **variantIndex:** An int containing the index of the part variant to store.

> **preferredInventory:** The preferred inventory to store the part in.

> **removeResources:** A bool indicating whether or not to remove resources when storing the part. Default is true.

> #### Return value
> The Part that the item was stored in, or null if no place could be found for the part.

### GetPrintableParts(System.Single,System.String)
Retrieves a list of parts that can be printed by the specified max print volume.
> #### Parameters
> **maxPrintVolume:** A float containing the max possible print volume.

> **maxPartDimensions:** An optional string containing the max possible print dimensions.

> #### Return value
> A List of AvailablePart objects that can be printed.

### GetWorldSpawnPrintableParts(System.Single,System.String)
Retrieves parts that can be printed directly into the world. Unlike , this includes cargo parts whose packed volume is negative because world-spawned parts do not need to fit in a stock inventory.
> #### Parameters
> **maxPrintVolume:** Maximum bounding-box volume in liters, or a non-positive value for no volume limit.

> **maxPartDimensions:** Optional maximum part dimensions in meters.

> #### Return value
> A list of parts eligible for direct world spawning.

### TryGetPartBounds(AvailablePart,UnityEngine.Bounds@)
Calculates a part prefab's active model bounds in part-local space.
> #### Parameters
> **availablePart:** The part definition to measure.

> **partBounds:** The calculated local bounds.

> #### Return value
> True when at least one active model mesh was measured.

### GetTexture(System.String)
Retrieves the thumbnail texture that depicts the specified part name.
> #### Parameters
> **partName:** A string containing the name of the part.

> #### Return value
> A Texture2D if the texture exists, or a blank texture if not.

### GetTexture(System.String,System.Int32)
Retrieves the thumbnail texture that depicts the specified part name.
> #### Parameters
> **partName:** A string containing the name of the part.

> **variantIndex:** An int containing the index of the desired part variant image.

> #### Return value
> A Texture2D if the texture exists, or a blank texture if not.

### GetFilePathForThumbnail(AvailablePart,System.Int32,System.Boolean)
Returns the full path to the part's thumbnail image.
> #### Parameters
> **availablePart:** An AvailablePart to check for images.

> **variantIndex:** An int containing the variant index to check for. Default is -1.

> **useDefaultPath:** A bool indicating whether or not to use the default thumbnails path.

> #### Return value
> 

### SpawnGroundPart(AvailablePart,Part,UnityEngine.Transform,System.Boolean)
Drops a completed print onto the ground using KSP's stock EVA construction proto-vessel path. This API intentionally does not support orbital spawning; use for that case.
> #### Parameters
> **availablePart:** The part definition to place into the world.

> **parentPart:** The printer part whose vessel supplies the spawn environment.

> **dropTransform:** The printer transform that defines position and orientation.

> **repositionPart:** Whether to move the print beyond the spawn boundary.

> #### Return value
> True if the ground-drop request was accepted.

### SpawnOrbitalPart(AvailablePart,System.Int32,Part,UnityEngine.Transform,System.Boolean,System.Boolean,Callback{DockedVesselInfo})
Spawns a one-part vessel in a stable orbit and couples it to its printer. The part is wrapped in a so it uses the same launch, orbit synchronization, and coupling path as WBIShipwright.
> #### Parameters
> **availablePart:** The part definition to place into the world.

> **variantIndex:** The selected part variant index.

> **parentPart:** The printer part whose vessel supplies the orbit.

> **dropTransform:** The printer transform that defines position and orientation.

> **repositionPart:** Whether to move the print beyond the spawn boundary.

> **removeResources:** Whether printable resources should be emptied.

> **onPartCoupled:** Callback invoked after the part is coupled.

> #### Return value
> True if orbital construction and spawning started.

### TryGetPartLocalBounds(Part,UnityEngine.Transform,UnityEngine.Bounds@)
Calculates the bounds of a part in the coordinate system of the supplied reference transform.

### movePartAboveTerrain(UnityEngine.Vector3@,UnityEngine.Quaternion,UnityEngine.Bounds,CelestialBody)
Raises a prospective part placement until its lowest bounds point is one meter above the local terrain.

### CreateSinglePartConstruct(AvailablePart,System.Int32)
Creates an independent one-part construct from a part-loader prefab. Saving and reloading the temporary construct clones the prefab into the initialized form expected by .
> #### Parameters
> **availablePart:** The part definition to clone.

> **variantIndex:** The selected variant index.

> #### Return value
> A launchable one-part construct, or null on failure.

### TryPositionShipConstruct(ShipConstruct,Part,UnityEngine.Transform,System.Boolean,UnityEngine.Vector3@,UnityEngine.Quaternion@,UnityEngine.Bounds@)
Positions an unassembled craft relative to a printer and optionally keeps its complete bounds beyond the spawn transform's virtual boundary.

### TryPositionLandedShipConstruct(ShipConstruct,Part,UnityEngine.Transform,System.Boolean,UnityEngine.Vector3@,UnityEngine.Quaternion@,UnityEngine.Bounds@)
Uses KSP's ground-placement logic on an unassembled craft and captures the resulting transform relative to the printer's spawn transform.

### TryGetConstructLocalBounds(ShipConstruct,UnityEngine.Transform,UnityEngine.Bounds@)
Calculates construct bounds in the coordinate system of a supplied reference transform without requiring an assembled Vessel.

### TryGetConstructBounds(ShipConstruct,UnityEngine.Bounds@)
Calculates world-space bounds for a ShipConstruct before it has been assembled into a Vessel. Collider bounds are preferred because they match the coordinate space used by the spawn collision checks.

### refreshVesselControl(Vessel)
Rebuilds command-source registration after KSP creates a vessel by undocking already-started parts. ModuleCommand.Start does not run a second time, so its CommNet registration can otherwise remain tied to the vessel that temporarily contained the printed craft.
> #### Parameters
> **vessel:** The vessel created by the undock operation.


### normalizePersistentStringFields(Part)
Replaces null persistent string fields with empty strings before KSP serializes a part prefab into an inventory snapshot. ConfigNode cannot store null strings, and part modules may legitimately leave an optional persistent string unset until it is first used.
> #### Parameters
> **partPrefab:** The part prefab that KSP is about to store in an inventory.


