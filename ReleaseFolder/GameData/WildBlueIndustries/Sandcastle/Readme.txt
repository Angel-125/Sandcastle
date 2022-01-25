Sandcastle: 3D Printed Bases

Real-world references

https://www.youtube.com/watch?v=yp_Xz6r2Aso
https://room.eu.com/article/How_to_3Dprint_a_habitat_on_Mars
https://www.nasa.gov/directorates/spacetech/centennial_challenges/3DPHab/index.html

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Sandcastle

Changes

New Parts

- Quicksand 3D Print Shop: The size of the stock Hitchhiker, this print shop is able to print small cargo items. It can be used as an Extraplanetary Launchpads workshop that also produces Rocket Parts.

- Sandcastle 3D Print Shop: This module is Size 2 (2.5m diameter) and it has a large 3D printer that's capable of printing objects up to the size of the FL-TX1800 Fuel Tank. It can be used as an Extraplanetary Launchpads workshop that also produces Rocket Parts.

- UHC-4K Cargo Storage Unit: This Size 2 (2.5m diameter) part stores 4,000l of stock cargo items in 24 slots.

- UHC-8K Cargo Storage Unit: This Size 2 (2.5m diameter) part stores 8,000l of stock cargo items in 24 slots.

- UHC-16K Cargo Storage Unit: This Size 2 (2.5m diameter) part stores 16,000l of stock cargo items in 24 slots.

- MS-37 Yard Frame: Inspired by the shipyard from Star Trek The Motion Picture, this Modular Shipyard component is a Size 3 (3.75m) panel that lets you configure it like a grid in order to keep the part count down. It also has angled variants.

- MS-75 Yard Frame: Inspired by the shipyard from Star Trek The Motion Picture, this Modular Shipyard component is a Size 5 (7.5m) panel that lets you configure it like a grid in order to keep the part count down. It also has angled variants.

- MS-L Lighting Panel: This panel provides lighting for your orbital shipyard's construction projects.

- EB-1V Variable Extension Boom: Similar to the stock girder, this is designed for robot arms that are used to build spacecraft. Its length can vary.

Extraplanetary Launchpad Parts

These parts are only available if you have Extraplanetary Launchpads installed.

- Sand Caster 3D Printer: This automated 3D printer is capable of creating whole vessels and bases without the need for kerbals on site. It will be slower than having kerbals around, but it will get the job done. it is designed for ground-based operations, and it's inspired by NASA's 3D habitat concept printers.

- EL-M Construction Marker: Equivalent to the Extraplanetary Launchpads' KS-MP Disposable Pad, the Konstruction Marker depicts where new builds will be added to the vessel. The Marker will be consumed when the new assembly is attached. It can be placed into stock inventories.

- STK-1 Survey Cone: Functionally equivalent to the KS-BBQ Survey Stake, the Survey Cone can be placed in stock inventories. It marks where new constructions will appear.

- CD-10 Cone Dispenser: This dispenser holds 10 Survey Cones and can both drop them on the ground and pick them up again without the need for a kerbal.

- EL-OCD Construction Manipulator: Equivalent to the Extraplanetary Launchpads' KS-OCD Orbital Construction Dock, the Konstruction Manipulator enables shipyards to build new vessels in orbit.

- EL-MTL Smelter: This is a Size 2 part that is designed to convert Metal Ore into Metal, and Scrap Metal into Metal.

- ELC-8 Rocket Parts Container: This Size 2 container holds up to 1,600 Rocket Parts. If you have Wild Blue Tools installed, then it becomes an omni storage container with an 8,000 L capacity. If you don't have Wild Blue Tools but you have B9PartSwitch, then it can switch between Rocket Parts, Metal, Scrap Metal, and Metal Ore.

Plugin

- WBIPrintShop and WBICargoRecycler now support animations when operating.
- You can now add the removeResources field to a part config. Setting removeResources = false will prevent the print shop from removing resources when it prints a part and places it into an inventory.
- WBIPrintShop now supports part variants.
- WBIPrintShop now supports Community Category Kit.
- ModulePartGridVariants: This new part module supports mesh grids.
- ModuleCargoDispenser: This part module lets you drop cargo parts from an inventory onto the ground or release them into space. No kerbal needed.
- ModuleCargoGrabber: This part module lets you pick up cargo parts and place them in an inventory. No kerbal needed.
- ModuleDefaultInventoryStack: ModuleInventoryPart's DEFAULTPARTS doesn't support stacked parts. This part module gets around the problem by maxing out the default all the parts' stack sizes when the part is first loaded into the editor.
- ModuleStorablePart: This is just a thinly veiled ModuleCargoPart that enables parts to both have an inventory and to be placed in an inventory. Without this module, parts cannot have both a ModuleInventoryPart and a ModuleCargoPart.

Patches

- Disabled the Module Manager patch that added a 3D printer to the stock science lab. This was always intended to be temporary until Sandcastle had custom parts. If you want this functionality back, go to the Sandcastle/Patches folder and rename ScienceLab3DPrinter from .txt to .cfg.

- HorizontalPrintShops: Rename this from .txt to .cfg to use a horizontally orented IVA in the print shops.

- OmniStorage: Adds omni storage capacity for the Cargo Storage Units and the Rocket Parts Kontainer if Wild Blue Tools is installed.

- B9PS: Adds additional resource storage options to the Rocket Parts Kontainer if B9 Part Switch is installed and Wild Blue Tools isn't installed.

Known Issues

- When a printer finishes printing a part and placing it in the inventory, the image might not appear. This appears to be an issue with the stock inventory module.

---LICENSE---
Near Future Props by Nertea, licensed under CC-BY-NC-SA-4.0

Art Assets, including .mu, .png, and .dds files are copyright 2021 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.