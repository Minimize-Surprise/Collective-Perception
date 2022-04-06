using System;
using UnityEngine;

namespace Assets
{
    public class DiffDriveMotionHandler : MotionHandler
    {
        public struct Pose
        {
            public float x, y, theta;

            public Pose(float x, float y, float theta)
            {
                this.x = x;
                this.y = y;
                this.theta = theta;
            }
        }
        
        private Pose currentPose;
        private Pose nextPose;

        private Vector3 parentArenaOffset;

        /// <summary>
        ///  Radius of diffdrive rotation
        /// </summary>
        private float R;
        
        private Rigidbody rigidBody; 
        
        public DiffDriveMotionHandler(BotController botControllerParent) : base(botControllerParent)
        {
        }
        
        public override void Initialize()
        {
            base.Initialize();
            UpdateCurrentPoseFromGameObj();
            this.rigidBody = gameObject.GetComponent<Rigidbody>();
            parentArenaOffset = botControllerParent.parentScript.transform.parent.position;
        }
        
        public override void Reinitialize()
        {
            base.Reinitialize();
            UpdateCurrentPoseFromGameObj();
        }

        public override void TurnInDirection(TurnDirection turnDirection)
        {
            var turnSpeed = botControllerParent.forwardSpeed;
            if (turnDirection == TurnDirection.ClockWise)
            {
                MotionWrapper(turnSpeed, -turnSpeed);
            }
            else
            {
                MotionWrapper(-turnSpeed, turnSpeed);
            }
        }

        public override void MoveStraight()
        {
            MotionWrapper(botControllerParent.forwardSpeed, botControllerParent.forwardSpeed);
        }

        /// <summary>
        /// Calculates and returns the updated pose of the bot (new x, y, theta) using differential drive motion.
        ///
        /// <br />
        /// Sources:  <br />
        /// - https://github.com/GkcA/Wheelchair-Unity3D/blob/master/Assets/Scripts/DifferentialDriveControl.cs#L125 <br />
        /// - http://ais.informatik.uni-freiburg.de/teaching/ss17/robotics/exercises/solutions/03/sheet03sol.pdf
        /// </summary>
        /// <param name="v_l">Speed of left wheel</param>
        /// <param name="v_r">Speed of right wheel</param>
        /// <param name="t">Driving time (delta)</param>
        /// <param name="l">Distance between wheels of bot</param>
        /// <returns>Pose object with updated parameters</returns>
        public Pose CalcNewPoseDiffDrive(float v_l, float v_r, float t, float l)
        {
            float x_new, y_new;
            float theta_new;

            // Straight line
            if (Math.Abs(v_l - v_r) < 0.005f)
            {
                theta_new = currentPose.theta;
                x_new = currentPose.x + v_l * t * Mathf.Cos(currentPose.theta);
                y_new = currentPose.y + v_l * t * Mathf.Sin(currentPose.theta);

            }
            else  // Circular motion
            {
                //  Calculate radius
                R = l / 2.0f * ((v_l + v_r) / (v_r - v_l));

                // Computing center of curvature
                float ICC_x = currentPose.x - R * Mathf.Sin(currentPose.theta);
                float ICC_y = currentPose.y + R * Mathf.Cos(currentPose.theta);

                // Compute the angular velocity
                float omega = (v_r - v_l) / l;

                // Computing angle change
                float dtheta = omega * t;

                // Forward kinematics for differential drive
                x_new = Mathf.Cos(dtheta) * (currentPose.x - ICC_x) - Mathf.Sin(dtheta) * (currentPose.y - ICC_y) + ICC_x;
                y_new = Mathf.Sin(dtheta) * (currentPose.x - ICC_x) + Mathf.Cos(dtheta) * (currentPose.y - ICC_y) + ICC_y;
                theta_new = currentPose.theta + dtheta;
            }
            return new Pose(x_new, y_new, theta_new);
        }

        private bool print = true;
        public void MotionWrapper(float v_l, float v_r)
        {
            var t = Time.deltaTime;
            var l = botControllerParent.parentScript.masterParams.beeWidth;
            this.nextPose = CalcNewPoseDiffDrive(v_l, v_r, t, l);
            
            if (IsNextPositionInArena(new Vector2(nextPose.x, nextPose.y)))
            {
                UpdatePositionWithNextPose();
                print = true;
            }
            else
            {
                if (!print) return;
                Debug.Log($"Next pose ({nextPose.x} - {nextPose.y}) not in Arena.", 
                    this.botControllerParent.parentScript);
                print = false;
            }
        }

        private void UpdatePositionWithNextPose()
        {
            Move();
            Turn();
            this.currentPose = this.nextPose;
        }

        public void Turn()
        {
            float thetaDeg = -Mathf.Rad2Deg * this.nextPose.theta + 90f;
            Quaternion rotate = Quaternion.Euler(0f, thetaDeg, 0f);
            this.rigidBody.MoveRotation(rotate);
        }

        public void Move()
        {
            Vector3 newPosition = new Vector3(nextPose.x, gameObject.transform.localPosition.y, nextPose.y);
            this.rigidBody.MovePosition(parentArenaOffset + newPosition);
        }

        public void UpdateCurrentPoseFromGameObj()
        {
            var localPosition = gameObject.transform.localPosition;
            this.currentPose = new Pose(localPosition.x,
                localPosition.z,
                - (gameObject.transform.eulerAngles.y -90) / Mathf.Rad2Deg); 
            // theta calculation = inverse of the calculation above
        }

        
    }
}