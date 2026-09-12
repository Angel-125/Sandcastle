using Experience;
using HarmonyLib;
using ModuleWheels;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sandcastle.PartModules.KerbalGear
{
    /// <summary>
    /// Loads the experience-effect names that are permitted to perform stock EVA repairs.
    /// </summary>
    internal static class EVARepairSkillRegistry
    {
        const string kConfigNodeName = "EVA_REPAIR_SKILLS";
        const string kSkillNameField = "skillName";
        static readonly HashSet<string> repairSkillNames =
            new HashSet<string>(StringComparer.Ordinal);
        static bool isLoaded;

        /// <summary>
        /// Determines whether the active EVA kerbal carries any configured repair skill.
        /// </summary>
        internal static bool ActiveKerbalHasRepairSkill()
        {
            loadRepairSkills();
            if (repairSkillNames.Count == 0 || FlightGlobals.ActiveVessel == null ||
                !FlightGlobals.ActiveVessel.isEVA)
            {
                return false;
            }

            List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
            for (int crewIndex = 0; crewIndex < crew.Count; crewIndex++)
            {
                ProtoCrewMember crewMember = crew[crewIndex];
                if (crewMember == null || crewMember.experienceTrait == null)
                    continue;

                List<ExperienceEffect> effects = crewMember.experienceTrait.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    ExperienceEffect effect = effects[effectIndex];
                    if (effect != null && repairSkillNames.Contains(effect.Name))
                        return true;
                }
            }
            return false;
        }

        static void loadRepairSkills()
        {
            if (isLoaded || GameDatabase.Instance == null)
                return;

            repairSkillNames.Clear();
            ConfigNode[] configNodes =
                GameDatabase.Instance.GetConfigNodes(kConfigNodeName);
            for (int nodeIndex = 0; nodeIndex < configNodes.Length; nodeIndex++)
            {
                string[] configuredNames =
                    configNodes[nodeIndex].GetValues(kSkillNameField);
                for (int nameIndex = 0; nameIndex < configuredNames.Length; nameIndex++)
                {
                    string skillName = configuredNames[nameIndex].Trim();
                    if (!string.IsNullOrEmpty(skillName))
                        repairSkillNames.Add(skillName);
                }
            }

            isLoaded = true;
            Debug.Log("[Sandcastle] Loaded " + repairSkillNames.Count +
                " configured EVA repair skill(s).");
        }
    }

    /// <summary>
    /// Temporarily exposes repair level 1 only while a stock repair method is executing. This
    /// lets configured marker skills pass every stock repair and repair-kit check without making
    /// RepairSkill visible to EVAConstructionModeController.
    /// </summary>
    [HarmonyPatch]
    internal static class EVARepairSkillPatches
    {
        sealed class RepairSkillScope
        {
            internal Part part;
            internal Vessel vessel;
            internal EventValueComparison<int>.OnEvent repairSkillProvider;

            /// <summary>
            /// Supplies the temporary repair level. This must be an instance method because KSP's
            /// EventValueComparison assumes every registered delegate has a non-null Target.
            /// </summary>
            internal int ProvideRepairSkill()
            {
                return 1;
            }
        }

        /// <summary>
        /// Patches only the stock chute, deployable-part, and wheel repair entry points.
        /// EVAConstructionModeController is deliberately not included.
        /// </summary>
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ModuleParachute), nameof(ModuleParachute.Repack));
            yield return AccessTools.Method(typeof(ModuleDeployablePart),
                nameof(ModuleDeployablePart.EventRepairExternal));
            yield return AccessTools.Method(typeof(ModuleWheelDamage),
                nameof(ModuleWheelDamage.EventRepairExternal));
        }

        static void Prefix(out RepairSkillScope __state)
        {
            __state = null;
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || vessel.rootPart == null ||
                vessel.VesselValues.RepairSkill.value >= 1 ||
                !EVARepairSkillRegistry.ActiveKerbalHasRepairSkill())
            {
                return;
            }

            RepairSkillScope scope = new RepairSkillScope
            {
                part = vessel.rootPart,
                vessel = vessel
            };
            scope.repairSkillProvider = scope.ProvideRepairSkill;
            __state = scope;
            scope.part.PartValues.RepairSkill.Add(scope.repairSkillProvider);
            scope.part.PartValues.RepairSkill.Update();
            scope.vessel.VesselValues.RepairSkill.ResetValueCache();
        }

        static void Postfix(RepairSkillScope __state)
        {
            removeRepairSkillScope(__state);
        }

        static Exception Finalizer(Exception __exception, RepairSkillScope __state)
        {
            removeRepairSkillScope(__state);
            return __exception;
        }

        static void removeRepairSkillScope(RepairSkillScope scope)
        {
            if (scope == null || scope.repairSkillProvider == null)
                return;

            if (scope.part != null && scope.part.PartValues != null)
            {
                scope.part.PartValues.RepairSkill.Remove(scope.repairSkillProvider);
                scope.part.PartValues.RepairSkill.Update();
            }
            if (scope.vessel != null && scope.vessel.VesselValues != null)
                scope.vessel.VesselValues.RepairSkill.ResetValueCache();
            scope.repairSkillProvider = null;
        }
    }
}
