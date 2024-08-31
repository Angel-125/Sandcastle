            
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


