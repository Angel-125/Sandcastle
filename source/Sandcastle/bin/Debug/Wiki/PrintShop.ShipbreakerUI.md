            
Represents the Print Shop UI
        
## Fields

### recycleQueue
Represents the list of build items to recycle.
### jobStatus
Status of the current print job.
### onCancelVesselBuild
Callback to tell the controller to cancel the build.
### onDecoupleShip
Callback to stop deconstruction and release the captured vessel remains.
### onToggleCaptureState
Callback to toggle capture state for recycling.
### onToggleAutoStarRecycling
Delegate to toggle auto-recycling state. If enabled, craft will automatically be recycled upon captured. If disabled, the player must manually start the recycling process.
### onRecycleStatusUpdate
Callback to let the controller know about the recycle state.
### onTogglePreferStorageToRecycle
Delegate to toggle preferring storage over recycling.
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
### resourceRecylePercent
Percentage of the resources that can be recycled.
### supportShipbreakers
List of support shipbreakers
### autoStartRecycling
Flag indicating if the recycler should immediately start recycling upon capturing a craft.
### enableVesselCapture
Flag indicating if the recycler should enable its vessel capturing.
### preferStoreBeforeRecycle
Flag to indicate whether or not to try to store parts before recyling them.
## Methods


### SetVisible(System.Boolean)
Toggles window visibility
> #### Parameters
> **newValue:** A flag indicating whether the window shoudld be visible or not.


### DrawWindowContents(System.Int32)
Draws the window
> #### Parameters
> **windowId:** An int representing the window ID.


