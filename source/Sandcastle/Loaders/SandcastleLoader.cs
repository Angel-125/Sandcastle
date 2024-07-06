using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sandcastle
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    sealed class SandcastleLoader : MonoBehaviour
    {
        class MassFixModuleLoader : LoadingSystem
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

                    // Add bot repairs module
//                    addMassFixModule(availablePart);
                }
            }
            public override string ProgressTitle()
            {
                return "Part mass fixer";
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
                        MassFixModuleLoader modulesLoader = gameObject.AddComponent<MassFixModuleLoader>();
                        loaders.Insert(index + 1, modulesLoader);
                        break;
                    }
                }
            }
        }
    }
}
