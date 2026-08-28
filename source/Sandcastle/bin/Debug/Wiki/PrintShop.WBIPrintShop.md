            
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
## Methods


### updateUIStatus(System.String)
Updates the print-job status shown in the print-shop UI when the UI exists. Print jobs can run during catch-up before the window is opened, so this must remain safe when the printer is operating headlessly.
> #### Parameters
> **statusUpdate:** The new localized or formatted status.


### updateUIStatus(System.Boolean)
Updates the running state shown in the print-shop UI when the UI exists. Print jobs can run during catch-up before the window is opened, so this must remain safe when the printer is operating headlessly.
> #### Parameters
> **isPrinting:** Whether the current queue is printing.


### spaceRequirementsMet(Sandcastle.PrintShop.BuildItem)
Verifies that the vessel has room to store the completed cargo part unless this printer is configured to spawn completed parts directly into the world.
> #### Parameters
> **buildItem:** The print job whose output needs inventory space.

> #### Return value
> True when the completed part can be handled by this printer.

