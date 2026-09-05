using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sandcastle.PartModules
{
    /// <summary>
    /// Lets any landed or splashed EVA kerbal pick up and drop single cargo
    /// items from the stock Cargo panel without granting access to EVA Construction.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal sealed class EVACargoGroundDropper : MonoBehaviour
    {
        private const int TerrainLayerMask = 1 << 15;
        private const float GroundOffset = 0.1f;
        private static readonly FieldInfo PanelModeField =
            AccessTools.Field(typeof(EVAConstructionModeController), "panelMode");

        /// <summary>
        /// Picks up a nearby one-part DroppedPart vessel, or converts a terrain
        /// click into one while the Cargo panel owns a single inventory item.
        /// </summary>
        public void Update()
        {
            if (!Input.GetKeyUp(KeyCode.Mouse0) || FlightDriver.Pause)
                return;

            EVAConstructionModeController constructionController =
                EVAConstructionModeController.Instance;
            UIPartActionControllerInventory inventoryController =
                UIPartActionControllerInventory.Instance;
            Vessel activeVessel = FlightGlobals.ActiveVessel;

            if (!IsCargoPanelOpen(constructionController) ||
                inventoryController == null ||
                activeVessel == null ||
                !activeVessel.isEVA ||
                !activeVessel.LandedOrSplashed ||
                EventSystem.current == null ||
                EventSystem.current.IsPointerOverGameObject() ||
                UIPartActionControllerInventory.heldPartIsStack)
            {
                return;
            }

            Part heldPart = inventoryController.CurrentCargoPart;
            if (heldPart == null)
            {
                TryPickupCargoPart(constructionController, inventoryController,
                    activeVessel);
                return;
            }

            TryDropCargoPart(constructionController, inventoryController,
                activeVessel, heldPart);
        }

        /// <summary>
        /// Converts a nearby one-part DroppedPart vessel into stock held cargo.
        /// The stock inventory UI subsequently enforces slot, volume, and mass
        /// limits when the player chooses an inventory.
        /// </summary>
        private static void TryPickupCargoPart(
            EVAConstructionModeController constructionController,
            UIPartActionControllerInventory inventoryController,
            Vessel activeVessel)
        {
            Part cargoPart = Mouse.HoveredPart;
            if (cargoPart == null ||
                cargoPart.vessel == null ||
                cargoPart.vessel.vesselType != VesselType.DroppedPart ||
                cargoPart.vessel.parts == null ||
                cargoPart.vessel.parts.Count != 1 ||
                cargoPart.FindModuleImplementing<ModuleCargoPart>() == null ||
                constructionController.evaEditor == null ||
                Vector3.Distance(cargoPart.transform.position,
                    (Vector3)activeVessel.GetWorldPos3D()) >
                    GameSettings.EVA_INVENTORY_RANGE)
            {
                return;
            }

            ModuleCargoPart cargoModule =
                cargoPart.FindModuleImplementing<ModuleCargoPart>();
            if (cargoModule.packedVolume < 0f)
                return;

            Part inventoryPart = null;
            try
            {
                ProtoPartSnapshot snapshot = new ProtoPartSnapshot(cargoPart,
                    cargoPart.vessel.protoVessel);
                inventoryController.CurrentInventory = null;
                inventoryController.CurrentInventorySlotClicked = null;
                inventoryController.CurrentInventorySlotEmptied = null;
                inventoryPart = inventoryController.CreatePartFromInventory(snapshot);
                if (inventoryPart == null)
                    return;

                UIPartActionControllerInventory.stackSize = 1;
                UIPartActionControllerInventory.heldPartIsStack = false;
                inventoryController.CurrentCargoPart = inventoryPart;
                inventoryController.isSetAsPart = true;

                // CurrentCargoPart assigns evaEditor's selectedPart through the
                // stock OnInventoryPartOnMouseChanged event.
                constructionController.evaEditor.CreateSelectedPartIcon();
                InputLockManager.SetControlLock(ControlTypes.UI_DRAGGING,
                    "CargoPartHeld");

                Vessel droppedVessel = cargoPart.vessel;
                GameEvents.onEditorPartEvent.Fire(
                    ConstructionEventType.PartPicked, cargoPart);
                inventoryController.PlayPartSelectedSFX();
                droppedVessel.Die();
                Debug.Log("[Sandcastle] Picked up " + cargoPart.partInfo.title +
                    " from the ground using the EVA Cargo panel.");
            }
            catch (Exception ex)
            {
                // Do not remove the source vessel unless the complete stock held-
                // cargo state was created successfully.
                if (inventoryController.CurrentCargoPart == inventoryPart)
                    inventoryController.ResetInventoryCacheValues();
                else if (inventoryPart != null)
                    Destroy(inventoryPart.gameObject);

                InputLockManager.RemoveControlLock("CargoPartHeld");
                Debug.LogError("[Sandcastle] Unable to pick up EVA cargo item " +
                    cargoPart.partInfo.title + ": " + ex);
            }
        }

        /// <summary>
        /// Converts stock held cargo into a one-part DroppedPart vessel.
        /// </summary>
        private static void TryDropCargoPart(
            EVAConstructionModeController constructionController,
            UIPartActionControllerInventory inventoryController,
            Vessel activeVessel, Part heldPart)
        {
            UIPartActionInventorySlot sourceSlot =
                inventoryController.CurrentInventorySlotClicked;
            if (heldPart.FindModuleImplementing<ModuleCargoPart>() == null ||
                (sourceSlot != null &&
                    sourceSlot.inventoryPartActionUI == null) ||
                constructionController.evaEditor == null ||
                Camera.main == null)
            {
                return;
            }

            RaycastHit groundHit;
            Ray cursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Match stock EVA Construction's terrain-only raycast so the held
            // cargo object's colliders cannot intercept the surface click.
            if (!Physics.Raycast(cursorRay, out groundHit, 10000f,
                    TerrainLayerMask) ||
                Vector3.Distance(groundHit.point,
                    (Vector3)activeVessel.GetWorldPos3D()) > GameSettings.EVA_INVENTORY_RANGE)
            {
                return;
            }

            Vector3 upAxis = (Vector3)FlightGlobals.getUpAxis(
                activeVessel.mainBody, groundHit.point);
            float centerPointOffset;
            float partOffset;
            bool heldPartWasActive = heldPart.gameObject.activeSelf;
            try
            {
                // Cargo mode hides the live 3D part and displays only its UI
                // icon. GetBoundsPoints cannot see inactive child colliders.
                if (!heldPartWasActive)
                    heldPart.gameObject.SetActive(true);

                partOffset = heldPart.GetBoundsPoints(
                    groundHit.normal, out centerPointOffset) + GroundOffset;
            }
            finally
            {
                heldPart.gameObject.SetActive(heldPartWasActive);
            }

            if (float.IsNaN(partOffset) || float.IsInfinity(partOffset))
            {
                Debug.LogError("[Sandcastle] Unable to drop EVA cargo item " +
                    heldPart.partInfo.title +
                    ": its ground-placement bounds are invalid.");
                return;
            }

            Vector3 dropPosition = groundHit.point + upAxis * partOffset;
            Quaternion worldRotation = GetGroundRotation(activeVessel, upAxis);
            Quaternion bodyRelativeRotation =
                Quaternion.Inverse(activeVessel.mainBody.bodyTransform.rotation) *
                worldRotation;

            try
            {
                ConfigNode vesselNode;
                EVAConstructionUnderwaterProtoVesselPatch.BeginCargoPanelPlacement();
                try
                {
                    vesselNode = constructionController.evaEditor.GetProtoVesselNode(
                        heldPart.partInfo.title, dropPosition, bodyRelativeRotation,
                        activeVessel, heldPart);
                }
                finally
                {
                    EVAConstructionUnderwaterProtoVesselPatch.EndCargoPanelPlacement();
                }

                ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(vesselNode);

                for (int index = 0; index < FlightGlobals.VesselsUnloaded.Count; index++)
                {
                    Vessel droppedVessel = FlightGlobals.VesselsUnloaded[index];
                    if (droppedVessel.persistentId != protoVessel.persistentId)
                        continue;

                    droppedVessel.SetPhysicsHoldExpiryOverride(5);
                    break;
                }

                GameEvents.onEditorPartEvent.Fire(
                    ConstructionEventType.PartDropped, heldPart);
                if (sourceSlot != null)
                {
                    sourceSlot.UpdateCurrentSelectedSlot(false);
                    sourceSlot.inventoryPartActionUI.DestroyHeldPart();
                }
                else
                {
                    InputLockManager.RemoveControlLock("CargoPartHeld");
                }
                inventoryController.PlayPartDroppedSFX();
                inventoryController.ResetInventoryCacheValues();
                Debug.Log("[Sandcastle] Dropped " + heldPart.partInfo.title +
                    " from the EVA Cargo panel.");
            }
            catch (Exception ex)
            {
                // Leave the item held so the player can return it to an inventory.
                Debug.LogError("[Sandcastle] Unable to drop EVA cargo item " +
                    heldPart.partInfo.title + ": " + ex);
            }
        }

        /// <summary>
        /// Reports whether stock is showing its inventory-only Cargo panel.
        /// </summary>
        private static bool IsCargoPanelOpen(
            EVAConstructionModeController constructionController)
        {
            return constructionController != null &&
                constructionController.IsOpen &&
                PanelModeField != null &&
                (EVAConstructionModeController.PanelMode)PanelModeField.GetValue(
                    constructionController) ==
                    EVAConstructionModeController.PanelMode.Cargo;
        }

        /// <summary>
        /// Aligns the dropped part with local up while retaining the kerbal's heading.
        /// </summary>
        private static Quaternion GetGroundRotation(Vessel activeVessel,
            Vector3 upAxis)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                activeVessel.ReferenceTransform.forward, upAxis);
            if (forward.sqrMagnitude < 1E-08f)
            {
                forward = Vector3.ProjectOnPlane(
                    activeVessel.ReferenceTransform.up, upAxis);
            }

            if (forward.sqrMagnitude < 1E-08f)
                forward = Vector3.Cross(upAxis, Vector3.right);
            if (forward.sqrMagnitude < 1E-08f)
                forward = Vector3.Cross(upAxis, Vector3.forward);

            return Quaternion.LookRotation(forward.normalized, upAxis);
        }
    }
}
