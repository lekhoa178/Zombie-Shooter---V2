using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerTree
{
    public class Heart : MonoBehaviour
    {
        [SerializeField] private Image _heartImage;          // The image component to display the heart
        
        [SerializeField] private Sprite fullHeartSprite;    // 1.0f
        [SerializeField] private Sprite halfHeartSprite;    // 0.5f
        [SerializeField] private Sprite emptyHeartSprite;   // 0.0f
        [SerializeField] private float heartAmount = 1f;
        
        public float HeartAmount
        {
            get => heartAmount;
            set
            {
                heartAmount = Mathf.Clamp01(value);
                OnValidate();
            }
        }
        
        public bool IsFull => Mathf.Approximately(heartAmount, 1f);
        public bool IsHalf => Mathf.Approximately(heartAmount, 0.5f);
        public bool IsEmpty => Mathf.Approximately(heartAmount, 0f);

        private void Awake()
        {
            if (_heartImage == null)
            {
                _heartImage = GetComponent<Image>();
            }
        }

        private void OnValidate()
        {
            if (_heartImage == null)
            {
                return;
            }
            
            if (Mathf.Approximately(heartAmount, 0))
            {
                _heartImage.sprite = emptyHeartSprite;
            }
            else if (Mathf.Approximately(heartAmount, 0.5f))
            {
                _heartImage.sprite = halfHeartSprite;
            }
            else if (Mathf.Approximately(heartAmount, 1))
            {
                _heartImage.sprite = fullHeartSprite;
            }
        }
        
        public void SetHeartAmount(float amount)
        {
            heartAmount = Mathf.Clamp01(amount);
            OnValidate();
        }
    }
}