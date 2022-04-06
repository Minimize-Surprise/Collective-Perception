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
    public class NeuralNetTests
    {
        [Test]
        public void SimpleFFNTest()
        {
            NeuralNetwork ffn = new NeuralNetwork(new[] {2, 1, 2}, 1, 1, "testnet", 
                NeuralNetwork.FitnessCalculation.AverageFitness, false);
            
            var inputs = new[] {1f, -1f};
            
            // weights = mat[layer][fromNeuron][toNeuron]
            var weights = new []
            {
                new [] // layer 1-2
                {
                    new[] {0.1f, -0.3f},
                }, 
                new [] // layer 2-3
                {
                    new[] {0.5f},
                    new[] {0.8f}
                }  
            };

            // 1 per neuron (=> same shape as neurons)
            var biases = new[]
            {
                new[] {0f, 0f},
                new[] {0f},
                new[] {0f, 0f, 0f}
            };
            
            ffn.SetWeights(weights);
            ffn.SetBiases(biases);
            
            Debug.Log(ffn.GetDescriptionString());
            Debug.Log("Output: "+
                      ffn.PropagateInput(inputs,0, 0).ArrayToString()
            );
        }

        [Test]
        public void MathFFNTest()
        {
            var input1 = 1f;
            var input2 = -1f;

            var weight00 = 0.1f;
            var weight01 = -0.3f;

            var hidden = NeuralNetwork.ActivateTanh(
                input1 * weight00 + input2 * weight01
            );

            var weight10 = 0.5f;
            var weight11 = 0.8f;

            var output1 = NeuralNetwork.ActivateTanh(hidden * weight10);
            var output2 = NeuralNetwork.ActivateTanh(hidden * weight11);

            Debug.Log($"Calculated values: hidden = {hidden}, out1 = {output1}, out2 = {output2}");
        }

        [Test]
        public void RNNTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(new[] {2, 1, 2}, new []{false, true, false}, 
                1, 1,false, "testnet", 
                NeuralNetwork.FitnessCalculation.AverageFitness, false);
            
            var inputs = new[] {1f, -1f};
            
            // weights = mat[layer][fromNeuron][toNeuron]
            var weights = new []
            {
                new [] // layer 1-2
                {
                    new[] {0.1f, -0.3f},
                }, 
                new [] // layer 2-3
                {
                    new[] {0.5f},
                    new[] {0.8f}
                }
            };

            // 1 per neuron (non-recurrent layers stay empty)
            var recurrentWeights = new[]
            {
                new float[0],
                new[] {0.8f},
                new float[0]
            };
            
            // 1 per neuron (=> same shape as neurons)
            var biases = new[]
            {
                new[] {0f, 0f},
                new[] {0f},
                new[] {0f, 0f, 0f}
            };
            
            rnn.SetWeights(weights);
            rnn.SetRecurrentWeights(recurrentWeights);
            rnn.SetBiases(biases);
            
            Debug.Log(rnn.GetDescriptionString());
            Debug.Log("Output at t=1: "+
                      rnn.PropagateInput(inputs, 0, 0).ArrayToString()
            );
            
            Debug.Log("Recurrent storage after t=1: " +
                      rnn.GetRecurrentStorageString());
            
            Debug.Log("Output at t=2: "+
                      rnn.PropagateInput(inputs, 0, 0).ArrayToString()
            );
        }

        [Test]
        public void RNNMultipleBotsTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(new[] {2, 1, 2},
                new []{false, true, false}, 
                3,
                2,
                false,
                "testnet",
                NeuralNetwork.FitnessCalculation.AverageFitness,
                false);
            
            var inputBot1 = new[] {1f, -1f};
            var inputBot2 = new[] {-0.5f, -1f};

            Debug.Log("Expected behavior: Bot1 and Bot3 should have identical results, but different from Bot2");
            
            Debug.Log("### t=1");
            Debug.Log("Output bot 1: "+rnn.PropagateInput(inputBot1, 0, 0).ArrayToString());
            Debug.Log("Output bot 2: "+rnn.PropagateInput(inputBot2, 1, 0).ArrayToString());
            Debug.Log("Output bot 3: "+rnn.PropagateInput(inputBot1, 2, 0).ArrayToString());
            
            Debug.Log("### t=2");
            Debug.Log("Output bot 1: "+rnn.PropagateInput(inputBot1, 0, 0).ArrayToString());
            Debug.Log("Output bot 2: "+rnn.PropagateInput(inputBot2, 1, 0).ArrayToString());
            Debug.Log("Output bot 3: "+rnn.PropagateInput(inputBot1, 2, 0).ArrayToString());
        }

        [Test]
        public void PredictionNetAvgFitnessTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(NeuralNetwork.PredictionNetTopology,
                NeuralNetwork.PredictionNetRecurrentLayers, 
                1,
                1,
                true,
                "testnet",
                NeuralNetwork.FitnessCalculation.AverageFitness,
                false);

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
        
        [Test]
        public void PredictionNetMinFitnessTest()
        {
            NeuralNetwork rnn = new NeuralNetwork(NeuralNetwork.PredictionNetTopology,
                NeuralNetwork.PredictionNetRecurrentLayers, 
                1,
                1,
                true,
                "testnet",
                NeuralNetwork.FitnessCalculation.MinFitness,
                false);

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


        private float ManuallyCalcError(float[] output)
        {
            var error = 0f;
            for (int i = 0; i < output.Length; i++)
            {
                error += Mathf.Abs(0.5f - output[i]);
            }

            return error;
        }


        [Test]
        public void MutationLogicSingleValTest()
        {
            var weights = new float[1000].Populate(0f);
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = NeuralNetwork.MutationLogicForSingleValue(weights[i], 0.05f, -0.5f, 0.5f);       
            }
            
            var zeroCount = weights.Count(x => x == 0f);
            Debug.Log($"{zeroCount} of {weights.Length} ({(float) zeroCount / weights.Length * 100:F2} %) are still 0.0f.");
        }

        [Test]
        public void NetworkCopyTest()
        {
            int nParallelArenas = 2;
            NeuralNetwork ffn = new NeuralNetwork(new[] {3, 4, 2}, 1, nParallelArenas, "testnet", NeuralNetwork.FitnessCalculation.AverageFitness, false);
            
            NeuralNetwork copy = ffn.CreateCopy(nParallelArenas);

            for (int i = 0; i < ffn.unitsPerLayer.Length; i++)
            {
                Assert.AreEqual(ffn.unitsPerLayer[i], copy.unitsPerLayer[i]);
            }

            for (int i = 0; i < ffn.isLayerRecurrent.Length; i++)
            {
                Assert.AreEqual(ffn.isLayerRecurrent[i], copy.isLayerRecurrent[i]);
            }
            
            for (int i = 0; i < ffn.biases.Length; i++)
            {
                for (int j = 0; j < ffn.biases[j].Length; j++)
                {
                    Assert.AreEqual(ffn.biases[i][j], copy.biases[i][j]);   
                }
            }

            for (int i = 0; i < ffn.weights.Length; i++)
            {
                for (int j = 0; j < ffn.weights[i].Length; j++)
                {
                    for (int k = 0; k < ffn.weights[i][j].Length; k++)
                    {
                        Assert.AreEqual(ffn.weights[i][j][k], copy.weights[i][j][k]);    
                    }
                }
            }
        }


        [Test]
        public void ManualReferenceTest()
        {
            int nParallelArenas = 2;
            var floatTolerance = 0.0001f;

            NeuralNetwork ffn = new NeuralNetwork(new[] {20, 30, 10}, 1, nParallelArenas, "testnet",
                NeuralNetwork.FitnessCalculation.AverageFitness, false);

            NeuralNetwork copy = ffn.CreateCopy(nParallelArenas);

            var x = copy.weights[0][0][0];
            Debug.Log($"ffn.weights[0][0][0] = {ffn.weights[0][0][0]}, copy.weights[0][0][0] = {copy.weights[0][0][0]}");
            ffn.weights[0][0][0] = 10f;
            Debug.Log("Setting to 10...");
            Debug.Log($"ffn.weights[0][0][0] = {ffn.weights[0][0][0]}, copy.weights[0][0][0] = {copy.weights[0][0][0]}");
            Assert.AreApproximatelyEqual(10f, ffn.weights[0][0][0], floatTolerance);
            Assert.AreApproximatelyEqual(x, copy.weights[0][0][0], floatTolerance);
        }

        [Test]
        public void CompleteNetworkMutationTest() {
            int nParallelArenas = 2;
            var floatTolerance = 0.001f;
            
            NeuralNetwork ffn = new NeuralNetwork(new[] {20, 30, 10}, 
                1,
                nParallelArenas, 
                "testnet", 
                NeuralNetwork.FitnessCalculation.AverageFitness,
                false);

            NeuralNetwork copy = ffn.CreateCopy(nParallelArenas);
            
            ffn.MutationWrapper(0.1f, -0.4f, 0.4f);
            
            for (int i = 0; i < ffn.unitsPerLayer.Length; i++)
            {
                Assert.AreEqual(ffn.unitsPerLayer[i], copy.unitsPerLayer[i]);
            }

            for (int i = 0; i < ffn.isLayerRecurrent.Length; i++)
            {
                Assert.AreEqual(ffn.isLayerRecurrent[i], copy.isLayerRecurrent[i]);
            }

            var allBiasCounter = 0;
            var mutatedBiasCounter = 0;
            var mutationDiffList = new List<float>();
            
            for (int i = 0; i < ffn.biases.Length; i++)
            {
                for (int j = 0; j < ffn.biases[i].Length; j++)
                {
                    allBiasCounter++;
                    if (Math.Abs(ffn.biases[i][j] - copy.biases[i][j]) > floatTolerance)
                    {
                        mutatedBiasCounter++;
                        mutationDiffList.Add(ffn.biases[i][j] - copy.biases[i][j]);
                    }
                }
            }

            var allWeightsCounter = 0;
            var mutatedWeightsCounter = 0;
            var mutatedWeightsDiffList = new List<float>();

            for (int i = 0; i < ffn.weights.Length; i++)
            {
                for (int j = 0; j < ffn.weights[i].Length; j++)
                {
                    for (int k = 0; k < ffn.weights[i][j].Length; k++)
                    {
                        allWeightsCounter++;
                        if (Math.Abs(ffn.weights[i][j][k] - copy.weights[i][j][k]) > floatTolerance)
                        {
                            mutatedWeightsCounter++;
                            mutatedWeightsDiffList.Add(ffn.weights[i][j][k] - copy.weights[i][j][k]);
                        }
                    }
                }
            }

            var totalWeights = allBiasCounter + allWeightsCounter;
            var totalMutated = mutatedBiasCounter + mutatedWeightsCounter;
            var diffArr = mutationDiffList.Concat(mutatedWeightsDiffList).ToArray();
            
            Debug.Log($"{totalMutated} of {totalWeights} weights & biases (= {(float) totalMutated / totalWeights * 100:F2} %) mutated.");
            if (diffArr.Length > 0)
                Debug.Log($"Mutation difference range: min={diffArr.Min()}, max={diffArr.Max()}, mean={diffArr.Average()}");
            
        }
    }
}