            
The stock EVA Construction system lets you drag and drop inventory parts onto the ground, but it requires a kerbal to do so. This part module enables non-kerbal parts to remove items from the part's inventory and drop them onto the ground. This code is based on vessel creation code from Extraplanetary Launchpads by Taniwha and is used under the GNU General Public License.
        
## Fields

### debugMode
Debug flag.
### dropTransformName
Name of the transform where dropped cargo items will appear.
### animationName
Optional name of the animation to play when dropping an item.
## Methods


### DropPart
Drops the desired item.

### ChangePartToDrop
Changes the desired item to drop.

### ChangePartToDrop(System.Int32)
Changes the desired item to drop to the desired inventory slot index (if it exists).
> #### Parameters
> **inventoryIndex:** An int containing the inventory index of the item to drop.


### DropPart(System.Int32)
Drops the item in the desired inventory index (if it exists)
> #### Parameters
> **inventoryIndex:** An int containing the index of the inventory item to drop.


### DropPart(AvailablePart)
Drops the desired part if it is in the inventory.
> #### Parameters
> **availablePart:** An AvailablePart containing the item to drop.


