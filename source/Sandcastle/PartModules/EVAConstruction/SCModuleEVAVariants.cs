using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PartModules
{
    /// <summary>
    /// This helper part module makes it possible to change part variants during EVA Construction.
    /// </summary>
    public class SCModuleEVAVariants: BasePartModule
    {
        const float kInteractionRange = 10.0f;

        ModulePartVariants partVariants = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            GameEvents.OnEVAConstructionMode.Add(onEVAConstructionMode);

            partVariants = part.FindModuleImplementing<ModulePartVariants>();
        }

        public void OnDestroy()
        {
            GameEvents.OnEVAConstructionMode.Remove(onEVAConstructionMode);
        }

        void onEVAConstructionMode(bool inConstructionMode)
        {
            if (partVariants == null)
            {
                Debug.Log("[SCModuleEVAVariants] - onEVAConstructionMode: partVariants == null for part " + part.partInfo.title);
                return;
            }

            if (inConstructionMode)
                enableVariantSwitching();
            else
                disableVariantSwitching();
        }

        /// <summary>
        /// Enables in-flight variant switching
        /// </summary>
        public void enableVariantSwitching()
        {
            UI_VariantSelector variantSelector = partVariants.Fields["variantIndex"].uiControlEditor as UI_VariantSelector;
            if (variantSelector == null)
            {
                Debug.Log("[SCModuleEVAVariants] - Unable to find variant selector control for part " + part.partInfo.title);
                return;
            }
            variantSelector.SetSceneVisibility(UI_Scene.Flight, true);
            variantSelector.variants = partVariants.variantList;
            variantSelector.onFieldChanged = variantSelector.onFieldChanged + new Callback<BaseField, object>(onVariantChanged);
            variantSelector.onSymmetryFieldChanged = variantSelector.onSymmetryFieldChanged + new Callback<BaseField, object>(onVariantChanged);

            partVariants.Fields["variantIndex"].guiActiveUnfocused = true;
            partVariants.Fields["variantIndex"].guiUnfocusedRange = kInteractionRange;
            partVariants.Fields["variantIndex"].uiControlFlight = variantSelector;
            partVariants.moduleJettison = partVariants.part.Modules.GetModule<ModuleJettison>();

            partVariants.RefreshVariant();
        }

        /// <summary>
        /// Disables in-flight variant switching
        /// </summary>
        public void disableVariantSwitching()
        {
            UI_VariantSelector variantSelector = partVariants.Fields["variantIndex"].uiControlEditor as UI_VariantSelector;
            if (variantSelector == null)
            {
                Debug.Log("[SCModuleEVAVariants] - Unable to find variant selector control for part " + part.partInfo.title);
                return;
            }
            variantSelector.SetSceneVisibility(UI_Scene.Flight, false);
            variantSelector.onFieldChanged = variantSelector.onFieldChanged - new Callback<BaseField, object>(onVariantChanged);
            variantSelector.onSymmetryFieldChanged = variantSelector.onSymmetryFieldChanged - new Callback<BaseField, object>(onVariantChanged);

            partVariants.Fields["variantIndex"].guiActiveUnfocused = false;
            partVariants.Fields["variantIndex"].uiControlFlight = null;

            partVariants.RefreshVariant();
        }

        private void onVariantChanged(BaseField field, object obj)
        {
            partVariants.RefreshVariant();
        }
    }
}
