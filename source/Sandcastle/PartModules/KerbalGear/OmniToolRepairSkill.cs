using Experience;

namespace Sandcastle.PartModules.KerbalGear
{
    /// <summary>
    /// Marker experience effect granted by an equipped Omni-Tool. The effect itself does not
    /// modify RepairSkill, which prevents it from enabling stock EVA Construction. Sandcastle's
    /// repair Harmony patches recognize this marker through EVA_REPAIR_SKILLS instead.
    /// </summary>
    public sealed class OmniToolRepairSkill : ExperienceEffect
    {
        /// <summary>
        /// Creates the marker effect for a kerbal's experience trait.
        /// </summary>
        public OmniToolRepairSkill(ExperienceTrait parent)
            : base(parent)
        {
        }
    }
}
