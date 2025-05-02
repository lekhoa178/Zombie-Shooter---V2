// Helper class for spawn points

using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.5f);

    private void OnDrawGizmos()
    {
        if (drawGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 1f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}