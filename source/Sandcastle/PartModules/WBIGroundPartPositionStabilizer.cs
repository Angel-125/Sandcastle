using System.Collections;
using UnityEngine;

namespace Sandcastle
{
    /// <summary>
    /// Persists the final settled pose of a stock ModuleGroundPart and restores it after KSP reloads the vessel.
    /// </summary>
    public class WBIGroundPartPositionStabilizer : PartModule
    {
        /// <summary>
        /// Indicates that this part has recorded the final static-attached ground pose.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool hasStableGroundPose;

        /// <summary>
        /// Indicates that this ground part was deployed while the stabilizer was installed.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool stabilizationEnabled;

        /// <summary>
        /// Latitude of the vessel origin after the ground part has settled.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double stableLatitude;

        /// <summary>
        /// Longitude of the vessel origin after the ground part has settled.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double stableLongitude;

        /// <summary>
        /// Absolute altitude of the vessel origin after the ground part has settled.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double stableAltitude;

        /// <summary>
        /// Height of the vessel origin above the PQS terrain at the saved latitude and longitude.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double stableTerrainOffset;

        /// <summary>
        /// Enables concise diagnostics for saved and restored ground poses.
        /// </summary>
        [KSPField]
        public bool debugLog;

        private const int RestoreDelayFrames = 6;
        private const double PositionTolerance = 0.01;

        private ModuleGroundPart groundPart;
        private bool restoreStarted;
        private bool restoreComplete;
        private bool captureStarted;

        /// <summary>
        /// Locates the stock ground part module and schedules a post-load restore when a saved pose is available.
        /// </summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            groundPart = part.FindModuleImplementing<ModuleGroundPart>();
            if (!HighLogic.LoadedSceneIsFlight || groundPart == null)
                return;

            if (IsBeingDeployed())
                EnableStabilizer("deployment startup");

            if (hasStableGroundPose)
                StartPoseRestore();
        }

        /// <summary>
        /// Watches newly deployed ground parts until stock has finished static-attaching them, then records their pose.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight ||
                groundPart == null ||
                restoreStarted ||
                captureStarted)
            {
                return;
            }

            if (IsBeingDeployed())
                EnableStabilizer("active deployment");

            if (stabilizationEnabled && IsStaticGroundPart())
                part.StartCoroutine(CaptureSettledPose());
        }

        /// <summary>
        /// Starts the delayed restore coroutine once per vessel load.
        /// </summary>
        private void StartPoseRestore()
        {
            if (restoreStarted)
                return;

            restoreStarted = true;
            part.StartCoroutine(RestoreSavedPose());
        }

        /// <summary>
        /// Waits for stock ground positioning to complete and then restores the saved terrain-relative position.
        /// </summary>
        private IEnumerator RestoreSavedPose()
        {
            for (int index = 0; index < RestoreDelayFrames; index++)
                yield return new WaitForFixedUpdate();

            if (!CanUseStablePose())
                yield break;

            Vessel vessel = part.vessel;
            while (vessel != null && vessel.packed)
                yield return new WaitForFixedUpdate();

            if (!CanUseStablePose())
                yield break;

            Vector3d savedPosition = GetSavedWorldPosition(vessel.mainBody);
            Vector3d currentPosition = vessel.GetWorldPos3D();
            if ((savedPosition - currentPosition).magnitude > PositionTolerance)
            {
                vessel.SetPosition(savedPosition, false);
                vessel.SetWorldVelocity(Vector3d.zero);
                vessel.latitude = stableLatitude;
                vessel.longitude = stableLongitude;
                vessel.altitude = stableAltitude;

                if (debugLog)
                {
                    Debug.Log("[Sandcastle] Restored static ground pose for " +
                        part.partInfo.title + " by " +
                        (savedPosition - currentPosition).magnitude.ToString("0.000") + "m.");
                }
            }

            restoreComplete = true;
        }

        /// <summary>
        /// Lets stock complete its deployment coroutine, then captures the settled position for future reloads.
        /// </summary>
        private IEnumerator CaptureSettledPose()
        {
            captureStarted = true;

            for (int index = 0; index < RestoreDelayFrames; index++)
                yield return new WaitForFixedUpdate();

            if (IsStaticGroundPart() && (!hasStableGroundPose || restoreComplete))
                CaptureCurrentPose();

            captureStarted = false;
        }

        /// <summary>
        /// Captures the current vessel origin as both an absolute altitude and a terrain-relative offset.
        /// </summary>
        private void CaptureCurrentPose()
        {
            Vessel vessel = part.vessel;
            if (vessel == null || vessel.mainBody == null)
                return;

            Vector3d position = vessel.GetWorldPos3D();
            vessel.mainBody.GetLatLonAlt(position, out stableLatitude, out stableLongitude, out stableAltitude);
            stableTerrainOffset = stableAltitude - GetSurfaceAltitude(vessel.mainBody, stableLatitude, stableLongitude);
            hasStableGroundPose = true;
            stabilizationEnabled = true;

            if (debugLog)
            {
                Debug.Log("[Sandcastle] Captured static ground pose for " +
                    part.partInfo.title + " at terrain offset " +
                    stableTerrainOffset.ToString("0.000") + "m.");
            }
        }

        /// <summary>
        /// Rebuilds the saved world position from the current PQS surface plus the saved terrain offset.
        /// </summary>
        private Vector3d GetSavedWorldPosition(CelestialBody body)
        {
            double currentSurfaceAltitude = GetSurfaceAltitude(body, stableLatitude, stableLongitude);
            stableAltitude = currentSurfaceAltitude + stableTerrainOffset;
            return body.GetWorldSurfacePosition(stableLatitude, stableLongitude, stableAltitude);
        }

        /// <summary>
        /// Gets the current PQS terrain altitude at the supplied latitude and longitude.
        /// </summary>
        private double GetSurfaceAltitude(CelestialBody body, double latitude, double longitude)
        {
            if (body == null || body.pqsController == null)
                return stableAltitude - stableTerrainOffset;

            Vector3d surfaceNVector = body.GetRelSurfaceNVector(latitude, longitude);
            return body.pqsController.GetSurfaceHeight(surfaceNVector) - body.Radius;
        }

        /// <summary>
        /// Reports whether the saved pose has enough data to restore a landed ground part.
        /// </summary>
        private bool CanUseStablePose()
        {
            return hasStableGroundPose &&
                stabilizationEnabled &&
                IsStaticGroundPart() &&
                part.vessel != null &&
                part.vessel.mainBody != null;
        }

        /// <summary>
        /// Enables stabilization only after this module observes a fresh ModuleGroundPart deployment.
        /// </summary>
        private void EnableStabilizer(string reason)
        {
            if (stabilizationEnabled)
                return;

            stabilizationEnabled = true;

            if (debugLog)
            {
                Debug.Log("[Sandcastle] Enabled static ground pose stabilization for " +
                    part.partInfo.title + " during " + reason + ".");
            }
        }

        /// <summary>
        /// Reports whether stock ModuleGroundPart is currently performing its initial deployment.
        /// </summary>
        private bool IsBeingDeployed()
        {
            return groundPart != null && groundPart.Fields.GetValue<bool>("beingDeployed");
        }

        /// <summary>
        /// Reports whether stock has completed the ModuleGroundPart deployment/static-attach sequence.
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
    }
}
