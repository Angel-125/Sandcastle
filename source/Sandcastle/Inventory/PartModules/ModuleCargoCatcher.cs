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
    /// Catches and stores cargo items into the part's inventory as long as they fit. Does not require a kerbal. This only works on single-part vessels.
    /// Note that you'll need a trigger collider set up in the part containing this part module in order to trigger the catch and store operation.
    /// </summary>
    public class ModuleCargoCatcher: WBIPartModule
    {
        enum CargoCatcherStates
        {
            idle,
            animationDeploy,
            animationRetract
        }

        #region Fields
        /// <summary>
        /// Flag to indicate that we're in debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// Optional name of the animation to play when preparing the catcher to catch cargo parts.
        /// </summary>
        [KSPField]
        public string deployAnimationName = string.Empty;

        /// <summary>
        /// Flag to indicate that we can catch parts.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool canCatchParts;
        #endregion

        #region Housekeeping
        CargoCatcherStates catcherState = CargoCatcherStates.idle;
        Animation animation = null;
        AnimationState animationState;
        string[] partCatchWhitelist = new string[] { };
        ModuleInventoryPart inventory = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            setupAnimation();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the catch part whitelist (if any)
            ConfigNode node = getPartConfigNode();
            if (node != null)
            {
                if (node.HasValue("catchPartName"))
                {
                    partCatchWhitelist = node.GetValues("catchPartName");
                }
            }

            // Get the inventory
            inventory = part.FindModuleImplementing<ModuleInventoryPart>();
            if (inventory == null)
                return;

            // Setup events
            setupButtons();
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SANDCASTLE_catcherModuleDescription");
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            switch (catcherState)
            {
                case CargoCatcherStates.idle:
                    break;
                case CargoCatcherStates.animationDeploy:
                    if (animation != null && !animation.isPlaying)
                    {
                        catcherState = CargoCatcherStates.idle;
                    }
                    break;
                case CargoCatcherStates.animationRetract:
                    if (animation != null && !animation.isPlaying)
                    {
                        catcherState = CargoCatcherStates.idle;
                        setupButtons();

                        // If the dispenser is on the part then enable it again.
                        ModuleCargoDispenser dispenser = part.FindModuleImplementing<ModuleCargoDispenser>();
                        if (dispenser != null)
                        {
                            dispenser.EnableDispenser();
                        }
                    }
                    break;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Arms the catcher, enabling it to catch parts.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SANDCASTLE_armCatcherEventName")]
        public void ArmCatcher()
        {
            canCatchParts = true;
            setupButtons();

            // if the dispenser is present on the part then disable it to prevent its use.
            ModuleCargoDispenser dispenser = part.FindModuleImplementing<ModuleCargoDispenser>();
            if (dispenser != null)
            {
                dispenser.DisableDispenser();
            }

            // Play animation
            if (animation != null)
            {
                catcherState = CargoCatcherStates.animationDeploy;
                animation[deployAnimationName].time = 0f;
                animation[deployAnimationName].speed = 1.0f;
                animation.Play(deployAnimationName);
            }
            else
            {
                catcherState = CargoCatcherStates.idle;
            }
        }

        /// <summary>
        /// Disarms the catcher, preventing it from catching parts.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SANDCASTLE_disarmCatcherEventName")]
        public void DisarmCatcher()
        {
            canCatchParts = false;
            Events["DisarmCatcher"].guiActive = false;

            // Play animation in reverse.
            if (animation != null)
            {
                catcherState = CargoCatcherStates.animationRetract;
                animation[deployAnimationName].time = 1f;
                animation[deployAnimationName].speed = -1.0f;
                animation.Play(deployAnimationName);
            }
            else
            {
                catcherState = CargoCatcherStates.idle;
            }
        }
        #endregion

        #region API
        public void EnableCatcher()
        {
            setupButtons();
        }

        public void DisableCatcher()
        {
            canCatchParts = false;
            catcherState = CargoCatcherStates.idle;
            Events["DisarmCatcher"].guiActive = false;
            Events["ArmCatcher"].guiActive = false;
        }
        #endregion

        #region Trigger handling
        public void OnTriggerEnter(Collider collider)
        {
            if (!canCatchParts || collider.attachedRigidbody == null || !collider.CompareTag("Untagged") || inventory == null)
                return;

            //Get the vessel that collided with the trigger
            Part collidedPart = collider.attachedRigidbody.GetComponent<Part>();
            if (collidedPart == null)
                return;
            Vessel triggeredVessel = collidedPart.vessel;

            // We only support single-part vessels.
            if (triggeredVessel.Parts.Count > 1)
                return;

            // Get the root part.
            Part rootPart = triggeredVessel.rootPart;

            // If we have a whitelist and the part isn't on it, then we're done.
            string partName = rootPart.partInfo.name;
            if (partCatchWhitelist.Length > 0 && !partCatchWhitelist.Contains(partName))
                return;

            // Now check the inventory space.
            if (InventoryUtils.InventoryHasSpace(inventory, rootPart.partInfo))
                return;

            ModuleCargoPart cargoPart = rootPart.FindModuleImplementing<ModuleCargoPart>();
            bool canBeStacked = cargoPart.stackableQuantity > 1;
            bool inventoryContainsPart = inventory.ContainsPart(partName);
            StoredPart storedPart;
            bool addToEmptySpace = false;
            bool partAddedToInventory = false;

            // Store the part
            for (int index = 0; index < inventory.InventorySlots; index++)
            {
                // If the part can't be stacked then find an empty inventory slot.
                if (!canBeStacked && inventory.IsSlotEmpty(index))
                {
                    inventory.StoreCargoPartAtSlot(rootPart, index);
                    partAddedToInventory = true;
                    break;
                }

                // Part can be stacked. If the inventory doesn't contain the part, then find an empty slot and add it.
                else if (!inventoryContainsPart && inventory.IsSlotEmpty(index))
                {
                    inventory.StoreCargoPartAtSlot(rootPart, index);
                    partAddedToInventory = true;
                    break;
                }

                // Part can be stacked, but we need an empty slot to store it.
                else if (inventory.IsSlotEmpty(index) && addToEmptySpace)
                {
                    inventory.StoreCargoPartAtSlot(rootPart, index);
                    partAddedToInventory = true;
                    break;
                }

                // Inventory contains the part. Find the slot that it is in and add it there. If the stack is full then we need to find an empty slot.
                else if (inventory.storedParts[index].partName == partName)
                {
                    storedPart = inventory.storedParts[index];
                    if (storedPart.quantity + 1 <= storedPart.stackCapacity)
                    {
                        inventory.UpdateStackAmountAtSlot(index, storedPart.quantity + 1);
                        partAddedToInventory = true;
                        break;
                    }
                    else
                    {
                        addToEmptySpace = true;
                    }
                }
            }

            // If we successfully added the part to the inventory then delete the vessel.
            if (partAddedToInventory)
            {
                triggeredVessel.Die();
            }
        }
        #endregion

        #region Helpers
        private void setupButtons()
        {
            Events["DisarmCatcher"].guiActive = canCatchParts;
            Events["ArmCatcher"].guiActive = !canCatchParts;
        }

        private void setupAnimation()
        {
            Animation[] animations = this.part.FindModelAnimators(deployAnimationName);
            if (animations == null || animations.Length == 0)
                return;

            animation = animations[0];
            if (animation == null)
                return;

            animationState = animation[deployAnimationName];
            animationState.wrapMode = WrapMode.Once;
            animation.Stop();
        }
        #endregion
    }
}
