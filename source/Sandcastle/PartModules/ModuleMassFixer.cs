using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle
{
    public class ModuleMassFixer : BasePartModule
    {
        public override void OnUpdate()
        {
            base.OnUpdate();
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;
                float prefabMass = part.partInfo.partPrefab.mass;
                if (part.prefabMass.Equals(prefabMass))
                {
                    return;
                }
                Debug.Log("[ModuleMassFixer] - Resetting mass on " + part.partInfo.name);
                part.needPrefabMass = true;
                part.UpdateMass();

                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has new mass: " + part.mass);
                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has new prefab mass: " + prefabMass);
                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has module mass: " + part.GetModuleMass(prefabMass));
                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has resource mass: " + part.GetResourceMass());
                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has vessel mass after fix: " + part.vessel.GetTotalMass());
                Debug.Log("[ModuleMassFixer OnUpdate] - " + part.partInfo.name + " has new rb mass: " + part.rb.mass);
            }
            catch (Exception ex)
            { }
        }
    }
}
