using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PartModules
{
    public class TestRotate: BasePartModule
    {
        Quaternion originalRotation;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            originalRotation = part.vessel.srfRelRotation;
        }

        [KSPEvent(guiActive = true, guiName = "Reset Rotation")]
        public void ResetRotation()
        {
            FlightLogger.IgnoreGeeForces(20f);

            FlightGlobals.ActiveVessel.SetRotation(originalRotation);
        }

        [KSPEvent(guiActive = true, guiName = "Test Rotate")]
        public void Rotate()
        {
            FlightLogger.IgnoreGeeForces(20f);
            float inclination = 0f;
            float heading = vessel.srfRelRotation.Yaw();
            Vector3 rotation = new Vector3(inclination, 0.0f, heading);
            vessel.SetRotation(Quaternion.identity);
            Vector3 planeVector = Vector3.ProjectOnPlane(FlightGlobals.currentMainBody.position + (Vector3d)FlightGlobals.currentMainBody.transform.up * FlightGlobals.currentMainBody.Radius - vessel.transform.position, vessel.transform.position - FlightGlobals.currentMainBody.position.normalized);
            FlightGlobals.ActiveVessel.SetRotation(Quaternion.LookRotation(planeVector.normalized, (vessel.transform.position - FlightGlobals.currentMainBody.position).normalized) * Quaternion.Inverse(vessel.ReferenceTransform.rotation) * Quaternion.AngleAxis(90f, vessel.ReferenceTransform.right) * Quaternion.AngleAxis(rotation.z, -vessel.ReferenceTransform.forward) * Quaternion.AngleAxis(rotation.x, -vessel.ReferenceTransform.right));
        }
    }
}
