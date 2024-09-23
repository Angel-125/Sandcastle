using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PrintShop
{
    internal class WireframeBox
    {
        #region Constants
        const int kDisplayLayer = 11;
        #endregion

        #region Fields
        public Bounds bounds;
        public Color lineColor = XKCDColors.White;
        public bool _isVisible = false;
        public float width = 0.05f;

        public bool isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                _isVisible = value;
                lineRenderer.enabled = isVisible;
            }
        }
        #endregion

        #region Housekeeping
        string vectorID;
        LineRenderer lineRenderer;
        GameObject lineObject;
        #endregion

        #region Constructors
        public WireframeBox()
        {
            setupDrawingElements();
        }

        public WireframeBox(Bounds bounds, Color color)
        {
            this.lineColor = color;

            setupDrawingElements();

            this.bounds = bounds;
        }

        ~WireframeBox()
        {
            lineObject.DestroyGameObjectImmediate();
        }
        #endregion

        #region API
        public void SetupWireframe(Transform transform, Bounds bounds, Vector3 offset)
        {
            this.bounds = bounds;
            Vector3[] vertices = GetWireframeBoxVertices(transform, bounds, offset);
            lineRenderer.positionCount = vertices.Length;
            lineRenderer.SetPositions(vertices);
        }

        public void Draw()
        {
            if (!isVisible || bounds == null)
                return;

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        public Vector3[] GetWireframeBoxVertices(Transform transform, Bounds bounds, Vector3 offset)
        {
            // Calculate the 8 corners of the box in local space (relative to the Bounds)
            Vector3[] vertices = new Vector3[8];

            // Local space vertices, relative to the center of the bounds
            Vector3 extents = bounds.extents;
            vertices[0] = bounds.center + new Vector3(-extents.x, -extents.y, -extents.z);  // Bottom-left front
            vertices[1] = bounds.center + new Vector3(extents.x, -extents.y, -extents.z);   // Bottom-right front
            vertices[2] = bounds.center + new Vector3(extents.x, extents.y, -extents.z);    // Top-right front
            vertices[3] = bounds.center + new Vector3(-extents.x, extents.y, -extents.z);   // Top-left front
            vertices[4] = bounds.center + new Vector3(-extents.x, -extents.y, extents.z);   // Bottom-left back
            vertices[5] = bounds.center + new Vector3(extents.x, -extents.y, extents.z);    // Bottom-right back
            vertices[6] = bounds.center + new Vector3(extents.x, extents.y, extents.z);     // Top-right back
            vertices[7] = bounds.center + new Vector3(-extents.x, extents.y, extents.z);    // Top-left back

            // Apply offset
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += offset;
            }

            // Convert the vertices from local space to world space by applying the Transform's position and rotation
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.TransformPoint(vertices[i] - bounds.center);  // Move from bounds' local center to world space using Transform
            }

            // Create a 90-degree rotation around the Z-axis
            Quaternion rotation = Quaternion.AngleAxis(90, transform.forward);  // Rotate around the Z-axis

            // Apply the rotation to all the vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotation * (vertices[i] - transform.position) + transform.position;  // Rotate around the Transform's position
            }

            // Define the edges of the wireframe box
            Vector3[] wireframePositions = new Vector3[16];

            // Front face (clockwise)
            wireframePositions[0] = vertices[0];   // Bottom-left front
            wireframePositions[1] = vertices[1];   // Bottom-right front
            wireframePositions[2] = vertices[2];   // Top-right front
            wireframePositions[3] = vertices[3];   // Top-left front
            wireframePositions[4] = vertices[0];   // Bottom-left front

            // Back face (clockwise)
            wireframePositions[5] = vertices[4];  // Bottom-left back
            wireframePositions[6] = vertices[5];  // Bottom-right back
            wireframePositions[7] = vertices[6];  // Top-right back
            wireframePositions[8] = vertices[7];  // Top-left back
            wireframePositions[9] = vertices[4];  // Bottom-left back
            wireframePositions[10] = vertices[7];  // Top-left back

            // Side faces
            wireframePositions[11] = vertices[3];   // Top-left front
            wireframePositions[12] = vertices[2];   // Top-right front
            wireframePositions[13] = vertices[6];  // Top-right back
            wireframePositions[14] = vertices[5];  // Bottom-right back
            wireframePositions[15] = vertices[1];   // Bottom-right front

            return wireframePositions;
        }
        #endregion

        #region Helpers
        private void setupDrawingElements()
        {
            vectorID = Guid.NewGuid().ToString();

            lineObject = new GameObject(vectorID + "lineObject");
            lineObject.layer = kDisplayLayer;

            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.enabled = _isVisible;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }
        #endregion
    }
}
