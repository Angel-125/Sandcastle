# Sandcastle


# PartModules.WBIModuleEVAVariants
            
This helper part module makes it possible to change part variants during EVA Construction.
        
## Methods


### enableVariantSwitching
Enables in-flight variant switching

### disableVariantSwitching
Disables in-flight variant switching

# PartModules.EVAConstructionBridge
            
Shared state and stock-call adapters for vessel-hosted EVA Construction.
        
## Methods


### IsStackNodeAlignmentEnabled
Reports whether the active construction path has opted into complete stack-node alignment.

### Activate(Sandcastle.PartModules.WBIEVAConstructionManipulator)
Makes a part module the active stock-construction host and hides conflicting flight UI.

### Deactivate(Sandcastle.PartModules.WBIEVAConstructionManipulator)
Releases the active host and restores every flight UI state captured during activation.

### LockVesselControls
Blocks flight-control inputs without locking camera, PAW, editor, pause, save, or scene controls.

### UnlockVesselControls
Removes only the flight-control lock owned by vessel-hosted EVA Construction.

### CloseHostedConstruction
Closes the stock construction panel when it was opened by a vessel-mounted host.

### MaintainHostUI
Reapplies hidden UI states that stock flight code may change while parts are attached.

### HideStagingQuadrant
Captures and collapses the lower-left staging controls.

### RestoreStagingQuadrant
Returns the staging quadrant to the state it had before construction opened.

### HideFlightModeFrame
Hides the flight-mode buttons without deactivating their stock transition object.

### RestoreFlightModeFrame
Restores the flight-mode transition and CanvasGroup values captured on activation.

### GetManipulatorConstructionWeightLimit
Converts the host's configurable metric-ton mass limit into stock's local-weight limit.

### IsUnderManipulatorMassLimit(Part)
Tests a candidate part's dry and resource mass against the active host's mass limit.

### GetConstructionDistance
Returns the active manipulator's construction distance or stock's distance for ordinary EVA.

### GetInventoryDistance
Returns the host workspace distance for hosted inventory access or stock's inventory distance otherwise.

### GetInventoryDisplayDistance(UnityEngine.Vector3,UnityEngine.Vector3)
Returns zero distance for inventories on the active host vessel so its entire storage network remains visible.

### IsHostInventory(ModuleInventoryPart)
Reports whether an inventory belongs to the vessel hosting the active construction manipulator.

### IsHostInventoryPosition(UnityEngine.Vector3)
Reports whether a stock inventory-display position belongs to an inventory on the host vessel.

### GetInventoryPosition(ModuleInventoryPart)
Reproduces the position stock uses when measuring an inventory for construction display.

### PositionsMatch(UnityEngine.Vector3,UnityEngine.Vector3)
Compares inventory positions with enough tolerance for transforms updated during the current frame.

### CanOpenConstructionPanel
Reproduces stock panel-opening guards that remain relevant without an EVA vessel.

### IsConstructionVessel(Vessel)
Treats the active host vessel as EVA only for patched construction-workspace checks.

### GetConstructionOrigin(Vessel)
Returns the host model transform position, falling back to stock vessel positioning.

### GetInventoryOrigin
Returns the position from which stock should measure access to nearby construction inventories.

### GetConstructionOriginFromTransform(UnityEngine.Transform)
Substitutes the hosted construction origin while preserving the transform supplied by ordinary stock EVA.

### GetConstructionReferenceTransform(Vessel)
Returns the host model transform used to orient stock placement calculations.

### InterruptWeld(KerbalEVA)
Calls the stock weld-interruption path when a real KerbalEVA controller exists.

### Weld(KerbalEVA,Part)
Calls the stock weld path when construction is genuinely hosted by a KerbalEVA.

### ClearHostedAttachmentHighlights
Restores normal flight highlighting on parts attached by a vessel-hosted construction session.

# PartModules.EVAConstructionHarmonyLoader
            
Installs Sandcastle's opt-in patches for the stock EVA Construction editor.
        
## Methods


### Awake
Installs Harmony patches and subscribes the bridge to stock construction lifecycle events.

### OnDestroy
Unsubscribes lifecycle events and restores any UI still owned by an active host.

### LateUpdate
Enforces hosted-construction UI state after stock flight UI has updated for the frame.

### OnEVAConstructionMode(System.Boolean)
Releases the bridge whenever the stock construction panel reports that it closed.

### OnVesselChange(Vessel)
Closes part-hosted construction if the final active vessel differs from the host vessel.

### OnVesselSwitching(Vessel,Vessel)
Closes part-hosted construction at the start of a loaded or unloaded vessel switch.

### OnCrewOnEva(GameEvents.FromToAction{Part,Part})
Closes part-hosted construction whenever any crew member goes on EVA.

### OnGameSceneLoadRequested(GameScenes)
Releases hosted construction before leaving the current KSP scene.

# PartModules.EVAConstructionStackNodeAlignmentPatch
            
Gives vessel-hosted stack attachment the deterministic node-frame roll alignment used by KIS.
        
## Methods


### TargetMethod
Locates stock's private attachment test so its completed stack result can be adjusted.

### Postfix(EVAConstructionModeEditor,Part,Attachment)
Initializes roll when a new stack-node pair is acquired without overriding later player rotations.

### ResetTracking
Clears the remembered node pair so the next snap receives a fresh initial alignment.

### IsTrackedPair(EVAConstructionModeEditor,Part,AttachNode,Part,AttachNode)
Reports whether stock is still evaluating the node pair that was already initialized.

### TrackPair(EVAConstructionModeEditor,Part,AttachNode,Part,AttachNode)
Remembers a node pair before attempting alignment to avoid repeated warnings on bad nodes.

### TryGetSourceNodeLocalRotation(Part,AttachNode,UnityEngine.Quaternion@)
Gets the source node's complete frame relative to the selected part.

### TryGetTargetNodeWorldRotation(Part,AttachNode,UnityEngine.Quaternion@)
Gets the target node's complete world-space frame, including its vessel-relative roll.

### TryCreateNodeRotation(UnityEngine.Vector3,UnityEngine.Quaternion@)
Creates the same orientation-based node frame that KIS uses for config-defined nodes.

### IsUsable(UnityEngine.Quaternion)
Rejects invalid quaternion values before they can corrupt the selected part transform.

### IsFinite(System.Single)
Reports whether a floating-point component is neither NaN nor infinite.

# PartModules.EVAConstructionUnderwaterProtoVesselPatch
            
Allows submerged EVA actors to place loose parts on the seabed and applies persistent zero buoyancy.
        
## Methods


### TargetMethod
Locates the stock method that builds the proto-vessel for a loose construction part.

### Prefix(EVAConstructionModeEditor,UnityEngine.Vector3,Vessel,Part,Sandcastle.PartModules.EVAConstructionUnderwaterProtoVesselPatch.VesselSituationState@)
Makes stock's landed-only serializer handle a valid seabed target selected by a splashed construction host.

### Postfix(Vessel,ConfigNode)
Applies the underwater policy using the stock construction actor supplied to the spawn method.

### Finalizer(System.Exception,Sandcastle.PartModules.EVAConstructionUnderwaterProtoVesselPatch.VesselSituationState)
Restores the construction host's live state after all proto-vessel postfixes, including on failure.

### IsSplashedSeabedPlacement(EVAConstructionModeEditor,UnityEngine.Vector3,Vessel,Part)
Reports whether a splashed EVA or active manipulator is placing a part on terrain below an ocean surface.

# PartModules.EVAConstructionUnderwaterProtoVesselPatch.VesselSituationState
            
Remembers the live EVA vessel state while stock serializes a seabed placement as landed.
        

# PartModules.EVAConstructionUnderwaterAttachedPartPatch
            
Adds persistent zero buoyancy to parts attached by landed underwater EVA Construction actors.
        
## Methods


### TargetMethod
Locates the stock method that converts a held cargo part into a live attached part.

### Postfix(Part)
Applies the underwater policy after stock has created and welded the attached part.

# PartModules.EVAConstructionGroundPartDeploymentPatch
            
Converts vessel-hosted terrain placement of a ground part into stock's ground-deployment state.
        
## Methods


### TargetMethod
Locates the stock method that builds the proto-vessel for a part dropped by EVA Construction.

### Postfix(EVAConstructionModeEditor,Part,ConfigNode)
Gives a hosted, terrain-placed ModuleGroundPart the same startup state as an inventory deployment.

### IsTerrainPlacement(EVAConstructionModeEditor,Part)
Recovers stock ground placement when its cursor ray misses but the fallback placement plane leaves the part on terrain.

# PartModules.EVAConstructionCargoPartHighlightPatch
            
Makes mounted cargo parts use the manipulator origin and range when deciding construction eligibility.
        
## Methods


### TargetMethod
Locates the stock cargo-part update that highlights parts eligible for vessel detachment.

### Transpiler(System.Collections.Generic.IEnumerable{HarmonyLib.CodeInstruction})
Replaces the active-vessel origin and Kerbal range while leaving ordinary EVA behavior unchanged.

# PartModules.EVAConstructionInventoryDisplayPatch
            
Makes the stock construction inventory list use the vessel-hosted workspace origin and distance.
        
## Methods


### TargetMethod
Locates the stock method that adds and removes inventory panes as their distance changes.

### Transpiler(System.Collections.Generic.IEnumerable{HarmonyLib.CodeInstruction})
Replaces the active-vessel origin and fixed EVA inventory radius used by the stock inventory pane.

# PartModules.EVAConstructionInventoryInteractionPatch
            
Lets stock inventory slot interactions use the same range calculation as the hosted inventory display.
        
## Methods


### Prefix(ModuleInventoryPart,System.Boolean@)
Evaluates inventory access from the manipulator instead of stock's absent EVA Kerbal.

# PartModules.EVAGroundPartPickupVolumeMessagePatch
            
Replaces stock's generic deployed-part pickup capacity warning when packed volume is the constraint.
        
## Methods


### TargetMethod
Locates the protected stock capacity check used by ModuleGroundPart.RetrievePart.

### Prefix(ModuleGroundPart,System.Boolean@)
Reports the live, ModuleManager-adjusted EVA inventory volume limit and suppresses stock's vague warning.

# PartModules.EVAConstructionGroundPartPickupPatch
            
Routes a simple deployed ground part through stock's loose-part pickup and inventory cursor workflow.
        
## Methods


### TargetMethod
Locates stock's click-to-pick-up implementation.

### Prefix(Sandcastle.PartModules.EVAConstructionGroundPartPickupPatch.GroundPartPickupState@)
Temporarily presents a deployed ground part as loose cargo so stock can create the held inventory part.

### Postfix(EVAConstructionModeEditor,Sandcastle.PartModules.EVAConstructionGroundPartPickupPatch.GroundPartPickupState)
Keeps the cargo state after a successful pickup, or restores the deployed state if stock rejected it.

# PartModules.EVAConstructionGroundPartPickupPatch.GroundPartPickupState
            
Stores the original ground state so a rejected pickup can leave the deployed vessel untouched.
        

# PartModules.WBIEVAConstructionManipulator
            
Allows a vessel-mounted manipulator to act as the origin for stock EVA Construction. This is an experimental module and requires the Sandcastle Harmony bridge.
        
## Fields

### constructionTransformName
Model transform used as the center of the stock construction workspace.
### maxPartMass
Maximum movable part mass, including resources, in metric tons.
### maxConstructionDistance
Maximum distance from the construction transform at which parts can be manipulated, in meters.
## Properties

### ConstructionTransform
World-space transform used as the construction origin.
## Methods


### ToggleEVAConstruction
Opens or closes the stock EVA Construction interface using this part as its host.

### OnStart(PartModule.StartState)
Resolves the configured construction transform and initializes PAW visibility.
> #### Parameters
> **state:** KSP's current part-module startup state.


### OnDestroy
Releases this part as the construction host if its part or vessel is destroyed.

# Inventory.InventoryUtils
            
An inventory helper class
        
## Methods


### GetPartFromAvailablePart(AvailablePart)
Retrieves an instantiated part from the supplied available part.
> #### Parameters
> **availablePart:** The AvailablePart

> #### Return value
> 

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

### SpawnPart(AvailablePart,Part,UnityEngine.Transform,System.Boolean,Callback{DockedVesselInfo})
Spawns a completed print at a printer transform and optionally couples it to the printer vessel.
> #### Parameters
> **availablePart:** The part definition to place into the world.

> **parentPart:** The printer part whose vessel supplies the spawn environment.

> **dropTransform:** The printer transform that defines position and orientation.

> **repositionPart:** Whether to move the print beyond the spawn boundary.

> **onPartCoupled:** An optional callback used after the new part is coupled.


### stabilizeSpawnedPart(Vessel,Part,UnityEngine.Transform,UnityEngine.Vector3,UnityEngine.Quaternion,Callback{DockedVesselInfo})
Keeps a newly spawned orbital part synchronized with the live printer frame until KSP has initialized it and it can safely enter physics.

### TryGetPartLocalBounds(Part,UnityEngine.Transform,UnityEngine.Bounds@)
Calculates the bounds of a part in the coordinate system of the supplied reference transform.

### movePartAboveTerrain(UnityEngine.Vector3@,UnityEngine.Quaternion,UnityEngine.Bounds,CelestialBody)
Raises a prospective part placement until its lowest bounds point is one meter above the local terrain.

### TryPositionShipConstruct(ShipConstruct,Part,UnityEngine.Transform,System.Boolean,UnityEngine.Vector3@,UnityEngine.Quaternion@,UnityEngine.Bounds@)
Positions an unassembled craft relative to a printer and optionally keeps its complete bounds beyond the spawn transform's virtual boundary.

### TryPositionLandedShipConstruct(ShipConstruct,Part,UnityEngine.Transform,System.Boolean,UnityEngine.Vector3@,UnityEngine.Quaternion@,UnityEngine.Bounds@)
Uses KSP's ground-placement logic on an unassembled craft and captures the resulting transform relative to the printer's spawn transform.

### TryGetConstructLocalBounds(ShipConstruct,UnityEngine.Transform,UnityEngine.Bounds@)
Calculates construct bounds in the coordinate system of a supplied reference transform without requiring an assembled Vessel.

### TryGetConstructBounds(ShipConstruct,UnityEngine.Bounds@)
Calculates world-space bounds for a ShipConstruct before it has been assembled into a Vessel. Collider bounds are preferred because they match the coordinate space used by the spawn collision checks.

### GetBounds(Part)
Courtesy of MechJeb by Sarbian Licensed under GPLV3 Computes the Bounds of the supplied part. This works both in the editor and flight. EX: Bounds partBounds = FlightGlobals.ActiveVessel.rootPart.GetBounds();
> #### Parameters
> **part:** A Part object to compute the Bounds for.

> #### Return value
> A Bounds object containing the part's bounds.

### GetBounds(Vessel)
Courtesy of MechJeb by Sarbian Licensed under GPLV3 Computes the Bounds of the supplied vessel. This works both in the editor and in flight. EX: Bounds vesselBounds = FlightGlobals.ActiveVessel.GetBounds();
> #### Parameters
> **vessel:** A Vessel object to compute the bounds for.

> #### Return value
> A Bounds object containing the vessel's bounds.

### releaseOrbitalPrintedPart(Part,DockedVesselInfo,Part,UnityEngine.Transform,System.Boolean)
Releases a printed part in orbit while preserving its position and synchronizing its orbit and velocity with the printing vessel.
> #### Parameters
> **rootPart:** The root part of the coupled printed part.

> **dockedVesselInfo:** The information used to undock the part.

> **parentPart:** A part on the printing vessel.

> **anchorTransform:** The transform used to position the printed part.

> **switchToVessel:** Whether to make the released part's vessel active.


### normalizePersistentStringFields(Part)
Replaces null persistent string fields with empty strings before KSP serializes a part prefab into an inventory snapshot. ConfigNode cannot store null strings, and part modules may legitimately leave an optional persistent string unset until it is first used.
> #### Parameters
> **partPrefab:** The part prefab that KSP is about to store in an inventory.


# Inventory.ModuleCargoCatcher
            
Catches and stores cargo items into the part's inventory as long as they fit. Does not require a kerbal. This only works on single-part vessels. Note that you'll need a trigger collider set up in the part containing this part module in order to trigger the catch and store operation.
        
## Fields

### deployAnimationName
Optional name of the animation to play when preparing the catcher to catch cargo parts.
### canCatchParts
Flag to indicate that we can catch parts.
## Methods


### ArmCatcher
Arms the catcher, enabling it to catch parts.

### DisarmCatcher
Disarms the catcher, preventing it from catching parts.

# Inventory.ModuleDefaultInventoryStack
            
ModuleInventoryPart's DEFAULTPARTS doesn't support stacked parts. This part module gets around the problem. Add this part module AFTER ModuleInventoryPart and part stacks will be filled out to their max stack size in the editor.
        
## Fields

### inventoryInitialized
Flag to indicate that the part's stackable inventory items has been initialized.

# Inventory.ModuleCargoDispenser
            
The stock EVA Construction system lets you drag and drop inventory parts onto the ground, but it requires a kerbal to do so. This part module enables non-kerbal parts to remove items from the part's inventory and drop them onto the ground. This code is based on vessel creation code from Extraplanetary Launchpads by Taniwha and is used under the GNU General Public License.
        
## Fields

### dropTransformName
Name of the transform where dropped cargo items will appear.
### animationName
Optional name of the animation to play when dropping an item.
## Methods


### DropPart
Drops the desired item.

### ChangePartToDrop
Changes the desired item to drop.

### ChangePartToDrop(System.Int32)
Changes the desired item to drop to the desired inventory slot index (if it exists).
> #### Parameters
> **inventoryIndex:** An int containing the inventory index of the item to drop.


### DropPart(System.Int32)
Drops the item in the desired inventory index (if it exists)
> #### Parameters
> **inventoryIndex:** An int containing the index of the inventory item to drop.


### DropPart(AvailablePart)
Drops the desired part if it is in the inventory.
> #### Parameters
> **availablePart:** An AvailablePart containing the item to drop.


# WBIGroundPlatform
            
Levels a deck above a ground-attached pivot and projects visual pylons into the terrain.
        
## Fields

### pivotTransformName
Transform used as the invisible mast pivot. If empty, the part transform is used.
### deckTransformName
Transform that represents the level platform deck.
### mastHeight
Height above the pivot point where the deck is placed after leveling.
### minMastHeight
Minimum in-flight mast height allowed by the PAW slider.
### maxMastHeight
Maximum in-flight mast height allowed by the PAW slider.
### mastHeightStep
Increment used by the in-flight mast height PAW slider.
### pylonShaftTransformNames
Comma-separated list of pylon shaft transforms. Each shaft raycasts along its local +Z axis.
### pylonFootTransformNames
Comma-separated list of pylon foot transforms, matching pylonShaftTransformNames by index.
### pylonAxis
Local shaft axis used for pylon length and terrain raycasts. Defaults to +Z.
### footUpAxis
Local foot axis that should point away from the terrain surface. Defaults to +Y.
### pylonMaxLength
Maximum raycast distance used to find terrain beneath each pylon.
### pylonEmbedDepth
Extra distance pushed below the terrain hit point so feet look embedded.
### pylonModelLength
Original model length represented by a shaft scale of 1.
### attachNodeNames
Comma-separated attach node names that should be synced after deck alignment.
### attachNodeMarkerNames
Comma-separated marker transform names that drive attachNodeNames by index.
### showRealignEvent
Enables a PAW event that reruns alignment when the configured attach nodes are empty.
### debugLog
Enables concise diagnostics for platform alignment.
### platformAligned
Indicates that the platform has solved and saved its deck and pylon transforms.
### savedDeckLocalPosition
Saved local deck position.
### savedDeckLocalRotation
Saved local deck rotation.
### savedPylonLocalPositions
Saved local pylon shaft positions.
### savedPylonLocalRotations
Saved local pylon shaft rotations.
### savedPylonLocalScales
Saved local pylon shaft scales.
### savedFootLocalPositions
Saved local pylon foot positions.
### savedFootLocalRotations
Saved local pylon foot rotations.
## Methods


### OnStart(PartModule.StartState)
Resolves configured transforms and restores saved geometry when the vessel loads.

### OnDestroy
Removes PAW field callbacks when KSP destroys the part module.

### OnUpdate
Waits for stock ModuleGroundPart to finish static attachment before doing first-time alignment.

### AlignPlatform
Manually reruns deck and pylon alignment when no configured attach nodes are occupied.

### AlignAfterGroundAttach
Delays alignment until the stock ground part coroutine has had time to freeze the vessel.

### RestoreSavedPlatform
Restores saved deck, pylon, and attach-node geometry after KSP rebuilds the loaded vessel.

### ResolveConfiguration
Resolves all named model transforms and comma-separated node identifiers.

### ConfigureMastHeightSlider
Configures the PAW slider from part config and subscribes to height changes.

### UnsubscribeMastHeightField
Removes the mast height callback from the PAW field.

### UpdateMastHeightFieldVisibility
Shows the mast height slider only when the platform can safely move its attach nodes.

### OnMastHeightModified(System.Object)
Re-solves the deck, pylons, and attach nodes when the PAW slider changes mast height.

### TryAlignPlatform
Levels the deck, projects pylons along each shaft's local pylon axis, and updates free attach nodes.

### ProjectPylon(System.Int32,UnityEngine.Vector3)
Raycasts one pylon along its shaft axis, extends the shaft, and aligns its foot to terrain.

### SyncAttachNodesFromMarkers(System.Boolean)
Updates configured AttachNodes from marker transforms, optionally requiring the nodes to be empty.

### AreAttachNodesFree
Reports whether every configured AttachNode is empty and safe to move.

### SavePlatformState
Saves local transform state so the solved platform geometry survives reload.

### IsStaticGroundPart
Reports whether stock has completed the ModuleGroundPart static-attach sequence.

### ResolveTransforms(System.String)
Finds every transform listed in a comma-separated field.

### SplitNames(System.String)
Splits comma-separated config names while trimming whitespace and empty entries.

### ScaleAlongDominantAxis(UnityEngine.Vector3,UnityEngine.Vector3,System.Single)
Scales the component that most closely matches the configured pylon axis.

### GetLocalScales(UnityEngine.Transform[])
Captures model-authored local scales so pylon extension can be applied relatively.

### ApplySavedTransform(UnityEngine.Transform,UnityEngine.Vector3,UnityEngine.Quaternion)
Applies saved local position and rotation to one transform.

### ApplySavedTransforms(UnityEngine.Transform[],System.String,System.String,System.String)
Applies saved local transform lists to a matching transform array.

### WriteLocalPositions(UnityEngine.Transform[])
Serializes local positions for a transform list.

### WriteLocalRotations(UnityEngine.Transform[])
Serializes local rotations for a transform list.

### WriteLocalScales(UnityEngine.Transform[])
Serializes local scales for a transform list.

### ReadVectorList(System.String)
Parses a semicolon-separated Vector3 list.

### ReadQuaternionList(System.String)
Parses a semicolon-separated Quaternion list.

### LogFailure(System.String)
Logs an alignment failure when diagnostics are enabled.

# WBIGroundPartPositionStabilizer
            
Persists the final settled pose of a stock ModuleGroundPart and restores it after KSP reloads the vessel.
        
## Fields

### hasStableGroundPose
Indicates that this part has recorded the final static-attached ground pose.
### stabilizationEnabled
Indicates that this ground part was deployed while the stabilizer was installed.
### stableLatitude
Latitude of the vessel origin after the ground part has settled.
### stableLongitude
Longitude of the vessel origin after the ground part has settled.
### stableAltitude
Absolute altitude of the vessel origin after the ground part has settled.
### stableTerrainOffset
Height of the vessel origin above the PQS terrain at the saved latitude and longitude.
### debugLog
Enables concise diagnostics for saved and restored ground poses.
## Methods


### OnStart(PartModule.StartState)
Locates the stock ground part module and schedules a post-load restore when a saved pose is available.

### OnUpdate
Watches newly deployed ground parts until stock has finished static-attaching them, then records their pose.

### StartPoseRestore
Starts the delayed restore coroutine once per vessel load.

### RestoreSavedPose
Waits for stock ground positioning to complete and then restores the saved terrain-relative position.

### CaptureSettledPose
Lets stock complete its deployment coroutine, then captures the settled position for future reloads.

### CaptureCurrentPose
Captures the current vessel origin as both an absolute altitude and a terrain-relative offset.

### GetSavedWorldPosition(CelestialBody)
Rebuilds the saved world position from the current PQS surface plus the saved terrain offset.

### GetSurfaceAltitude(CelestialBody,System.Double,System.Double)
Gets the current PQS terrain altitude at the supplied latitude and longitude.

### CanUseStablePose
Reports whether the saved pose has enough data to restore a landed ground part.

### EnableStabilizer(System.String)
Enables stabilization only after this module observes a fresh ModuleGroundPart deployment.

### IsBeingDeployed
Reports whether stock ModuleGroundPart is currently performing its initial deployment.

### IsStaticGroundPart
Reports whether stock has completed the ModuleGroundPart deployment/static-attach sequence.

# WBIUnderwaterSpawnBuoyancy
            
Persists a zero-buoyancy adjustment made when a part is placed by underwater construction.
        
## Fields

### disableBuoyancy
Indicates that this specific part instance was placed by landed underwater construction.
## Methods


### OnLoad(ConfigNode)
Reapplies zero buoyancy while KSP is restoring the part from its snapshot.

### OnStart(PartModule.StartState)
Reapplies zero buoyancy after the part has completed normal flight startup.

### DisableBuoyancy
Sets the owning part's stock buoyancy multiplier to zero when the part is available.

# UnderwaterSpawnUtils
            
Applies Sandcastle's shared underwater spawn policy to proto and live parts.
        
## Methods


### ShouldDisableBuoyancy(Vessel)
Reports whether the actor is exactly landed beneath an ocean surface and the feature is enabled.

### ApplyToProtoVessel(ConfigNode,Vessel,System.String)
Adds the persistent zero-buoyancy marker to every part in a new proto-vessel.

### ApplyToPart(Part,Vessel,System.String)
Sets a newly attached live part to zero buoyancy and adds the persistent marker module.

### FindBuoyancyMarker(ConfigNode)
Finds the persistent zero-buoyancy marker in a proto-part snapshot.

### LogAdjustment(Vessel,System.String)
Writes a concise diagnostic describing why the new part received zero buoyancy.

# PrintShop.ShipbreakerUI
            
Represents the Print Shop UI
        
## Fields

### recycleQueue
Represents the list of build items to recycle.
### jobStatus
Status of the current print job.
### onCancelVesselBuild
Callback to tell the controller to cancel the build.
### isRecycling
Flag indicating that the printer is recycling
### part
The Part associated with the UI.
### showDecoupleButton
Flag to indicate whether or not to show the decouple button.
### craftName
Name of the craft being printed.
### estimatedCompletion
Estimated time to completion of the vessel.
### createAlarm
Flag to indicate if an alarm shoudl be created for print job completion.
### onRecycleStatusUpdate
Callback to let the controller know about the recycle state.
### resourceRecylePercent
Percentage of the resources that can be recycled.
### supportShipbreakers
List of support shipbreakers
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


# PrintShop.SpawnShipDelegate
            
Asks the delegate to spawn the ship that's just been printed.
        

# PrintShop.DecoupleShipDelegate
            
Asks the delegate to decouple the ship that's just been printed.
        

# PrintShop.SelectShipDelegate
            
Delegate to get the ship to print.
        

# PrintShop.CancelBuildDelegate
            
Delegate to cancel the build.
        

# PrintShop.ShipwrightUI
            
Represents the Print Shop UI
        
## Fields

### printQueue
Represents the list of build items to print.
### jobStatus
Status of the current print job.
### onPrintStatusUpdate
Callback to let the controller know about the print state.
### gravityRequirementsMet
Callback to see if the part's gravity requirements are met.
### pressureRequrementsMet
Callback to see if the part's pressure requirements are met.
### onSpawnShip
Callback to let the controller to spawn the printed ship.
### onDecoupleShip
Callback to let the controller to decouple the printed ship.
### onOpenCraftBrowser
Callback to select a ship to print.
### onCancelVesselBuild
Callback to tell the controller to cancel the build.
### isPrinting
Flag indicating that the printer is printing
### part
The Part associated with the UI.
### showSpawnButton
Flag to indicate whether or not to show the spawn button.
### showDecoupleButton
Flag to indicate whether or not to show the decouple button.
### craftName
Name of the craft being printed.
### estimatedCompletion
Estimated time to completion of the vessel.
### createAlarm
Flag to indicate if an alarm shoudl be created for print job completion.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


# PrintShop.RecyclerUI
            
Represents the Print Shop UI
        
## Fields

### titleText
Title of the selection dialog.
### partsList
Complete list of recyclable parts.
### recycleQueue
Represents the list of build items to print.
### jobStatus
Status of the current print job.
### onRecycleStatus
Callback to let the controller know about the print state.
### isRecycling
Flag indicating that the printer is printing
### part
The Part associated with the UI.
### recyclePercentage
How much of the part's resources are recycled.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


### updatePartPreview(System.Int32)
Updates the part preview
> #### Parameters
> **partIndex:** An Int containing the index of the part to preview


### updateThumbnails
Updates the part thumbnails

# PrintShop.UpdatePrintStatusDelegate
            
Callback to let the controller know about the print state.
        

# PrintShop.GravityRequirementsMetDelegate
            
Asks the delegate if the minimum gravity requirements are met.
            
> **minimumGravity:** A float containing the minimum required gravity.

            
> true if the requirement can be met, false if not.
        

# PrintShop.PressureRequirementMetDelegate
            
Asks the delegate if the minimum pressure requirements are met.
            
> **minimumPressure:** A float containing the minimum required pressure.

            
> true if the requirement can be met, false if not.
        

# PrintShop.SpawnPartDelegate
            
Asks the delegate to spawn the current part that's just been printed.
        

# PrintShop.PrintShopUI
            
Represents the Print Shop UI
        
## Fields

### partsList
Complete list of printable parts.
### printQueue
Represents the list of build items to print.
### jobStatus
Status of the current print job.
### onPrintStatusUpdate
Callback to let the controller know about the print state.
### gravityRequirementsMet
Callback to see if the part's gravity requirements are met.
### pressureRequrementsMet
Callback to see if the part's pressure requirements are met.
### onSpawnPrintedPart
Callback to let the controller to spawn the printed part.
### onDecouplePrintedPart
Callback to release an orbital printed part from the printer.
### isPrinting
Flag indicating that the printer is printing
### part
The Part associated with the UI.
### whitelistedCategories
Whitelisted categories that the printer can print from.
### showPartSpawnButton
Flag to indicate whether or not to show the part spawn button.
### showPartDecoupleButton
Flag indicating whether to show the printed-part release button.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


### updateVisibleCategories
Builds the toolbar from categories that contain at least one part in the printer's filtered list.

### selectPopulatedCategory
Retains the current selection when populated, otherwise selects the first visible category.

### hasPrintableStockCategory(System.String)
Determines whether the filtered printable list contains a stock part category.
> #### Parameters
> **category:** The stock category identifier.

> #### Return value
> True when at least one printable part belongs to the category.

### hasPrintableCCKCategory(System.String)
Determines whether the filtered printable list contains a Community Category Kit tag.
> #### Parameters
> **category:** The Community Category Kit category identifier.

> #### Return value
> True when at least one printable part carries the category tag.

# PrintShop.WBIShipbreaker
            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### recycleSpeedUSec
The number of resource units per second that the recycler can recycle.
### UseSpecialistBonus
Flag to indicate whether or not to allow specialists to improve the recycle speed. Exactly how the specialist(s) does that is a trade secret.
### SpecialistBonus
Per experience rating, how much to improve the recycle speed by. The print shop part must have crew capacity.
### ExperienceEffect
The skill required to improve the recycle speed.
### runningEffect
Name of the effect to play from the part's EFFECTS node when the printer is running.
### recyclePercentage
What percentage of resources will be recycled.
### animationName
Name of the animation to play during printing.
### vesselCaptureEnabled
Flag to indicate if vessel capture is enabled.
### maxBuildingDistance
Maximum distance allowed for other shipbreakers to help break up a vessel.
### recycleQueue
Represents the list of build items to recycle.
### recycleState
Current state of the recycler.
### recycleStatusText
status text.
### lastUpdateTime
Describes when the recycler was last updated.
### currentJob
Current job being recycled.

# PrintShop.WBICargoRecycler
            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### recycleSpeedUSec
The number of resource units per second that the recycler can recycle.
### UseSpecialistBonus
Flag to indicate whether or not to allow specialists to improve the recycle speed. Exactly how the specialist(s) does that is a trade secret.
### SpecialistBonus
Per experience rating, how much to improve the recycle speed by. The print shop part must have crew capacity.
### ExperienceEffect
The skill required to improve the recycle speed.
### runningEffect
Name of the effect to play from the part's EFFECTS node when the printer is running.
### recyclePercentage
What percentage of resources will be recycled.
### animationName
Name of the animation to play during printing.
### recycleQueue
Represents the list of build items to recycle.
### recycleState
Current state of the recycler.
### lastUpdateTime
Describes when the recycler was last updated.
### currentJob
Current job being recycled.

# PrintShop.BuildItem
            
Represents an item that needs to be built.
        
## Fields

### kBuildItemNode
Build item node identifier
### partName
Name of the part being built.
### availablePart
The Available part representing the build item.
### materials
List of resource materials required. Rate in this context represents the amount of the resource required in order to complete the part.
### requiredComponents
List of parts required to complete the build item. The parts must be in the vessel inventory.
### totalUnitsRequired
Total units required to produce the item, determined from all required resources.
### totalUnitsPrinted
Total units printed to date, determined from all required resources.
### isBeingRecycled
Flag indicating whether or not the part is being recycled.
### minimumGravity
The mininum gravity, in m/sec^2, that the part requires in order for the printer to print it. If set to 0, then the printer's vessel must be orbiting, sub-orbital, or on an escape trajectory, and not under acceleration. The default is -1, which ignores this requirement.
### minimumPressure
The minimum pressure, in kPA, that the part required in order for the printer to print it. If set to > 1, then the printer's vessel must be in an atmosphere or submerged. If set to 0, then the printer's vessel must be in a vacuum.
### removeResources
Determines whether or not the printer should remove the part's resources before placing the printed part in an inventory.
### variantIndex
Index of the part variant to use (if any).
### packedVolume
Volume of the item being printed.
### isBlacklisted
Flag indicating if the part is blacklisted or not. If blacklisted then it can't be printed by a shipwright printer.
### mass
Mass of the part including variant.
### unpackedVolume
Volume of the part when unpacked.
### isUnpacked
Flag to indicate whether or not the part is unpacked.
### flightId
ID of the part.
### waitForSupportCompletion
Flag to wait for a support unit to complete the job.
### skipInventoryAdd
Flag to indicate whether or not to add the item to the inventory when printing has completed. This is used by printers that are supporting a lead Shipwright. Instead of storing the part, they hand it over to the lead Shipwright for inclusion in a vessel.
## Methods


### Constructor
Constructs a new build item from the supplied config node.
> #### Parameters
> **node:** A ConfigNode containing data for the build item.


### Constructor
Constructs a build item from the supplied available part.
> #### Parameters
> **availablePart:** The AvailablePart to base the build item on.


### Save
Saves the build item.
> #### Return value
> A ConfigNode containing serialized data.

# PrintShop.MaterialsList
            
Represents a list of resources needed to build an item of a particular part category.
        
## Fields

### kMaterialsListNode
Node ID for a materials list.
### kTechNodeMaterials
Node ID for tech node materials. Parts in a specific tech node can require additional materials.
### kDefaultMaterialsListName
Name of the default materials list.
### kResourceNode
Represents a resource node.
### name
Name of the materials list. This should correspond to one of the part categories.
### materials
List of resource materials required.
### requiredComponents
List of components required by the materials list.
### materialsLists
A map of all materials lists, keyed by part category name.
## Methods


### LoadLists
Loads the materials lists that specify what materials are required to produce an item from a particular category.
> #### Return value
> A Dictionary containing the list names as keys and MaterialList objects as values.

### GetListForCategory(System.String)
Returns the materials list for the requested category, or the default list if the list for the requested category doesn't exist.
> #### Parameters
> **categoryName:** A string containing the desired category.

> #### Return value
> A MaterialsList if one exists for the desired category, or the default list.

### GetDefaultList
Creates the default materials list.
> #### Return value
> A MaterialsList containing the default materials.

# PrintShop.WBIPrintShop
            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### printShopGUIName
GUI name to use for the event that opens the printer GUI.
### printShopwGroupDisplayName
Alternate group display name to use.
### printShopDialogTitle
Title to use for the print shop dialog
### printStateString
Current print state.
### enablePartSpawn
Flag indicating that part spawn is enabled. This lets the printer spawn parts into the world instead of putting them into an inventory.
### maxPartDimensions
Maximum possible craft size that can be printed: Height (X) Width (Y) Length (Z). Leave empty for unlimited printing.
### repositionCraftBeforeSpawning
Flag to indicate if it should offset the printed vessel to avoid collisions. Recommended to set to FALSE for printers with enclosed printing spaces.

# PrintShop.WBIModuleEVAPrintShop
            
Provides a KerbalGear-activated print shop that consumes resources exposed on an EVA Kerbal and stores completed cargo parts in the Kerbal's inventory.
        
## Fields

### printShopGUIName
Localized label used by the event that opens the EVA print-shop window.
### printShopDialogTitle
Localized title used by the EVA print-shop window.
### maxPartDimensions
Maximum printable-part dimensions expressed as Height (X), Width (Y), Length (Z). Leave empty to restrict printing by volume only.
### printStateString
Current state displayed in the EVA Kerbal's part action window.
## Methods


### OnAwake
Creates the reusable print-shop UI and connects it to this printer's queue.

### OnStart(PartModule.StartState)
Initializes the EVA printer after KSP has loaded its configured fields.
> #### Parameters
> **state:** KSP's current part-module startup state.


### OnLoad(ConfigNode)
Keeps the UI attached to the queue instance restored by KSP.
> #### Parameters
> **node:** The module's saved or prefab configuration.


### OnActive
Activates the printer when its KerbalGear item is present in the EVA inventory.

### OnInactive
Deactivates the printer and cancels its queue when the enabling gear is removed. Retained gear no longer receives lifecycle callbacks during ordinary inventory refreshes.

### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Refreshes the printer's inventory reference and UI wiring after a retained gear item changes.
> #### Parameters
> **changedInventory:** The EVA inventory whose contents changed.


### OnUpdate
Updates PAW state only while the EVA printer gear is active.

### FixedUpdate
Advances printing only while the EVA printer gear is active.

### onVesselChange(Vessel)
Closes the printer window when focus changes to another vessel.
> #### Parameters
> **newVessel:** The newly active vessel.


### OnDestroy
Cancels pending jobs when the EVA vessel is destroyed, including when the Kerbal boards.

### GetModuleDisplayName
Returns the localized module title shown by KSP.
> #### Return value
> The EVA print-shop title.

### buildItemCompleted(Sandcastle.PrintShop.BuildItem)
Adds a completed cargo part to the EVA Kerbal's inventory.
> #### Parameters
> **buildItem:** The completed print job.


### OpenGUI
Toggles the EVA print-shop window while its printer gear is active.

### updateUIStatus(System.String)
Updates the print-job status shown in the shared print-shop UI.
> #### Parameters
> **statusUpdate:** The new localized or formatted status.


### updateUIStatus(System.Boolean)
Updates the running state shown in the shared print-shop UI.
> #### Parameters
> **isPrinting:** Whether the current queue is printing.


### spaceRequirementsMet(Sandcastle.PrintShop.BuildItem)
Verifies that the EVA inventory has room for the completed cargo part.
> #### Parameters
> **buildItem:** The print job whose output needs inventory space.

> #### Return value
> True when an inventory can accept the completed part.

### calculateSpecialistBonus
Calculates the EVA Kerbal's specialist bonus without relying on part CrewCapacity.
> #### Return value
> The multiplier applied to the EVA printer's base speed.

### onSupportPrintingRequest(Sandcastle.PrintShop.WBIShipwright,System.Collections.Generic.List{Sandcastle.PrintShop.BuildItem})
Prevents a personal EVA printer from accepting distributed shipwright jobs.
> #### Parameters
> **sender:** The shipwright requesting printer support.

> **buildList:** The shipwright's remaining build items.


### ensureInitialized
Resolves the EVA inventory and finishes wiring the shared UI.

### configureUI
Connects the reusable print-shop window to this EVA printer instance.

### refreshPrintableParts
Builds the list of cargo parts this EVA printer can produce.

### getWhitelistedCategories(ConfigNode)
Gets the configured category whitelist or all stock categories when none is supplied.
> #### Parameters
> **printerNode:** The EVA printer's injected module configuration.

> #### Return value
> Category names accepted by the printer UI.

### getEVAPrinterConfigNode
Finds this dynamically injected module's original KERBAL_EVA_MODULES configuration.
> #### Return value
> The matching MODULE node, or null when none can be found.

### onCrewBoardVessel(GameEvents.FromToAction{Part,Part})
Cancels all jobs immediately when this EVA Kerbal boards another part.
> #### Parameters
> **data:** The EVA part being boarded from and the destination part.


### cancelPrintQueue
Permanently discards all pending EVA print jobs and resets printer state.

### closeUI
Hides the reusable print-shop window if it is currently open.

### stopPrinterEffects
Stops optional part effects and animations when EVA printing is disabled.

### updateEventAvailability
Shows the PAW event only while the KerbalGear printer is active and initialized.

# PrintShop.WBIPrinterRequirements
            
Describes the 3D Printer requirements for the part. This is a stub part module; the real functionality is over in PrinterInfoHelper. We have to do this because GetInfo is called during game start, we rely on PartLoader to get information about other parts that are needed to 3D print this part, and not all of the parts will be loaded when GetInfo is called.
        

# PrintShop.WBIShipwright
            
Prints entire vessels
        
## Fields

### repositionCraftBeforeSpawning
Flag to indicate whether the printed vessel must remain beyond the selected spawn transform's virtual boundary. Recommended to set to FALSE for printers with enclosed printing spaces.
### printStateString
Current printer state.
### maxCraftDimensions
Maximum possible craft size that can be printed: Height (X) Width (Y) Length (Z). Leave empty for unlimited printing.

# PrintShop.WBIPrintStates
            
Lists the different printer states
        
## Fields

### Idle
Printer is idle, nothing to print.
### Paused
Printer has an item to print but is paused.
### Printing
Printer is printing something.
### Recycling
The recycler is recycling something.
### Unavailable
The printer cannot operate in its current situation.

# Utilities.PrinterInfoHelper
            
This helper fills out the info text for the WBIPrinterRequirements part module. During the game startup, it asks part modules to GetInfo. WBIPrinterRequirements is no exception. However, because it relies on the PartLoader to obtain information about prerequisite components, WBIPrinterRequirements can't completely fill out its info. We get around the problem by waiting until we load into the editor, and manually changing the ModuleInfo associated with WBIPrinterRequirements. It's crude but effective.
        