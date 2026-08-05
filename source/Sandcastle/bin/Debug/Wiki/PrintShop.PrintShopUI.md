            
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

