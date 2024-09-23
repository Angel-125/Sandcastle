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
    internal class VectorLine
    {
        #region Constants
        const int kDisplayLayer = 11;
        const float kArrowheadWidthMultiplier = 2f;
        #endregion

        #region Fields
        public Vector3 origin;
        public Vector3 direction;
        public Color lineColor = XKCDColors.White;
        public float length = 1.0f;
        public float width = 0.1f;
        public bool _isVisible = false;

        public string label
        {
            get
            {
                return labelString;
            }
            set
            {
                labelString = value;
                if (labelText != null)
                {
                    labelText.text = labelString;
                }
            }
        }

        public bool isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                _isVisible = value;
                lineRenderer.enabled = _isVisible;
                arrowheadRenderer.enabled = _isVisible;
                labelText.enabled = _isVisible;
            }
        }
        #endregion

        #region Housekeeping
        string labelString;
        LineRenderer lineRenderer;
        LineRenderer arrowheadRenderer;
        GameObject lineObject;
        GameObject arrowheadObject;
        GameObject labelObject;
        Text labelText;
        string vectorID;
        Vector3 lineEnd = Vector3.zero;
        Vector3 arrowheadEnd = Vector3.zero;
        #endregion

        #region Constructors
        public VectorLine()
        {
            this.origin = Vector3.zero;
            this.direction = Vector3.up;
            setupDrawingElements();
        }

        public VectorLine(Vector3 origin, Vector3 direction, float length, Color color)
        {
            this.origin = origin;
            this.length = length;
            this.direction = direction;
            lineColor = color;

            lineEnd = origin + direction * length;
            arrowheadEnd = lineEnd + direction * 1.01f;

            setupDrawingElements();
        }

        ~VectorLine()
        {
            lineObject.DestroyGameObjectImmediate();
            arrowheadObject.DestroyGameObjectImmediate();
            labelObject.DestroyGameObjectImmediate();
        }
        #endregion

        #region API
        public void Draw()
        {
            if (!isVisible)
                return;

            // Set the color
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            arrowheadRenderer.startColor = lineColor;
            arrowheadRenderer.endColor = lineColor;
            labelText.color = lineColor;

            // Draw the line segments
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, lineEnd);

            // Draw arrowhead
            arrowheadRenderer.positionCount = 2;
            arrowheadRenderer.SetPosition(0, lineEnd);
            arrowheadRenderer.SetPosition(1, arrowheadEnd);
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

            arrowheadObject = new GameObject(vectorID + "arrowheadObject");
            arrowheadObject.layer = kDisplayLayer;

            arrowheadRenderer = arrowheadObject.AddComponent<LineRenderer>();
            arrowheadRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            arrowheadRenderer.startWidth = width * kArrowheadWidthMultiplier;
            arrowheadRenderer.endWidth = 0f;
            arrowheadRenderer.enabled = _isVisible;

            labelObject = new GameObject(vectorID + "labelObject", typeof(Text));
            labelText = labelObject.GetComponent<Text>();
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = labelString;
            labelText.enabled = _isVisible;
        }
        #endregion
    }
}
