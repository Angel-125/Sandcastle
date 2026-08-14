using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sandcastle
{
    /// <summary>
    /// Levels a deck above a ground-attached pivot and projects visual pylons into the terrain.
    /// </summary>
    public class WBIGroundPlatform : PartModule
    {
        /// <summary>
        /// Transform used as the invisible mast pivot. If empty, the part transform is used.
        /// </summary>
        [KSPField]
        public string pivotTransformName = string.Empty;

        /// <summary>
        /// Transform that represents the level platform deck.
        /// </summary>
        [KSPField]
        public string deckTransformName = "deck";

        /// <summary>
        /// Height above the pivot point where the deck is placed after leveling.
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveUnfocused = true,
            guiName = "Mast Height", guiUnits = "m", unfocusedRange = 4.0f)]
        [UI_FloatRange(scene = UI_Scene.Flight, minValue = 0.5f, maxValue = 20.0f, stepIncrement = 0.1f)]
        public float mastHeight = 4.0f;

        /// <summary>
        /// Minimum in-flight mast height allowed by the PAW slider.
        /// </summary>
        [KSPField]
        public float minMastHeight = 0.5f;

        /// <summary>
        /// Maximum in-flight mast height allowed by the PAW slider.
        /// </summary>
        [KSPField]
        public float maxMastHeight = 20.0f;

        /// <summary>
        /// Increment used by the in-flight mast height PAW slider.
        /// </summary>
        [KSPField]
        public float mastHeightStep = 0.1f;

        /// <summary>
        /// Comma-separated list of pylon shaft transforms. Each shaft raycasts along its local +Z axis.
        /// </summary>
        [KSPField]
        public string pylonShaftTransformNames = string.Empty;

        /// <summary>
        /// Comma-separated list of pylon foot transforms, matching pylonShaftTransformNames by index.
        /// </summary>
        [KSPField]
        public string pylonFootTransformNames = string.Empty;

        /// <summary>
        /// Local shaft axis used for pylon length and terrain raycasts. Defaults to +Z.
        /// </summary>
        [KSPField]
        public Vector3 pylonAxis = Vector3.forward;

        /// <summary>
        /// Local foot axis that should point away from the terrain surface. Defaults to +Y.
        /// </summary>
        [KSPField]
        public Vector3 footUpAxis = Vector3.up;

        /// <summary>
        /// Maximum raycast distance used to find terrain beneath each pylon.
        /// </summary>
        [KSPField]
        public float pylonMaxLength = 12.0f;

        /// <summary>
        /// Extra distance pushed below the terrain hit point so feet look embedded.
        /// </summary>
        [KSPField]
        public float pylonEmbedDepth = 0.15f;

        /// <summary>
        /// Original model length represented by a shaft scale of 1.
        /// </summary>
        [KSPField]
        public float pylonModelLength = 1.0f;

        /// <summary>
        /// Comma-separated attach node names that should be synced after deck alignment.
        /// </summary>
        [KSPField]
        public string attachNodeNames = string.Empty;

        /// <summary>
        /// Comma-separated marker transform names that drive attachNodeNames by index.
        /// </summary>
        [KSPField]
        public string attachNodeMarkerNames = string.Empty;

        /// <summary>
        /// Enables a PAW event that reruns alignment when the configured attach nodes are empty.
        /// </summary>
        [KSPField]
        public bool showRealignEvent = true;

        /// <summary>
        /// Enables concise diagnostics for platform alignment.
        /// </summary>
        [KSPField]
        public bool debugLog;

        /// <summary>
        /// Indicates that the platform has solved and saved its deck and pylon transforms.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool platformAligned;

        /// <summary>
        /// Saved local deck position.
        /// </summary>
        [KSPField(isPersistant = true)]
        public Vector3 savedDeckLocalPosition;

        /// <summary>
        /// Saved local deck rotation.
        /// </summary>
        [KSPField(isPersistant = true)]
        public Quaternion savedDeckLocalRotation = Quaternion.identity;

        /// <summary>
        /// Saved local pylon shaft positions.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string savedPylonLocalPositions = string.Empty;

        /// <summary>
        /// Saved local pylon shaft rotations.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string savedPylonLocalRotations = string.Empty;

        /// <summary>
        /// Saved local pylon shaft scales.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string savedPylonLocalScales = string.Empty;

        /// <summary>
        /// Saved local pylon foot positions.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string savedFootLocalPositions = string.Empty;

        /// <summary>
        /// Saved local pylon foot rotations.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string savedFootLocalRotations = string.Empty;

        private const int TerrainLayerMask = 32768;
        private const int AlignmentDelayFrames = 6;
        private const float TinyDirection = 0.0001f;

        private ModuleGroundPart groundPart;
        private Transform pivotTransform;
        private Transform deckTransform;
        private Transform[] pylonShafts = new Transform[0];
        private Transform[] pylonFeet = new Transform[0];
        private Vector3[] basePylonLocalScales = new Vector3[0];
        private string[] attachNodeIds = new string[0];
        private string[] attachNodeMarkerIds = new string[0];
        private bool alignmentStarted;
        private bool mastHeightFieldSubscribed;
        private float previousMastHeight;

        /// <summary>
        /// Resolves configured transforms and restores saved geometry when the vessel loads.
        /// </summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            groundPart = part.FindModuleImplementing<ModuleGroundPart>();
            ResolveConfiguration();
            Events[nameof(AlignPlatform)].active = showRealignEvent;
            previousMastHeight = mastHeight;
            ConfigureMastHeightSlider();
            UpdateMastHeightFieldVisibility();

            if (!HighLogic.LoadedSceneIsFlight || groundPart == null)
                return;

            if (platformAligned)
                part.StartCoroutine(RestoreSavedPlatform());
        }

        /// <summary>
        /// Removes PAW field callbacks when KSP destroys the part module.
        /// </summary>
        public void OnDestroy()
        {
            UnsubscribeMastHeightField();
        }

        /// <summary>
        /// Waits for stock ModuleGroundPart to finish static attachment before doing first-time alignment.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            UpdateMastHeightFieldVisibility();

            if (!HighLogic.LoadedSceneIsFlight ||
                groundPart == null ||
                platformAligned ||
                alignmentStarted ||
                !IsStaticGroundPart())
            {
                return;
            }

            part.StartCoroutine(AlignAfterGroundAttach());
        }

        /// <summary>
        /// Manually reruns deck and pylon alignment when no configured attach nodes are occupied.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Realign Platform", unfocusedRange = 4.0f)]
        public void AlignPlatform()
        {
            if (!HighLogic.LoadedSceneIsFlight || alignmentStarted)
                return;

            part.StartCoroutine(AlignAfterGroundAttach());
        }

        /// <summary>
        /// Delays alignment until the stock ground part coroutine has had time to freeze the vessel.
        /// </summary>
        private IEnumerator AlignAfterGroundAttach()
        {
            alignmentStarted = true;

            for (int index = 0; index < AlignmentDelayFrames; index++)
                yield return new WaitForFixedUpdate();

            if (TryAlignPlatform())
                SavePlatformState();

            alignmentStarted = false;
        }

        /// <summary>
        /// Restores saved deck, pylon, and attach-node geometry after KSP rebuilds the loaded vessel.
        /// </summary>
        private IEnumerator RestoreSavedPlatform()
        {
            for (int index = 0; index < AlignmentDelayFrames; index++)
                yield return new WaitForFixedUpdate();

            ApplySavedTransform(deckTransform, savedDeckLocalPosition, savedDeckLocalRotation);
            ApplySavedTransforms(pylonShafts, savedPylonLocalPositions, savedPylonLocalRotations, savedPylonLocalScales);
            ApplySavedTransforms(pylonFeet, savedFootLocalPositions, savedFootLocalRotations, string.Empty);
            SyncAttachNodesFromMarkers(false);
        }

        /// <summary>
        /// Resolves all named model transforms and comma-separated node identifiers.
        /// </summary>
        private void ResolveConfiguration()
        {
            pivotTransform = string.IsNullOrEmpty(pivotTransformName)
                ? part.transform
                : part.FindModelTransform(pivotTransformName);
            deckTransform = part.FindModelTransform(deckTransformName);
            pylonShafts = ResolveTransforms(pylonShaftTransformNames);
            pylonFeet = ResolveTransforms(pylonFootTransformNames);
            basePylonLocalScales = GetLocalScales(pylonShafts);
            attachNodeIds = SplitNames(attachNodeNames);
            attachNodeMarkerIds = SplitNames(attachNodeMarkerNames);
        }

        /// <summary>
        /// Configures the PAW slider from part config and subscribes to height changes.
        /// </summary>
        private void ConfigureMastHeightSlider()
        {
            BaseField mastHeightField = Fields[nameof(mastHeight)];
            if (mastHeightField == null)
                return;

            UI_FloatRange slider;
            if (Fields.TryGetFieldUIControl<UI_FloatRange>(nameof(mastHeight), out slider))
            {
                slider.minValue = minMastHeight;
                slider.maxValue = Mathf.Max(minMastHeight, maxMastHeight);
                slider.stepIncrement = Mathf.Max(0.01f, mastHeightStep);
            }

            if (!mastHeightFieldSubscribed)
            {
                mastHeightField.OnValueModified += OnMastHeightModified;
                mastHeightFieldSubscribed = true;
            }
        }

        /// <summary>
        /// Removes the mast height callback from the PAW field.
        /// </summary>
        private void UnsubscribeMastHeightField()
        {
            if (!mastHeightFieldSubscribed)
                return;

            BaseField mastHeightField = Fields[nameof(mastHeight)];
            if (mastHeightField != null)
                mastHeightField.OnValueModified -= OnMastHeightModified;

            mastHeightFieldSubscribed = false;
        }

        /// <summary>
        /// Shows the mast height slider only when the platform can safely move its attach nodes.
        /// </summary>
        private void UpdateMastHeightFieldVisibility()
        {
            BaseField mastHeightField = Fields[nameof(mastHeight)];
            if (mastHeightField == null)
                return;

            bool canAdjustHeight = HighLogic.LoadedSceneIsFlight &&
                IsStaticGroundPart() &&
                AreAttachNodesFree();

            mastHeightField.guiActive = canAdjustHeight;
            mastHeightField.guiActiveUnfocused = canAdjustHeight;
            Events[nameof(AlignPlatform)].active = showRealignEvent && canAdjustHeight;
        }

        /// <summary>
        /// Re-solves the deck, pylons, and attach nodes when the PAW slider changes mast height.
        /// </summary>
        private void OnMastHeightModified(object newValue)
        {
            if (!HighLogic.LoadedSceneIsFlight || alignmentStarted)
                return;

            if (!AreAttachNodesFree())
            {
                mastHeight = previousMastHeight;
                ScreenMessages.PostScreenMessage(
                    "Cannot adjust mast height while platform attach nodes are occupied.",
                    3.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            mastHeight = Mathf.Clamp(mastHeight, minMastHeight, Mathf.Max(minMastHeight, maxMastHeight));
            if (TryAlignPlatform())
            {
                SavePlatformState();
                previousMastHeight = mastHeight;
            }
            else
            {
                mastHeight = previousMastHeight;
            }
        }

        /// <summary>
        /// Levels the deck, projects pylons along each shaft's local pylon axis, and updates free attach nodes.
        /// </summary>
        private bool TryAlignPlatform()
        {
            if (pivotTransform == null || deckTransform == null || pylonShafts.Length < 1)
                return LogFailure("missing pivot, deck, or pylon shaft transforms.");

            if (!AreAttachNodesFree())
                return LogFailure("one or more configured attach nodes are occupied.");

            Vessel vessel = part.vessel;
            if (vessel == null || vessel.mainBody == null)
                return LogFailure("missing vessel or celestial body.");

            Vector3 worldUp = FlightGlobals.getUpAxis(vessel.mainBody, pivotTransform.position);
            Vector3 deckForward = Vector3.ProjectOnPlane(pivotTransform.forward, worldUp).normalized;
            if (deckForward.sqrMagnitude < TinyDirection)
                deckForward = Vector3.ProjectOnPlane(part.transform.forward, worldUp).normalized;
            if (deckForward.sqrMagnitude < TinyDirection)
                deckForward = Vector3.ProjectOnPlane(part.transform.right, worldUp).normalized;

            deckTransform.position = pivotTransform.position + worldUp * mastHeight;
            deckTransform.rotation = Quaternion.LookRotation(deckForward, worldUp);

            for (int index = 0; index < pylonShafts.Length; index++)
                ProjectPylon(index, deckForward);

            SyncAttachNodesFromMarkers(true);
            platformAligned = true;

            if (debugLog)
                Debug.Log("[Sandcastle] Aligned ground platform " + part.partInfo.title + ".");

            return true;
        }

        /// <summary>
        /// Raycasts one pylon along its shaft axis, extends the shaft, and aligns its foot to terrain.
        /// </summary>
        private void ProjectPylon(int index, Vector3 deckForward)
        {
            Transform shaft = pylonShafts[index];
            if (shaft == null)
                return;

            Vector3 localAxis = pylonAxis.sqrMagnitude > TinyDirection ? pylonAxis.normalized : Vector3.forward;
            Vector3 rayDirection = shaft.TransformDirection(localAxis).normalized;
            RaycastHit hit;
            bool hitTerrain = Physics.Raycast(shaft.position, rayDirection, out hit, pylonMaxLength,
                TerrainLayerMask, QueryTriggerInteraction.Ignore);

            Vector3 footPosition = hitTerrain
                ? hit.point + rayDirection * pylonEmbedDepth
                : shaft.position + rayDirection * pylonMaxLength;
            Vector3 pylonVector = footPosition - shaft.position;
            float pylonLength = pylonVector.magnitude;

            if (pylonLength > 0.001f)
                shaft.rotation = Quaternion.FromToRotation(shaft.TransformDirection(localAxis), pylonVector.normalized) * shaft.rotation;

            Vector3 localScale = index < basePylonLocalScales.Length
                ? basePylonLocalScales[index]
                : shaft.localScale;
            localScale = ScaleAlongDominantAxis(localScale, localAxis, pylonLength / Math.Max(0.001f, pylonModelLength));
            shaft.localScale = localScale;

            if (index >= pylonFeet.Length || pylonFeet[index] == null)
                return;

            Transform foot = pylonFeet[index];
            foot.position = footPosition;

            if (hitTerrain)
            {
                Vector3 footForward = Vector3.ProjectOnPlane(deckForward, hit.normal).normalized;
                if (footForward.sqrMagnitude < TinyDirection)
                    footForward = Vector3.ProjectOnPlane(deckTransform.right, hit.normal).normalized;

                Quaternion desiredFootRotation = Quaternion.LookRotation(footForward, hit.normal);
                Vector3 localFootUp = footUpAxis.sqrMagnitude > TinyDirection ? footUpAxis.normalized : Vector3.up;
                foot.rotation = desiredFootRotation * Quaternion.FromToRotation(localFootUp, Vector3.up);
            }
        }

        /// <summary>
        /// Updates configured AttachNodes from marker transforms, optionally requiring the nodes to be empty.
        /// </summary>
        private void SyncAttachNodesFromMarkers(bool requireFreeNodes)
        {
            int nodeCount = Math.Min(attachNodeIds.Length, attachNodeMarkerIds.Length);
            for (int index = 0; index < nodeCount; index++)
            {
                AttachNode attachNode = part.FindAttachNode(attachNodeIds[index]);
                Transform marker = part.FindModelTransform(attachNodeMarkerIds[index]);
                if (attachNode == null || marker == null)
                    continue;
                if (requireFreeNodes && attachNode.attachedPart != null)
                    continue;

                attachNode.position = part.transform.InverseTransformPoint(marker.position);
                attachNode.orientation = part.transform.InverseTransformDirection(marker.up).normalized;
                attachNode.originalPosition = attachNode.position;
                attachNode.originalOrientation = attachNode.orientation;

                if (attachNode.nodeTransform != null)
                {
                    attachNode.nodeTransform.position = marker.position;
                    attachNode.nodeTransform.rotation = marker.rotation;
                }
            }
        }

        /// <summary>
        /// Reports whether every configured AttachNode is empty and safe to move.
        /// </summary>
        private bool AreAttachNodesFree()
        {
            for (int index = 0; index < attachNodeIds.Length; index++)
            {
                AttachNode attachNode = part.FindAttachNode(attachNodeIds[index]);
                if (attachNode != null && attachNode.attachedPart != null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Saves local transform state so the solved platform geometry survives reload.
        /// </summary>
        private void SavePlatformState()
        {
            savedDeckLocalPosition = deckTransform.localPosition;
            savedDeckLocalRotation = deckTransform.localRotation;
            savedPylonLocalPositions = WriteLocalPositions(pylonShafts);
            savedPylonLocalRotations = WriteLocalRotations(pylonShafts);
            savedPylonLocalScales = WriteLocalScales(pylonShafts);
            savedFootLocalPositions = WriteLocalPositions(pylonFeet);
            savedFootLocalRotations = WriteLocalRotations(pylonFeet);
        }

        /// <summary>
        /// Reports whether stock has completed the ModuleGroundPart static-attach sequence.
        /// </summary>
        private bool IsStaticGroundPart()
        {
            if (part == null || part.vessel == null || groundPart == null)
                return false;

            bool deployedOnGround = groundPart.Fields.GetValue<bool>("deployedOnGround");
            return deployedOnGround ||
                part.PermanentGroundContact ||
                part.vessel.vesselType == VesselType.DeployedGroundPart;
        }

        /// <summary>
        /// Finds every transform listed in a comma-separated field.
        /// </summary>
        private Transform[] ResolveTransforms(string transformNames)
        {
            string[] names = SplitNames(transformNames);
            List<Transform> transforms = new List<Transform>();
            for (int index = 0; index < names.Length; index++)
                transforms.Add(part.FindModelTransform(names[index]));

            return transforms.ToArray();
        }

        /// <summary>
        /// Splits comma-separated config names while trimming whitespace and empty entries.
        /// </summary>
        private string[] SplitNames(string names)
        {
            if (string.IsNullOrEmpty(names))
                return new string[0];

            string[] rawNames = names.Split(',');
            List<string> trimmedNames = new List<string>();
            for (int index = 0; index < rawNames.Length; index++)
            {
                string trimmedName = rawNames[index].Trim();
                if (!string.IsNullOrEmpty(trimmedName))
                    trimmedNames.Add(trimmedName);
            }

            return trimmedNames.ToArray();
        }

        /// <summary>
        /// Scales the component that most closely matches the configured pylon axis.
        /// </summary>
        private Vector3 ScaleAlongDominantAxis(Vector3 scale, Vector3 axis, float lengthScale)
        {
            Vector3 absAxis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            if (absAxis.x >= absAxis.y && absAxis.x >= absAxis.z)
                scale.x *= lengthScale;
            else if (absAxis.y >= absAxis.x && absAxis.y >= absAxis.z)
                scale.y *= lengthScale;
            else
                scale.z *= lengthScale;

            return scale;
        }

        /// <summary>
        /// Captures model-authored local scales so pylon extension can be applied relatively.
        /// </summary>
        private Vector3[] GetLocalScales(Transform[] transforms)
        {
            Vector3[] scales = new Vector3[transforms.Length];
            for (int index = 0; index < transforms.Length; index++)
                scales[index] = transforms[index] != null ? transforms[index].localScale : Vector3.one;

            return scales;
        }

        /// <summary>
        /// Applies saved local position and rotation to one transform.
        /// </summary>
        private void ApplySavedTransform(Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            if (target == null)
                return;

            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }

        /// <summary>
        /// Applies saved local transform lists to a matching transform array.
        /// </summary>
        private void ApplySavedTransforms(Transform[] targets, string positions, string rotations, string scales)
        {
            Vector3[] savedPositions = ReadVectorList(positions);
            Quaternion[] savedRotations = ReadQuaternionList(rotations);
            Vector3[] savedScales = ReadVectorList(scales);
            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                    continue;

                if (index < savedPositions.Length)
                    targets[index].localPosition = savedPositions[index];
                if (index < savedRotations.Length)
                    targets[index].localRotation = savedRotations[index];
                if (index < savedScales.Length)
                    targets[index].localScale = savedScales[index];
            }
        }

        /// <summary>
        /// Serializes local positions for a transform list.
        /// </summary>
        private string WriteLocalPositions(Transform[] transforms)
        {
            List<string> values = new List<string>();
            for (int index = 0; index < transforms.Length; index++)
                values.Add(transforms[index] != null ? ConfigNode.WriteVector(transforms[index].localPosition) : string.Empty);

            return string.Join(";", values.ToArray());
        }

        /// <summary>
        /// Serializes local rotations for a transform list.
        /// </summary>
        private string WriteLocalRotations(Transform[] transforms)
        {
            List<string> values = new List<string>();
            for (int index = 0; index < transforms.Length; index++)
                values.Add(transforms[index] != null ? ConfigNode.WriteQuaternion(transforms[index].localRotation) : string.Empty);

            return string.Join(";", values.ToArray());
        }

        /// <summary>
        /// Serializes local scales for a transform list.
        /// </summary>
        private string WriteLocalScales(Transform[] transforms)
        {
            List<string> values = new List<string>();
            for (int index = 0; index < transforms.Length; index++)
                values.Add(transforms[index] != null ? ConfigNode.WriteVector(transforms[index].localScale) : string.Empty);

            return string.Join(";", values.ToArray());
        }

        /// <summary>
        /// Parses a semicolon-separated Vector3 list.
        /// </summary>
        private Vector3[] ReadVectorList(string values)
        {
            if (string.IsNullOrEmpty(values))
                return new Vector3[0];

            string[] rawValues = values.Split(';');
            Vector3[] vectors = new Vector3[rawValues.Length];
            for (int index = 0; index < rawValues.Length; index++)
                vectors[index] = string.IsNullOrEmpty(rawValues[index])
                    ? Vector3.zero
                    : ConfigNode.ParseVector3(rawValues[index]);

            return vectors;
        }

        /// <summary>
        /// Parses a semicolon-separated Quaternion list.
        /// </summary>
        private Quaternion[] ReadQuaternionList(string values)
        {
            if (string.IsNullOrEmpty(values))
                return new Quaternion[0];

            string[] rawValues = values.Split(';');
            Quaternion[] rotations = new Quaternion[rawValues.Length];
            for (int index = 0; index < rawValues.Length; index++)
                rotations[index] = string.IsNullOrEmpty(rawValues[index])
                    ? Quaternion.identity
                    : ConfigNode.ParseQuaternion(rawValues[index]);

            return rotations;
        }

        /// <summary>
        /// Logs an alignment failure when diagnostics are enabled.
        /// </summary>
        private bool LogFailure(string reason)
        {
            if (debugLog)
                Debug.LogWarning("[Sandcastle] WBIGroundPlatform could not align " +
                    part.partInfo.title + ": " + reason);

            return false;
        }
    }
}
