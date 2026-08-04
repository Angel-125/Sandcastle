using UnityEngine;

namespace Sandcastle
{
    /// <summary>
    /// Persists a zero-buoyancy adjustment made when a part is placed by underwater construction.
    /// </summary>
    public class WBIUnderwaterSpawnBuoyancy : PartModule
    {
        /// <summary>
        /// Indicates that this specific part instance was placed by landed underwater construction.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool disableBuoyancy;

        /// <summary>
        /// Reapplies zero buoyancy while KSP is restoring the part from its snapshot.
        /// </summary>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            DisableBuoyancy();
        }

        /// <summary>
        /// Reapplies zero buoyancy after the part has completed normal flight startup.
        /// </summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            DisableBuoyancy();
        }

        /// <summary>
        /// Sets the owning part's stock buoyancy multiplier to zero when the part is available.
        /// </summary>
        private void DisableBuoyancy()
        {
            if (disableBuoyancy && part != null)
                part.buoyancy = 0.0f;
        }
    }

    /// <summary>
    /// Applies Sandcastle's shared landed-underwater spawn policy to proto and live parts.
    /// </summary>
    internal static class UnderwaterSpawnUtils
    {
        private const string PartNodeName = "PART";
        private const string ModuleNodeName = "MODULE";
        private const string ModuleNameValue = "name";

        /// <summary>
        /// Reports whether the actor is exactly landed beneath an ocean surface and the feature is enabled.
        /// </summary>
        internal static bool ShouldDisableBuoyancy(Vessel actorVessel)
        {
            // An exact LANDED test intentionally excludes SPLASHED and every in-flight situation.
            if (!SandcastleSettings.DisableBuoyancyForUnderwaterConstruction ||
                actorVessel == null ||
                actorVessel.situation != Vessel.Situations.LANDED ||
                actorVessel.mainBody == null ||
                !actorVessel.mainBody.ocean)
            {
                return false;
            }

            return FlightGlobals.getAltitudeAtPos(
                actorVessel.GetWorldPos3D(), actorVessel.mainBody) < 0.0;
        }

        /// <summary>
        /// Adds the persistent zero-buoyancy marker to every part in a new proto-vessel.
        /// </summary>
        internal static bool ApplyToProtoVessel(ConfigNode vesselNode, Vessel actorVessel,
            string source)
        {
            if (vesselNode == null || !ShouldDisableBuoyancy(actorVessel))
                return false;

            bool markerActivated = false;
            foreach (ConfigNode partNode in vesselNode.GetNodes(PartNodeName))
            {
                ConfigNode moduleNode = FindBuoyancyMarker(partNode);
                if (moduleNode == null)
                {
                    moduleNode = partNode.AddNode(ModuleNodeName);
                    moduleNode.AddValue(ModuleNameValue, nameof(WBIUnderwaterSpawnBuoyancy));
                }

                moduleNode.SetValue(nameof(WBIUnderwaterSpawnBuoyancy.disableBuoyancy), true, true);
                markerActivated = true;
            }

            if (markerActivated)
                LogAdjustment(actorVessel, source);

            return markerActivated;
        }

        /// <summary>
        /// Sets a newly attached live part to zero buoyancy and adds the persistent marker module.
        /// </summary>
        internal static bool ApplyToPart(Part spawnedPart, Vessel actorVessel, string source)
        {
            if (spawnedPart == null || !ShouldDisableBuoyancy(actorVessel))
                return false;

            spawnedPart.buoyancy = 0.0f;
            WBIUnderwaterSpawnBuoyancy marker =
                spawnedPart.FindModuleImplementing<WBIUnderwaterSpawnBuoyancy>();
            if (marker == null)
            {
                ConfigNode moduleNode = new ConfigNode(ModuleNodeName);
                moduleNode.AddValue(ModuleNameValue, nameof(WBIUnderwaterSpawnBuoyancy));
                marker = spawnedPart.AddModule(moduleNode, true) as WBIUnderwaterSpawnBuoyancy;
                if (marker == null)
                {
                    Debug.LogWarning("[Sandcastle] Could not add the underwater buoyancy marker to " +
                        spawnedPart.partInfo.title + ". Buoyancy is zero for this session but may not persist.");
                    return false;
                }
            }

            marker.disableBuoyancy = true;

            LogAdjustment(actorVessel, source);
            return true;
        }

        /// <summary>
        /// Finds the persistent zero-buoyancy marker in a proto-part snapshot.
        /// </summary>
        private static ConfigNode FindBuoyancyMarker(ConfigNode partNode)
        {
            foreach (ConfigNode moduleNode in partNode.GetNodes(ModuleNodeName))
            {
                if (moduleNode.GetValue(ModuleNameValue) == nameof(WBIUnderwaterSpawnBuoyancy))
                    return moduleNode;
            }

            return null;
        }

        /// <summary>
        /// Writes a concise diagnostic describing why the new part received zero buoyancy.
        /// </summary>
        private static void LogAdjustment(Vessel actorVessel, string source)
        {
            Debug.Log("[Sandcastle] " + source +
                " placed a part from landed underwater vessel " + actorVessel.vesselName +
                "; buoyancy set to zero.");
        }
    }
}
