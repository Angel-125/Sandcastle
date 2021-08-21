            
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


