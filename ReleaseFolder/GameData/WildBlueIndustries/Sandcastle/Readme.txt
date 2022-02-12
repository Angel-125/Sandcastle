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

- Updated support for the latest version of Extraplanetary Launchpads.
- Added new ModuleMeshGrid part module (see below). This is in the experimental stage. Warranty void if used in your favorite save. You can find some parts to try out in the Sandcastle/Parts/Experimental folder. Just rename then to .cfg to give them a look.

Part Modules

Below is an example of how to use the new ModuleMeshGrid:

	// This module lets you create a grid of meshes out of a single model.
	// You can create large grids out of the single model without the need for large numbers of parts.
	MODULE
	{
		name = ModuleMeshGrid

		// Name of the mesh transform that we'll clone into a grid.
		meshTransformName = Panel2

		// Whenever you update the grid, ModuleMeshGrid will fire onEditorVariantApplied and onVariantApplied.
		// You can apply key-value pairs when that happens via the EXTRA_INFO node of the VARIANT node.
		// Part modules like Buffalo2's ModuleResourceVariants can make use of the key-value pairs.
		// You can specify fixed values, but ModuleMeshGrid recognizes a few keywords and will
		// replace the keyword with the appropriate value. The keywords are:
		// <<Rows>> = current row count in the grid.
		// <<Columns>> = current column count in the grid.
		// <<Stacks>> = current stack height of the grid.
		// <<Rows*Columns>> = current row count multiplied by the current column count.
		// <<Rows*Columns*Stacks>> = current row count multiplied by the current column count 
		// multiplied by the current stack height.
		// Below is an example for Buffalo2:
//		VARIANT
//		{
//			// Fields like name, displayName, etc. are automatically filled out by ModuleMeshGrid.
//
//			EXTRA_INFO
//			{
//				// ModuleMeshGrid doesn't know what packedVolumeLimit is, but it will
//				// include it along with its value when it fires the variant applied events.
//				packedVolumeLimit = 100
//
//				// ModuleMeshGrid doesn't know what packedVolumeMultiplier is, but it does know to
//				// set its value to <<Rows*Columns*Stacks>>
//				packedVolumeMultiplier = <<Rows*Columns*Stacks>>
//
//				// ModuleMeshGrid doesn't know what resourceMultiplier is, but it does know to
//				// set its value to <<Rows*Columns>>
//				resourceMultiplier = <<Rows*Columns>>
//			}
//		}
	}

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