using KSP.Localization;
using UnityEngine;

namespace Sandcastle.PartModules
{
    /// <summary>
    /// Allows a vessel-mounted manipulator to act as the origin for stock EVA Construction.
    /// This is an experimental module and requires the Sandcastle Harmony bridge.
    /// </summary>
    public class WBIEVAConstructionManipulator : PartModule
    {
        /// <summary>
        /// Model transform used as the center of the stock construction workspace.
        /// </summary>
        [KSPField(groupName = "#LOC_SANDCASTLE_evaConstructionGroupName", groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        public string constructionTransformName = "LaunchPos";

        /// <summary>
        /// Maximum movable part mass, including resources, in metric tons.
        /// </summary>
        [KSPField(groupName = "#LOC_SANDCASTLE_evaConstructionGroupName", groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        public double maxPartMass = 1000.0;

        /// <summary>
        /// Maximum distance from the construction transform at which parts can be manipulated, in meters.
        /// </summary>
        [KSPField(groupName = "#LOC_SANDCASTLE_evaConstructionGroupName", groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        public float maxConstructionDistance = 7.0f;

        private Transform constructionTransform;

        /// <summary>
        /// World-space transform used as the construction origin.
        /// </summary>
        public Transform ConstructionTransform
        {
            get
            {
                if (constructionTransform == null && part != null)
                    constructionTransform = part.FindModelTransform(constructionTransformName);

                return constructionTransform != null ? constructionTransform : part != null ? part.transform : null;
            }
        }

        /// <summary>
        /// Opens or closes the stock EVA Construction interface using this part as its host.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SANDCASTLE_openEVAConstruction",
            groupName = "#LOC_SANDCASTLE_evaConstructionGroupName", groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        public void ToggleEVAConstruction()
        {
            if (!HighLogic.LoadedSceneIsFlight || part == null || part.vessel == null)
                return;

            if (part.vessel != FlightGlobals.ActiveVessel)
            {
                ScreenMessages.PostScreenMessage(
                    Localizer.Format("#LOC_SANDCASTLE_evaConstructionActiveVessel"),
                    5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            EVAConstructionModeController controller = EVAConstructionModeController.Instance;
            if (controller == null)
            {
                ScreenMessages.PostScreenMessage(
                    Localizer.Format("#LOC_SANDCASTLE_evaConstructionUnavailable"),
                    5f,
                    ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (controller.IsOpen)
            {
                controller.ClosePanel();
                return;
            }

            EVAConstructionBridge.Activate(this);
            controller.OpenConstructionPanel();

            if (!controller.IsOpen)
            {
                EVAConstructionBridge.Deactivate(this);
                ScreenMessages.PostScreenMessage(
                    Localizer.Format("#LOC_SANDCASTLE_evaConstructionUnavailable"),
                    5f,
                    ScreenMessageStyle.UPPER_CENTER);
            }
        }

        /// <summary>
        /// Resolves the configured construction transform and initializes PAW visibility.
        /// </summary>
        /// <param name="state">KSP's current part-module startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            constructionTransform = part.FindModelTransform(constructionTransformName);

            BaseEvent toggleEvent = Events[nameof(ToggleEVAConstruction)];
            if (toggleEvent != null)
                toggleEvent.guiActive = HighLogic.LoadedSceneIsFlight;
        }

        /// <summary>
        /// Releases this part as the construction host if its part or vessel is destroyed.
        /// </summary>
        public void OnDestroy()
        {
            EVAConstructionBridge.Deactivate(this);
        }
    }
}
