using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using MinimalSurprise;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class FitnessPenaltyTests
    {

        [Test]
        public void FitnessPenaltyTest()
        {
            var errorOpinionDistExpectedFitnessTriples = new []
            {
                (0.2f, 1f, 0.8f),
                (0.2f, 0f, 0.4f),
                (0.2f, 0.25f, 0.625f * 0.8f),
                (0.2f, 0.5f, 0.6f),
                (0.2f, 0.75f, 0.875f * 0.8f)
            };

            foreach (var element in errorOpinionDistExpectedFitnessTriples)
            {
                Assert.AreApproximatelyEqual(element.Item3, 
                    1-NeuralNetwork.CalcPenalizedError(element.Item1, element.Item2), 
                    0.0005f);
            }
            
            // MaxPenalty = 4
            var errorOpinionDistExpectedFitnessTriples2 = new []
            {
                (0.2f, 1f, 0.8f),
                (0.2f, 0f, 0.25f * 0.8f),
                (0.2f, 0.25f, (0.25f + 0.25f * 0.75f) * 0.8f),
                (0.2f, 0.5f, (0.25f + 0.5f * 0.75f) * 0.8f),
                (0.2f, 0.75f, (0.25f + 0.75f * 0.75f) * 0.8f)
            };
            
            foreach (var element in errorOpinionDistExpectedFitnessTriples2)
            {
                Assert.AreApproximatelyEqual(element.Item3, 
                    1-NeuralNetwork.CalcPenalizedError(element.Item1, element.Item2, 4), 
                    0.0005f);
            }

        }
        
        /*
        [Test]
        public void PredictionNetAvgFitnessTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(NeuralNetwork.PredictionNetTopology,
                NeuralNetwork.PredictionNetRecurrentLayers, 
                1,
                1,
                true,
                "testnet",
                NeuralNetwork.FitnessCalculation.AverageFitness);

            var input = new[] {0.5f, 0.5f, 0.5f, 0.5f};
           
            var a0_out1 = rnn.PropagateInput(input, 0, 0);
            var a0_out2 = rnn.PropagateInput(input, 0, 0);
            var a0_out3 = rnn.PropagateInput(input, 0, 0);


            var nOutputNodes = a0_out1.Length;
            
            Debug.Log($"Input: {input.ArrayToString()}");
            Debug.Log($"Output 1: {a0_out1.ArrayToString()}");
            Debug.Log($"Output 2: {a0_out2.ArrayToString()}");
            Debug.Log($"Output 3: {a0_out3.ArrayToString()}");

            rnn.ResetForNextRound(new[] {1f});
            var error = rnn.GetNormalizedError(true);
            Debug.Log($"======\nNormalized error: {error}");
            
            var manError1 = ManuallyCalcError(a0_out1);
            var manError2 = ManuallyCalcError(a0_out2);

            var manOverallError = (manError1 + manError2) / nOutputNodes / 2;

            Assert.AreApproximatelyEqual(manOverallError, error, .0005f);
        }
        */
        
        /*        
        [Test]
        public void PredictionNetMinFitnessTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(NeuralNetwork.PredictionNetTopology,
                NeuralNetwork.PredictionNetRecurrentLayers, 
                1,
                1,
                true,
                "testnet",
                NeuralNetwork.FitnessCalculation.MinFitness);

            var input = new[] {0.5f, 0.5f, 0.5f, 0.5f};

            var nRounds = 3;
            var nPredictions = 4;
            var outputStore = new float[nRounds][][];

            for (int i = 0; i < nRounds; i++)
            {
                for (int j = 0; j < nPredictions; j++)
                {
                    if (outputStore[i] == null)
                    {
                        outputStore[i] = new float[nPredictions][];
                    }

                    outputStore[i][j] = rnn.PropagateInput(input, 0, 0);;
                }
                
                rnn.ResetForNextRound(new [] {1f});
                rnn.MutateWeights(0.99f, -0.4f, 0.4f);
            }

            var manualErrors = new float[nRounds][];
            for (int i = 0; i < nRounds; i++)
            {
                manualErrors[i] = new float[nPredictions-1];
                for (int j = 0; j < nPredictions-1; j++)
                {
                    manualErrors[i][j] = ManuallyCalcError(outputStore[i][j]);
                }
            }

            var nOutputNodes = outputStore[0][0].Length;
            var expected = manualErrors.Select(x => x.Sum() / (nPredictions - 1)).Max() / nOutputNodes;
            var actual = rnn.GetNormalizedError(true);
            
            Debug.Log($"Expected: {expected} vs. actual: {actual}");
            
            Assert.AreApproximatelyEqual(expected, actual, .0005f);
        }
        */


        private float ManuallyCalcError(float[] output)
        {
            var error = 0f;
            for (int i = 0; i < output.Length; i++)
            {
                error += Mathf.Abs(0.5f - output[i]);
            }

            return error;
        }
    }
    
}