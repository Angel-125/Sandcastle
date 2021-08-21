            
Represents a list of resources needed to build an item of a particular part category.
        
## Fields

### kMaterialsListNode
Node ID for a materials list.
### kTechNodeMaterials
Node ID for tech node materials. Parts in a specific tech node can require additional materials.
### kDefaultMaterialsListName
Name of the default materials list.
### kResourceNode
Represents a resource node.
### name
Name of the materials list. This should correspond to one of the part categories.
### materials
List of resource materials required.
### materialsLists
A map of all materials lists, keyed by part category name.
## Methods


### LoadLists
Loads the materials lists that specify what materials are required to produce an item from a particular category.
> #### Return value
> A Dictionary containing the list names as keys and MaterialList objects as values.

### GetListForCategory(System.String)
Returns the materials list for the requested category, or the default list if the list for the requested category doesn't exist.
> #### Parameters
> **categoryName:** A string containing the desired category.

> #### Return value
> A MaterialsList if one exists for the desired category, or the default list.

### GetDefaultList
Creates the default materials list.
> #### Return value
> A MaterialsList containing the default materials.

