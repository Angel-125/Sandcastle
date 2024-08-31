using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;
using WildBlueCore;
using WildBlueCore.PartModules.Decals;

namespace Sandcastle.PartModules
{
    public class SCModuleEVAFlagSwitch: BasePartModule
    {
        const float kInteractionRange = 10.0f;

        FlagDecalBackground flagDecalBackground;
        FlagDecal flagDecal;
        ModuleDecal decalModule;
        FlagBrowser flagBrowser;

        #region Lifecycle Methods
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            GameEvents.OnEVAConstructionMode.Add(onEVAConstructionMode);

            flagDecalBackground = part.FindModuleImplementing<FlagDecalBackground>();
            flagDecal = part.FindModuleImplementing<FlagDecal>();
            decalModule = part.FindModuleImplementing<ModuleDecal>();
        }

        public void OnDestroy()
        {
            GameEvents.OnEVAConstructionMode.Remove(onEVAConstructionMode);
        }
        #endregion

        [KSPEvent(guiActive = false, guiActiveEditor = false, unfocusedRange = kInteractionRange, guiName = "#autoLOC_6006058")]
        public void EVASetFlag()
        {
            // We use our on flag setter since it seems that the stock part modules only work in the editor.
            string decalURL = HighLogic.CurrentGame.flagURL;
            flagBrowser = null;
            flagBrowser = (Instantiate((UnityEngine.Object)(new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = onFlagSelected;
        }

        #region Helpers
        private void onFlagSelected(FlagBrowser.FlagEntry selected)
        {
            // DO NOT set ModuleDecal! Let it handle flag switching.

            string flagURL = selected.textureInfo.name;
            part.flagURL = flagURL;

            if (flagDecalBackground != null)
            {
                flagDecalBackground.updateFlag(flagURL);
                flagDecalBackground.UpdateFlagRenderers();
                part.RefreshHighlighter();
            }

            if (flagDecal != null)
            {
                List<FlagDecal> flagDecals = part.vessel.FindPartModulesImplementing<FlagDecal>();
                int count = flagDecals.Count;
                for (int index = 0; index < count; index++)
                {
                    flagDecals[index].part.flagURL = flagURL;
                    flagDecals[index].UpdateFlagTexture();
                }
            }
        }

        private void onEVAConstructionMode(bool inConstructionMode)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Enable/disable stock flag part modules
            setFlagDecalBackgroundEVA(inConstructionMode);
            setFlagDecalEVA(inConstructionMode);

            // Enable/disable our events
            Events["EVASetFlag"].guiActiveUnfocused = inConstructionMode;

            // Enable/disable ModuleDecal event.
            if (decalModule != null)
            {
                decalModule.Events["SelectDecal"].guiActiveUnfocused = inConstructionMode;
                decalModule.Events["SelectDecal"].unfocusedRange = kInteractionRange;

                decalModule.Events["ToggleDecal"].guiActiveUnfocused = inConstructionMode;
                decalModule.Events["ToggleDecal"].unfocusedRange = kInteractionRange;

                decalModule.Events["ReverseDecal"].guiActiveUnfocused = inConstructionMode;
                decalModule.Events["ReverseDecal"].unfocusedRange = kInteractionRange;
            }

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(part);
        }

        public void setFlagDecalBackgroundEVA(bool enabled)
        {
            if (!HighLogic.LoadedSceneIsFlight || flagDecalBackground == null)
                return;

            flagDecalBackground.Fields["displayingPortrait"].guiActiveUnfocused = enabled;
            flagDecalBackground.Fields["displayingPortrait"].guiUnfocusedRange = kInteractionRange;

            flagDecalBackground.Fields["displayingPortraitLabel"].guiActiveUnfocused = enabled;

            flagDecalBackground.Fields["flagSize"].guiActiveUnfocused = enabled;
            flagDecalBackground.Fields["flagSize"].guiUnfocusedRange = kInteractionRange;

            UI_ChooseOption chooseOption = flagDecalBackground.Fields["flagSize"].uiControlEditor as UI_ChooseOption;
            chooseOption.SetSceneVisibility(UI_Scene.Flight, enabled);
            if (enabled)
            {
                flagDecalBackground.Fields["flagSize"].uiControlFlight = chooseOption;
                flagDecalBackground.uI_ChooseOption = chooseOption;
                flagDecalBackground.UpdateUIChooseOptions();
                chooseOption.onFieldChanged = chooseOption.onFieldChanged + new Callback<BaseField, object>(flagDecalBackground.EnableCurrentFlagMesh);
            }
            else
            {
                flagDecalBackground.Fields["flagSize"].uiControlFlight = null;
                flagDecalBackground.uI_ChooseOption = null;
                chooseOption.onFieldChanged = chooseOption.onFieldChanged - new Callback<BaseField, object>(flagDecalBackground.EnableCurrentFlagMesh);
            }

            flagDecalBackground.Fields["flagSizeLabel"].guiActiveUnfocused = enabled;

            flagDecalBackground.Events["ToggleFlag"].guiActiveUnfocused = enabled;
            flagDecalBackground.Events["ToggleFlag"].unfocusedRange = kInteractionRange;
        }

        public void setFlagDecalEVA(bool enabled)
        {
            if (!HighLogic.LoadedSceneIsFlight || flagDecal == null)
                return;

            flagDecal.Events["ToggleFlag"].guiActiveUnfocused = enabled;
            flagDecal.Events["ToggleFlag"].unfocusedRange = kInteractionRange;

            flagDecal.Events["MirrorFlag"].guiActiveUnfocused = enabled;
            flagDecal.Events["MirrorFlag"].unfocusedRange = kInteractionRange;
        }
        #endregion
    }
}
