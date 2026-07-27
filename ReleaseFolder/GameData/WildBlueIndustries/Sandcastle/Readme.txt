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
- Added Chinese language support. Thank you Aebestach! :)

Bug Fixes

- Fixed duplicate print-queue entries after vessel reloads by ensuring printer state is loaded and saved only once.
- Fixed print-queue clearing and several null-reference exceptions during printing and vessel spawning.
- Fixed orbital vessel spawn positioning so it remains aligned with the printer throughout its orbit.
- Fixed orbital and landed craft orientation using a consistent LaunchPos transform coordinate system. Details in the part config files.
- Fixed landed vessel placement, terrain clearance, wireframe alignment, and printer collision boundaries.
- Prevented the placement wireframe and movement gizmo from appearing for orbital spawns.
- Removed obsolete VAB- and SPH-specific spawn-transform fields in favor of a single spawnTransformName.

Special Thanks liujisi and mjungnickel18 who provided several of these bug fixes. :)

- Fixed issue with the storage containers where ModuleCargoPart needed to appear before ModuleInventoryPart for proper EVA manipulation.
- Fixed issues with construction cone orientation.
- Fixed UI issue preventing correct updates when user quicksaves/quickloads and a printed vessel hasn't been detached from the printer.
- Restored the printer’s running UI state after vessel reloads and cleared it when the print queue becomes empty.
- Fixed stock inventories with zero occupied volume being incorrectly rejected as print destinations.
- Fixed issues with construction cone not being oriented properly for Extraplanetary Launchpads.

WildBlueCore

- Added new Mk18 Ram Air Parachute- a steerable chute for vehicles!

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
