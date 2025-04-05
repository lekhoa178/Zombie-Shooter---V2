using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShadowDashEffect : MonoBehaviour
{
    private List<Transform> shadowMeshes;

    public float Interval = 0.1f;

    private void Awake()
    {
        shadowMeshes = new List<Transform>(GetComponentsInChildren<Transform>());

        // Remove the parent object itself from the list
        shadowMeshes.Remove(transform);
        
        foreach (var shadowMesh in shadowMeshes)
        {
            shadowMesh.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(ShadowDash());
    }

    private IEnumerator ShadowDash()
    {
        foreach (var shadowMesh in shadowMeshes)
        {
            shadowMesh.gameObject.SetActive(true);
            yield return new WaitForSeconds(Interval);
            // shadowMesh.gameObject.SetActive(false);
        }
    }
}
