Sandcastle: 3D Printed Crafts & Items

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

IMPORTANT NOTE:

Sandcastle now requires Harmony for KSP. Be sure to download Harmony for KSP before downloading the latest Sandcastle.
You can find it here: https://github.com/KSPModdingLibs/HarmonyKSP
And on CKAN.

---CHANGES---

Sandcastle

At long last, Sandcastle is feature complete! This release completes nearly all of the original vision that I had for Sandcastle- a mod that lets players build parts and vessels via 3D printers, use EVA Construction to build craft piece by piece- with or without kerbals- and even conduct underwater construction. The only thing that I didn't include, habitat parts that would have become a soft-replacement for Pathfinder- are now slated for Pathfinder's successor. Sandcastle is a foundation mod that'll let me do so much more in the future.

New Parts

- Added the Mini Ore Cartridge, a (very) small container of Ore that holds a mere 4 units. But hey, it can be carried by kerbals and slapped onto the hull of your craft!
- Added the Porta Printer, a deployable ground part capable of printing small items. It remotely pulls resources from nearby vessels, but will pull from the Mini Ore Cartridge and electrical power providers when attached to the printer.
- Added the Omni-tool, an advanced, wearable, Mass Effect-inspired device capable of printing small items. It remotely pulls resources from nearby vessels, but carried Mini Ore Cartridges and Z-100 batteries can fuel and power the Omni Tool as well. Additionally, it grants wearers the Repair skill- expert systems can walk a kerbal through making repairs, but the expert system can't replace an experienced engineer.
- Added the Cutting Torch, a wearable item that, when worn and its part destroyer is enabled, highlights parts in red that are in range. So long as the highlighted part has no child parts, you can click on it to destroy it- Michael Bay style. ;)

Changes

- Moved Extraplanetary Launchpads (EL) support to the Extras folder. To add EL, rename the file from .txt to .cfg.
- Renamed the Part Printer to Part Assembler on the EL-ODC Konstruction Manipulator and the Sandcaster 3D Printer.
- The EL-OCD Konstruction Manipulator, and the Sandcaster 3D Printer, can now initiate EVA Construction without the need for a kerbal.
- The EL-OCD Konstruction Manipulator, and the Sandcaster 3D Printer, can now ground-attach parts like the Stamp-O-Tron without the need for a kerbal.
- The EL-OCD Konstruction Manipulator, and the Sandcaster 3D Printer, Finalize Printing buttons on the Part Assembler and Shipwright will list the part/vessel being finalized, respectively.
- The EL-OCD Konstruction Manipulator, and the Sandcaster 3D Printer, will now honor the Infinite Electricity and Infinite Propellant debug options.
- The EL-OCD Konstruction Manipulator, and the Sandcaster 3D Printer, now have the option to display a sphere that depicts the EVA Construction range.
- Any kerbal can now remove an item from inventory and drop it onto the ground, or pick up an item and put it in an inventory when in cargo transfer mode (clicking that cargo icon in the apps button list). That action is no longer restricted to just Engineers- it doesn't take an engineering degree to move boxes...
- When node-attaching parts, Sandcastle ensures that the part you are attaching will snap to the same angle as the node you're attaching the part to. No more weird angles when attaching parts via EVA Construction!
- When printing a part underwater, or dragging a part out of inventory underwater, the part's buoyancy will be set to 0- if you place the part on the seabed. This prevents parts from floating away during construction.
- When a kerbal attempts to pick up a ground-attached part, and doesn't have enough volumetric capacity, now you'll get an appropriate error message instead of KSP's generic "Picking up a deployed part would exceed Kerbal carrying capacity." KSP's generic error now applies to a part that's too heavy to carry.
- When capturing a vessel for recycling, you now have the option to either automatically start recycling the vessel (default), or manually starting the process.
- When recycling a vessel, you can now halt the process and release the remains.
- When recycling a vessel, you have the option to store parts instead of recycling them if there is storage space available. It's on by default, mirroring how the Shipbreaker used to automatically try to store parts without giving you the option.
- Moved vessel capture functionality from the PAW to the Shipbreaker UI window.
- Empty print categories will now be hidden.
- Kerbals and vessels that are in the splashed state can now pull items out of inventory and place them onto the seabed floor without KSP complaining. Hence, kerbals and vessels can be swimming above the seabed instead of landed on it, and place inventory items onto the seabed.
- Fixed issue where the Aero category was missing from the Print Shop.
- Fixed issue where printing a single-part vessel in space would send it off flying. Thus, single-part printing in space is now possible again and re-enabled.
- Fixed issue where the WBIShipbreaker's RecycleTarget was getting in the way of manipulating parts during EVA Construction, and remained active even when WBIShipbreaker wasn't active.
- Fixed issue where, after the first material requirement is met, the print job is declared completed. Thanks for the assessment, Daring_Jeb! :)
- Fixed issue where support printers might get stuck while printing parts for a Shipwright.
- Fixed issue where support printers kept storing parts that they printed in support of a Shipwright.
- Fixed edge case issue where spawned craft fell through the world.
- Print queue fixes: thanks liujisi! :)

Wild Blue Core

New Parts

- BFP-5 Backpack Paramotor: This electrically powered fan provides forward thrust to kerbals wanting to fly around with their parachutes. Carry extra batteries for longer flight times.

Changes

- Kerbals now have 6 inventory slots and slightly increased volume and carrying capacity- thanks JadeOfMaar!
- Made some KerbalGear optimizations to improve framerates, organize configurations, and cut memory usage.
- Added WBIModuleEVAAblator, an EVA part module designed to help kerbals keep cool.
- Added WBIModuleEVAResourceTransfer, an EVA part module designed to make a cargo part's resources available to the kerbal- much like part resources are usable by parts.
- Added WBIModuleEVAMotor, an EVA part module that provides motive force for a kerbal on EVA.
- The Z-100 battery pack can now be used by kerbals to power various devices if carried in their inventory.
- Fixed issue in DialogManager preventing proper initialization of GUI dialogs.
- Fixed issue with mismatched suit textures and suit meshes.
- Fixed missing localized strings issue in the KerbalGear prop editor window.

--END CHANGES--

---LICENSE---
Near Future Props by Nertea, licensed under CC-BY-NC-SA-4.0

Portions of this code provided courtesy of Extraplanetary Launchpads and are licensed under GPLV3.

Art Assets, including .mu, .png, and .dds files are copyright 2024 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this code were done in collaboration with ChatGPT. Thanks for handling the drudgery!

Source code copyright 2021-2026 by Michael Billard (Angel-125)

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
