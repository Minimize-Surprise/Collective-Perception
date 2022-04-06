using UnityEngine;

namespace Assets
{
    public static class GizmoUtilities
    {
        private const float GIZMO_DISK_THICKNESS = 0.01f;
        public static void DrawGizmoDisk(this Transform t, float radius)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(1f, 0.9f, 0.53f, 0.2f); 
            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, new Vector3(1, GIZMO_DISK_THICKNESS, 1));
            Gizmos.DrawSphere(Vector3.zero, radius);
            Gizmos.matrix = oldMatrix;
        }
    }
    
    [System.Serializable]
    public class GizmoManager
    {
        public bool drawCommunicationRange = false;
        public bool drawProximSensorRange = false;
    }
}