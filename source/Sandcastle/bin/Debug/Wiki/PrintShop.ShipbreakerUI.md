            
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


