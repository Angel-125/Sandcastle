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
using System;
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
    public class ModulePartModuleFactory: BasePartModule
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
            moduleAdded = part.AddModule(partModuleName, true);
            if (moduleAdded != null)
            {
                addedPartModules.Add(moduleAdded);
                loadModuleSettings(moduleAdded, 0);
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

        #region Helpers
        protected void loadModuleSettings(PartModule module, int index)
        {
            if (HighLogic.LoadedSceneIsFlight == false && HighLogic.LoadedSceneIsEditor == false && HighLogic.LoadedScene != GameScenes.SPACECENTER)
                return;

            Debug.Log("loadModuleSettings called");
            if (index > moduleSettings.Count - 1)
            {
                Debug.Log("Index > moduleSettings.Count!");
                return;
            }
            ConfigNode nodeSettings = moduleSettings[index];

            //nodeSettings may have persistent fields. If so, then set them.
            foreach (ConfigNode.Value nodeValue in nodeSettings.values)
            {
                try
                {
                    if (module.Fields[nodeValue.name] != null)
                    {
                        Debug.Log("Set Field " + nodeValue.name + " to " + nodeValue.value);
                        module.Fields[nodeValue.name].Read(nodeValue.value, module);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Encountered an exception while setting values for " + nodeValue.name + ": " + ex);
                    continue;
                }
            }

            //Actions
            if (nodeSettings.HasNode("ACTIONS"))
            {
                ConfigNode actionsNode = nodeSettings.GetNode("ACTIONS");
                BaseAction action;

                foreach (ConfigNode node in actionsNode.nodes)
                {
                    action = module.Actions[node.name];
                    if (action != null)
                    {
                        action.actionGroup = (KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), node.GetValue("actionGroup"));
                        Debug.Log("Set " + node.name + " to " + action.actionGroup);
                    }
                }
            }
        }
        #endregion
    }
}
