using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using ExtraplanetaryLaunchpads;

/*
This file is part of EL Helper.
EL Helper is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
Extraplanetary Launchpads is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with EL Helper.  If not, see
<http://www.gnu.org/licenses/>.
*/

namespace ELHelper
{
    public class WBIELGroundLaunchpad: ELLaunchpad, ELBuildControl.IBuilder, ELControlInterface
    {
        #region Fields
        [KSPField]
        public string SpawnTransformOriginal = "LaunchPosOriginal";

        [KSPField]
        public string transformOffsetAxis = "0,0,1";

        [KSPField]
        public float groundOffset = 0.1f;
        #endregion

        [KSPEvent(guiActive = true, guiActiveEditor = true)]
        public void MoveLaunchPos()
        {
            // Get the launch position transform
            if (!string.IsNullOrEmpty(SpawnTransform))
            {
                launchTransform = part.FindModelTransform(SpawnTransform);
                if (launchTransform == null)
                    return;
            }
            launchTransform.position = launchTransformOriginalPosition;

            // Using the vessel bounds, calculate how far to offset the transform along the desired axis.
            float length = 11f;
            relativePosition = offsetAxis * length;
            Vector3 pos = launchTransform.TransformPoint(relativePosition);
            launchTransform.Translate(relativePosition);
//            launchTransform.position = pos;

            // Account for ground.
            RaycastHit terrainHit;
            float heightOffset = 0f;
            if (Physics.Raycast(launchTransform.position, part.vessel.transform.forward, out terrainHit, 1000f, -1))
            {
                //See if we found the ground. 15 = Local Scenery, 28 = TerrainColliders
                if (terrainHit.collider.gameObject.layer == 15 || terrainHit.collider.gameObject.layer == 28)
                {
                    heightOffset = terrainHit.distance;
                    relativePosition = new Vector3(0, -1, 0) * (heightOffset - groundOffset);
                    launchTransform.Translate(relativePosition);
//                    pos = launchTransform.TransformPoint(relativePosition);
//                    launchTransform.position = pos;
                }
            }

        }

        #region Housekeeping
        Transform launchTransform = null;
        Vector3 launchTransformOriginalPosition = Vector3.zero;
        Vector3 offsetAxis = new Vector3(0, 0, 1);
        Quaternion relativeRotaion;
        Vector3 relativePosition;
        static Quaternion[] rotations = {
            new Quaternion(0, 0, 0, 1),
            new Quaternion(0, -0.707106781f, 0, 0.707106781f),
            new Quaternion(0, 1, 0, 0),
            new Quaternion(0, 0.707106781f, 0, 0.707106781f),
        };
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            string vesselName = part.vessel.vesselName;
            CelestialBody body = part.vessel.mainBody;

            if (!string.IsNullOrEmpty(SpawnTransform))
            {
                launchTransform = part.FindModelTransform(SpawnTransform);
            }
            if (!string.IsNullOrEmpty(SpawnTransformOriginal))
            {
                Transform originalTransform = part.FindModelTransform(SpawnTransformOriginal);
                if (originalTransform != null)
                    launchTransformOriginalPosition = originalTransform.position;
            }
        }
        #endregion

        #region ELBuildControl.IBuilder
        public Transform PlaceShip(Transform shipTransform, Box vessel_bounds)
        {
            // Restore the launch position transform
            if (launchTransform == null)
                return null;
            launchTransform.position = launchTransformOriginalPosition;

            // Get the vessel length so we can offset the transform.
            ConfigNode node = control.craftConfig;
            if (node == null)
                return null;
            float length = Mathf.Abs(shipTransform.position.z) - Mathf.Abs(vessel_bounds.max.z);
            if (node.HasValue("size"))
            {
                string size = node.GetValue("size");
                string[] dimensions = size.Split(new char[] { ',' });
                if (dimensions.Length >= 3)
                    float.TryParse(dimensions[2], out length);
            }

            // Now move the transform.
            relativePosition = offsetAxis * length;
            launchTransform.Translate(relativePosition);
            Vector3 pos = launchTransform.TransformPoint(relativePosition);
//            launchTransform.position = pos;

            // Account for ground.
            RaycastHit terrainHit;
            float heightOffset = 0f;
            if (Physics.Raycast(launchTransform.position, part.vessel.transform.forward, out terrainHit, 1000f, -1))
            {
                //See if we found the ground. 15 = Local Scenery, 28 = TerrainColliders
                if (terrainHit.collider.gameObject.layer == 15 || terrainHit.collider.gameObject.layer == 28)
                {
                    heightOffset = terrainHit.distance;
                    relativePosition = new Vector3(0, -1, 0) * (heightOffset - groundOffset);
                    launchTransform.Translate(relativePosition);
//                    pos = launchTransform.TransformPoint(relativePosition);
//                    launchTransform.position = pos;
                }
            }

            // Now set up the ship transform
            float height = shipTransform.position.y - vessel_bounds.min.y;
            relativeRotaion = rotations[rotationIndex] * shipTransform.rotation;
            relativePosition = new Vector3(0, height, 0);
            Quaternion rot = launchTransform.rotation * relativeRotaion;
            pos = launchTransform.TransformPoint(relativePosition);
            shipTransform.rotation = rot;
            shipTransform.position = pos;
            return shipTransform;
        }

        public void RepositionShip(Vessel ship)
        {
            Quaternion rot = launchTransform.rotation * relativeRotaion;
            Vector3 pos = launchTransform.TransformPoint(relativePosition);
            ship.SetRotation(rot, false);
            ship.SetPosition(pos, true);
        }
        #endregion

        #region ELControlInterface
        public bool canOperate
        {
            get { return Operational; }
            set { Operational = value; }
        }
        #endregion
    }
}
