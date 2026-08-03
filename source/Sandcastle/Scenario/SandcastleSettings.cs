using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Sandcastle
{
    public class SandcastleSettings: GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool debugMode = false;

        [GameParameters.CustomParameterUI("#LOC_SANDCASTLE_checkKerbalsDesc", toolTip = "#LOC_SANDCASTLE_checkKerbalsTip", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool checkForKerbals = true;

        /// <summary>
        /// Enables deterministic stack-node roll alignment for stock construction used by an EVA Kerbal.
        /// </summary>
        [GameParameters.CustomParameterUI("#LOC_SANDCASTLE_alignEVAStackNodesDesc", toolTip = "#LOC_SANDCASTLE_alignEVAStackNodesTip", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool alignEVAConstructionStackNodes = true;

        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "Sandcastle";
            }
        }

        public override string Title
        {
            get
            {
                return "Sandcastle";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            base.SetDifficultyPreset(preset);
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public static bool DebugModeEnabled
        {
            get
            {
                SandcastleSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<SandcastleSettings>();
                return settings.debugMode;
            }
        }

        public static bool CheckForKerbals
        {
            get
            {
                SandcastleSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<SandcastleSettings>();
                return settings.checkForKerbals;
            }
        }

        /// <summary>
        /// Indicates whether ordinary Kerbal EVA Construction should initialize complete stack-node frames.
        /// </summary>
        public static bool AlignEVAConstructionStackNodes
        {
            get
            {
                SandcastleSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<SandcastleSettings>();
                return settings.alignEVAConstructionStackNodes;
            }
        }

    }
}
