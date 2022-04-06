using System.Linq;
using Random = UnityEngine.Random;

namespace Assets
{
    public class DecisionMaking
    {
        public static Opinion MajorityRuleDecision(OpinionMessage[] relevantOpinionMessages, Opinion ownOpinion)
        {
            var blackCounter = relevantOpinionMessages.Count(msg => msg.opinion == Opinion.Black);
            var whiteCounter = relevantOpinionMessages.Length - blackCounter;

            if (ownOpinion == Opinion.Black)
                blackCounter++;
            else
                whiteCounter++;

            if (blackCounter > whiteCounter)
                return Opinion.Black;
            else if (whiteCounter > blackCounter)
                return Opinion.White;
            else
                return ownOpinion;
        }

        public static Opinion VoterModelDecision(OpinionMessage[] relevantOpinionMessages, Opinion ownOpinion)
        {
            if (relevantOpinionMessages.Length == 0)
                return ownOpinion;
            
            var randomIndex = Random.Range(0, relevantOpinionMessages.Length);
            return relevantOpinionMessages[randomIndex].opinion;
        }
    }
    
    public enum DecisionRule
    {
        MajorityRule,
        VoterModel,
        MinimizeSurprise
    }
}