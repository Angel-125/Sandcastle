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
        internal static WBIEVAConstructionManipulator ActiveHost { get; private set; }
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
            if (HasActiveHost)
                return ActiveHost.alignStackNodeRotation;

            Vessel activeVessel = FlightGlobals.ActiveVessel;
            return activeVessel != null && activeVessel.isEVA &&
                   global::Sandcastle.SandcastleSettings.AlignEVAConstructionStackNodes;
        }

        /// <summary>
        /// Makes a part module the active stock-construction host and hides conflicting flight UI.
        /// </summary>
        internal static void Activate(WBIEVAConstructionManipulator host)
        {
            EVAConstructionStackNodeAlignmentPatch.ResetTracking();
            ActiveHost = host;

            if (!stageStackHidden && HighLogic.LoadedSceneIsFlight)
            {
                KSP.UI.Screens.StageManager.ShowHideStageStack(false);
                stageStackHidden = true;
            }

            HideStagingQuadrant();
            HideFlightModeFrame();

            Debug.Log("[Sandcastle] Vessel-hosted EVA Construction activated; flight staging and mode UI hidden.");
        }

        /// <summary>
        /// Releases the active host and restores every flight UI state captured during activation.
        /// </summary>
        internal static void Deactivate(WBIEVAConstructionManipulator host = null)
        {
            if (host != null && ActiveHost != host)
                return;

            bool wasActive = ActiveHost != null || stageStackHidden || stagingQuadrantHidden || flightModeFrameHidden;
            ActiveHost = null;
            EVAConstructionStackNodeAlignmentPatch.ResetTracking();

            if (stageStackHidden)
            {
                if (HighLogic.LoadedSceneIsFlight)
                    KSP.UI.Screens.StageManager.ShowHideStageStack(true);

                stageStackHidden = false;
            }

            RestoreStagingQuadrant();
            RestoreFlightModeFrame();

            if (wasActive)
                Debug.Log("[Sandcastle] Vessel-hosted EVA Construction released; flight staging and mode UI restored.");
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
                evaController.Weld(targetPart);
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
        /// Closes part-hosted construction when a crew member exits the host vessel on EVA.
        /// </summary>
        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            WBIEVAConstructionManipulator host = EVAConstructionBridge.ActiveHost;
            if (host == null)
                return;

            Vessel hostVessel = host.part != null ? host.part.vessel : null;
            if (action.from != null && action.from.vessel == hostVessel)
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
                !(bool)IsPlacementOnGroundField.GetValue(__instance) ||
                part.FindModuleImplementing<ModuleGroundPart>() == null)
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
    }

    [HarmonyPatch]
    internal static class EVAConstructionWorkspacePatch
    {
        private static readonly MethodInfo VesselIsEVAGetter = AccessTools.PropertyGetter(typeof(Vessel), nameof(Vessel.isEVA));
        private static readonly MethodInfo VesselWorldPosition = AccessTools.Method(typeof(Vessel), nameof(Vessel.GetWorldPos3D));
        private static readonly MethodInfo VesselReferenceTransformGetter = AccessTools.PropertyGetter(typeof(Vessel), nameof(Vessel.ReferenceTransform));
        private static readonly MethodInfo IsConstructionVesselMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.IsConstructionVessel));
        private static readonly MethodInfo GetConstructionOriginMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetConstructionOrigin));
        private static readonly MethodInfo GetConstructionReferenceTransformMethod = AccessTools.Method(typeof(EVAConstructionBridge), nameof(EVAConstructionBridge.GetConstructionReferenceTransform));

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
        /// Redirects EVA identity, workspace origin, and reference-transform calls through the bridge.
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
