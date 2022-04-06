using UnityEngine;

namespace Assets
{
    public class ProximitySensor
    {

        public readonly float relativeAngle;
        private readonly Transform robotTransform;
        private readonly float maxSensingRange;
        public Vector3 sensorVector;

        private float lastSensorVectorUpdate = -1f;

        public ProximitySensor(float relativeAngle, Transform robotTransform, float maxSensingRange)
        {
            this.relativeAngle = relativeAngle;
            this.robotTransform = robotTransform;
            this.maxSensingRange = maxSensingRange;
            UpdateSensorVector();
        }

        public float GetAbsoluteAngle()
        {
            return Misc.mod((this.robotTransform.rotation.y + relativeAngle), 360f);
        }

        public void UpdateSensorVector()
        {
            if (lastSensorVectorUpdate < Time.time)
            {
                sensorVector = Quaternion.AngleAxis(relativeAngle, Vector3.up) * robotTransform.forward;
                lastSensorVectorUpdate = Time.time;
            }
        }

        public void DrawSensorRays(bool colorHits)
        {
            UpdateSensorVector();
            Color col;
            if (colorHits)
                col = BinaryRead() ? Color.red : Color.white;
            else
                col = Color.white;
            Debug.DrawRay(robotTransform.position, sensorVector * maxSensingRange, col); 
        }
        
        public bool BinaryRead()
        {
            UpdateSensorVector();
            RaycastHit hit;
            return Physics.Raycast(robotTransform.position, sensorVector, out hit, maxSensingRange);
        }

        public float DistanceRead()
        {
            UpdateSensorVector();
            RaycastHit hit;
            if (Physics.Raycast(robotTransform.position, sensorVector, out hit, maxSensingRange))
            {
                return hit.distance;
            }
            // Possibility: Check for tags (-> wall vs. bot)
            // hit.transform.gameObject.CompareTag("MONA"); 
            return -1f;
        }
    }
}