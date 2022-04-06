using System;
using System.Linq;
using Assets;
using UnityEngine;

namespace MinimalSurprise
{
    public static class NeuralNetAdapter
    {
        public static float[] AccessDecisionNetwork(OpinionMessage[] receivedMessages, int nMaxMessages,  
            float groundBrightness, Opinion currentOpinion,
            NeuralNetwork decisionNet, int botID, int arenaIndex)
        {
            var inputArr = CreateDecisionNetInputArr(receivedMessages, nMaxMessages, groundBrightness, currentOpinion);
            var output = decisionNet.PropagateInput(inputArr, botID, arenaIndex);
            
            return output;
        }

        public static float[] CreateDecisionNetInputArr(OpinionMessage[] receivedMessages, int nMaxMessages,
            float groundBrightness, Opinion currentOpinion)
        {
            float[] inputArr =
            {
                CalcNormalizedMessageCount(receivedMessages, nMaxMessages),
                CalcWhitePercentage(receivedMessages),
                groundBrightness,
                EncodeOpinion(currentOpinion)
            };
            return inputArr;
        }

        private static float EncodeOpinion(Opinion currentOpinion)
        {
            return currentOpinion == Opinion.White ? 1f : 0f;
        }

        public static float[] AccessPredictionNetwork(OpinionMessage[] receivedMessages, int nMaxMessages,  
            float groundBrightness, Opinion newOpinion,
            NeuralNetwork predictionNet, int botID, int arenaIndex)
        {
            var inputArr = CreatePredictionNetInputArr(receivedMessages, nMaxMessages, groundBrightness, newOpinion);

            var output = predictionNet.PropagateInput(inputArr, botID, arenaIndex);

            return output;
        }

        public static float[] CreatePredictionNetInputArr(OpinionMessage[] receivedMessages, int nMaxMessages,
            float groundBrightness, Opinion newOpinion)
        {
            float[] inputArr =
            {
                CalcNormalizedMessageCount(receivedMessages, nMaxMessages),
                CalcWhitePercentage(receivedMessages),
                groundBrightness,
                EncodeOpinion(newOpinion)
            };
            return inputArr;
        }

        public static Opinion DecodeOpinion(float outputVal)
        {
            if (outputVal < 0 || outputVal > 1)
                throw new Exception($"Decoding NN output that is not within bounds [0,1]: {outputVal}");
            var intVal = Mathf.RoundToInt(outputVal);
            return intVal == 1 ? Opinion.White : Opinion.Black;
        }
        
        private static float CalcNormalizedMessageCount(OpinionMessage[] receivedMessages, int nMaxMessages)
        {
            return (float) receivedMessages.Length / nMaxMessages;
        }

        private static float CalcWhitePercentage(OpinionMessage[] receivedMessages)
        {
            if (receivedMessages.Length == 0) return 0;
            
            return (float) receivedMessages.Count(x => x.opinion == Opinion.White) / receivedMessages.Length;
        }
    }
}