# Sandcastle


# Inventory.InventoryUtils
            
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

### HasEnoughSpace(Vessel,AvailablePart,System.Int32)
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


### AddItem(Vessel,AvailablePart,ModuleInventoryPart,System.Boolean)
Adds the item to the vessel inventory if there is enough room.
> #### Parameters
> **vessel:** The vessel to query.

> **availablePart:** The part to add to the inventory

> **preferredInventory:** The preferred inventory to store the part in.

> **removeResources:** A bool indicating whether or not to remove resources when storing the part. Default is true.

> #### Return value
> The Part that the item was stored in, or null if no place could be found for the part.

### FindThumbnailPaths
Searches the game folder for thumbnail images.

### GetTexture(System.String)
Retrieves the thumbnail texture that depicts the specified part name.
> #### Parameters
> **partName:** A string containing the name of the part.

> #### Return value
> A Texture2D if the texture exists, or a blank texture if not.

### GetPrintableParts(System.Single)
Retrieves a list of parts that can be printed by the specified max print volume.
> #### Parameters
> **maxPrintVolume:** A float containing the max possible print volume.

> #### Return value
> A List of AvailablePart objects that can be printed.

### loadTexture(System.String)
Retrieves the thumbnail texture that depicts the specified part name.
> #### Parameters
> **partName:** A string containing the name of the part.

> #### Return value
> A Texture2D if the texture exists, or a blank texture if not.

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
        

# PrintShop.PrintShopUI
            
Represents the Print Shop UI
        
## Fields

### titleText
Title of the selection dialog.
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
### isPrinting
Flag indicating that the printer is printing
### part
The Part associated with the UI.
### whitelistedCategories
Whitelisted categories that the printer can print from.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


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

# PrintShop.WBIPrintShop
            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### maxPrintVolume
The maximum volume that the printer can print, in liters. Set to less than 0 for no restrictions.
### printSpeedUSec
The number of resource units per second that the printer can print.
### UseSpecialistBonus
Flag to indicate whether or not to allow specialists to improve the print speed. Exactly how the specialist(s) does that is a trade secret.
### SpecialistBonus
Per experience rating, how much to improve the print speed by. The print shop part must have crew capacity.
### ExperienceEffect
The skill required to improve the print speed.
### runningEffect
Name of the effect to play from the part's EFFECTS node when the printer is running.
### printQueue
Represents the list of build items to print.
### printState
Current state of the printer.
### lastUpdateTime
Describes when the printer was last updated.
### currentJob
Current job being printed.

# PrintShop.WBIPrinterRequirements
            
Describes the 3D Printer requirements for the part. This is a stub part module; the real functionality is over in PrinterInfoHelper. We have to do this because GetInfo is called during game start, we rely on PartLoader to get information about other parts that are needed to 3D print this part, and not all of the parts will be loaded when GetInfo is called.
        

# Utilities.PrinterInfoHelper
            
This helper fills out the info text for the WBIPrinterRequirements part module. During the game startup, it asks part modules to GetInfo. WBIPrinterRequirements is no exception. However, because it relies on the PartLoader to obtain information about prerequisite components, WBIPrinterRequirements can't completely fill out its info. We get around the problem by waiting until we load into the editor, and manually changing the ModuleInfo associated with WBIPrinterRequirements. It's crude but effective.
        

# WBIPartModule
            
Just a simple base class to handle common functionality
        
## Methods


### getPartConfigNode
Retrieves the module's config node from the part config.
> #### Return value
> A ConfigNode for the part module.

### loadCurve(FloatCurve,System.String,ConfigNode)
Loads the desired FloatCurve from the desired config node.
> #### Parameters
> **curve:** The FloatCurve to load

> **curveNodeName:** The name of the curve to load

> **defaultCurve:** An optional default curve to use in case the curve's node doesn't exist in the part module's config.
