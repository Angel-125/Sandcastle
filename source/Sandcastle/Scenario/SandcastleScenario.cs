using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using Sandcastle.PrintShop;
using Sandcastle.Inventory;

namespace Sandcastle
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class SandcastleScenario: ScenarioModule
    {
        #region Housekeeping
        public static SandcastleScenario shared;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;

            if (HighLogic.LoadedSceneIsFlight)
            {
                MaterialsList.LoadLists();
                InventoryUtils.FindThumbnailPaths();
                GameEvents.onAttemptEva.Add(onAttemptEVA);
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
                GameEvents.onAttemptEva.Remove(onAttemptEVA);
        }
        #endregion

        #region Helpers
        void onAttemptEVA(ProtoCrewMember crewMemeber, Part evaPart, Transform transform)
        {
            // Let tourists outside!
            FlightEVA.fetch.overrideEVA = crewMemeber.trait.Equals(KerbalRoster.touristTrait);
        }
        #endregion
    }
}