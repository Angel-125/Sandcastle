Sandcastle: 3D Printed Bases

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Sandcastle

Changes

New Part Module

- WBIPrinterRequirements: Added to parts that have a ModuleCargoPart or ModuleGroundPart or a part module derived from ModuleCargoPart/ModuleGroundPart, this part module lists the material requirements and conditions needed to 3D print the part in the VAB/SPH part info panel.

3D Printers

- PARTS_BLACKLIST: You can specify one or more blacklistedPart entries in this node. The value is the name of the part (NOT the title). If a part is on the blacklist, then 3D printers cannot print it. If it's a required component for another part, then you'll have to add the blacklisted part to a vessel or inventory while in the VAB/SPH (or cheat and use something like Extraplanetary Launchpads).
NOTE: You can specify a global PARTS_BLACKLIST, which affects ALL 3D printers, and/or a PARTS_BLACKLIST to a WBIPrintShop which only affects that specific printer. if a part is on the global blacklist but not on the WBIPrintShop's local blacklist, then the part is still banned from being printed.
NOTE: The WBIPrintShop's PARTS_WHITELIST overrides the global/local PARTS_BLACKLIST.
Ex: The Kerbal Flying Saucers GND-00 Gravitic Engine requires a TD Blanket and a Black Box. Because they are specialized components that are listed on the global PARTS_BLACKLIST, no printer can print them. To make a GND-00 Gravitic Engine offworld, you'll need to ship the TD Blanket and Black Box to the printer.

- You can add an optional minimumGravity field to a part config to specify, in m/sec^2, how much gravity/acceleration is needed to print the part. If the printer's vessel is below the minimum threshold, then the part cannot be printed. If you specify a value of 0, then the printer will need to be in orbit, sub-orbital, or on an escape trajectory, and not accelerating. The default value is -1, which means that there are no g-level restrictions.
Ex: The Kerbal Flying Saucers GND-00 Gravitic Engine requires a GN Furnace, but the GN Furnace needs to be printed while in a high-gravity environment like Tylo.

- You can add an optional minimumPressure field to a part config to specify, in kPA, how much pressure is needed to print the part. If the printer's vessel is below the minimum threshold, then the part cannot be printed. If you specify a value of 0, then the printer will need to be in a vacuum. A value > 1 requires atmospheric pressure or the depths of an ocean. The default is -1, which means that there are no pressure restrictions.
Ex: The Kerbal Flying Saucers GND-00 Gravitic Engine requires a GN Furnace, but the GN Furnace needs to be printed while in a vacuum.

- You can add an optional canPrintHiddenPart = true field to a part config to add parts that have a ModuleCargoPart, ModuleGroundPart, or a part module derived from ModuleCargoPart, in their config file. Parts hidden from the tech tree and/or editor that have canPrintHiddenPart = true will be printable with a 3D printer.
IMPORTANT NOTE: The part config must set category = none and TechHidden = true.

- WBIPrintShop has a new part category that it can print from: none. It appears as "Special" in the GUI. Parts marked with category = none and TechHidden = true and canPrintHiddenPart = true will appear in this new part category. 

- Removed the optional requiredComponent field, and replaced it with the following config node:
REQUIRED_COMPONENT
{
	// The name of the part that is required as a component in order to print the part.
	name = somePartName

	// The required number of required parts that must be in the vessel's inventory in order to print the part. Default is 1.
	amount = 2
}

- When specifying one or more resources in a MATERIALS_LIST, the sum of each RESOURCE's rate field must be equal to or greater than 1. If that isn't the case, then the 3D printer will add Ore as a material requirement until the total sum of each RESOURCE's rate is equal to 1.

New Part

- Box of Generic Specialized Parts: This 0.625m cube contains various specialized components that were originally created by a heroic individual in a cave with a box of scraps. They are used as required components for some 3D printed parts. The specialized parts themselves cannot be 3D printed and must be shipped to the desired 3D printer.
NOTE: No parts currently require the Box of Generic Specialized Parts. This part is provided for modders who want a generic part that must be shipped in order to complete printing of a desired part.

Sample Config

Creative use of the new options makes it possible to restrict parts from being added in the VAB/SPH and requiring a 3D printer to make them.
Additionally, with the new prerequisite conditions, you can spread part creation throughout the solar system. For instance, maybe a component made from metastable metallic hydrogen needs Jool's unique conditions, while another component needs Tylo's environment.

Below are sample part configs that demonstrate the new features. These parts don't actually exist in Kerbal Flying Saucers- yet. Imagine that they did, and that KFS included these module manager patches.

// We're going to modify the GND-00 Gravitic Engine so that it can only be made in space. It requires specialized components, some of which can't be 3D printed,
// and others that can only be 3D printed.
@PART[wbiGND00]:NEEDS[Sandcastle]
{
	// Hide the part from the VAB/SPH so that it can only be printed from a 3D printer.
	category = none
	TechHidden = true

	// Make sure that the part can be printed from a 3D printer. It will show up in the printer's "Special" part category.
	canPrintHiddenPart = true

	// The GND-00 Gravitic Engine requires a TD Blanket, a GN Black Box, and a GN Furnce in order to be printed.
	REQUIRED_COMPONENT
	{
		name = wbiTDBlanket
		// Remmeber, amount = 1 is the default, so we don't have to include it.
	}
	REQUIRED_COMPONENT
	{
		name = wbiGNBlackBox
		amount = 2
	}
	REQUIRED_COMPONENT
	{
		name = wbiGNFurnace
	}
}

// The GN Furnace can't be bought in the VAB/SPH, but it can be printed.
// Here, we are defining the part- but only if Sandcastle is installed.
PART:NEEDS[Sandcastle]
{
	name = wbiGNFurnace

	// Hide the part from the VAB/SPH so that it can only be printed from a 3D printer.
	category = none
	TechHidden = true

	// Make sure that the part can be printed from a 3D printer.
	canPrintHiddenPart = true

	// The GN Furnace can only be made in a high gravity environment and in a vacuum- like, say, on Tylo...
	minimumGravity = 9.81
	minimumPressure = 0

	// ...And so on with the other config file entries for a typical KSP part...
}

// Blacklist the TD Blanket and GN Black Box so they MUST be sent from the VAB/SPH
// NOTE: You don't need one single global PARTS_BLACKLIST. You can define as many global PARTS_BLACKLIST nodes as you'd like, and Sandcastle will read all of them.
PARTS_BLACKLIST
{
	blacklistedPart = wbiTDBlanket
	blacklistedPart = wbiGNBlackBox
}

// Define the TD Blanked- but only if Sandcastle is installed. We do something similar for the GN Black Box.
PART:NEEDS[Sandcastle]
{
	name = wbiTDBlanket
	...
}

---LICENSE---
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