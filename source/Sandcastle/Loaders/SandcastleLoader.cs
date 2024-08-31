using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Sandcastle.PartModules;
using WildBlueCore.PartModules.Decals;

namespace Sandcastle
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    sealed class SandcastleLoader : MonoBehaviour
    {
        class SandcastleModuleLoader : LoadingSystem
        {
            public override bool IsReady()
            {
                return true;
            }

            public override void StartLoad()
            {
                int count = PartLoader.LoadedPartsList.Count;
                AvailablePart availablePart;

                for (int index = 0; index < count; index++)
                {
                    // Get the available part
                    availablePart = PartLoader.LoadedPartsList[index];

                    // Add modules
                    addEVAVariantsModule(availablePart);
                    addEVAFlagSwitchModule(availablePart);
                }
            }
            public override string ProgressTitle()
            {
                return "Sandcastle Modules";
            }

            private void addEVAFlagSwitchModule(AvailablePart availablePart)
            {
                if (availablePart == null || availablePart.partPrefab == null)
                    return;

                if (availablePart.partPrefab.HasModuleImplementing<FlagDecalBackground>() || availablePart.partPrefab.HasModuleImplementing<FlagDecal>() || availablePart.partPrefab.HasModuleImplementing<ModuleDecal>())
                {
                    try
                    {
                        availablePart.partPrefab.AddModule("SCModuleEVAVariants", true);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("[SandcastleLoader] - Exception while trying to add SCModuleEVAVariants: " + ex);
                    }
                }
            }

            private void addEVAVariantsModule(AvailablePart availablePart)
            {
                if (availablePart == null || availablePart.partPrefab == null)
                    return;

                if (!availablePart.partPrefab.HasModuleImplementing<ModulePartVariants>())
                    return;
                if (availablePart.partPrefab.HasModuleImplementing<SCModuleEVAVariants>())
                    return;

                try
                {
                    availablePart.partPrefab.AddModule("SCModuleEVAFlagSwitch", true);
                }
                catch (Exception ex)
                {
                    Debug.Log("[SandcastleLoader] - Exception while trying to add SCModuleEVAFlagSwitch: " + ex);
                }
            }

            private void addMassFixModule(AvailablePart availablePart)
            {
                if (availablePart == null || availablePart.partPrefab == null)
                    return;

                if (availablePart.partPrefab.HasModuleImplementing<ModuleMassFixer>())
                    return;

                try
                {
                    availablePart.partPrefab.AddModule("ModuleMassFixer", true);
                }
                catch (Exception ex)
                {
                    Debug.Log("[SandcastleLoader] - Exception while trying to add ModuleMassFixer: " + ex);
                }
            }
        }

        public void Awake()
        {
            List<LoadingSystem> loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                int count = loaders.Count;
                for (int index = 0; index < count; index++)
                {
                    if (loaders[index] is PartLoader)
                    {
                        GameObject gameObject = new GameObject();
                        SandcastleModuleLoader modulesLoader = gameObject.AddComponent<SandcastleModuleLoader>();
                        loaders.Insert(index + 1, modulesLoader);
                        break;
                    }
                }
            }
        }
    }
}
