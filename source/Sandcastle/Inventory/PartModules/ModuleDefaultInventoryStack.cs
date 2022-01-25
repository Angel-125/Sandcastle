/*
This file is part of Sandcastle.

Sandcastle is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Sandcastle is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Extraplanetary Launchpads.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;
using System.IO;

namespace Sandcastle.Inventory
{
    /// <summary>
    /// ModuleInventoryPart's DEFAULTPARTS doesn't support stacked parts.
    /// This part module gets around the problem. Add this part module AFTER ModuleInventoryPart
    /// and part stacks will be filled out to their max stack size in the editor.
    /// </summary>
    public class ModuleDefaultInventoryStack: WBIPartModule
    {
        /// <summary>
        /// Flag to indicate that the part's stackable inventory items has been initialized.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool inventoryInitialized;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor || inventoryInitialized)
                return;

            ModuleInventoryPart inventory = part.FindModuleImplementing<ModuleInventoryPart>();
            if (inventory == null)
                return;

            int[] keys = inventory.storedParts.Keys.ToArray();
            int partIndex = -1;
            StoredPart storedPart;
            int stackSpace = 0;
            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                partIndex = keys[keyIndex];
                storedPart = inventory.storedParts[partIndex];

                // Make sure there's room
                if (!inventory.IsStackable(partIndex) || !inventory.HasStackingSpace(partIndex))
                    continue;

                // Stack em!
                stackSpace = inventory.GetStackCapacityAtSlot(partIndex);
                inventory.UpdateStackAmountAtSlot(partIndex, stackSpace);
            }

            // Set flag to indicate that we've initalized the stack inventory.
            inventoryInitialized = true;
        }
    }
}
