using System;
using System.Collections.Generic;
using System.Text;
using Assets;
using UnityEngine;

namespace MotionStateMachine
{
    public class DecisionStateMachine
    {

        private Dictionary<string, DecisionState> states;

        protected BotController parentController;

        private DecisionState currentState;
        private DecisionState previousState;

        private float currentQualityEstimate;

        public bool currentlyListening = false;

        public DecisionStateMachine(BotController parentController)
        {
            this.parentController = parentController;
            CreateStates();
            StateTransition(InitName);
        }

        public void Reinitialize()
        {
            CreateStates();
            currentState = null;
            StateTransition(InitName);
        }

        

        private const string InitName = "INIT";
        private const string DisseminationName = "DISSEMINATION";
        private const string ExplorationName = "EXPLORATION";

        private void CreateStates()
        {
            states = new Dictionary<string, DecisionState>
            {
                {InitName, new DecisionInit(this)},
                {DisseminationName, new DecisionDissemination(this)},
                {ExplorationName, new DecisionExploration(this)}
            };
        }

        public void Step()
        {
            currentState.Step();
            currentState.CheckAndHandleStateTransition();
        }

        public void StateTransition(string newStateName)
        {
            if (currentState != null)
            {
                currentState.Leave();
                previousState = currentState;
            }

            currentState = states[newStateName];
            currentState.Enter();
        }

        public string GetFocusInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Decision Making State Machine Info</b>");
            sb.AppendLine("Current State: " + currentState.GetInfoString());
            return sb.ToString();
        }

        public void SetCurrentQualityEstimate(float currentEstimate)
        {
            this.currentQualityEstimate = currentEstimate;
        }

        public float GetCurrentQualityEstimate()
        {
            return this.currentQualityEstimate;
        }

        private abstract class DecisionState
        {
            protected DecisionStateMachine parentStateMachine;

            public DecisionState(DecisionStateMachine parentStateMachine)
            {
                this.parentStateMachine = parentStateMachine;
            }

            public abstract void Step();
            public abstract void Enter();
            
            public virtual void Leave()
            {
            }
            
            public abstract void CheckAndHandleStateTransition();
            public abstract override string ToString();
            public abstract string GetInfoString();
        }

        private class DecisionInit : DecisionState
        {
            public DecisionInit(DecisionStateMachine parentStateMachine) : base(parentStateMachine)
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
                parentStateMachine.StateTransition(ExplorationName);
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

        private class DecisionDissemination : DecisionState
        {
            private const float ListenInterval = 3f;

            private float enterTime;
            private float leaveTime;

            private const float BroadcastIntervalSecs = 1f;
            

            public DecisionDissemination(DecisionStateMachine parentStateMachine) : base(parentStateMachine)
            {
            }

            public override void Step()
            {
                if (Time.time > leaveTime - ListenInterval)
                {
                    parentStateMachine.currentlyListening = true;
                }
                
                parentStateMachine.parentController.opinionHandler.BroadCastOpinionEveryXSeconds(BroadcastIntervalSecs);
            }

            public override void Enter()
            {
                var g = parentStateMachine.parentController.botParams.g;
                var stayTime = Sampling.SampleFromExponentialDistribution(g * parentStateMachine.GetCurrentQualityEstimate()) + ListenInterval;
                
                enterTime = Time.time;
                leaveTime = enterTime + stayTime;

                parentStateMachine.currentlyListening = false;
            }

            public override void Leave()
            {
                parentStateMachine.parentController.opinionHandler.ApplyDecisionRule();
            }

            public override void CheckAndHandleStateTransition()
            {
                if (Time.time > leaveTime)
                {
                    parentStateMachine.StateTransition(ExplorationName);
                }
            }

            public override string ToString()
            {
                return DisseminationName;
            }

            public override string GetInfoString()
            {
                return ToString() + Environment.NewLine + $"(enter at {enterTime:F2}, leave at {leaveTime:F2)}, " +
                       $"currently listening? {parentStateMachine.currentlyListening})";
            }
        }


        private class DecisionExploration : DecisionState
        {
            private float enterTime;
            private float leaveTime;

            private float qualityTimerOverall = 0f;
            private float qualityTimerOpinionObserved = 0f;
            
            private float intervalPeriod = 0.0f;
            private const float GroundMeasureInterval = 0.2f;

            public DecisionExploration(DecisionStateMachine parentStateMachine) : base(parentStateMachine)
            {
            }

            public override void Step()
            {
                QualityEstimationEveryXSecs(GroundMeasureInterval);
            }

            private void QualityEstimationEveryXSecs(float secs)
            {
                if (intervalPeriod > secs)
                {
                    QualityEstimationStep();
                    intervalPeriod = 0;
                }

                intervalPeriod += Time.deltaTime;
            }

            private void QualityEstimationStep()
            {   
                var currentGroundOpinion = parentStateMachine.parentController.GetCurrentGroundOpinion();
                var myCurrentOpinion = parentStateMachine.parentController.opinionHandler.currentOpinion;

                this.qualityTimerOverall += Time.deltaTime;

                if (currentGroundOpinion == myCurrentOpinion)
                {
                    qualityTimerOpinionObserved += Time.deltaTime;
                }
                
                parentStateMachine.SetCurrentQualityEstimate(qualityTimerOpinionObserved / qualityTimerOverall);
            }

            public override void Enter()
            {
                ResetQualityEstimate();

                enterTime = Time.time;
                var stayTime = Sampling.SampleFromExponentialDistribution(parentStateMachine.parentController.botParams.sigma);

                leaveTime = enterTime + stayTime;
            }

            private void ResetQualityEstimate()
            {
                this.qualityTimerOverall = 0f;
                this.qualityTimerOpinionObserved = 0f;
                parentStateMachine.SetCurrentQualityEstimate(0f);
            }

            public override void CheckAndHandleStateTransition()
            {
                if (Time.time > this.leaveTime)
                {
                    parentStateMachine.StateTransition(DisseminationName);
                }
            }

            public override string ToString()
            {
                return ExplorationName;
            }

            public override string GetInfoString()
            {
                return ToString() + Environment.NewLine + $"(enter at {enterTime:F2}, leave at {leaveTime:F2)}, " +
                       $"current rho: {qualityTimerOpinionObserved / qualityTimerOverall:F2})";
            }
        }
    }
}