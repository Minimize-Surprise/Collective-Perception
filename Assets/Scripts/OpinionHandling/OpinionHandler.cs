using System;
using System.Text;
using UnityEngine;

namespace Assets
{
    [System.Serializable]
    public class OpinionHandler
    {
        [System.NonSerialized]
        internal BotController botControllerParent;
        
        public Opinion currentOpinion;

        private MeshRenderer opinionIndicator;

        [SerializeField]
        private Material blackMaterial;
        [SerializeField]
        private Material whiteMaterial;

        private float intervalPeriod = 0.0f;

        private MessageStorage messageStorage;

        public void Step()
        {
            //BroadCastOpinionEveryXSeconds(3);
        }

        public void BroadCastOpinionEveryXSeconds(float secs)
        {
            if (intervalPeriod > secs)
            {
                BroadcastCurrentOpinion();
                intervalPeriod = 0;
            }
            
            intervalPeriod += Time.deltaTime;
        }

        public OpinionHandler(Opinion initialOpinion, BotController botControllerParent)
        {
            this.currentOpinion = initialOpinion;
            this.botControllerParent = botControllerParent;
        }

        public void Initialize()
        {
            this.opinionIndicator = botControllerParent.parentScript.gameObject.transform.Find("OpinionIndicator").GetComponent<MeshRenderer>();
            ChangeOpinionColor();
            
            this.messageStorage = new MessageStorage(botControllerParent.botParams.maxMsgQueueSize);
        }
        
        public void Reinitialize(Opinion newOpinion)
        {
            messageStorage.Clear();
            SetNewCurrentOpinion(newOpinion, false);
        }

        public void LoadMaterials()
        {
            Debug.Log("(Re)loading opinion materials.");
            this.blackMaterial = Resources.Load("Resources/Material/OpinionBlack.mat", typeof(Material)) as Material;
            this.whiteMaterial = Resources.Load("Resources/Material/OpinionWhite.mat", typeof(Material)) as Material;
        }
        
        public void SetNewCurrentOpinion(Opinion newOpinion, bool updateDistributionAtMaster=true)
        {
            if (updateDistributionAtMaster && newOpinion != this.currentOpinion)
            {
                this.botControllerParent.parentScript.masterParams.UpdateCurrentOpinionDistribution(newOpinion,botControllerParent.homeArena);
            }
            this.currentOpinion = newOpinion;
            ChangeOpinionColor();
        }

        private void ChangeOpinionColor()
        {
            if (blackMaterial == null || whiteMaterial == null)
                LoadMaterials();
            
            if (currentOpinion == Opinion.Black)
            {
                this.opinionIndicator.material = blackMaterial;
            }
            else if (currentOpinion == Opinion.White)
            {
                this.opinionIndicator.material = whiteMaterial;
            }
            else
            {
                Debug.Log($"Unknown current opinion: {currentOpinion}");
            }
        }

        public void ApplyParams(BotController botController, Opinion botParamsInitialOpinion)
        {
            this.botControllerParent = botController;
            this.currentOpinion = botParamsInitialOpinion;
        }
        
        public Opinion QueryCurrentOpinion()
        {
            return this.currentOpinion;
        }

        public void BroadcastCurrentOpinion()
        {
            this.botControllerParent.parentScript.masterParams.communicationModule.SendOpinion(
                this.botControllerParent.parentScript.gameObject, currentOpinion);
        }

        public void HandleIncomingOpinionMessage(OpinionMessage msg)
        {
            /*
            Debug.Log(
                $"I ({botControllerParent.parentScript.gameObject.name}) received a msg from {msg.sender} with opinion: {msg.opinion}",
                botControllerParent.parentScript);
                */
            
            if (botControllerParent.IsListening())
                messageStorage.EnqueueNewMessage(msg);
        }


        public void ApplyDecisionRule()
        {
            var rule = botControllerParent.parentScript.masterParams.decisionRule;
            Opinion newOpinion; 
            switch (rule)
            {
                case DecisionRule.MajorityRule:
                    newOpinion = DecisionMaking.MajorityRuleDecision(messageStorage.GetMessageArray(), currentOpinion);
                    break;
                
                case DecisionRule.VoterModel:
                    newOpinion = DecisionMaking.VoterModelDecision(messageStorage.GetMessageArray(), currentOpinion);
                    break;
                case DecisionRule.MinimizeSurprise:
                    newOpinion = botControllerParent.parentScript.masterParams.AccessNeuralNetworks(botControllerParent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            SetNewCurrentOpinion(newOpinion);
            messageStorage.Clear();
        }

        public string GetFocusInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Opinion Handler Info</b>");
            sb.AppendLine($"Current opinion: {this.currentOpinion}"); 
            
            return sb + messageStorage.GetFocusInfoText();
        }

        public OpinionMessage[] GetCurrentMessages()
        {
            return this.messageStorage.GetMessageArray();
        }
    }
}