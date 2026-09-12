using HarmonyLib;
using Highlighting;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
        private static Part groundPickedCargoPart;
        private Part preparedPreviewPart;

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

            if (groundPickedCargoPart != null &&
                (inventoryController == null ||
                    inventoryController.CurrentCargoPart != groundPickedCargoPart))
            {
                groundPickedCargoPart = null;
            }

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
        /// Replaces Cargo mode's cursor icon with the highlighted, full-scale
        /// part preview that stock normally creates only in Construction mode.
        /// </summary>
        public void LateUpdate()
        {
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
                UIPartActionControllerInventory.heldPartIsStack)
            {
                ResetWorldPreview(inventoryController);
                return;
            }

            Part heldPart = inventoryController.CurrentCargoPart;
            if (heldPart == null)
            {
                preparedPreviewPart = null;
                return;
            }

            bool cursorOverUI = EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();
            Vector3 previewPosition;
            Quaternion previewRotation;
            if (cursorOverUI ||
                !TryGetGroundPlacement(activeVessel, heldPart,
                    out previewPosition, out previewRotation))
            {
                SetPreviewVisibility(inventoryController, heldPart, false);
                return;
            }

            PrepareWorldPreview(inventoryController, heldPart);
            heldPart.transform.position = previewPosition;
            heldPart.transform.rotation = previewRotation;
            heldPart.SetHighlightColor(Highlighter.colorConstructionPartDropAsNewVessel);
            heldPart.SetHighlightType(Part.HighlightType.AlwaysOn);
            heldPart.SetHighlight(true, true);
            SetPreviewVisibility(inventoryController, heldPart, true);
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
                groundPickedCargoPart = inventoryPart;
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

                if (groundPickedCargoPart == inventoryPart)
                    groundPickedCargoPart = null;

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

            Vector3 dropPosition;
            Quaternion worldRotation;
            if (!TryGetGroundPlacement(activeVessel, heldPart,
                    out dropPosition, out worldRotation))
                return;

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
        /// Finds the same terrain placement used by the visible preview and the
        /// final proto-vessel spawn.
        /// </summary>
        private static bool TryGetGroundPlacement(Vessel activeVessel,
            Part heldPart, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (activeVessel == null || activeVessel.mainBody == null ||
                heldPart == null || Camera.main == null)
            {
                return false;
            }

            RaycastHit groundHit;
            Ray cursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Match stock EVA Construction's terrain-only raycast so the held
            // cargo object's colliders cannot intercept the surface click.
            if (!Physics.Raycast(cursorRay, out groundHit, 10000f,
                    TerrainLayerMask) ||
                Vector3.Distance(groundHit.point,
                    (Vector3)activeVessel.GetWorldPos3D()) >
                    GameSettings.EVA_INVENTORY_RANGE)
            {
                return false;
            }

            Vector3 upAxis = (Vector3)FlightGlobals.getUpAxis(
                activeVessel.mainBody, groundHit.point);
            bool heldPartWasActive = heldPart.gameObject.activeSelf;
            float centerPointOffset;
            float partOffset;
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
                Debug.LogError("[Sandcastle] Unable to position EVA cargo item " +
                    heldPart.partInfo.title +
                    ": its ground-placement bounds are invalid.");
                return false;
            }

            position = groundHit.point + upAxis * partOffset;
            rotation = GetGroundRotation(activeVessel, upAxis);
            return true;
        }

        /// <summary>
        /// Configures the stock inventory clone as a non-physical flight-scene
        /// preview. Collider triggers prevent the preview from affecting physics.
        /// </summary>
        private void PrepareWorldPreview(
            UIPartActionControllerInventory inventoryController, Part heldPart)
        {
            heldPart.gameObject.SetActive(true);
            if (preparedPreviewPart == heldPart)
                return;

            heldPart.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(heldPart.gameObject,
                SceneManager.GetActiveScene());
            heldPart.transform.localScale = Vector3.one;
            heldPart.gameObject.SetLayerRecursive(0, true, 2097152);

            if (heldPart.rb == null)
                heldPart.rb = heldPart.gameObject.AddComponent<Rigidbody>();
            heldPart.rb.isKinematic = true;
            heldPart.rb.useGravity = false;

            Collider[] colliders = heldPart.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
                colliders[index].isTrigger = true;

            heldPart.highlighter.ReinitMaterials();
            inventoryController.isSetAsPart = true;
            preparedPreviewPart = heldPart;
        }

        /// <summary>
        /// Switches between the full-size world preview and Cargo mode's icon.
        /// </summary>
        private static void SetPreviewVisibility(
            UIPartActionControllerInventory inventoryController, Part heldPart,
            bool showWorldPreview)
        {
            heldPart.gameObject.SetActive(showWorldPreview);
            if (inventoryController.CurrentInventoryOnlyIcon != null)
            {
                inventoryController.CurrentInventoryOnlyIcon.gameObject.SetActive(
                    !showWorldPreview);
            }
        }

        /// <summary>
        /// Restores Cargo mode's icon presentation when its panel or vessel
        /// context is no longer eligible for a world preview.
        /// </summary>
        private void ResetWorldPreview(
            UIPartActionControllerInventory inventoryController)
        {
            if (preparedPreviewPart != null && inventoryController != null &&
                inventoryController.CurrentCargoPart == preparedPreviewPart)
            {
                SetPreviewVisibility(inventoryController,
                    preparedPreviewPart, false);
            }

            preparedPreviewPart = null;
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

        /// <summary>
        /// Returns stock's inventory mass for cargo picked up through this
        /// Cargo-mode extension without changing the live preview part.
        /// </summary>
        internal static bool TryGetGroundPickedCargoMass(Part cargoPart,
            out float cargoMass)
        {
            cargoMass = 0f;
            if (cargoPart == null || cargoPart != groundPickedCargoPart ||
                cargoPart.partInfo == null)
            {
                return false;
            }

            AvailablePart availablePart = PartLoader.getPartInfoByName(
                cargoPart.partInfo.name);
            if (availablePart == null || availablePart.partPrefab == null)
                return false;

            cargoMass = availablePart.partPrefab.mass +
                cargoPart.GetResourceMass();
            return true;
        }

        /// <summary>
        /// Replaces only the stored snapshot's flight-adjusted dry mass with
        /// the configured prefab mass. Resource amounts remain those captured
        /// from the ground part.
        /// </summary>
        internal static void NormalizeGroundPickedCargoSnapshot(
            ProtoPartSnapshot snapshot)
        {
            if (snapshot == null || snapshot.partRef == null ||
                snapshot.partRef != groundPickedCargoPart ||
                snapshot.partInfo == null ||
                snapshot.partInfo.partPrefab == null)
            {
                return;
            }

            snapshot.mass = snapshot.partInfo.partPrefab.mass;
        }
    }

    /// <summary>
    /// Keeps stock inventory capacity checks from using flight-adjusted mass for
    /// a cargo item picked up from the ground outside Construction mode.
    /// </summary>
    [HarmonyPatch(typeof(ModuleInventoryPart), nameof(ModuleInventoryPart.GetPartMass))]
    internal static class GroundPickedCargoMassPatch
    {
        public static void Postfix(Part part, ref float __result)
        {
            float cargoMass;
            if (EVACargoGroundDropper.TryGetGroundPickedCargoMass(
                    part, out cargoMass))
            {
                __result = cargoMass;
            }
        }
    }

    /// <summary>
    /// Prevents a ground part's flight-adjusted dry mass from being persisted
    /// after stock accepts it into an inventory.
    /// </summary>
    [HarmonyPatch]
    internal static class GroundPickedCargoSnapshotMassPatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ModuleInventoryPart),
                nameof(ModuleInventoryPart.StoreCargoPartAtSlot),
                new Type[] { typeof(ProtoPartSnapshot), typeof(int) });
        }

        public static void Prefix(ProtoPartSnapshot pPart)
        {
            EVACargoGroundDropper.NormalizeGroundPickedCargoSnapshot(pPart);
        }
    }
}
