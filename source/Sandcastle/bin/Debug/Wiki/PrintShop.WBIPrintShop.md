            
Represents a shop that is capable of printing items and placing them in an available inventory.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### maxPrintVolume
The maximum volume that the printer can print, in liters. Set to less than 0 for no restrictions.
### printSpeedUSec
The number of resource units per second that the printer can print.
### UseSpecialistBonus
Flag to indicate whether or not to allow specialists to improve the print speed. Exactly how the specialist(s) does that is a trade secret.
### SpecialistBonus
Per experience rating, how much to improve the print speed by. The print shop part must have crew capacity.
### ExperienceEffect
The skill required to improve the print speed.
### runningEffect
Name of the effect to play from the part's EFFECTS node when the printer is running.
### printQueue
Represents the list of build items to print.
### printState
Current state of the printer.
### lastUpdateTime
Describes when the printer was last updated.
### currentJob
Current job being printed.
### animationName
Name of the animation to play during printing.

