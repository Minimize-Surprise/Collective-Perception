using System;
using System.Collections.Generic;
using System.Text;
using Assets;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace MotionStateMachine
{

    [System.Serializable]
    public class RandomWalkStateMachine
    {
        private Dictionary<string, State> states;

        protected BotController parentController;
        
        private State currentState;
        private State previousState;

        private float linMotionTimerInterval = 7.5f;
        private float linMotionTimer;

        public RandomWalkStateMachine(BotController parentController)
        {
            this.parentController = parentController;
            CreateStates();
            StateTransition(InitName);
            this.linMotionTimer = linMotionTimerInterval;
        }

        public void Reinitialize()
        {
            currentState = null;
            CreateStates();
            StateTransition(InitName);
        }
        
        private void CreateStates()
        {
            states = new Dictionary<string, State>
            {
                {InitName, new RWInit(this)},
                {LinMotionName, new RWLinearMotion(this)},
                {RotationName, new RWRotation(this)},
                {ObstAvoidName, new RWObstacleAvoidance(this)}
            };
        }

        public void Step()
        {
            currentState.Step();
            currentState.UpdateStateMachineLinMotionTimer();
            currentState.CheckAndHandleStateTransition();
        }

        public void StateTransition(string newStateName)
        {
            previousState = currentState;
            currentState = states[newStateName];
            currentState.Enter();
        }

        private abstract class State
        {
            protected RandomWalkStateMachine parentStateMachine;
            
            public State(RandomWalkStateMachine stateMachine)
            {
                this.parentStateMachine = stateMachine;
            }

            public abstract void Step();
            public abstract void Enter();
            public abstract void CheckAndHandleStateTransition();
            public abstract void UpdateStateMachineLinMotionTimer();
            public abstract override string ToString();
            public abstract string GetInfoString();
        }

        private const string InitName = "INIT";
        private const string LinMotionName = "LIN_MOTION";
        private const string RotationName = "ROTATION";
        private const string ObstAvoidName = "OBSTACLE_AVOIDANCE";

        private class RWInit: State
        {
            public RWInit(RandomWalkStateMachine stateMachine) : base(stateMachine)
            {
            }
            
            public override void Step()
            {
            }

            public override void Enter()
            {
            }

            public override void CheckAndHandleStateTransition()
            {
                parentStateMachine.StateTransition(LinMotionName);
            }

            public override void UpdateStateMachineLinMotionTimer()
            {
                parentStateMachine.linMotionTimer -= Time.deltaTime;
            }

            public override string ToString()
            {
                return InitName;
            }

            public override string GetInfoString()
            {
                return ToString();
            }
        }
        
        private class RWLinearMotion : State
        {
            private float enterTime;
            private float leaveTime;
            
            private float intervalPeriod = 100.0f; // start with a check! 
            private const float CollisionCheckFrequency = 0.15f;
            
            public RWLinearMotion(RandomWalkStateMachine stateMachine) : base(stateMachine)
            {
            }
            
            public override void Step()
            {
                parentStateMachine.parentController.motionHandler.MoveStraight();
            }

            public override void Enter()
            {
                if (!String.Equals(parentStateMachine.previousState.ToString(), ObstAvoidName))
                {
                    this.enterTime = Time.time;
                    // In original paper code done with:
                    // Ceil(m_pcRNG->Exponential((Real)simulationParameters.LAMBDA) * 4);
                    // where 4: empiric scaling factor and LAMBDA = 10
                    var stayTime = Sampling.SampleFromExponentialDistribution(40f);
                    this.leaveTime = enterTime + stayTime;    
                }
            }

            public override void CheckAndHandleStateTransition()
            {
                var timeUp = Time.time >= leaveTime;
                if (timeUp)
                {
                    parentStateMachine.StateTransition(RotationName);
                    return;
                }

                if (intervalPeriod >= CollisionCheckFrequency)
                {
                    intervalPeriod = 0f;
                    if (parentStateMachine.parentController.CheckObstacleInRange())
                    {
                        parentStateMachine.StateTransition(ObstAvoidName);
                        return;
                    }    
                }
                intervalPeriod += Time.deltaTime;
                
            }

            public override void UpdateStateMachineLinMotionTimer()
            {
                parentStateMachine.linMotionTimer = Mathf.Min(parentStateMachine.linMotionTimerInterval,
                    parentStateMachine.linMotionTimer + Time.deltaTime);
            }

            public override string ToString()
            {
                return LinMotionName;
            }

            public override string GetInfoString()
            {
                return ToString() + Environment.NewLine + $"(enter at {enterTime:F2}, leave at {leaveTime:F2})";
            }
        }

        private class RWRotation : State
        {
            private TurnDirection turnDirection;
            private float enterTime;
            private float leaveTime;
            
            public RWRotation(RandomWalkStateMachine stateMachine) : base(stateMachine)
            {
            }
            public override void Step()
            {
                parentStateMachine.linMotionTimer -= Time.deltaTime;
            }

            public override void Enter()
            {
                this.turnDirection = Sampling.SampleRandomBool() ? TurnDirection.ClockWise : TurnDirection.CounterClockWise;
                this.enterTime = Time.time;
                var stayTime = Sampling.SampleFromUniformRange(0f, 4.5f);
                this.leaveTime = enterTime + stayTime;
            }

            public override void CheckAndHandleStateTransition()
            {
                if (Time.time > this.leaveTime)
                {
                    parentStateMachine.StateTransition(LinMotionName);
                }
            }

            public override void UpdateStateMachineLinMotionTimer()
            {
                parentStateMachine.parentController.motionHandler.TurnInDirection(this.turnDirection);
            }

            public override string ToString()
            {
                return RotationName;
            }

            public override string GetInfoString()
            {
                return ToString() + Environment.NewLine + $"(enter at {enterTime:F2}, leave at {leaveTime:F2)}, turn in {turnDirection})";
            }
        }
        
        private class RWObstacleAvoidance : State
        {
            private Vector3 targetOrientationVector;
            private TurnDirection turnDirection;

            private bool stuckInCorner = false;
            
            public RWObstacleAvoidance(RandomWalkStateMachine stateMachine) : base(stateMachine)
            {
            }

            public override void Step()
            {
                parentStateMachine.parentController.motionHandler.TurnInDirection(this.turnDirection);

                if (parentStateMachine.linMotionTimer < 0f)
                {
                    stuckInCorner = true;
                }
            }

            public override void Enter()
            {
                var sensorReadings = parentStateMachine.parentController.ReadProximitySensors();
                SetTargetOrientationAndDirection(sensorReadings);
            }

            private void SetTargetOrientationAndDirection((ProximitySensor, float)[] sensorReadings)
            {
                var currMinReading = GetMinSensorReading(sensorReadings);
                this.targetOrientationVector = currMinReading.Item1.sensorVector * -1;
                
                
                // Select random offset & apply
                var maxCARandomOffset = 25f;
                var randomOffset = Sampling.SampleFromUniformRange(-maxCARandomOffset, 
                    maxCARandomOffset);

                this.targetOrientationVector =
                    Misc.RotateVectorInArenaSpace(this.targetOrientationVector, randomOffset);
                
                
                this.turnDirection = DetermineShortestTurnDirection(targetOrientationVector);
            }

            private (ProximitySensor, float) GetMinSensorReading((ProximitySensor, float)[] sensorReadings)
            {
                // Get Min Reading
                var currMin = float.MaxValue;
                var currMinReading = sensorReadings[0];

                foreach (var reading in sensorReadings)
                {
                    if (reading.Item2 < currMin)
                    {
                        currMin = reading.Item2;
                        currMinReading = reading;
                    }
                }

                return currMinReading;
            }
            
            private TurnDirection DetermineShortestTurnDirection(Vector3 targetOrientationVector)
            {
                var currOrientation =
                    parentStateMachine.parentController.parentScript.gameObject.transform.forward;

                var signedAngle = Vector3.SignedAngle(currOrientation, targetOrientationVector, Vector3.up);

                return signedAngle > 0 ? TurnDirection.ClockWise : TurnDirection.CounterClockWise;
            }

            public override void CheckAndHandleStateTransition()
            {
                if (stuckInCorner)
                {
                    var readings = parentStateMachine.parentController.ReadProximitySensors();
                    if (readings.Length == 0)
                    {
                        parentStateMachine.StateTransition(LinMotionName);
                        stuckInCorner = false;
                        return;
                    }
                }
                
                if (IsTargetOrientationReached())
                {
                    parentStateMachine.StateTransition(LinMotionName);
                }
            }

            public override void UpdateStateMachineLinMotionTimer()
            {
                parentStateMachine.linMotionTimer -= Time.deltaTime;
            }

            public override string ToString()
            {
                return ObstAvoidName;
            }

            public override string GetInfoString()
            {
                return ToString() + Environment.NewLine + $"(target orientation: {Misc.VectorToArenaOrientation(targetOrientationVector)}, turn direction: {turnDirection})";
            }

            private bool IsTargetOrientationReached()
            {
                const float tolerance = 5f;
                return Mathf.Abs(Vector3.Angle(this.targetOrientationVector,
                    parentStateMachine.parentController.parentScript.transform.forward)) < tolerance;
            }
        }

        public string GetFocusInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Random Walk State Machine Info </b>");
            sb.AppendLine($"Linear Motion timer: {this.linMotionTimer:F2} / {linMotionTimerInterval:F2}");
            sb.AppendLine("Current State: " + currentState.GetInfoString());
            return sb.ToString();
        }
    }
}