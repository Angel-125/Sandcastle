using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Sandcastle.PartModules
{
    /// <summary>
    /// Shared state and stock-call adapters for vessel-hosted EVA Construction.
    /// </summary>
    internal static class EVAConstructionBridge
    {
        private const string VesselControlLockName = "SandcastleHostedEVAConstruction";
        private const ControlTypes VesselControlLocks =
            ControlTypes.PITCH |
            ControlTypes.ROLL |
            ControlTypes.YAW |
            ControlTypes.THROTTLE |
            ControlTypes.LINEAR |
            ControlTypes.WHEEL_STEER |
            ControlTypes.WHEEL_THROTTLE |
            ControlTypes.SAS |
            ControlTypes.RCS |
            ControlTypes.THROTTLE_CUT_MAX |
            ControlTypes.STAGING;

        internal static WBIEVAConstructionManipulator ActiveHost { get; private set; }
        private static bool vesselControlsLocked;
        private static bool stageStackHidden;
        private static bool stagingQuadrantHidden;
        private static string stagingQuadrantPreviousState;
        private static bool flightModeFrameHidden;
        private static int flightModeFramePreviousState;
        private static CanvasGroup flightModeCanvasGroup;
        private static bool flightModeCanvasGroupAdded;
        private static float flightModeFramePreviousAlpha;
        private static bool flightModeFramePreviouslyInteractable;
        private static bool flightModeFramePreviouslyBlockedRaycasts;
        private static readonly HashSet<Part> hostedAttachedParts = new HashSet<Part>();
        private static readonly PropertyInfo CargoMassForWeightTesting =
            AccessTools.Property(typeof(ModuleCargoPart), "MassForWeightTesting");

        internal static bool HasActiveHost
        {
            get
            {
                return ActiveHost != null &&
                       ActiveHost.part != null &&
                       ActiveHost.part.vessel != null &&
                       ActiveHost.part.vessel == FlightGlobals.ActiveVessel;
            }
        }

        /// <summary>
        /// Reports whether the active construction path has opted into complete stack-node alignment.
        /// </summary>
        internal static bool IsStackNodeAlignmentEnabled()
        {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            bool supportedConstructionHost = HasActiveHost || (activeVessel != null && activeVessel.isEVA);
            return supportedConstructionHost &&
                global::Sandcastle.SandcastleSettings.AlignEVAConstructionStackNodes;
        }

        /// <summary>
        /// Makes a part module the active stock-construction host and hides conflicting flight UI.
        /// </summary>
        internal static void Activate(WBIEVAConstructionManipulator host)
        {
            ClearHostedAttachmentHighlights();
            EVAConstructionStackNodeAlignmentPatch.ResetTracking();
            ActiveHost = host;
            LockVesselControls();

            if (!stageStackHidden && HighLogic.LoadedSceneIsFlight)
            {
                KSP.UI.Screens.StageManager.ShowHideStageStack(false);
                stageStackHidden = true;
            }

            HideStagingQuadrant();
            HideFlightModeFrame();

            Debug.Log("[Sandcastle] Vessel-hosted EVA Construction activated; vessel controls locked and flight staging/mode UI hidden.");
        }

        /// <summary>
        /// Releases the active host and restores every flight UI state captured during activation.
        /// </summary>
        internal static void Deactivate(WBIEVAConstructionManipulator host = null)
        {
            if (host != null && ActiveHost != host)
                return;

            bool wasActive = ActiveHost != null || vesselControlsLocked || stageStackHidden || stagingQuadrantHidden || flightModeFrameHidden;
            ActiveHost = null;
            UnlockVesselControls();
            EVAConstructionStackNodeAlignmentPatch.ResetTracking();
            ClearHostedAttachmentHighlights();

            if (stageStackHidden)
            {
                if (HighLogic.LoadedSceneIsFlight)
                    KSP.UI.Screens.StageManager.ShowHideStageStack(true);

                stageStackHidden = false;
            }

            RestoreStagingQuadrant();
            RestoreFlightModeFrame();

            if (wasActive)
                Debug.Log("[Sandcastle] Vessel-hosted EVA Construction released; vessel controls unlocked and flight staging/mode UI restored.");
        }

        /// <summary>
        /// Blocks flight-control inputs without locking camera, PAW, editor, pause, save, or scene controls.
        /// </summary>
        private static void LockVesselControls()
        {
            if (vesselControlsLocked || !HighLogic.LoadedSceneIsFlight)
                return;

            InputLockManager.SetControlLock(VesselControlLocks, VesselControlLockName);
            vesselControlsLocked = true;
        }

        /// <summary>
        /// Removes only the flight-control lock owned by vessel-hosted EVA Construction.
        /// </summary>
        private static void UnlockVesselControls()
        {
            if (!vesselControlsLocked)
                return;

            InputLockManager.RemoveControlLock(VesselControlLockName);
            vesselControlsLocked = false;
        }

        /// <summary>
        /// Closes the stock construction panel when it was opened by a vessel-mounted host.
        /// </summary>
        internal static void CloseHostedConstruction()
        {
            if (ActiveHost == null)
                return;

            EVAConstructionModeController controller = EVAConstructionModeController.Instance;
            if (controller != null && controller.IsOpen)
                controller.ClosePanel();

            // ClosePanel normally fires OnEVAConstructionMode(false), which deactivates
            // the bridge. Keep this fallback for interrupted scene or UI teardown paths.
            if (ActiveHost != null)
                Deactivate();
        }

        /// <summary>
        /// Reapplies hidden UI states that stock flight code may change while parts are attached.
        /// </summary>
        internal static void MaintainHostUI()
        {
            if (!HasActiveHost || !HighLogic.LoadedSceneIsFlight)
                return;

            KSP.UI.Screens.StageManager stageManager = KSP.UI.Screens.StageManager.Instance;
            if (stageManager != null && stageManager.Visible)
                KSP.UI.Screens.StageManager.ShowHideStageStack(false);

            FlightUIModeController flightUI = FlightUIModeController.Instance;
            if (flightUI != null && flightUI.stagingQuadrant != null && flightUI.stagingQuadrant.State != "Out")
                flightUI.stagingQuadrant.TransitionImmediate("Out");

            // Stock IVAEVACollapseGroups uses transition index 1 for panels hidden on EVA.
            if (flightUI != null && flightUI.uiModeFrame != null && flightUI.uiModeFrame.StateIndex != 1)
                flightUI.uiModeFrame.TransitionImmediate(1);

            if (flightModeCanvasGroup == null)
                HideFlightModeFrame();

            if (flightModeCanvasGroup != null)
            {
                flightModeCanvasGroup.alpha = 0.0f;
                flightModeCanvasGroup.interactable = false;
                flightModeCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Captures and collapses the lower-left staging controls.
        /// </summary>
        private static void HideStagingQuadrant()
        {
            FlightUIModeController flightUI = FlightUIModeController.Instance;
            if (flightUI == null || flightUI.stagingQuadrant == null)
                return;

            if (!stagingQuadrantHidden)
                stagingQuadrantPreviousState = flightUI.stagingQuadrant.State;

            flightUI.stagingQuadrant.TransitionImmediate("Out");
            stagingQuadrantHidden = true;
        }

        /// <summary>
        /// Returns the staging quadrant to the state it had before construction opened.
        /// </summary>
        private static void RestoreStagingQuadrant()
        {
            if (!stagingQuadrantHidden)
                return;

            FlightUIModeController flightUI = FlightUIModeController.Instance;
            if (flightUI != null && flightUI.stagingQuadrant != null && !string.IsNullOrEmpty(stagingQuadrantPreviousState))
                flightUI.stagingQuadrant.TransitionImmediate(stagingQuadrantPreviousState);

            stagingQuadrantPreviousState = null;
            stagingQuadrantHidden = false;
        }

        /// <summary>
        /// Hides the flight-mode buttons without deactivating their stock transition object.
        /// </summary>
        private static void HideFlightModeFrame()
        {
            FlightUIModeController flightUI = FlightUIModeController.Instance;
            if (flightUI == null || (flightUI.uiModeFrame == null && flightUI.UIScaleModeFrame == null))
                return;

            if (!flightModeFrameHidden)
            {
                if (flightUI.uiModeFrame != null)
                    flightModeFramePreviousState = flightUI.uiModeFrame.StateIndex;

                if (flightUI.UIScaleModeFrame != null)
                {
                    flightModeCanvasGroup = flightUI.UIScaleModeFrame.GetComponent<CanvasGroup>();
                    if (flightModeCanvasGroup == null)
                    {
                        flightModeCanvasGroup = flightUI.UIScaleModeFrame.AddComponent<CanvasGroup>();
                        flightModeCanvasGroupAdded = true;
                    }

                    flightModeFramePreviousAlpha = flightModeCanvasGroup.alpha;
                    flightModeFramePreviouslyInteractable = flightModeCanvasGroup.interactable;
                    flightModeFramePreviouslyBlockedRaycasts = flightModeCanvasGroup.blocksRaycasts;
                }
            }

            if (flightUI.uiModeFrame != null)
                flightUI.uiModeFrame.TransitionImmediate(1);

            // The stock collapsed position can remain partly visible at some UI scales.
            // Keep the object active for stock transition coroutines, but make it invisible
            // and non-interactive for the duration of vessel-hosted construction.
            if (flightModeCanvasGroup != null)
            {
                flightModeCanvasGroup.alpha = 0.0f;
                flightModeCanvasGroup.interactable = false;
                flightModeCanvasGroup.blocksRaycasts = false;
            }

            flightModeFrameHidden = true;
        }

        /// <summary>
        /// Restores the flight-mode transition and CanvasGroup values captured on activation.
        /// </summary>
        private static void RestoreFlightModeFrame()
        {
            if (!flightModeFrameHidden)
                return;

            FlightUIModeController flightUI = FlightUIModeController.Instance;
            if (flightUI != null && flightUI.uiModeFrame != null)
                flightUI.uiModeFrame.TransitionImmediate(flightModeFramePreviousState);

            if (flightModeCanvasGroup != null)
            {
                if (flightModeCanvasGroupAdded)
                    UnityEngine.Object.Destroy(flightModeCanvasGroup);
                else
                {
                    flightModeCanvasGroup.alpha = flightModeFramePreviousAlpha;
                    flightModeCanvasGroup.interactable = flightModeFramePreviouslyInteractable;
                    flightModeCanvasGroup.blocksRaycasts = flightModeFramePreviouslyBlockedRaycasts;
                }
            }

            flightModeFramePreviousState = 0;
            flightModeCanvasGroup = null;
            flightModeCanvasGroupAdded = false;
            flightModeFramePreviousAlpha = 0.0f;
            flightModeFramePreviouslyInteractable = false;
            flightModeFramePreviouslyBlockedRaycasts = false;
            flightModeFrameHidden = false;
        }

        /// <summary>
        /// Converts the host's configurable metric-ton mass limit into stock's local-weight limit.
        /// </summary>
        internal static double GetManipulatorConstructionWeightLimit()
        {
            if (!HasActiveHost)
                return 0.0;

            double gravity = PhysicsGlobals.GravitationalAcceleration;
            if (ActiveHost.part.vessel != null)
                gravity = EVAConstructionUtil.GetConstructionGee(ActiveHost.part.vessel);

            return Math.Max(0.0, ActiveHost.maxPartMass) * 1000.0 * Math.Max(gravity, 1E-06);
        }

        /// <summary>
        /// Tests a candidate part's dry and resource mass against the active host's mass limit.
        /// </summary>
        internal static bool IsUnderManipulatorMassLimit(Part candidatePart)
        {
            if (!HasActiveHost || candidatePart == null)
                return false;

            double dryMass = candidatePart.mass;
            ModuleCargoPart cargoPart = candidatePart.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart != null && CargoMassForWeightTesting != null)
            {
                object cargoMass = CargoMassForWeightTesting.GetValue(cargoPart, null);
                if (cargoMass != null)
                    dryMass = Convert.ToDouble(cargoMass);
            }

            double totalMass = dryMass + candidatePart.GetResourceMass();
            return totalMass <= Math.Max(0.0, ActiveHost.maxPartMass);
        }

        /// <summary>
        /// Returns the active manipulator's construction distance or stock's distance for ordinary EVA.
        /// </summary>
        internal static float GetConstructionDistance()
        {
            if (HasActiveHost)
                return Math.Max(0.0f, ActiveHost.maxConstructionDistance);

            return GameSettings.EVA_CONSTRUCTION_RANGE;
        }

        /// <summary>
        /// Returns the host workspace distance for hosted inventory access or stock's inventory distance otherwise.
        /// </summary>
        internal static float GetInventoryDistance()
        {
            if (HasActiveHost)
                return Math.Max(0.0f, ActiveHost.maxConstructionDistance);

            return GameSettings.EVA_INVENTORY_RANGE;
        }

        /// <summary>
        /// Returns zero distance for inventories on the active host vessel so its entire storage network remains visible.
        /// </summary>
        internal static float GetInventoryDisplayDistance(Vector3 inventoryPosition, Vector3 constructionOrigin)
        {
            if (HasActiveHost && IsHostInventoryPosition(inventoryPosition))
                return 0.0f;

            return Vector3.Distance(inventoryPosition, constructionOrigin);
        }

        /// <summary>
        /// Reports whether an inventory belongs to the vessel hosting the active construction manipulator.
        /// </summary>
        internal static bool IsHostInventory(ModuleInventoryPart inventoryPart)
        {
            if (!HasActiveHost || inventoryPart == null)
                return false;

            Vessel hostVessel = ActiveHost.part.vessel;
            if (inventoryPart.part != null && inventoryPart.part.vessel == hostVessel)
                return true;

            foreach (Part hostPart in hostVessel.parts)
            {
                if (hostPart.protoModuleCrew == null)
                    continue;

                foreach (ProtoCrewMember crewMember in hostPart.protoModuleCrew)
                {
                    if (crewMember != null && crewMember.KerbalInventoryModule == inventoryPart)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reports whether a stock inventory-display position belongs to an inventory on the host vessel.
        /// </summary>
        private static bool IsHostInventoryPosition(Vector3 inventoryPosition)
        {
            Vessel hostVessel = ActiveHost.part.vessel;
            foreach (Part hostPart in hostVessel.parts)
            {
                ModuleInventoryPart partInventory = hostPart.FindModuleImplementing<ModuleInventoryPart>();
                if (partInventory != null && PositionsMatch(GetInventoryPosition(partInventory), inventoryPosition))
                    return true;

                if (hostPart.protoModuleCrew == null)
                    continue;

                foreach (ProtoCrewMember crewMember in hostPart.protoModuleCrew)
                {
                    ModuleInventoryPart crewInventory = crewMember != null
                        ? crewMember.KerbalInventoryModule
                        : null;
                    if (crewInventory != null && PositionsMatch(GetInventoryPosition(crewInventory), inventoryPosition))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reproduces the position stock uses when measuring an inventory for construction display.
        /// </summary>
        private static Vector3 GetInventoryPosition(ModuleInventoryPart inventoryPart)
        {
            if (inventoryPart.kerbalMode || inventoryPart.part == null)
                return inventoryPart.transform.position;

            return inventoryPart.part.transform.position;
        }

        /// <summary>
        /// Compares inventory positions with enough tolerance for transforms updated during the current frame.
        /// </summary>
        private static bool PositionsMatch(Vector3 first, Vector3 second)
        {
            return (first - second).sqrMagnitude <= 1E-06f;
        }

        /// <summary>
        /// Reproduces stock panel-opening guards that remain relevant without an EVA vessel.
        /// </summary>
        internal static bool CanOpenConstructionPanel()
        {
            if (!HasActiveHost || !HighLogic.LoadedSceneIsFlight || MapView.MapIsEnabled || FlightDriver.Pause)
                return false;

            ActionGroupsFlightController actionGroups = ActionGroupsFlightController.Instance;
            return actionGroups == null || !actionGroups.IsOpen;
        }

        /// <summary>
        /// Treats the active host vessel as EVA only for patched construction-workspace checks.
        /// </summary>
        internal static bool IsConstructionVessel(Vessel vessel)
        {
            return vessel != null && (vessel.isEVA || (HasActiveHost && vessel == ActiveHost.part.vessel));
        }

        /// <summary>
        /// Returns the host model transform position, falling back to stock vessel positioning.
        /// </summary>
        internal static Vector3d GetConstructionOrigin(Vessel vessel)
        {
            if (HasActiveHost && ActiveHost.ConstructionTransform != null)
                return ActiveHost.ConstructionTransform.position;

            return vessel != null ? vessel.GetWorldPos3D() : Vector3d.zero;
        }

        /// <summary>
        /// Returns the position from which stock should measure access to nearby construction inventories.
        /// </summary>
        internal static Vector3 GetInventoryOrigin()
        {
            if (HasActiveHost && ActiveHost.ConstructionTransform != null)
                return ActiveHost.ConstructionTransform.position;

            Vessel activeVessel = FlightGlobals.ActiveVessel;
            return activeVessel != null ? activeVessel.transform.position : Vector3.zero;
        }

        /// <summary>
        /// Substitutes the hosted construction origin while preserving the transform supplied by ordinary stock EVA.
        /// </summary>
        internal static Vector3 GetConstructionOriginFromTransform(Transform stockOrigin)
        {
            if (HasActiveHost && ActiveHost.ConstructionTransform != null)
                return ActiveHost.ConstructionTransform.position;

            return stockOrigin != null ? stockOrigin.position : Vector3.zero;
        }

        /// <summary>
        /// Returns the host model transform used to orient stock placement calculations.
        /// </summary>
        internal static Transform GetConstructionReferenceTransform(Vessel vessel)
        {
            if (HasActiveHost && ActiveHost.ConstructionTransform != null)
                return ActiveHost.ConstructionTransform;

            return vessel != null ? vessel.ReferenceTransform : null;
        }

        /// <summary>
        /// Calls the stock weld-interruption path when a real KerbalEVA controller exists.
        /// </summary>
        internal static void InterruptWeld(KerbalEVA evaController)
        {
            if (evaController != null)
                evaController.InterruptWeld();
        }

        /// <summary>
        /// Calls the stock weld path when construction is genuinely hosted by a KerbalEVA.
        /// </summary>
        internal static void Weld(KerbalEVA evaController, Part targetPart)
        {
            if (evaController != null)
            {
                evaController.Weld(targetPart);
                return;
            }

            // Stock leaves an attached part highlighted until the Kerbal weld lifecycle and
            // construction shutdown finish. Remember hosted attachments so Deactivate can
            // perform the equivalent visual cleanup without a KerbalEVA controller.
            if (HasActiveHost && targetPart != null)
                hostedAttachedParts.Add(targetPart);
        }

        /// <summary>
        /// Restores normal flight highlighting on parts attached by a vessel-hosted construction session.
        /// </summary>
        private static void ClearHostedAttachmentHighlights()
        {
            int clearedPartCount = 0;
            foreach (Part attachedPart in hostedAttachedParts)
            {
                if (attachedPart == null)
                    continue;

                attachedPart.SetHighlightColor(Highlighting.Highlighter.colorPartHighlightDefault);
                attachedPart.SetHighlightType(Part.HighlightType.OnMouseOver);
                attachedPart.SetHighlight(false, true);
                clearedPartCount++;
            }

            hostedAttachedParts.Clear();
            if (clearedPartCount > 0)
                Debug.Log("[Sandcastle] Cleared hosted EVA Construction highlighting from " +
                    clearedPartCount + " attached part(s).");
        }
    }

    /// <summary>
    /// Installs Sandcastle's opt-in patches for the stock EVA Construction editor.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    internal sealed class EVAConstructionHarmonyLoader : MonoBehaviour
    {
        private const string HarmonyId = "com.wildblueindustries.sandcastle.evaconstruction";
        private static Harmony harmony;
        private static EVAConstructionHarmonyLoader instance;

        /// <summary>
        /// Installs Harmony patches and subscribes the bridge to stock construction lifecycle events.
        /// </summary>
        public void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            instance = this;
            try
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(typeof(EVAConstructionHarmonyLoader).Assembly);
                GameEvents.OnEVAConstructionMode.Add(OnEVAConstructionMode);
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselSwitching.Add(OnVesselSwitching);
                GameEvents.onVesselSwitchingToUnloaded.Add(OnVesselSwitching);
                GameEvents.onCrewOnEva.Add(OnCrewOnEva);
                GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
                DontDestroyOnLoad(gameObject);
                Debug.Log("[Sandcastle] Experimental vessel-hosted EVA Construction patches installed.");
            }
            catch (Exception ex)
            {
                if (harmony != null)
                    harmony.UnpatchAll(HarmonyId);

                harmony = null;
                instance = null;
                Debug.LogError("[Sandcastle] Unable to install EVA Construction patches: " + ex);
                Destroy(this);
            }
        }

        /// <summary>
        /// Unsubscribes lifecycle events and restores any UI still owned by an active host.
        /// </summary>
        public void OnDestroy()
        {
            if (instance != this)
                return;

            GameEvents.OnEVAConstructionMode.Remove(OnEVAConstructionMode);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onVesselSwitching.Remove(OnVesselSwitching);
            GameEvents.onVesselSwitchingToUnloaded.Remove(OnVesselSwitching);
            GameEvents.onCrewOnEva.Remove(OnCrewOnEva);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            EVAConstructionBridge.Deactivate();
            instance = null;
        }

        /// <summary>
        /// Enforces hosted-construction UI state after stock flight UI has updated for the frame.
        /// </summary>
        public void LateUpdate()
        {
            EVAConstructionBridge.MaintainHostUI();
        }

        /// <summary>
        /// Releases the bridge whenever the stock construction panel reports that it closed.
        /// </summary>
        private void OnEVAConstructionMode(bool opened)
        {
            if (!opened)
                EVAConstructionBridge.Deactivate();
        }

        /// <summary>
        /// Closes part-hosted construction if the final active vessel differs from the host vessel.
        /// </summary>
        private void OnVesselChange(Vessel vessel)
        {
            WBIEVAConstructionManipulator host = EVAConstructionBridge.ActiveHost;
            if (host == null)
                return;

            Vessel hostVessel = host.part != null ? host.part.vessel : null;
            if (vessel != hostVessel)
                EVAConstructionBridge.CloseHostedConstruction();
        }

        /// <summary>
        /// Closes part-hosted construction at the start of a loaded or unloaded vessel switch.
        /// </summary>
        private void OnVesselSwitching(Vessel fromVessel, Vessel toVessel)
        {
            WBIEVAConstructionManipulator host = EVAConstructionBridge.ActiveHost;
            if (host == null)
                return;

            Vessel hostVessel = host.part != null ? host.part.vessel : null;
            if (toVessel != hostVessel)
                EVAConstructionBridge.CloseHostedConstruction();
        }

        /// <summary>
        /// Closes part-hosted construction whenever any crew member goes on EVA.
        /// </summary>
        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (EVAConstructionBridge.ActiveHost != null)
                EVAConstructionBridge.CloseHostedConstruction();
        }

        /// <summary>
        /// Releases hosted construction before leaving the current KSP scene.
        /// </summary>
        private void OnGameSceneLoadRequested(GameScenes scene)
        {
            EVAConstructionBridge.Deactivate();
        }
    }

    [HarmonyPatch]
    internal static class EVAConstructionCanOpenPatch
    {
        /// <summary>
        /// Locates stock's private construction-panel eligibility method.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EVAConstructionModeController), "CanOpenConstructionPanel");
        }

        /// <summary>
        /// Supplies host-aware eligibility while preserving stock behavior for ordinary EVA.
        /// </summary>
        private static bool Prefix(ref bool __result)
        {
            if (!EVAConstructionBridge.HasActiveHost)
                return true;

            __result = EVAConstructionBridge.CanOpenConstructionPanel();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class EVAConstructionWeightLimitPatch
    {
        /// <summary>
        /// Locates the weight-limit getter consumed by stock checks and UI labels.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(EVAConstructionModeEditor), nameof(EVAConstructionModeEditor.CombinedConstructionWeightLimit));
        }

        /// <summary>
        /// Reports the manipulator limit while hosted construction is active.
        /// </summary>
        private static bool Prefix(ref double __result)
        {
            if (!EVAConstructionBridge.HasActiveHost)
                return true;

            __result = EVAConstructionBridge.GetManipulatorConstructionWeightLimit();
            return false;
        }
    }

    [HarmonyPatch(typeof(Part), nameof(Part.IsUnderConstructionWeightLimit))]
    internal static class EVAConstructionPartMassCheckPatch
    {
        /// <summary>
        /// Replaces stock's Kerbal mass predicate only during part-hosted construction.
        /// </summary>
        private static bool Prefix(Part __instance, ref bool __result)
        {
            if (!EVAConstructionBridge.HasActiveHost)
                return true;

            __result = EVAConstructionBridge.IsUnderManipulatorMassLimit(__instance);
            return false;
        }
    }

    /// <summary>
    /// Gives vessel-hosted stack attachment the deterministic node-frame roll alignment used by KIS.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionStackNodeAlignmentPatch
    {
        private static readonly FieldInfo SelectedPartOriginalRotation =
            AccessTools.Field(typeof(EVAConstructionModeEditor), "selectedPartOriginalRotation");
        private static readonly Quaternion OpposingNodeFrame = Quaternion.AngleAxis(180.0f, Vector3.up);

        private static EVAConstructionModeEditor trackedEditor;
        private static Part trackedSelectedPart;
        private static AttachNode trackedSourceNode;
        private static Part trackedTargetPart;
        private static AttachNode trackedTargetNode;

        /// <summary>
        /// Locates stock's private attachment test so its completed stack result can be adjusted.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EVAConstructionModeEditor), "CheckAttach", new Type[] { typeof(Part) });
        }

        /// <summary>
        /// Initializes roll when a new stack-node pair is acquired without overriding later player rotations.
        /// </summary>
        private static void Postfix(EVAConstructionModeEditor __instance, Part selPart, Attachment __result)
        {
            if (!EVAConstructionBridge.IsStackNodeAlignmentEnabled() ||
                __result == null || __result.mode != AttachModes.STACK ||
                __result.callerPartNode == null || __result.otherPartNode == null ||
                __result.potentialParent == null)
            {
                ResetTracking();
                return;
            }

            AttachNode sourceNode = __result.callerPartNode;
            Part targetPart = __result.potentialParent;
            AttachNode targetNode = __result.otherPartNode;
            if (IsTrackedPair(__instance, selPart, sourceNode, targetPart, targetNode))
                return;

            TrackPair(__instance, selPart, sourceNode, targetPart, targetNode);

            Quaternion sourceNodeLocalRotation;
            Quaternion targetNodeWorldRotation;
            if (!TryGetSourceNodeLocalRotation(selPart, sourceNode, out sourceNodeLocalRotation) ||
                !TryGetTargetNodeWorldRotation(targetPart, targetNode, out targetNodeWorldRotation))
            {
                Debug.LogWarning("[Sandcastle] Unable to initialize EVA Construction stack-node roll alignment because a node has no usable orientation.");
                return;
            }

            // Face the source node into the target node while retaining the target frame's up axis.
            Quaternion finalPartRotation =
                targetNodeWorldRotation * OpposingNodeFrame * Quaternion.Inverse(sourceNodeLocalRotation);

            // Stock applies attRotation after this baseline. Keeping it separate allows the normal
            // editor rotation keys and gizmo to continue working after the one-time alignment.
            Quaternion baselineRotation = finalPartRotation * Quaternion.Inverse(selPart.attRotation);
            SelectedPartOriginalRotation.SetValue(__instance, baselineRotation);
            __result.rotation = baselineRotation;
            __result.position =
                targetPart.transform.TransformPoint(targetNode.position + targetNode.offset) -
                finalPartRotation * sourceNode.position;

            Debug.Log("[Sandcastle] Initialized EVA Construction stack-node roll alignment for " +
                      selPart.partInfo.title + " on " + targetPart.partInfo.title + ".");
        }

        /// <summary>
        /// Clears the remembered node pair so the next snap receives a fresh initial alignment.
        /// </summary>
        internal static void ResetTracking()
        {
            trackedEditor = null;
            trackedSelectedPart = null;
            trackedSourceNode = null;
            trackedTargetPart = null;
            trackedTargetNode = null;
        }

        /// <summary>
        /// Reports whether stock is still evaluating the node pair that was already initialized.
        /// </summary>
        private static bool IsTrackedPair(EVAConstructionModeEditor editor, Part selectedPart,
            AttachNode sourceNode, Part targetPart, AttachNode targetNode)
        {
            return trackedEditor == editor &&
                   trackedSelectedPart == selectedPart &&
                   trackedSourceNode == sourceNode &&
                   trackedTargetPart == targetPart &&
                   trackedTargetNode == targetNode;
        }

        /// <summary>
        /// Remembers a node pair before attempting alignment to avoid repeated warnings on bad nodes.
        /// </summary>
        private static void TrackPair(EVAConstructionModeEditor editor, Part selectedPart,
            AttachNode sourceNode, Part targetPart, AttachNode targetNode)
        {
            trackedEditor = editor;
            trackedSelectedPart = selectedPart;
            trackedSourceNode = sourceNode;
            trackedTargetPart = targetPart;
            trackedTargetNode = targetNode;
        }

        /// <summary>
        /// Gets the source node's complete frame relative to the selected part.
        /// </summary>
        private static bool TryGetSourceNodeLocalRotation(Part selectedPart, AttachNode node,
            out Quaternion rotation)
        {
            if (node.nodeTransform != null)
            {
                rotation = Quaternion.Inverse(selectedPart.transform.rotation) * node.nodeTransform.rotation;
                return IsUsable(rotation);
            }

            return TryCreateNodeRotation(node.orientation, out rotation);
        }

        /// <summary>
        /// Gets the target node's complete world-space frame, including its vessel-relative roll.
        /// </summary>
        private static bool TryGetTargetNodeWorldRotation(Part targetPart, AttachNode node,
            out Quaternion rotation)
        {
            if (node.nodeTransform != null)
            {
                rotation = node.nodeTransform.rotation;
                return IsUsable(rotation);
            }

            Quaternion localRotation;
            if (!TryCreateNodeRotation(node.orientation, out localRotation))
            {
                rotation = Quaternion.identity;
                return false;
            }

            rotation = targetPart.transform.rotation * localRotation;
            return IsUsable(rotation);
        }

        /// <summary>
        /// Creates the same orientation-based node frame that KIS uses for config-defined nodes.
        /// </summary>
        private static bool TryCreateNodeRotation(Vector3 orientation, out Quaternion rotation)
        {
            if (orientation.sqrMagnitude < 1E-08f)
            {
                rotation = Quaternion.identity;
                return false;
            }

            rotation = Quaternion.LookRotation(orientation.normalized);
            return IsUsable(rotation);
        }

        /// <summary>
        /// Rejects invalid quaternion values before they can corrupt the selected part transform.
        /// </summary>
        private static bool IsUsable(Quaternion rotation)
        {
            return IsFinite(rotation.x) && IsFinite(rotation.y) &&
                   IsFinite(rotation.z) && IsFinite(rotation.w);
        }

        /// <summary>
        /// Reports whether a floating-point component is neither NaN nor infinite.
        /// </summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Adds persistent zero buoyancy to loose parts placed by landed underwater EVA Construction actors.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionUnderwaterProtoVesselPatch
    {
        /// <summary>
        /// Locates the stock method that builds the proto-vessel for a loose construction part.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(EVAConstructionModeEditor),
                "GetProtoVesselNode",
                new Type[] { typeof(string), typeof(Vector3), typeof(Quaternion), typeof(Vessel), typeof(Part) });
        }

        /// <summary>
        /// Applies the underwater policy using the stock construction actor supplied to the spawn method.
        /// </summary>
        private static void Postfix(Vessel vessel, ConfigNode __result)
        {
            global::Sandcastle.UnderwaterSpawnUtils.ApplyToProtoVessel(
                __result, vessel, "EVA Construction");
        }
    }

    /// <summary>
    /// Adds persistent zero buoyancy to parts attached by landed underwater EVA Construction actors.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionUnderwaterAttachedPartPatch
    {
        /// <summary>
        /// Locates the stock method that converts a held cargo part into a live attached part.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(EVAConstructionModeEditor),
                "AttachPart",
                new Type[] { typeof(Part), typeof(Attachment) });
        }

        /// <summary>
        /// Applies the underwater policy after stock has created and welded the attached part.
        /// </summary>
        private static void Postfix(Part __result)
        {
            Vessel actorVessel = EVAConstructionBridge.HasActiveHost
                ? EVAConstructionBridge.ActiveHost.part.vessel
                : FlightGlobals.ActiveVessel;
            global::Sandcastle.UnderwaterSpawnUtils.ApplyToPart(
                __result, actorVessel, "EVA Construction");
        }
    }

    /// <summary>
    /// Converts vessel-hosted terrain placement of a ground part into stock's ground-deployment state.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionGroundPartDeploymentPatch
    {
        private static readonly FieldInfo IsPlacementOnGroundField =
            AccessTools.Field(typeof(EVAConstructionModeEditor), "isPlacementOnGround");

        /// <summary>
        /// Locates the stock method that builds the proto-vessel for a part dropped by EVA Construction.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(EVAConstructionModeEditor),
                "GetProtoVesselNode",
                new Type[] { typeof(string), typeof(Vector3), typeof(Quaternion), typeof(Vessel), typeof(Part) });
        }

        /// <summary>
        /// Gives a hosted, terrain-placed ModuleGroundPart the same startup state as an inventory deployment.
        /// </summary>
        private static void Postfix(EVAConstructionModeEditor __instance, Part part, ConfigNode __result)
        {
            if (!EVAConstructionBridge.HasActiveHost ||
                __instance == null ||
                part == null ||
                __result == null ||
                IsPlacementOnGroundField == null ||
                part.FindModuleImplementing<ModuleGroundPart>() == null ||
                !IsTerrainPlacement(__instance, part))
            {
                return;
            }

            bool groundPartStateUpdated = false;
            foreach (ConfigNode partNode in __result.GetNodes("PART"))
            {
                foreach (ConfigNode moduleNode in partNode.GetNodes("MODULE"))
                {
                    string moduleName = string.Empty;
                    if (!moduleNode.TryGetValue("name", ref moduleName) || moduleName != nameof(ModuleGroundPart))
                        continue;

                    moduleNode.SetValue("beingDeployed", true, true);
                    moduleNode.SetValue("beingSettled", true, true);
                    groundPartStateUpdated = true;
                }
            }

            if (!groundPartStateUpdated)
            {
                Debug.LogWarning("[Sandcastle] Hosted terrain placement found ModuleGroundPart on " +
                    part.partInfo.title + " but not in its proto-part snapshot.");
                return;
            }

            // ModuleGroundPart disables itself on DroppedPart vessels. Unknown is the stock inventory-deployment
            // state, and ModuleGroundPart changes it to DeployedGroundPart after successful ground contact.
            __result.SetValue("type", VesselType.Unknown.ToString(), false);
            if (part.vessel != null && part.vessel.mainBody != null)
                __result.SetValue("landedAt", part.vessel.mainBody.name, true);

            Debug.Log("[Sandcastle] Prepared " + part.partInfo.title +
                " for hosted ModuleGroundPart terrain deployment.");
        }

        /// <summary>
        /// Recovers stock ground placement when its cursor ray misses but the fallback placement plane leaves the part on terrain.
        /// </summary>
        private static bool IsTerrainPlacement(EVAConstructionModeEditor editor, Part part)
        {
            if ((bool)IsPlacementOnGroundField.GetValue(editor))
                return true;

            WBIEVAConstructionManipulator host = EVAConstructionBridge.ActiveHost;
            if (host == null || host.part == null || host.part.vessel == null || host.part.vessel.mainBody == null)
                return false;

            Vector3 upAxis = (Vector3)FlightGlobals.getUpAxis(host.part.vessel.mainBody, part.transform.position);
            Bounds partBounds = PartGeometryUtil.MergeBounds(
                PartGeometryUtil.GetPartRendererBounds(part),
                part.transform);
            float probeDistance = Math.Max(0.5f,
                partBounds.extents.magnitude + editor.placementGroundOffset + 0.5f);
            RaycastHit groundHit;

            if (!Physics.Raycast(part.transform.position + upAxis * 0.05f, -upAxis, out groundHit, probeDistance, 32768))
                return false;

            Debug.Log("[Sandcastle] Recovered near-ground placement for " + part.partInfo.title +
                " after the stock EVA Construction ground ray missed.");
            return true;
        }
    }

    /// <summary>
    /// Makes mounted cargo parts use the manipulator origin and range when deciding construction eligibility.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionCargoPartHighlightPatch
    {
        private static readonly MethodInfo TransformPositionGetter =
            AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
        private static readonly FieldInfo StockConstructionDistance =
            AccessTools.Field(typeof(GameSettings), nameof(GameSettings.EVA_CONSTRUCTION_RANGE));
        private static readonly MethodInfo GetConstructionOriginMethod =
            AccessTools.Method(
                typeof(EVAConstructionBridge),
                nameof(EVAConstructionBridge.GetConstructionOriginFromTransform),
                new Type[] { typeof(Transform) });
        private static readonly MethodInfo GetConstructionDistanceMethod =
            AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetConstructionDistance));

        /// <summary>
        /// Locates the stock cargo-part update that highlights parts eligible for vessel detachment.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ModuleCargoPart), "OnUpdateHighlight");
        }

        /// <summary>
        /// Replaces the active-vessel origin and Kerbal range while leaving ordinary EVA behavior unchanged.
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>(instructions);
            int originReplacements = 0;
            int distanceReplacements = 0;

            foreach (CodeInstruction instruction in patchedInstructions)
            {
                if (Equals(instruction.operand, StockConstructionDistance))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetConstructionDistanceMethod;
                    distanceReplacements++;
                    continue;
                }

                // The first Transform.position read is the active-vessel origin. The next
                // belongs to this cargo part and must remain stock so distance stays meaningful.
                if (originReplacements != 0 || !Equals(instruction.operand, TransformPositionGetter))
                    continue;

                instruction.opcode = OpCodes.Call;
                instruction.operand = GetConstructionOriginMethod;
                originReplacements++;
            }

            if (originReplacements != 1 || distanceReplacements != 2)
            {
                Debug.LogWarning("[Sandcastle] EVA Construction cargo highlight patch was only partially applied: origins=" +
                    originReplacements + ", distance limits=" + distanceReplacements + ".");
            }

            return patchedInstructions;
        }
    }

    /// <summary>
    /// Makes the stock construction inventory list use the vessel-hosted workspace origin and distance.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionInventoryDisplayPatch
    {
        private static readonly MethodInfo TransformPositionGetter =
            AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
        private static readonly MethodInfo StockVectorDistance =
            AccessTools.Method(typeof(Vector3), nameof(Vector3.Distance),
                new Type[] { typeof(Vector3), typeof(Vector3) });
        private static readonly FieldInfo StockInventoryDistance =
            AccessTools.Field(typeof(GameSettings), nameof(GameSettings.EVA_INVENTORY_RANGE));
        private static readonly MethodInfo GetInventoryOriginFromTransformMethod =
            AccessTools.Method(
                typeof(EVAConstructionBridge),
                nameof(EVAConstructionBridge.GetConstructionOriginFromTransform),
                new Type[] { typeof(Transform) });
        private static readonly MethodInfo GetInventoryDistanceMethod =
            AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetInventoryDistance));
        private static readonly MethodInfo GetInventoryDisplayDistanceMethod =
            AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetInventoryDisplayDistance));

        /// <summary>
        /// Locates the stock method that adds and removes inventory panes as their distance changes.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EVAConstructionModeController), "UpdateDisplayedInventories");
        }

        /// <summary>
        /// Replaces the active-vessel origin and fixed EVA inventory radius used by the stock inventory pane.
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>(instructions);
            int originReplacements = 0;
            int distanceReplacements = 0;
            int distanceCalculationReplacements = 0;

            for (int index = 0; index < patchedInstructions.Count; index++)
            {
                CodeInstruction instruction = patchedInstructions[index];
                if (Equals(instruction.operand, StockInventoryDistance))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetInventoryDistanceMethod;
                    distanceReplacements++;
                    continue;
                }

                if (Equals(instruction.operand, StockVectorDistance))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetInventoryDisplayDistanceMethod;
                    distanceCalculationReplacements++;
                    continue;
                }

                // The first Transform.position read initializes the origin from ActiveVessel.
                // Later reads belong to the candidate inventory and must remain unchanged.
                if (originReplacements != 0 || !Equals(instruction.operand, TransformPositionGetter))
                    continue;

                instruction.opcode = OpCodes.Call;
                instruction.operand = GetInventoryOriginFromTransformMethod;
                originReplacements++;
            }

            if (originReplacements != 1 || distanceReplacements != 2 || distanceCalculationReplacements != 2)
            {
                // Inventory display support is an enhancement; never let a future stock/mod IL variation
                // abort the core Harmony patches that make vessel-hosted construction available.
                Debug.LogWarning("[Sandcastle] EVA Construction inventory display patch was only partially applied: origins=" +
                    originReplacements + ", distance limits=" + distanceReplacements +
                    ", distance calculations=" + distanceCalculationReplacements + ".");
            }

            return patchedInstructions;
        }
    }

    /// <summary>
    /// Lets stock inventory slot interactions use the same range calculation as the hosted inventory display.
    /// </summary>
    [HarmonyPatch(typeof(UIPartActionControllerInventory), nameof(UIPartActionControllerInventory.IsKerbalWithinRange))]
    internal static class EVAConstructionInventoryInteractionPatch
    {
        /// <summary>
        /// Evaluates inventory access from the manipulator instead of stock's absent EVA Kerbal.
        /// </summary>
        private static bool Prefix(ModuleInventoryPart inventoryPart, ref bool __result)
        {
            if (!EVAConstructionBridge.HasActiveHost)
                return true;

            if (inventoryPart == null)
            {
                __result = false;
                return false;
            }

            if (EVAConstructionBridge.IsHostInventory(inventoryPart))
            {
                __result = true;
                return false;
            }

            __result = Vector3.Distance(
                EVAConstructionBridge.GetInventoryOrigin(),
                inventoryPart.transform.position) <= EVAConstructionBridge.GetInventoryDistance();
            return false;
        }
    }

    /// <summary>
    /// Routes a simple deployed ground part through stock's loose-part pickup and inventory cursor workflow.
    /// </summary>
    [HarmonyPatch]
    internal static class EVAConstructionGroundPartPickupPatch
    {
        private static readonly FieldInfo DeployedOnGroundField =
            AccessTools.Field(typeof(ModuleGroundPart), "deployedOnGround");

        /// <summary>
        /// Stores the original ground state so a rejected pickup can leave the deployed vessel untouched.
        /// </summary>
        private sealed class GroundPartPickupState
        {
            internal Vessel vessel;
            internal ModuleGroundPart groundPart;
            internal VesselType vesselType;
            internal bool deployedOnGround;
        }

        /// <summary>
        /// Locates stock's click-to-pick-up implementation.
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EVAConstructionModeEditor), "PickupPart");
        }

        /// <summary>
        /// Temporarily presents a deployed ground part as loose cargo so stock can create the held inventory part.
        /// </summary>
        private static void Prefix(out GroundPartPickupState __state)
        {
            __state = null;
            if (!EVAConstructionBridge.HasActiveHost || DeployedOnGroundField == null)
                return;

            Part hoveredPart = Mouse.HoveredPart;
            if (hoveredPart == null ||
                hoveredPart.vessel == null ||
                hoveredPart.vessel.vesselType != VesselType.DeployedGroundPart ||
                hoveredPart.vessel.parts == null ||
                hoveredPart.vessel.parts.Count != 1 ||
                hoveredPart.FindModuleImplementing<ModuleGroundSciencePart>() != null ||
                hoveredPart.FindModuleImplementing<ModuleGroundExpControl>() != null)
            {
                return;
            }

            ModuleGroundPart groundPart = hoveredPart.FindModuleImplementing<ModuleGroundPart>();
            if (groundPart == null)
                return;

            __state = new GroundPartPickupState
            {
                vessel = hoveredPart.vessel,
                groundPart = groundPart,
                vesselType = hoveredPart.vessel.vesselType,
                deployedOnGround = (bool)DeployedOnGroundField.GetValue(groundPart)
            };

            // Stock PickupPart accepts only DroppedPart and Debris. ModuleGroundPart.RetrievePart performs
            // the equivalent deployed-state reset before taking its inventory snapshot for a Kerbal.
            DeployedOnGroundField.SetValue(groundPart, false);
            hoveredPart.vessel.vesselType = VesselType.DroppedPart;
        }

        /// <summary>
        /// Keeps the cargo state after a successful pickup, or restores the deployed state if stock rejected it.
        /// </summary>
        private static void Postfix(EVAConstructionModeEditor __instance, GroundPartPickupState __state)
        {
            if (__state == null)
                return;

            Part heldPart = __instance != null ? __instance.SelectedPart : null;
            if (heldPart == null)
            {
                if (__state.vessel != null)
                    __state.vessel.vesselType = __state.vesselType;
                if (__state.groundPart != null)
                    DeployedOnGroundField.SetValue(__state.groundPart, __state.deployedOnGround);
                return;
            }

            ModuleGroundPart heldGroundPart = heldPart.FindModuleImplementing<ModuleGroundPart>();
            if (heldGroundPart != null)
                DeployedOnGroundField.SetValue(heldGroundPart, false);

            Debug.Log("[Sandcastle] Picked up deployed ground part " + heldPart.partInfo.title +
                " for hosted EVA Construction inventory storage.");
        }
    }

    [HarmonyPatch]
    internal static class EVAConstructionWorkspacePatch
    {
        private static readonly MethodInfo VesselIsEVAGetter = AccessTools.PropertyGetter(typeof(Vessel), nameof(Vessel.isEVA));
        private static readonly MethodInfo VesselWorldPosition = AccessTools.Method(typeof(Vessel), nameof(Vessel.GetWorldPos3D));
        private static readonly MethodInfo VesselReferenceTransformGetter = AccessTools.PropertyGetter(typeof(Vessel), nameof(Vessel.ReferenceTransform));
        private static readonly FieldInfo StockConstructionDistance = AccessTools.Field(typeof(GameSettings), nameof(GameSettings.EVA_CONSTRUCTION_RANGE));
        private static readonly MethodInfo IsConstructionVesselMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.IsConstructionVessel));
        private static readonly MethodInfo GetConstructionOriginMethod =
            AccessTools.Method(
                typeof(EVAConstructionBridge),
                nameof(EVAConstructionBridge.GetConstructionOrigin),
                new Type[] { typeof(Vessel) });
        private static readonly MethodInfo GetConstructionReferenceTransformMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetConstructionReferenceTransform));
        private static readonly MethodInfo GetConstructionDistanceMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetConstructionDistance));

        /// <summary>
        /// Selects stock editor methods that assume the active construction vessel is EVA.
        /// </summary>
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type editorType = typeof(EVAConstructionModeEditor);
            yield return AccessTools.Method(editorType, "UpdatePartPlacementPosition");
            yield return AccessTools.Method(editorType, "PickupPartInput");
            yield return AccessTools.Method(editorType, "ProcessAttachNodes");
            yield return AccessTools.Method(editorType, "UpdateVesselsInConstructionRange");
            yield return AccessTools.Method(editorType, "DetachInput");
        }

        /// <summary>
        /// Redirects EVA identity, workspace origin, reference transform, and construction distance through the bridge.
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>();
            int replacements = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (Equals(instruction.operand, VesselIsEVAGetter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = IsConstructionVesselMethod;
                    replacements++;
                }
                else if (Equals(instruction.operand, VesselWorldPosition))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetConstructionOriginMethod;
                    replacements++;
                }
                else if (Equals(instruction.operand, VesselReferenceTransformGetter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetConstructionReferenceTransformMethod;
                    replacements++;
                }
                else if (Equals(instruction.operand, StockConstructionDistance))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetConstructionDistanceMethod;
                    replacements++;
                }

                patchedInstructions.Add(instruction);
            }

            if (replacements == 0)
                throw new MissingMethodException("No EVA Construction workspace calls were found in " + __originalMethod.Name + ".");

            return patchedInstructions;
        }
    }

    [HarmonyPatch]
    internal static class EVAConstructionWeldPatch
    {
        private static readonly MethodInfo StockInterruptWeld = AccessTools.Method(typeof(KerbalEVA), nameof(KerbalEVA.InterruptWeld));
        private static readonly MethodInfo StockWeld = AccessTools.Method(typeof(KerbalEVA), nameof(KerbalEVA.Weld), new Type[] { typeof(Part) });
        private static readonly MethodInfo BridgeInterruptWeld = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.InterruptWeld));
        private static readonly MethodInfo BridgeWeld = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.Weld));

        /// <summary>
        /// Selects stock pickup and attachment methods that directly call KerbalEVA weld APIs.
        /// </summary>
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type editorType = typeof(EVAConstructionModeEditor);
            yield return AccessTools.Method(editorType, "PickupPart");
            yield return AccessTools.Method(editorType, "AttachPart");
        }

        /// <summary>
        /// Replaces direct KerbalEVA weld calls with null-safe bridge adapters.
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>();
            int replacements = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (Equals(instruction.operand, StockInterruptWeld))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = BridgeInterruptWeld;
                    replacements++;
                }
                else if (Equals(instruction.operand, StockWeld))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = BridgeWeld;
                    replacements++;
                }

                patchedInstructions.Add(instruction);
            }

            if (replacements == 0)
                throw new MissingMethodException("No EVA Construction weld call was found in " + __originalMethod.Name + ".");

            return patchedInstructions;
        }
    }
}
