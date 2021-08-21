            
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

