using System;
using UnityEngine;
using DG.Tweening;
using LocomotionSystem.Utilities;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace LocomotionSystem
{
    /// <summary>
    /// Base class for all interactable items with common functionality
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class CollectableBase : InteractableBase
    {
        #region Unity Lifecycle

        protected override void FixedUpdate()
        {
            if (!m_IsSelected || !m_TextCanvas || !m_TextCanvas.gameObject.activeSelf) return;
            if (_mainCamera)
            {
                m_TextCanvas.transform.rotation = _mainCamera.transform.rotation;
            }
        }
        #endregion

        #region Public Methods
        public override bool OnInteract(GameObject collector)
        {
            if (m_InteractSound)
            {
                AudioSource.PlayClipAtPoint(m_InteractSound, transform.position);
            }
            
            Unregister();
            
            Destroy(gameObject);
            
            return true;
        }
        
        /// <summary>
        /// Implementation of IInteractable.GetTypeData method
        /// Base implementation returns null as it doesn't have specialized data
        /// </summary>
        public override T GetTypeData<T>() where T : class
        {
            return null;
        }
        #endregion

    }
} 