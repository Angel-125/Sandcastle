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
    /// The stock EVA Construction system lets you drag and drop inventory parts onto the ground, but it requires a kerbal to do so. This part module
    /// enables non-kerbal parts to remove items from the part's inventory and drop them onto the ground.
    /// This code is based on vessel creation code from Extraplanetary Launchpads by Taniwha and is used under the GNU General Public License.
    /// </summary>
    public class ModuleCargoDispenser: WBIPartModule
    {
        enum CargoDispenserStates
        {
            idle,
            animationDeploy,
            partDrop,
            animationRetract
        }

        #region Fields
        /// <summary>
        /// Debug flag.
        /// </summary>
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// Name of the transform where dropped cargo items will appear.
        /// </summary>
        [KSPField]
        public string dropTransformName = "dropTransform";

        /// <summary>
        /// Optional name of the animation to play when dropping an item.
        /// </summary>
        [KSPField]
        public string animationName = string.Empty;
        #endregion

        #region Housekeeping
        ModuleInventoryPart inventory;
        int itemIndex = -1;
        AvailablePart dropItem;
        Transform dropTransform = null;
        Animation animation = null;
        AnimationState animationState;
        CargoDispenserStates dispenserState = CargoDispenserStates.idle;
        string[] partDropWhitelist = new string[] { };
        bool canDispenseCargo = true;
        #endregion

        #region Events
        /// <summary>
        /// Drops the desired item.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SANDCASTLE_dropEventName")]
        public void DropPart()
        {
            // Disable the part catcher if it exists
            ModuleCargoCatcher catcher = part.FindModuleImplementing<ModuleCargoCatcher>();
            if (catcher != null)
                catcher.DisableCatcher();

            DropPart(dropItem);
        }

        /// <summary>
        /// Changes the desired item to drop.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SANDCASTLE_changeDropItemEventName")]
        public void ChangePartToDrop()
        {
            updateDropButtons();
            if (inventory == null || inventory.InventoryIsEmpty)
            {
                return;
            }

            int[] keys = inventory.storedParts.Keys.ToArray();
            itemIndex = (itemIndex + 1) % keys.Length;
            ChangePartToDrop(itemIndex);
        }

        /// <summary>
        /// Changes the desired item to drop to the desired inventory slot index (if it exists).
        /// </summary>
        /// <param name="inventoryIndex">An int containing the inventory index of the item to drop.</param>
        public void ChangePartToDrop(int inventoryIndex)
        {
            updateDropButtons();
            if (inventory == null || inventory.InventoryIsEmpty)
                return;

            if (inventory.storedParts.ContainsKey(inventoryIndex))
            {
                itemIndex = inventoryIndex;
                StoredPart storedPart = inventory.storedParts[inventoryIndex];
                dropItem = PartLoader.getPartInfoByName(storedPart.partName);
                Events["DropPart"].guiName = Localizer.Format("#LOC_SANDCASTLE_dropEventName", new string[1] { dropItem.title });
            }
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            setupAnimation();

            ConfigNode node = getPartConfigNode();
            if (node != null)
            {
                if (node.HasValue("dropPartName"))
                {
                    partDropWhitelist = node.GetValues("dropPartName");
                }
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            inventory = part.FindModuleImplementing<ModuleInventoryPart>();
            if (inventory == null)
                return;

            if (!string.IsNullOrEmpty(dropTransformName))
                dropTransform = part.FindModelTransform(dropTransformName);

            GameEvents.onModuleInventoryChanged.Add(onModuleInventoryChanged);
            ChangePartToDrop();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            switch (dispenserState)
            {
                case CargoDispenserStates.idle:
                    break;
                case CargoDispenserStates.animationDeploy:
                    if (animation != null && !animation.isPlaying)
                        dispenserState = CargoDispenserStates.partDrop;
                    break;
                case CargoDispenserStates.partDrop:
                    if (animation != null)
                        animation.Stop();
                    drop_part(dropItem);
                    break;
                case CargoDispenserStates.animationRetract:
                    if (animation != null && !animation.isPlaying)
                    {
                        updateDropButtons();
                        dispenserState = CargoDispenserStates.idle;
                    }

                    // Re-enable the part catcher if it exists
                    ModuleCargoCatcher catcher = part.FindModuleImplementing<ModuleCargoCatcher>();
                    if (catcher != null)
                        catcher.EnableCatcher();

                    break;
            }
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SANDCASTLE_dispenserModuleDescription");
        }
        #endregion

        #region API
        public void DisableDispenser()
        {
            canDispenseCargo = false;
            updateDropButtons();
        }

        public void EnableDispenser()
        {
            canDispenseCargo = true;
            updateDropButtons();
        }

        /// <summary>
        /// Drops the item in the desired inventory index (if it exists)
        /// </summary>
        /// <param name="inventoryIndex">An int containing the index of the inventory item to drop.</param>
        public void DropPart(int inventoryIndex)
        {
            updateDropButtons();
            if (inventory == null || inventory.InventoryIsEmpty || dropItem == null || dropTransform == null)
                return;

            if (inventory.storedParts.ContainsKey(inventoryIndex))
            {
                ChangePartToDrop(inventoryIndex);
                DropPart(dropItem);
            }
        }

        /// <summary>
        /// Drops the desired part if it is in the inventory.
        /// </summary>
        /// <param name="availablePart">An AvailablePart containing the item to drop.</param>
        public void DropPart(AvailablePart availablePart)
        {
            updateDropButtons();
            if (inventory == null || inventory.InventoryIsEmpty || dropItem == null || dropTransform == null)
                return;

            // If we have a whitelist then make sure that the part to drop is allowed.
            if (partDropWhitelist.Length > 0 && !partDropWhitelist.Contains(availablePart.name))
            {
                string message = Localizer.Format("#LOC_SANDCASTLE_partDropRestricted", new string[1] { availablePart.title });
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // Remove the item from the inventory. If it doesn't exist then we're done.
            if (inventory.TotalAmountOfPartStored(availablePart.name) <= 0)
            {
                string message = Localizer.Format("#LOC_SANDCASTLE_missingPartToDrop", new string[1] { availablePart.title });
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            inventory.RemoveNPartsFromInventory(availablePart.name, 1);

            // Play the drop animation (if any). If we have no animation then immediatedly proceed to drop the part.
            if (animation != null)
            {
                dropItem = availablePart;

                dispenserState = CargoDispenserStates.animationDeploy;

                animation[animationName].time = 0f;
                animation[animationName].speed = 1.0f;
                animation.Play(animationName);

                // Disable the drop button and change item button
                Events["DropPart"].guiActive = false;
                Events["ChangePartToDrop"].guiActive = false;
            }
            else
            {
                dispenserState = CargoDispenserStates.idle;
                dropItem = availablePart;
                drop_part(dropItem);
            }
        }
        #endregion

        #region Helpers
        private void drop_part(AvailablePart availablePart)
        {
            ShipConstruct shipConstruct = new ShipConstruct(availablePart.title, "Sandcaster created part", availablePart.partPrefab);
            shipConstruct.missionFlag = part.flagURL;

            // Zero out rotation before saving to the config node.
            Quaternion rotation = shipConstruct.parts[0].transform.rotation;
            shipConstruct.parts[0].transform.rotation = Quaternion.identity;
            ConfigNode node = shipConstruct.SaveShip();
            shipConstruct.parts[0].transform.rotation = rotation;

            // For debug, save the craft file so we an see what the file looks like.
            if (debugMode)
                saveCraftFile(availablePart);

            // Recreate the ship construct. This seems to set up ShipConstruct to handle ShipConstruction.AssembleForLaunch.
            shipConstruct = new ShipConstruct();
            shipConstruct.LoadShip(node);

            // Update node variants.
            applyNodeVariants(shipConstruct);

            // Set rotation and position to match the spawn transform.
            Part rootPart = shipConstruct.parts[0].localRoot;
            rootPart.transform.position = dropTransform.position;
            rootPart.transform.rotation = dropTransform.rotation;

            // Spawn the part-vessel into the game.
            ShipConstruction.AssembleForLaunch(shipConstruct, "", "", part.flagURL, FlightDriver.FlightStateCache, new VesselCrewManifest());
            Vessel craftVessel = shipConstruct.parts[0].localRoot.GetComponent<Vessel>();
            craftVessel.launchedFrom = "";

            // Now update orbit.
            setCraftOrbit(craftVessel, OrbitDriver.UpdateMode.IDLE);

            // Set the situation to match the dispenser part's parent vessel.
            craftVessel.situation = part.vessel.situation;

            // When we spawn the part-vessel in, the game switches to it. Switch back to the dispenser's vessel.
            FlightGlobals.ForceSetActiveVessel(part.vessel);

            // Reverse the drop animation (if any)
            if (animation != null)
            {
                dispenserState = CargoDispenserStates.animationRetract;
                animation[animationName].time = 1f;
                animation[animationName].speed = -1.0f;
                animation.Play(animationName);
            }
        }

        private void updateDropButtons()
        {
            if (inventory == null || inventory.InventoryIsEmpty || !canDispenseCargo)
            {
                Events["DropPart"].guiActive = false;
                Events["ChangePartToDrop"].guiActive = false;
            }
            else
            {
                Events["DropPart"].guiActive = true;
                Events["ChangePartToDrop"].guiActive = inventory.InventoryItemCount > 1;
            }
        }

        private void onModuleInventoryChanged(ModuleInventoryPart inventoryPart)
        {
            if (inventoryPart != inventory)
                return;

            itemIndex = -1;
            ChangePartToDrop();
        }

        private void applyNodeVariants(ShipConstruct ship)
        {
            for (int i = 0; i < ship.parts.Count; i++)
            {
                var p = ship.parts[i];
                var pv = p.FindModulesImplementing<ModulePartVariants>();
                for (int j = 0; j < pv.Count; j++)
                {
                    var variant = pv[j].SelectedVariant;
                    for (int k = 0; k < variant.AttachNodes.Count; k++)
                    {
                        var vnode = variant.AttachNodes[k];
                        updateAttachNode(p, vnode);
                    }
                }
            }
        }

        private void updateAttachNode(Part p, AttachNode vnode)
        {
            var pnode = p.FindAttachNode(vnode.id);
            if (pnode != null)
            {
                pnode.originalPosition = vnode.originalPosition;
                pnode.position = vnode.position;
                pnode.size = vnode.size;
            }
        }

        private Vector3 getVesselWorldCoM(Vessel v)
        {
            Vector3 com = v.localCoM;
            return v.rootPart.partTransform.TransformPoint(com);
        }

        private ShipConstruct createShipConstruct(AvailablePart availablePart)
        {
            ShipConstruct ship = new ShipConstruct(availablePart.title, "Sandcaster created part", availablePart.partPrefab);
            Quaternion rotation = ship.parts[0].transform.rotation;
            ship.parts[0].transform.rotation = Quaternion.identity;
            ConfigNode node = ship.SaveShip();
            ship.parts[0].transform.rotation = rotation;

            return ship;
        }

        private string saveCraftFile(AvailablePart availablePart)
        {
            ShipConstruct ship = new ShipConstruct(availablePart.title, "Sandcaster created part", availablePart.partPrefab);
            Quaternion rotation = ship.parts[0].transform.rotation;
            ship.parts[0].transform.rotation = Quaternion.identity;
            ConfigNode node = ship.SaveShip();
            ship.parts[0].transform.rotation = rotation;

            string dir = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}/Sandcaster/";
            string filePath = $"{dir}/temp.craft";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            node.Save(filePath);
            return filePath;
        }

        private void setCraftOrbit(Vessel craftVessel, OrbitDriver.UpdateMode mode)
        {
            craftVessel.orbitDriver.SetOrbitMode(mode);

            var craftCoM = getVesselWorldCoM(craftVessel);
            var vesselCoM = getVesselWorldCoM(part.vessel);
            var offset = (Vector3d.zero + craftCoM - vesselCoM).xzy;

            var corb = craftVessel.orbit;
            var orb = part.vessel.orbit;
            var UT = Planetarium.GetUniversalTime();
            var body = orb.referenceBody;
            corb.UpdateFromStateVectors(orb.pos + offset, orb.vel, body, UT);
        }

        private void setupAnimation()
        {
            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null || animations.Length == 0)
                return;

            animation = animations[0];
            if (animation == null)
                return;

            animationState = animation[animationName];
            animationState.wrapMode = WrapMode.Once;
            animation.Stop();
        }

        #endregion
    }
}
