            
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

