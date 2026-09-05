using System.Collections.Generic;
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
        private const string ConstructionRangeColorPickerField = "constructionRangeColorPicker";

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

        /// <summary>
        /// Shows a field effect that marks the stock EVA Construction workspace.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, isPersistant = true,
            guiName = "#LOC_SANDCASTLE_showConstructionRange",
            groupName = "#LOC_SANDCASTLE_evaConstructionGroupName",
            groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        [UI_Toggle(enabledText = "#LOC_SANDCASTLE_constructionRangeShown",
            disabledText = "#LOC_SANDCASTLE_constructionRangeHidden")]
        public bool showConstructionRange;

        /// <summary>
        /// Stock PAW color picker for the construction range field effect.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true,
            guiName = "#LOC_SANDCASTLE_constructionRangeColor",
            groupName = "#LOC_SANDCASTLE_evaConstructionGroupName",
            groupDisplayName = "#LOC_SANDCASTLE_evaConstructionGroupName")]
        [UI_ColorPicker(useFieldNameForColor = true)]
        public string constructionRangeColorPicker;

        /// <summary>
        /// Construction range RGB color encoded as #RRGGBB.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string constructionRangeColor = "#66CCFF";

        /// <summary>
        /// Controls the opacity of the three great-circle outlines that visualize
        /// the EVA Construction range. Valid values are 0.0 (invisible) through
        /// 1.0 (fully opaque). This can be overridden in the part's
        /// WBIEVAConstructionManipulator config node.
        /// </summary>
        [KSPField]
        public float constructionRangeOpacity = 0.75f;

        private Transform constructionTransform;
        private ConstructionRangeVisualizer rangeVisualizer;

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
            constructionRangeOpacity = Mathf.Clamp01(constructionRangeOpacity);

            BaseEvent toggleEvent = Events[nameof(ToggleEVAConstruction)];
            if (toggleEvent != null)
                toggleEvent.guiActive = HighLogic.LoadedSceneIsFlight;
        }

        /// <summary>
        /// Returns the stock light-color presets for the construction range picker.
        /// </summary>
        public override List<Color> PresetColors()
        {
            return GameSettings.GetLightPresetColors();
        }

        /// <summary>
        /// Supplies the persisted construction range color to the stock picker.
        /// </summary>
        public override Color GetCurrentColor(string fieldName)
        {
            if (fieldName == ConstructionRangeColorPickerField)
                return GetConstructionRangeColor(1f);

            return base.GetCurrentColor(fieldName);
        }

        /// <summary>
        /// Persists construction range color changes made with the stock picker.
        /// </summary>
        public override void OnColorChanged(Color color, string pickerID = "")
        {
            if (pickerID != ConstructionRangeColorPickerField)
                return;

            color.a = 1f;
            constructionRangeColor = "#" + ColorUtility.ToHtmlStringRGB(color);
            if (rangeVisualizer != null)
                rangeVisualizer.UpdateAppearance(color, constructionRangeOpacity);
        }

        /// <summary>
        /// Keeps the construction range effect centered on the configured transform.
        /// </summary>
        public void LateUpdate()
        {
            bool canShow = showConstructionRange && ConstructionTransform != null &&
                (HighLogic.LoadedSceneIsEditor ||
                (HighLogic.LoadedSceneIsFlight && part.vessel != null &&
                !part.vessel.packed && !MapView.MapIsEnabled));

            if (!canShow)
            {
                if (rangeVisualizer != null)
                    rangeVisualizer.IsVisible = false;
                return;
            }

            if (rangeVisualizer == null)
                rangeVisualizer = new ConstructionRangeVisualizer();

            rangeVisualizer.IsVisible = true;
            rangeVisualizer.UpdateTransform(ConstructionTransform.position,
                Mathf.Max(0.1f, maxConstructionDistance));
            rangeVisualizer.UpdateAppearance(GetConstructionRangeColor(1f),
                constructionRangeOpacity);
        }

        /// <summary>
        /// Releases this part as the construction host if its part or vessel is destroyed.
        /// </summary>
        public void OnDestroy()
        {
            EVAConstructionBridge.Deactivate(this);
            if (rangeVisualizer != null)
            {
                rangeVisualizer.Dispose();
                rangeVisualizer = null;
            }
        }

        private Color GetConstructionRangeColor(float alpha)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(constructionRangeColor, out color))
                color = new Color(0.4f, 0.8f, 1f, 1f);

            color.a = alpha;
            return color;
        }
    }

    /// <summary>
    /// Draws three great-circle guides in world space.
    /// </summary>
    internal sealed class ConstructionRangeVisualizer
    {
        private const int DisplayLayer = 11;
        private const int RingSegments = 96;

        private readonly GameObject rootObject;
        private readonly Material ringMaterial;
        private readonly LineRenderer[] rings = new LineRenderer[3];
        private float currentRadius = -1f;
        private Color currentColor;
        private float currentOpacity = -1f;

        internal bool IsVisible
        {
            get { return rootObject.activeSelf; }
            set
            {
                if (rootObject.activeSelf != value)
                    rootObject.SetActive(value);
            }
        }

        internal ConstructionRangeVisualizer()
        {
            rootObject = new GameObject("Sandcastle EVA Construction Range");
            rootObject.layer = DisplayLayer;

            Shader ringShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (ringShader != null)
            {
                ringMaterial = new Material(ringShader)
                {
                    name = "Sandcastle EVA Construction Range Ring Material",
                    renderQueue = 3001
                };
            }

            for (int index = 0; index < rings.Length; index++)
                rings[index] = CreateRing(index);

            rootObject.SetActive(false);
        }

        internal void UpdateTransform(Vector3 position, float radius)
        {
            rootObject.transform.position = position;
            if (Mathf.Approximately(radius, currentRadius))
                return;

            currentRadius = radius;
            UpdateRingGeometry(radius);
        }

        internal void UpdateAppearance(Color color, float opacity)
        {
            opacity = Mathf.Clamp01(opacity);
            if (color == currentColor && Mathf.Approximately(opacity, currentOpacity))
                return;

            currentColor = color;
            currentOpacity = opacity;

            Color ringColor = color;
            ringColor.a = opacity;
            for (int index = 0; index < rings.Length; index++)
            {
                if (rings[index] == null)
                    continue;
                rings[index].startColor = ringColor;
                rings[index].endColor = ringColor;
            }
        }

        internal void Dispose()
        {
            if (ringMaterial != null)
                Object.Destroy(ringMaterial);
            if (rootObject != null)
                Object.Destroy(rootObject);
        }

        private LineRenderer CreateRing(int ringIndex)
        {
            GameObject ringObject = new GameObject("Construction Range Ring " + ringIndex);
            ringObject.layer = DisplayLayer;
            ringObject.transform.SetParent(rootObject.transform, false);

            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.startWidth = 0.025f;
            ring.endWidth = 0.025f;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            if (ringMaterial != null)
                ring.sharedMaterial = ringMaterial;
            else
                ring.enabled = false;
            return ring;
        }

        private void UpdateRingGeometry(float radius)
        {
            for (int ringIndex = 0; ringIndex < rings.Length; ringIndex++)
            {
                LineRenderer ring = rings[ringIndex];
                if (ring == null)
                    continue;

                for (int segment = 0; segment < RingSegments; segment++)
                {
                    float angle = Mathf.PI * 2f * segment / RingSegments;
                    float firstAxis = Mathf.Cos(angle) * radius;
                    float secondAxis = Mathf.Sin(angle) * radius;
                    Vector3 point;
                    if (ringIndex == 0)
                        point = new Vector3(firstAxis, secondAxis, 0f);
                    else if (ringIndex == 1)
                        point = new Vector3(firstAxis, 0f, secondAxis);
                    else
                        point = new Vector3(0f, firstAxis, secondAxis);
                    ring.SetPosition(segment, point);
                }
            }
        }
    }
}
