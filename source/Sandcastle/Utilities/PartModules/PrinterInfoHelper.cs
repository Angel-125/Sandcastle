using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using Sandcastle.PrintShop;
using Sandcastle.Inventory;
using KSP.Localization;

namespace Sandcastle.Utilities
{
    /// <summary>
    /// This helper fills out the info text for the WBIPrinterRequirements part module. During the game startup, it asks part modules to GetInfo. WBIPrinterRequirements is no exception.
    /// However, because it relies on the PartLoader to obtain information about prerequisite components, WBIPrinterRequirements can't completely fill out its info.
    /// We get around the problem by waiting until we load into the editor, and manually changing the ModuleInfo associated with WBIPrinterRequirements.
    /// It's crude but effective.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class PrinterInfoHelper : MonoBehaviour
    {
        public static PrinterInfoHelper shared;

        public void Awake()
        {
            shared = this;

            if (HighLogic.LoadedSceneIsEditor)
            {
                // Initialize the materials lists
                MaterialsList.LoadLists();

                // Now update the module infos.
                updateModuleInfos();
            }
        }

        private void updateModuleInfos()
        {
            // Grab the list of cargo parts
            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableCargoParts();
            if (cargoParts == null || cargoParts.Count == 0)
                return;

            // Go through each part and find the ModuleInfo corresponding to the part's WBIPrinterRequirements. Then update its info text.
            int count = cargoParts.Count;
            AvailablePart availablePart;
            WBIPrinterRequirements printerRequirements;
            for (int index = 0; index < count; index++)
            {
                // Get the available part
                availablePart = cargoParts[index];

                // Get the WBIPrinterRequirements if it has one.
                printerRequirements = availablePart.partPrefab.FindModuleImplementing<WBIPrinterRequirements>();
                if (printerRequirements == null)
                    continue;

                // Now find the ModuleInfo that's associated with the printer requirements.
                int moduleInfoCount = availablePart.partPrefab.partInfo.moduleInfos.Count;
                AvailablePart.ModuleInfo moduleInfo;
                for (int moduleInfoIndex = 0; moduleInfoIndex < moduleInfoCount; moduleInfoIndex++)
                {
                    moduleInfo = availablePart.partPrefab.partInfo.moduleInfos[moduleInfoIndex];
                    if (moduleInfo.moduleName == printerRequirements.GUIName)
                    {
                        moduleInfo.info = getInfo(availablePart);
                        break;
                    }
                }
            }
        }

        private string getInfo(AvailablePart availablePart)
        {
            StringBuilder requirements = new StringBuilder();

            // If the part is on the blacklist then we're done.
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(WBIPrintShop.kPartBlackListNode);
            string[] values = null;
            if (nodes != null && nodes.Length > 0)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    values = nodes[index].GetValues(WBIPrintShop.kBlacklistedPart);
                    if (values.Contains(availablePart.name))
                    {
                        return Localizer.Format("#LOC_SANDCASTLE_printRequirementsBanned");
                    }
                }
            }

            // Check to see if the cargo part can't be placed in an inventory.
            ModuleCargoPart cargoPart = availablePart.partPrefab.FindModuleImplementing<ModuleCargoPart>();
            if (cargoPart != null && cargoPart.packedVolume < 0)
                return Localizer.Format("#LOC_SANDCASTLE_printRequirementsBanned");

            // For some reason, flat-packed and boxed Pathfinder parts list a negative prefab mass. We need to fix that.
            if (availablePart.partPrefab.mass < 0 && availablePart.partConfig != null && availablePart.partConfig.HasValue("mass"))
            {
                float.TryParse(availablePart.partConfig.GetValue("mass"), out availablePart.partPrefab.mass);
            }
            BuildItem item = new BuildItem(availablePart.partPrefab.partInfo);

            // Required resources
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            int count = item.materials.Count;
            if (count > 0)
            {
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredResources"));
                for (int index = 0; index < count; index++)
                {
                    if (definitions.Contains(item.materials[index].name))
                    {
                        resourceDef = definitions[item.materials[index].name];
                        requirements.AppendLine(string.Format("{0:s}: {1:n3}u", resourceDef.displayName, item.materials[index].amount));
                    }
                }
            }

            // Required parts
            count = item.requiredComponents.Count;
            if (count > 0)
            {
                requirements.AppendLine(" ");
                requirements.AppendLine(Localizer.Format("#LOC_SANDCASTLE_requiredParts"));
                AvailablePart requiredPart;
                int requiredPartCount = 0;
                for (int index = 0; index < count; index++)
                {
                    requiredPart = PartLoader.getPartInfoByName(item.requiredComponents[index].name);
                    if (requiredPart == null)
                        continue;
                    requiredPartCount = item.requiredComponents[index].amount;
                    if (requiredPartCount == 1)
                        requirements.AppendLine(requiredPart.title);
                    else
                        requirements.AppendLine(requiredPart.title + ": " + requiredPartCount.ToString());
                }
            }

            // Minimum gravity
            if (item.minimumGravity > -1)
            {
                string gravityRequirement = string.Empty;

                if (item.minimumGravity < 0.00001)
                   gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredMicrogravity");
                else
                    gravityRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredGravity", new string[1] { string.Format("{0:n2}", item.minimumGravity) });

                requirements.AppendLine(gravityRequirement);
            }

            // Minimum pressure
            if (item.minimumPressure > -1)
            {
                string pressureRequirement = string.Empty;

                if (item.minimumPressure < 0.001)
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredVacuum");
                else
                    pressureRequirement = Localizer.Format("#LOC_SANDCASTLE_requiredPressure", new string[1] { string.Format("{0:n2}", item.minimumPressure) });

                requirements.AppendLine(pressureRequirement);
            }

            return requirements.ToString();
        }
    }
}
