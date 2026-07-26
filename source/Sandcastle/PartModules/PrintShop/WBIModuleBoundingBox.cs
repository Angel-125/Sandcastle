using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;
using EditorGizmos;

namespace Sandcastle.PrintShop
{
    public class WBIModuleBoundingBox: WBIBasePartModule
    {
        #region Fields
        public Bounds wireframeBounds
        {
            get
            {
                return wireframeBox.bounds;
            }

            set
            {
                wireframeBox.bounds = value;
            }
        }

        public bool wireframeIsVisible
        {
            get
            {
                return wireframeBox.isVisible;
            }

            set
            {
                wireframeBox.isVisible = value;
            }
        }

        public Transform originTransform
        {
            get
            {
                return _originTransform;
            }

            set
            {
                _originTransform = value;
                upVectorLine = new VectorLine(_originTransform.position, _originTransform.transform.up, 5f, Color.green);
                fwdVectorLine = new VectorLine(_originTransform.position, _originTransform.transform.forward, 5f, Color.blue);
                rightVectorLine = new VectorLine(_originTransform.position, _originTransform.transform.right, 5f, Color.red);
            }
        }

        public bool originTransformIsVisible
        {
            get
            {
                return upVectorLine.isVisible;
            }

            set
            {
                upVectorLine.isVisible = value;
                fwdVectorLine.isVisible = value;
                rightVectorLine.isVisible = value;
            }
        }

        public bool moveGizmoIsVisible
        {
            get
            {
                return moveGizmo != null;
            }

            set
            {
                if (value)
                {
                    setupMoveGizmo();
                }
                else
                {
                    if (moveGizmo != null)
                    {
                        moveGizmo.Detach();
                        moveGizmo = null;
                    }
                }
            }
        }
        #endregion

        #region Housekeeping
        VectorLine upVectorLine;
        VectorLine fwdVectorLine;
        VectorLine rightVectorLine;
        WireframeBox wireframeBox;
        Transform _originTransform = null;
        GizmoOffset moveGizmo;
        Vector3 offset;
        Transform movementBoundary;
        Transform movementBoundaryInterior;
        #endregion


        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            upVectorLine = new VectorLine();
            fwdVectorLine = new VectorLine();
            rightVectorLine = new VectorLine();
            wireframeBox = new WireframeBox();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // The flight scene origin and the vessel transform both move during
            // orbital flight. Rebuild world-space vertices from the live origin
            // so the preview remains attached to the printer.
            if (wireframeIsVisible && _originTransform != null)
                wireframeBox.SetupWireframe(_originTransform, wireframeBox.bounds, offset);

            upVectorLine.Draw();
            fwdVectorLine.Draw();
            rightVectorLine.Draw();
            wireframeBox.Draw();
        }

        public void OnDestroy()
        {
            if (moveGizmo != null)
            {
                moveGizmo.Detach();
                moveGizmo = null;
            }
        }
        #endregion

        #region API
        public void SetupWireframe(Transform transform, Bounds bounds)
        {
            _originTransform = transform;
            offset = Vector3.zero;
            wireframeBox.SetupWireframe(transform, bounds, offset);
        }

        public void SetupWireframe(Transform transform, Bounds bounds, Vector3 offset)
        {
            _originTransform = transform;
            this.offset = offset;
            wireframeBox.SetupWireframe(transform, bounds, offset);
        }

        public void SetupMovementBoundary(Transform boundary, Transform interior)
        {
            movementBoundary = boundary;
            movementBoundaryInterior = interior;
        }

        public void ClearMovementBoundary()
        {
            movementBoundary = null;
            movementBoundaryInterior = null;
        }
        #endregion

        #region Helpers
        void onMoveComplete(Vector3 vector)
        {
            _originTransform.position = constrainToMovementBoundary(moveGizmo.transform.position);
            moveGizmo.transform.position = _originTransform.position;
            if (wireframeIsVisible)
            {
                wireframeBox.SetupWireframe(_originTransform, wireframeBox.bounds, offset);
            }
        }

        void onMove(Vector3 vector)
        {
            _originTransform.position = constrainToMovementBoundary(moveGizmo.transform.position);
            moveGizmo.transform.position = _originTransform.position;
            if (wireframeIsVisible)
            {
                wireframeBox.SetupWireframe(_originTransform, wireframeBox.bounds, offset);
            }
        }

        void setupMoveGizmo()
        {
            if (moveGizmo != null)
            {
                moveGizmo.Detach();
                moveGizmo = null;
            }

            moveGizmo = GizmoOffset.Attach(_originTransform, _originTransform.rotation, onMove, onMoveComplete, FlightCamera.fetch.mainCamera);
            moveGizmo.SetCoordSystem(Space.Self);

            Transform[] transforms = moveGizmo.gameObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if ((transforms[i].gameObject.GetComponent<Collider>() != null) && (transforms[i].gameObject.GetComponent<Collider>().isTrigger))
                    continue;
                transforms[i].gameObject.layer = 11;
            }

        }

        Vector3 constrainToMovementBoundary(Vector3 candidatePosition)
        {
            if (movementBoundary == null || movementBoundaryInterior == null)
                return candidatePosition;

            Vector3 groundNormal = part.vessel.upAxis.normalized;
            Vector3 boundaryNormal = Vector3.ProjectOnPlane(
                movementBoundary.position - movementBoundaryInterior.position,
                groundNormal).normalized;
            if (boundaryNormal == Vector3.zero)
                return candidatePosition;

            Vector3 extents = wireframeBox.bounds.extents;
            float supportRadius =
                Mathf.Abs(Vector3.Dot(boundaryNormal, _originTransform.right)) * extents.x +
                Mathf.Abs(Vector3.Dot(boundaryNormal, _originTransform.up)) * extents.y +
                Mathf.Abs(Vector3.Dot(boundaryNormal, _originTransform.forward)) * extents.z;
            float centerDistance = Vector3.Dot(
                candidatePosition - movementBoundary.position, boundaryNormal);

            if (centerDistance < supportRadius)
                candidatePosition += boundaryNormal * (supportRadius - centerDistance);

            return candidatePosition;
        }
        #endregion
    }
}
