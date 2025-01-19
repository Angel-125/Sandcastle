# Sandcastle


# PartModules.SCModuleEVAVariants
            
This helper part module makes it possible to change part variants during EVA Construction.
        
## Methods


### enableVariantSwitching
Enables in-flight variant switching

### disableVariantSwitching
Disables in-flight variant switching

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

### FindThumbnailPaths
Searches the game folder for thumbnail images.

### GetTexture(System.String)
Retrieves the thumbnail texture that depicts the specified part name.
> #### Parameters
> **partName:** A string containing the name of the part.

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

# Inventory.ModuleCargoCatcher
            
Catches and stores cargo items into the part's inventory as long as they fit. Does not require a kerbal. This only works on single-part vessels. Note that you'll need a trigger collider set up in the part containing this part module in order to trigger the catch and store operation.
        
## Fields

### debugMode
Flag to indicate that we're in debug mode.
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

### debugMode
Debug flag.
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
### isPrinting
Flag indicating that the printer is printing
### part
The Part associated with the UI.
### whitelistedCategories
Whitelisted categories that the printer can print from.
### showPartSpawnButton
Flag to indicate whether or not to show the part spawn button.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


# PrintShop.SCShipbreaker
            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
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

### debugMode
A flag to enable/disable debug mode.
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
### offsetAxis
Axis upon which to displace the part during spawn in. X, Y, Z
### maxPartDimensions
Maximum possible craft size that can be printed: Height (X) Width (Y) Length (Z). Leave empty for unlimited printing.
### repositionCraftBeforeSpawning
Flag to indicate if it should offset the printed vessel to avoid collisions. Recommended to set to FALSE for printers with enclosed printing spaces.

# PrintShop.WBIPrinterRequirements
            
Describes the 3D Printer requirements for the part. This is a stub part module; the real functionality is over in PrinterInfoHelper. We have to do this because GetInfo is called during game start, we rely on PartLoader to get information about other parts that are needed to 3D print this part, and not all of the parts will be loaded when GetInfo is called.
        

# PrintShop.SCShipwright
            
Prints entire vessels
        
## Fields

### spawnTransformVABName
Alternate transform to use for VAB craft.
### spawnTransformSPHName
Alternate transform to use for SPH craft.
### repositionCraftBeforeSpawning
Flag to indicate if it should offset the printed vessel to avoid collisions. Recommended to set to FALSE for printers with enclosed printing spaces.
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

# Utilities.PrinterInfoHelper
            
This helper fills out the info text for the WBIPrinterRequirements part module. During the game startup, it asks part modules to GetInfo. WBIPrinterRequirements is no exception. However, because it relies on the PartLoader to obtain information about prerequisite components, WBIPrinterRequirements can't completely fill out its info. We get around the problem by waiting until we load into the editor, and manually changing the ModuleInfo associated with WBIPrinterRequirements. It's crude but effective.
        