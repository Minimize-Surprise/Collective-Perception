using System.Collections;
using System.IO;
using UnityEngine;

namespace Assets
{
    [System.Serializable]
    public class BeeClustController : BotController
    {
        public float wMax = 60.0f;
        public float theta = 1000.0f;
        public int s;
        public float w;
        public bool frontSensingOnly = false;
        
        private bool waitTakeOver = false;
        public bool turning = false;
        
        private Vector3 initPos;
        
        public int collision;
        
        public BeeClustController(BeeClust beeClustScript, BotParams botParams) : base(beeClustScript, botParams)
        {
            motionHandler = new BeeClustMotionHandler(this);
            SetParameters(botParams);
        }

        public override void Initialize()
        {
            base.Initialize();
            motionHandler.Initialize();
        }
        
        public new void SetParameters(BotParams botParams)
        {
            base.SetParameters(botParams);
            var bcp = botParams;
            this.wMax = bcp.maxW;
            this.theta = bcp.theta;
        }
        
        public override void Step()
        {
            if (masterParameters.run)
            {
                // UpdateSensorVectors(); // not needed anymore - is done & cached automatically
                
                if (masterParameters.gizmoManager.drawProximSensorRange)
                    DrawSensorRays(false);
                
                if (!waitTakeOver)
                {
                    if (turning)
                    {
                        motionHandler.TurnInDirection(TurnDirection.ClockWise);
                    }
                    else
                    {
                        ((BeeClustMotionHandler) motionHandler).MoveStraight();
                        // Check forward
                        if (Physics.Raycast(transform.position, transform.forward, out frontSens, maxSensingRange))
                        {
                            if (!turning)
                            {
                                var startBearing = transform.rotation;
                                bool boolValue = (Random.Range(0, 2) == 0);
                                if (boolValue)
                                {
                                    ((BeeClustMotionHandler) motionHandler).targetRelativeTurnAngle = turnAngle1;
                                }
                                else
                                {
                                    ((BeeClustMotionHandler) motionHandler).targetRelativeTurnAngle = -turnAngle1;
                                }

                                if (frontSens.transform.gameObject.CompareTag("MONA"))
                                {
                                    WaitFunction();
                                }
                                else
                                {
                                    turning = true;
                                }
                            }
                        }

                        CheckSensorForTurning(irLeft1, turnAngle2);
                        CheckSensorForTurning(irLeft2, turnAngle3);
                        CheckSensorForTurning(irRight1, -turnAngle2);
                        CheckSensorForTurning(irRight2, -turnAngle3);
                    }
                }
            }
        }
        
        protected void CheckSensorForTurning(Vector3 irVector, float turnAngleToUse)
        {
            if (Physics.Raycast(transform.position, irVector, out frontSens, maxSensingRange))
            {
                if (!turning)
                {
                    ((BeeClustMotionHandler) motionHandler).startBearing = transform.rotation;
                    ((BeeClustMotionHandler) motionHandler).targetRelativeTurnAngle = turnAngleToUse;
                    if (frontSens.transform.gameObject.CompareTag("MONA") && !frontSensingOnly)
                    {
                        WaitFunction();
                    }
                    else
                    {
                        turning = true;
                    }
                }
            }
        }
        
        void WaitFunction()
        {
            if (masterParameters.tempPath != "")
            {

                int xTemp = (int)Mathf.Round(transform.position.x * 10.0f);
                int yTemp = (int)Mathf.Round(transform.position.z * 10.0f);

                s = masterParameters.tempEntries[masterParameters.arenaHeight * 10 - 1 - yTemp, xTemp];

                w = wMax * (Mathf.Pow(s, 2.0f) / (Mathf.Pow(s, 2.0f) + theta));
            }
            else
            {
                w = 0;
            }
            //Collision
            collision++;
            bool oncue = false;
            if (s != 0)
            {
                oncue = true;
            }
            else
            {
                oncue = false;
            }

            
        
            // Collision Logs
            WriteCollisionLogs(oncue);
            
        }
        
        private void WriteCollisionLogs(bool oncue)
        {
            float time = Time.time - masterParameters.currentRoundStartTime;
            StreamWriter writer = new StreamWriter(parentScript.pathCollisionLog, true);

            writer.WriteLine(masterParameters.currentSimulationRound + "\t" + time.ToString("F2") + "\t" + parentScript.gameObject.name + "\t" + transform.localPosition.x.ToString("F2") + "\t" + transform.localPosition.z.ToString("F2") + "\t" + collision + "\t" + oncue);
            writer.Close();
        
            StreamWriter writer_state = new StreamWriter(parentScript.pathStateLog, true);

            writer_state.WriteLine(masterParameters.currentSimulationRound + "\t" + time.ToString("F2") + "\t" + parentScript.gameObject.name + "\t" + transform.localPosition.x.ToString("F2") + "\t" + transform.localPosition.z.ToString("F2") + "\t" + "0" + "\t" + w + "\t" + oncue);
            writer_state.Close();

            parentScript.StartCoroutine(Delay(w));
        }
        
        IEnumerator Delay(float w)
        {
            waitTakeOver = true;
            yield return new WaitForSeconds(w);
            // Return here after w secs
            waitTakeOver = false;
            turning = true;
        }
    }
}