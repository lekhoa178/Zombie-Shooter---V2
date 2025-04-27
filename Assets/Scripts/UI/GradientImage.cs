using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UI
{
    /// <summary>
    /// Image component that supports gradient rendering in UI elements.
    /// </summary>
    [AddComponentMenu("UI/Gradient Image")]
    public class GradientImage : Image
    {
        #region Serialized Fields
        [SerializeField] private Gradient m_Gradient = new Gradient();
        [SerializeField] private GradientDirection m_GradientDirection = GradientDirection.Horizontal;
        #endregion

        #region Private Fields
        private List<UIVertex> m_VertexList = new List<UIVertex>(4);
        #endregion

        #region Public Properties
        /// <summary>
        /// The gradient used for coloring the image.
        /// </summary>
        public Gradient Gradient
        {
            get => m_Gradient;
            set
            {
                m_Gradient = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// The direction of the gradient.
        /// </summary>
        public GradientDirection GradientMode
        {
            get => m_GradientDirection;
            set
            {
                m_GradientDirection = value;
                SetVerticesDirty();
            }
        }
        #endregion

        #region Enums
        /// <summary>
        /// The direction in which the gradient is applied.
        /// </summary>
        public enum GradientDirection
        {
            Horizontal,
            Vertical,
            DiagonalLeftToRight,
            DiagonalRightToLeft,
            Radial
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Called by the Canvas when vertices need to be modified.
        /// </summary>
        /// <param name="_vh">The VertexHelper object to modify.</param>
        protected override void OnPopulateMesh(VertexHelper _vh)
        {
            base.OnPopulateMesh(_vh);
            
            if (m_Gradient == null)
                return;

            m_VertexList.Clear();
            _vh.GetUIVertexStream(m_VertexList);

            if (m_VertexList.Count == 0)
                return;
                
            ApplyGradient();

            _vh.Clear();
            _vh.AddUIVertexTriangleStream(m_VertexList);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Applies the gradient to the vertices based on the selected direction.
        /// </summary>
        private void ApplyGradient()
        {
            if (m_VertexList.Count == 0)
                return;

            Rect rect = GetPixelAdjustedRect();
            Vector2 rectMin = rect.min;
            Vector2 rectSize = rect.size;

            for (int i = 0; i < m_VertexList.Count; i++)
            {
                UIVertex vertex = m_VertexList[i];
                Vector2 normalizedPos = GetNormalizedPosition(vertex.position, rectMin, rectSize);
                float gradientPos = GetGradientPosition(normalizedPos);
                Color vertexColor = m_Gradient.Evaluate(gradientPos);
                
                // Preserve the original alpha
                vertexColor.a *= vertex.color.a / 255f;
                vertex.color = vertexColor;
                m_VertexList[i] = vertex;
            }
        }

        /// <summary>
        /// Calculates the normalized position of a vertex within the rect.
        /// </summary>
        /// <param name="_position">The position of the vertex.</param>
        /// <param name="_rectMin">The minimum point of the rect.</param>
        /// <param name="_rectSize">The size of the rect.</param>
        /// <returns>The normalized position (0-1) in both axes.</returns>
        private Vector2 GetNormalizedPosition(Vector3 _position, Vector2 _rectMin, Vector2 _rectSize)
        {
            Vector2 normalizedPos = new Vector2(
                (_position.x - _rectMin.x) / _rectSize.x,
                (_position.y - _rectMin.y) / _rectSize.y
            );
            
            return normalizedPos;
        }

        /// <summary>
        /// Calculates the gradient position based on the direction and normalized position.
        /// </summary>
        /// <param name="_normalizedPos">The normalized position of the vertex.</param>
        /// <returns>The position along the gradient (0-1).</returns>
        private float GetGradientPosition(Vector2 _normalizedPos)
        {
            switch (m_GradientDirection)
            {
                case GradientDirection.Horizontal:
                    return _normalizedPos.x;
                
                case GradientDirection.Vertical:
                    return _normalizedPos.y;
                
                case GradientDirection.DiagonalLeftToRight:
                    return (_normalizedPos.x + _normalizedPos.y) / 2f;
                
                case GradientDirection.DiagonalRightToLeft:
                    return (_normalizedPos.x + (1f - _normalizedPos.y)) / 2f;
                
                case GradientDirection.Radial:
                    Vector2 centerDelta = _normalizedPos - new Vector2(0.5f, 0.5f);
                    return Mathf.Clamp01(centerDelta.magnitude * 2f);
                
                default:
                    return 0f;
            }
        }
        #endregion
    }
} 