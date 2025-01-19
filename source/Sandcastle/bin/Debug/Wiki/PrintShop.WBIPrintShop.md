            
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

