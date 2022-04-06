using System;
using System.Linq;
using UnityEngine;

namespace Assets
{
    [System.Serializable]
    public class BotController
    {
        public int index;
        public int homeArena;
        
        public MotionHandler motionHandler;
        
        protected Parameters masterParameters;
        protected Transform transform;
        
        public float maxSensingRange;
        public float forwardSpeed;
        public float turnSpeed;
        
        public float turnAngle1;
        public float turnAngle2;
        public float turnAngle3;
        
        public float sensor2Angle;
        public float sensor3Angle;
        
        protected RaycastHit frontSens;
        protected Vector3 irLeft1;
        protected Vector3 irLeft2;
        protected Vector3 irRight1;
        protected Vector3 irRight2;

        private Renderer groundRenderer;
        public float lastMeasuredGroundBrightness;

        public BeeClust parentScript;
        
        public BotParams botParams;

        [SerializeField]
        public OpinionHandler opinionHandler;

        public ProximitySensor[] proximitySensors;

        public BotController(BeeClust parentScript, BotParams botParams)
        {
            this.parentScript = parentScript;
            this.botParams = botParams;
            SetParameters(botParams);
        }

        private ProximitySensor[] CreateProximitySensors()
        {
            var relativeAngles = new float[]{- sensor3Angle, -sensor2Angle, 0, sensor2Angle, sensor3Angle};
            var sensors = new ProximitySensor[relativeAngles.Length];

            for (int i = 0; i < relativeAngles.Length; i++)
            {
                sensors[i] = new ProximitySensor(relativeAngles[i], transform, maxSensingRange);
            }

            return sensors;
        }

        public void SetParameters(BotParams botParams)
        {
            this.index = botParams.botIndex;
            this.homeArena = botParams.homeArena;
            this.forwardSpeed = botParams.forwardSpeed;
            this.turnSpeed = botParams.turnSpeed;
            this.maxSensingRange = botParams.maxSensingRange;
        }

        public virtual void Initialize()
        {
            this.transform = parentScript.transform;
            this.masterParameters = parentScript.masterParams;
            this.opinionHandler.Initialize();
            this.proximitySensors = CreateProximitySensors();
        }
        
        public virtual void Reinitialize(Opinion newOpinion)
        {
            this.opinionHandler.Reinitialize(newOpinion);
            this.motionHandler.Reinitialize();
        }
        
        public virtual void Step()
        {
            throw new System.NotImplementedException();
        }

        protected void UpdateSensorVectors()
        {
            foreach (var sensor in proximitySensors)
            {
                sensor.UpdateSensorVector();
            }
        }

        protected void DrawSensorRays(bool colorHits)
        {
            foreach (var sensor in proximitySensors)
            {
                sensor.DrawSensorRays(colorHits);
            }
        }

        private void MeasureGroundSensor()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit))
            {
                if (groundRenderer == null)
                {
                    groundRenderer = hit.transform.GetComponent<Renderer>(); 
                }
                var texmap = groundRenderer.material.mainTexture as Texture2D;
                var pixelUV = hit.textureCoord;
                pixelUV.x *= texmap.width;
                pixelUV.y *= texmap.height;
                var col = texmap.GetPixel(Mathf.FloorToInt(pixelUV.x), Mathf.FloorToInt(pixelUV.y));

                float h, s, v;
                Color.RGBToHSV(col, out h, out s, out v);

                this.lastMeasuredGroundBrightness = v;
            }
        }

        public Opinion GetCurrentGroundOpinion()
        {
            const float brightnessTolerance = 0.49f;
            MeasureGroundSensor();
            return Math.Abs(lastMeasuredGroundBrightness - 1f) < brightnessTolerance ? Opinion.White : Opinion.Black;
        }
        
        public void ApplyParamsFromOldController(BotController oldBot)
        {
            this.opinionHandler = oldBot.opinionHandler;
            this.opinionHandler.ApplyParams(this, botParams.initialOpinion);
            this.turnAngle1 = oldBot.turnAngle1;
            this.turnAngle2 = oldBot.turnAngle2;
            this.turnAngle3 = oldBot.turnAngle3;
            this.sensor2Angle = oldBot.sensor2Angle;
            this.sensor3Angle = oldBot.sensor3Angle;
        }

        /// <summary>
        /// Returns a tuple-array of all proximity sensors with a sensor hit, with the hit distance.
        /// Sensors without hit are filtered out.
        /// </summary>
        /// <returns></returns>
        public (ProximitySensor, float)[] ReadProximitySensors()
        {
            return proximitySensors.Select(sensor => (sensor, sensor.DistanceRead())).
                Where(tuple => Math.Abs(tuple.Item2 - (-1f)) > 0.01).ToArray();
        }

        /// <summary>
        /// Check whether there is any obstacle within the sense range.
        /// </summary>
        /// <returns>Bool, true in the case there is 1+ obstacle. </returns>
        public bool CheckObstacleInRange()
        {
            var inRange = false;
            for (int i = 0; i < proximitySensors.Length; i++)
            {
                if (proximitySensors[i].BinaryRead())
                {
                    inRange = true;
                    break;
                }
            }

            return inRange;
        }

        public virtual string GetFocusInfoText()
        {
            var s = $"Bot ID: {this.index}";
            return s;
        }

        public virtual bool IsListening()
        {
            return true;
        }
    }
}