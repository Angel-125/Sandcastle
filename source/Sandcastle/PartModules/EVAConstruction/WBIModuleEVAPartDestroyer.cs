using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using KSP.Localization;
using UnityEngine;
using WildBlueCore;

namespace Sandcastle.PartModules.KerbalGear
{
    internal struct PartHighlight
    {
        public Part highlightedPart;
        public Part.HighlightType highlightType;
        public Color highlightColor;
    }

    /// <summary>
    /// Provides an EVA Kerbal with a selectable part-destruction mode. While enabled, eligible
    /// parts within range will be highlighted and the part beneath the mouse cursor can be
    /// selected for destruction.
    /// </summary>
    /// <remarks>
    /// This initial implementation intentionally contains stubs for range discovery, cursor
    /// raycasting, highlighting, and destruction. It establishes the module lifecycle and input
    /// boundaries without performing destructive operations.
    /// </remarks>
    [KSPModule("#LOC_SANDCASTLE_partDestroyerTitle")]
    public class WBIModuleEVAPartDestroyer : WBIBasePartModule
    {
        #region Constants
        private const string ToggleEventName = "TogglePartDestroyer";
        private const string EnableEventName = "#LOC_SANDCASTLE_enablePartDestroyer";
        private const string DisableEventName = "#LOC_SANDCASTLE_disablePartDestroyer";
        private const string InstructionsMessage = "#LOC_SANDCASTLE_partDestroyerInstructions";
        #endregion

        #region Fields
        /// <summary>
        /// Maximum distance in meters between the EVA Kerbal and a candidate part.
        /// </summary>
        [KSPField]
        public float destructionRange = 7.0f;

        /// <summary>
        /// Indicates whether the player has enabled part-destruction mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool partDestroyerIsEnabled;
        #endregion

        #region Housekeeping
        private readonly Dictionary<Part, PartHighlight> highlightedParts =
            new Dictionary<Part, PartHighlight>();
        private Part partUnderCursor;
        private bool kerbalGearIsActive;
        private bool uiHidden;
        private bool gamePaused;
        private bool shouldRestorHighlights;
        private Color destroyPartColor = Color.red;
        #endregion

        #region KSP Lifecycle
        /// <summary>
        /// Initializes the part destroyer and its action-window event.
        /// </summary>
        /// <param name="state">KSP's current part-module startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
            GameEvents.onGamePause.Add(onPaused);
            GameEvents.onGameUnpause.Add(onUnpaused);

            destructionRange = Mathf.Max(0.0f, destructionRange);
            updateToggleEvent();
        }

        /// <summary>
        /// Makes the toggle available when KerbalGear activates this EVA module.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();

            kerbalGearIsActive = true;
            updateToggleEvent();
            refreshContextWindows();
        }

        /// <summary>
        /// Disables destruction mode and clears its visual state when the enabling gear is removed.
        /// </summary>
        public override void OnInactive()
        {
            partDestroyerIsEnabled = false;
            kerbalGearIsActive = false;
            clearPartHighlights();
            updateToggleEvent();
            refreshContextWindows();

            base.OnInactive();
        }

        /// <summary>
        /// Updates the set of eligible, highlighted parts at the physics cadence.
        /// </summary>
        public void FixedUpdate()
        {
            if (!canOperate())
                return;

            updatePartsInRange();
        }

        /// <summary>
        /// Performs cursor raycasting and consumes the left mouse-down event at frame cadence.
        /// </summary>
        public void Update()
        {
            if (!canOperate())
                return;

            partUnderCursor = getPartUnderCursor();
            if (partUnderCursor != null && Input.GetMouseButtonDown(0))
                handleLeftClick(partUnderCursor);
        }

        /// <summary>
        /// Handles highlight restoration if needed
        /// </summary>
        public void LateUpdate()
        {
            if (!shouldRestorHighlights)
                return;

            shouldRestorHighlights = true;

            foreach (Part highlightedPart in highlightedParts.Keys)
            {
                if (highlightedPart == null)
                    continue;

                highlightedPart.SetHighlightColor(destroyPartColor);
                highlightedPart.SetHighlightType(Part.HighlightType.AlwaysOn);
                highlightedPart.SetHighlight(true, true);
            }
        }

        /// <summary>
        /// Clears transient highlighting if Unity destroys the dynamically injected module.
        /// </summary>
        public void OnDestroy()
        {
            partDestroyerIsEnabled = false;
            kerbalGearIsActive = false;
            clearPartHighlights();

            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);
            GameEvents.onGamePause.Remove(onPaused);
            GameEvents.onGameUnpause.Remove(onUnpaused);
        }

        /// <summary>
        /// Returns the localized module title displayed in the EVA Kerbal's action window.
        /// </summary>
        /// <returns>The localized part-destroyer title.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SANDCASTLE_partDestroyerTitle");
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SANDCASTLE_partDestroyerDesc");
        }
        #endregion

        #region Events
        /// <summary>
        /// Enables or disables part-destruction mode.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false,
            groupName = "#LOC_SANDCASTLE_partDestroyerTitle",
            groupDisplayName = "#LOC_SANDCASTLE_partDestroyerTitle",
            guiName = EnableEventName)]
        public void TogglePartDestroyer()
        {
            if (!HighLogic.LoadedSceneIsFlight || !kerbalGearIsActive)
                return;

            partDestroyerIsEnabled = !partDestroyerIsEnabled;
            if (!partDestroyerIsEnabled)
                clearPartHighlights();
            else
                ScreenMessages.PostScreenMessage(
                    Localizer.Format(InstructionsMessage),
                    5.0f,
                    ScreenMessageStyle.UPPER_CENTER);

            updateToggleEvent();
            refreshContextWindows();
        }
        #endregion

        #region Interaction Stubs
        /// <summary>
        /// Finds eligible parts within destructionRange, highlights newly eligible parts, and
        /// removes highlighting from parts that have left the range.
        /// </summary>
        private void updatePartsInRange()
        {
            Vector3 kerbalWorldPosition = part.transform.position;
            List<Part> doomed = new List<Part>();

            // Clear parts in our list that are out of range.
            foreach (KeyValuePair<Part, PartHighlight> highlightedEntry in highlightedParts)
            {
                Part highlightedPart = highlightedEntry.Key;
                if (highlightedPart == null)
                {
                    doomed.Add(highlightedPart);
                    continue;
                }

                float partDistance = Vector3.Distance(highlightedPart.transform.position, kerbalWorldPosition);
                if (partDistance > destructionRange)
                    doomed.Add(highlightedPart);
            }

            // Bring out your dead...
            foreach (Part doomedPart in doomed)
            {
                PartHighlight savedHighlight;
                if (!highlightedParts.TryGetValue(doomedPart, out savedHighlight))
                    continue;

                highlightedParts.Remove(doomedPart);
                restorePartHighlight(savedHighlight);
            }

            // Query loaded parts around the EVA Kerbal, reject unsafe candidates, and
            // synchronize highlightedParts with the resulting eligible set.
            PartHighlight highlightedPartToAdd;
            foreach (Vessel loadedVessel in FlightGlobals.VesselsLoaded)
            {
                foreach (Part vesselPart in loadedVessel.Parts)
                {
                    if (vesselPart == null || vesselPart == part || highlightedParts.ContainsKey(vesselPart))
                    {
                        continue;
                    }

                    // Exclude parts out of range
                    float partDistance = Vector3.Distance(vesselPart.transform.position, kerbalWorldPosition);
                    if (partDistance > destructionRange)
                        continue;

                    // Only parts without child parts can be selected
                    if (vesselPart.children.Count > 0)
                    {
                        continue;
                    }

                    // Ok, we good
                    highlightedPartToAdd = new PartHighlight
                    {
                        highlightedPart = vesselPart,
                        highlightType = vesselPart.highlightType,
                        highlightColor = vesselPart.highlightColor
                    };

                    highlightedParts.Add(vesselPart, highlightedPartToAdd);

                    vesselPart.SetHighlightColor(destroyPartColor);
                    vesselPart.SetHighlightType(Part.HighlightType.AlwaysOn);
                    vesselPart.SetHighlight(true, true);
                }
            }
        }

        /// <summary>
        /// Finds the eligible part beneath the mouse cursor.
        /// </summary>
        /// <returns>The eligible part under the cursor, or null when there is no valid target.</returns>
        private Part getPartUnderCursor()
        {
            Part hoveredPart = Mouse.HoveredPart;

            if (hoveredPart == null || !highlightedParts.ContainsKey(hoveredPart))
            {
                return null;
            }

            return hoveredPart;
        }

        /// <summary>
        /// Handles a left click on an eligible part.
        /// </summary>
        /// <param name="targetPart">The eligible part selected by the player.</param>
        private void handleLeftClick(Part targetPart)
        {
            if (targetPart == null || !highlightedParts.ContainsKey(targetPart))
            {
                return;
            }

            restorePartHighlight(highlightedParts[targetPart]);
            highlightedParts.Remove(targetPart);
            targetPart.explode(0.001f);
        }
        #endregion

        #region Helpers
        private void onHideUI()
        {
            uiHidden = true;
            hideTrackedHighlights();
        }

        private void onShowUI()
        {
            uiHidden = false;
            shouldRestorHighlights = true;
        }

        private void onPaused()
        {
            gamePaused = true;
            hideTrackedHighlights();
        }

        private void onUnpaused()
        {
            gamePaused = false;
            shouldRestorHighlights = true;
        }

        private void hideTrackedHighlights()
        {
            foreach (Part highlightedPart in highlightedParts.Keys)
            {
                if (highlightedPart != null)
                    highlightedPart.SetHighlight(false, true);
            }
        }

        /// <summary>
        /// Restores normal highlighting on every part tracked by this module.
        /// </summary>
        private void clearPartHighlights()
        {
            foreach (PartHighlight savedHighlight in highlightedParts.Values)
                restorePartHighlight(savedHighlight);

            highlightedParts.Clear();
            partUnderCursor = null;
        }

        /// <summary>
        /// Restores the highlight settings captured before this module highlighted a part.
        /// </summary>
        /// <param name="savedHighlight">The part and its previous highlight settings.</param>
        private void restorePartHighlight(PartHighlight savedHighlight)
        {
            if (savedHighlight.highlightedPart == null)
                return;

            savedHighlight.highlightedPart.SetHighlight(false, true);
            savedHighlight.highlightedPart.SetHighlightColor(savedHighlight.highlightColor);
            savedHighlight.highlightedPart.SetHighlightType(savedHighlight.highlightType);
        }

        /// <summary>
        /// Reports whether the module should currently process highlighting and input.
        /// </summary>
        /// <returns>True while an active EVA Kerbal has explicitly enabled destruction mode.</returns>
        private bool canOperate()
        {
            if (uiHidden || gamePaused)
                return false;

            return HighLogic.LoadedSceneIsFlight && kerbalGearIsActive &&
                partDestroyerIsEnabled && enabled && moduleIsEnabled && part != null &&
                part.vessel != null && part.vessel == FlightGlobals.ActiveVessel &&
                part.FindModuleImplementing<KerbalEVA>() != null;
        }

        /// <summary>
        /// Updates availability and localized text for the action-window toggle.
        /// </summary>
        private void updateToggleEvent()
        {
            if (Events == null || !Events.Contains(ToggleEventName))
                return;

            BaseEvent toggleEvent = Events[ToggleEventName];
            bool eventIsAvailable = HighLogic.LoadedSceneIsFlight && kerbalGearIsActive;
            toggleEvent.active = eventIsAvailable;
            toggleEvent.guiActive = eventIsAvailable;
            toggleEvent.guiName = Localizer.Format(partDestroyerIsEnabled
                ? DisableEventName
                : EnableEventName);
        }

        /// <summary>
        /// Refreshes any open action window after toggle state changes.
        /// </summary>
        private void refreshContextWindows()
        {
            if (part != null)
                MonoUtilities.RefreshContextWindows(part);
        }
        #endregion
    }
}
