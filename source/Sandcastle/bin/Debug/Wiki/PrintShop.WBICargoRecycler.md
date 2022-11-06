            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### recycleSpeedUSec
The number of resource units per second that the recycler can recycle.
### UseSpecialistBonus
Flag to indicate whether or not to allow specialists to improve the recycle speed. Exactly how the specialist(s) does that is a trade secret.
### SpecialistBonus
Per experience rating, how much to improve the recycle speed by. The print shop part must have crew capacity.
### ExperienceEffect
The skill required to improve the recycle speed.
### runningEffect
Name of the effect to play from the part's EFFECTS node when the printer is running.
### recyclePercentage
What percentage of resources will be recycled.
### animationName
Name of the animation to play during printing.
### recycleQueue
Represents the list of build items to recycle.
### recycleState
Current state of the recycler.
### lastUpdateTime
Describes when the recycler was last updated.
### currentJob
Current job being recycled.

