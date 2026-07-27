using System;
/*
This file is part of Sandcastle.

Sandcastle is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Sandcastle is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Extraplanetary Launchpads.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;
using System.IO;
using WildBlueCore;


namespace Sandcastle.PartModules.Inventory
{
    public class ModulePartModuleFactory: WBIBasePartModule
    {
        #region Fields
        [KSPField]
        public string partModuleName = string.Empty;
        #endregion

        #region Housekeeping
        PartModule moduleAdded;
        List<PartModule> addedPartModules;
        List<ConfigNode> moduleSettings;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            if (moduleSettings == null)
            {
                addedPartModules = new List<PartModule>();
                moduleSettings = new List<ConfigNode>();
            }
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (string.IsNullOrEmpty(partModuleName))
                return;
            if (part.Modules.Contains(partModuleName))
                return;

            // For the future: Support ability to add multiple part modules.
            if (moduleSettings.Count > 0)
            {
                ConfigNode savedModuleNode = moduleSettings[0].CreateCopy();
                if (savedModuleNode != null)
                {
                    savedModuleNode.name = "MODULE";
                    moduleAdded = part.AddModule(savedModuleNode, true);
                }
            }

            if (moduleAdded == null)
            {
                moduleAdded = part.AddModule(partModuleName, true);
            }

            if (moduleAdded != null)
            {
                addedPartModules.Add(moduleAdded);
                moduleAdded.OnStart(state);
            }
            if (Vessel.IsValidVesselName(part.vessel.name))
                GameEvents.onVesselRename.Fire(new GameEvents.HostedFromToAction<Vessel, string>(part.vessel, part.vessel.name, part.vessel.name));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ConfigNode[] moduleNodes = node.GetNodes("WBIMODULE");
            if (moduleNodes == null)
                return;

            //Save the module settings, we'll need these for later.
            if (moduleSettings == null)
            {
                addedPartModules = new List<PartModule>();
                moduleSettings = new List<ConfigNode>();
            }
            moduleSettings.Clear();
            foreach (ConfigNode moduleNode in moduleNodes)
                moduleSettings.Add(moduleNode);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            ConfigNode saveNode;

            if (addedPartModules == null)
            {
                return;
            }

            foreach (PartModule addedModule in addedPartModules)
            {
                //Create a node for the module
                saveNode = ConfigNode.CreateConfigFromObject(addedModule);
                if (saveNode == null)
                {
                    continue;
                }

                //Tell the module to save its data
                saveNode.name = "WBIMODULE";
                try
                {
                    addedModule.Save(saveNode);
                }
                catch (Exception ex)
                {
                    string exInfo = ex.ToString();
                }

                //Add it to our node
                node.AddNode(saveNode);
            }
        }
        #endregion
    }
}
