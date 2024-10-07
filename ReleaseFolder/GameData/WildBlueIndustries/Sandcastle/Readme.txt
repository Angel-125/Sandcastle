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
		WildBlueCore

Changes

- The Sandcaster's printers will now only be available when the printer arm has been deployed.
- Upon completion of printing, if a vessel is printed on the ground, then the printers will draw a movable box depicting where the vessel will spawn.
Simply use the movement arrows to place the box in the desired position before pressing the Finalize Printing button.
- The MATERIALS_LIST and TECH_NODE_MATERIALS config nodes now allow you to add REQUIRED_COMPONENT config nodes that specify what parts are required to complete parts in the part category and/or tech node, respectively.
- You can now specify a MATERIALS_LIST for a Community Category. 
NOTE: For a part to make use of a Community Category materials list, the part's "category" field must be set to "none" and you must properly define a Community Category in the part's "tag" field.
NOTE: The FIRST Community Category found in the part's config will be used as the part's category for the purposes of determining its MATERIALS_LIST.

Bug Fixes
- Fixed issue where Shipbreaker would get stuck if it had no storage capacity for a resource that it was trying to drain from the part being recycled.
- Fixed issue where Shipbreaker would get stuck if the recycled part's dry mass or variant mass is negative.
- Fixed issue where Shipbreaker wasn't emptying the inventory of stored parts from the ship being recycled.
- Fixed issue where Shipbreaker would store recycled parts in the vessel that it was recycling.
- Fixed issue where Shipbreaker's UI wasn't reflecting the parts that had been recycled.
- Fixed issue with duplicated parts being added to the Shipbreaker's recycling queue.

---LICENSE---
Near Future Props by Nertea, licensed under CC-BY-NC-SA-4.0

Portions of this code provided courtesy of Extraplanetary Launchpads and are licensed under GPLV3.

Art Assets, including .mu, .png, and .dds files are copyright 2024 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2024 by Michael Billard (Angel-125)

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