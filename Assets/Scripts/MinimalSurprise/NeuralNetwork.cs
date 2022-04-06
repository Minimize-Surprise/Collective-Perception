using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Assets;
using Assets.Logging;
using UnityEngine;

using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace MinimalSurprise
{
    public class NeuralNetwork
    {
        public string id;
        
        public static readonly int[] ActionNetTopology = {4, 3, 1};
        public static readonly int[] PredictionNetTopology = {4, 4, 3};
        public static readonly bool[] PredictionNetRecurrentLayers = {false, true, false};
        
        public readonly int[] unitsPerLayer;
        public readonly bool[] isLayerRecurrent;
        public float[][] neurons;
        public float[][] biases;
        public float[][][] weights;
        public float[][] recurrentWeights;

        public float[][][][] recurrentStorage;
        public bool isFFN = false;
        public readonly int nBots;

        private float[][][] predictionStorage;
        private float[] cumError;
        private int[] predictionCounter;

        private List<(int, float, float)> cumErrorCurrentGenStorage;
        private int copyCounter = 0;

        public bool calculateFitness;

        private int nParallelArenas;
        private FitnessCalculation fitnessCalc;
        private bool penalizeFitness;
        

        // self-assembly: 0
        // tidy-your-room: random but identical distribution for all bots
        private const float RecurrentStorageInitValue = 0f;

        public NeuralNetwork(int[] unitsPerLayer, bool[] isLayerRecurrent, 
            int nBots, int nParallelArenas, bool calculateFitness, string id,
            FitnessCalculation fitnessCalc, bool penalizeFitness)
        {
            if (unitsPerLayer.Length != isLayerRecurrent.Length)
            {
                throw new Exception(
                    $"NeuralNet must be initialized with same length argument arrays (unitsPerLayer and isLayerRecurrent)" +
                    $"- they are {unitsPerLayer.Length} vs. {isLayerRecurrent.Length}");
            }
            
            this.unitsPerLayer = unitsPerLayer;
            this.isLayerRecurrent = isLayerRecurrent;
            this.nBots = nBots;
            this.nParallelArenas = nParallelArenas;
            
            if (isLayerRecurrent.All(x=>!x)) isFFN = true;
            this.calculateFitness = calculateFitness;
            this.id = id;
            this.fitnessCalc = fitnessCalc;
            this.penalizeFitness = penalizeFitness;
            Setup();
        }
        
        /// <summary>
        /// Standard constructor for a feedforward network. Sets all recurrent parameters to 0.
        /// </summary>
        /// <param name="unitsPerLayer">Example:  public int[] layers = new int[3] { 5, 3, 2 };</param>
        /// <param name="nBots">number of bots (needed for prediction storage)</param>
        public NeuralNetwork(int[] unitsPerLayer, int nBots, int nParallelArenas, string id, FitnessCalculation fitnessCalc,
            bool penalizeFitness) : 
            this(unitsPerLayer, unitsPerLayer.Select(x => false).ToArray(), 
                nBots, nParallelArenas, false, id, fitnessCalc, penalizeFitness)
        {
        }

        public NeuralNetwork(NeuralNetJSONModel jsonModel, int nParallelArenas, FitnessCalculation fitnessCalc, 
            bool penalizeFitness) 
            : this(jsonModel.unitsPerLayer, jsonModel.isLayerRecurrent, 
                jsonModel.nBots, nParallelArenas, jsonModel.calculateFitness, jsonModel.id,
                fitnessCalc, penalizeFitness)
        {
            this.AcceptJsonModel(jsonModel);
        }

        public NeuralNetwork(NeuralNetwork network, int nParallelArenas, string id, FitnessCalculation fitnessCalc,
            bool penalizeFitness) : 
            this(network.unitsPerLayer, network.isLayerRecurrent,
            network.nBots, nParallelArenas, network.calculateFitness, id, fitnessCalc, penalizeFitness)
        {
            this.biases = network.biases.CopyArray2D();
            this.weights = network.weights.CopyArray3D();
            if (this.isFFN)
                this.recurrentWeights = network.recurrentWeights.CopyArray2D();
        }

        public NeuralNetwork CreateCopy(int nParallelArenasLocal)
        {
            var copyNet = new NeuralNetwork(this, nParallelArenasLocal, 
                id+"."+copyCounter, fitnessCalc, penalizeFitness);
            copyCounter++;
            return copyNet;
        }
        
        private void Setup()
        {
            InitNeurons();
            InitBiases();
            InitWeights();
            InitRecurrentWeights();
            InitRecurrentStorage();
            InitFitnessFields();
            InitPredictionStorage();
            cumErrorCurrentGenStorage = new List<(int, float, float)>();
        }

        public void ResetForNextRound(float[] currRoundOpinionOutcomes)
        {
            InitRecurrentStorage();
            InitPredictionStorage();
            SaveFitnessInStorage(currRoundOpinionOutcomes);
            InitFitnessFields();
        }

        private void SaveFitnessInStorage(float[] currRoundOpinionOutcomes)
        {
            for (int i = 0; i < nParallelArenas; i++)
            {
                if (predictionCounter[i] == 0) continue;
                cumErrorCurrentGenStorage.Add((predictionCounter[i], cumError[i], currRoundOpinionOutcomes[i]));
            }
        }

        public void ResetForNextGen()
        {
            // TODO: empty list okay here?
            ResetForNextRound(new float[0]);
            this.cumErrorCurrentGenStorage = new List<(int, float, float)>();
        }

        public void InitPredictionStorage()
        {
            this.predictionStorage = new float[nParallelArenas][][];
            for (int i = 0; i < nParallelArenas; i++)
            {
                predictionStorage[i] = new float[nBots][];
            }
        }
        
        private void InitFitnessFields()
        {
            this.cumError = (new float[nParallelArenas]).Populate(0f);
            this.predictionCounter = (new int[nParallelArenas]).Populate(0);
        }
        
        private void InitRecurrentStorage()
        {
            var ret = new List<float[][][]>();
            for (int iArena = 0; iArena < nParallelArenas; iArena++)
            {
                var perArenaList = new List<float[][]>();
                for (int iBot = 0; iBot < nBots; iBot++)
                {
                    var perBotArray = new List<float[]>();
                    
                    for (int iLayer = 0; iLayer < unitsPerLayer.Length; iLayer++)
                    {
                        if (isLayerRecurrent[iLayer])
                        {
                            perBotArray.Add(new float[unitsPerLayer[iLayer]].Populate(RecurrentStorageInitValue));
                        }
                        else
                        {
                            perBotArray.Add(new float[0]);
                        }
                    }
                    perArenaList.Add(perBotArray.ToArray());
                }
                ret.Add(perArenaList.ToArray());
            }
            
            // Outcome -> 4D-matrix: mat[arenas][bots][layers][neurons]
            this.recurrentStorage = ret.ToArray();
        }
        
        private float GetRecurrentStorageValue(int arenaIndex, int botID, int layerNo, int neuronNo)
        {
            return this.recurrentStorage[arenaIndex][botID][layerNo][neuronNo];
        }

        private void InitNeurons()
        {
            List<float[]> neuronsList = new List<float[]>();
            for (int i = 0; i < unitsPerLayer.Length; i++)
            {
                neuronsList.Add(new float[unitsPerLayer[i]]);
            }

            neurons = neuronsList.ToArray();
        }

        private void InitBiases()
        {
            List<float[]> biasList = new List<float[]>();
            for (int i = 0; i < unitsPerLayer.Length; i++)
            {
                float[] bias = new float[unitsPerLayer[i]];
                for (int j = 0; j < unitsPerLayer[i]; j++)
                {
                    bias[j] = UnityEngine.Random.Range(-0.5f, 0.5f);
                }

                biasList.Add(bias);
            }

            biases = biasList.ToArray();
        }

        private void InitWeights()
        {
            List<float[][]> weightsList = new List<float[][]>();
            for (int i = 1; i < unitsPerLayer.Length; i++)
            {
                List<float[]> layerWeightsList = new List<float[]>();
                int neuronsInPreviousLayer = unitsPerLayer[i - 1];
                for (int j = 0; j < neurons[i].Length; j++)
                {
                    float[] neuronWeights = new float[neuronsInPreviousLayer];
                    for (int k = 0; k < neuronsInPreviousLayer; k++)
                    {
                        neuronWeights[k] = UnityEngine.Random.Range(-0.5f, 0.5f);
                    }

                    layerWeightsList.Add(neuronWeights);
                }

                weightsList.Add(layerWeightsList.ToArray());
            }

            weights = weightsList.ToArray();
        }
        
        private void InitRecurrentWeights()
        {
            var recWeights = new List<float[]>();
            for (int i = 0; i < unitsPerLayer.Length; i++)
            {
                if (isLayerRecurrent[i] && i == 0)
                {
                    throw new Exception("1st Layer cannot be recurrent.");
                }
                else if (isLayerRecurrent[i])
                {
                    float[] currWeights = new float[unitsPerLayer[i]];
                    currWeights = currWeights.Select(x => Sampling.SampleFromUniformRange(-0.5f, 0.5f)).ToArray();
                    recWeights.Add(currWeights);
                }
                else
                {
                    float[] currWeights = new float[0];
                    recWeights.Add(currWeights);
                }
            }

            this.recurrentWeights = recWeights.ToArray();
        }

        public float[] PropagateInput(float[] inputs, int botID, int arenaIndex)
        {
            if (inputs.Length != neurons[0].Length)
            {
                throw new FormatException("ANN inputs and first layer shapes are not equal: " +
                                          $"{inputs.Length} and {neurons[0].Length}");
            }

            // Handle input layer (index = 0)
            for (int i = 0; i < inputs.Length; i++)
            {
                neurons[0][i] = inputs[i];
            }

            // Handle remaining layers (indices > 0)
            for (int i = 1; i < unitsPerLayer.Length; i++)
            {
                for (int j = 0; j < neurons[i].Length; j++)
                {
                    float value = 0f;
                    // Connections from previous layer (i-1)
                    for (int k = 0; k < neurons[i - 1].Length; k++)
                    {
                        value += weights[i - 1][j][k] * neurons[i - 1][k];
                    }
                    // Recurrent connectons (if present)
                    if (isLayerRecurrent[i])
                    {
                        value += recurrentWeights[i][j] * GetRecurrentStorageValue(arenaIndex, botID, i, j);
                    }
                    
                    neurons[i][j] = ActivateSigmoid(value + biases[i][j]);
                    if (isLayerRecurrent[i]) SaveInRecurrentStorage(neurons[i][j], arenaIndex, botID, i, j);
                }
            }
            
            var output =neurons[neurons.Length - 1];
            
            if (calculateFitness)
                FitnessLogic(inputs, output, botID, arenaIndex); // this step's input should have been the last step's output

            if (output.Count(x => float.IsNaN((x))) > 0) Debug.Log("At least 1 NaN in NN output: " + output.ArrayToString());
            
            return output.Select(x => x).ToArray();
        }

        private void FitnessLogic(float[] input, float[] output, int botID, int arenaIndex)
        {
            if (predictionStorage[arenaIndex][botID] != null) // exclude in first round
            {
                // Previous Output (predictionStorage) limits how many elements to sum up
                // -> Leave out the final output node (the new opinion)
                this.cumError[arenaIndex] += predictionStorage[arenaIndex][botID].ArrayAbsDifference(input);
                predictionCounter[arenaIndex]++;
            }

            predictionStorage[arenaIndex][botID] = output.Select(x => x).ToArray();
        }

        private void SaveInRecurrentStorage(float value, int arenaIndex, int botID, int layerNo, int neuronNo)
        {
            this.recurrentStorage[arenaIndex][botID][layerNo][neuronNo] = value;
        }

        public static float ActivateTanh(float value)
        {
            return (float) Math.Tanh(value);
        }

        public static float ActivateSigmoid(float val)
        {
            return  1 / (1 + Mathf.Exp(-val));
        }

        public void MutationWrapper(float mutationRate, float mutationLowerBound, float mutationUpperBound)
        {
            MutateWeights(mutationRate, mutationLowerBound, mutationUpperBound);
            MutateBiases(mutationRate, mutationLowerBound, mutationUpperBound);
            if (!isFFN) MutateRecurrentWeights(mutationRate, mutationLowerBound, mutationUpperBound);
        }

        private void MutateBiases(float mutationRate, float mutationLowerBound, float mutationUpperBound)
        {
            var biases = this.biases;
            for (int iLayer = 0; iLayer < biases.Length; iLayer++)
            {
                for (int jNeuron = 0; jNeuron < biases[iLayer].Length; jNeuron++)
                {
                    biases[iLayer][jNeuron] = MutationLogicForSingleValue(biases[iLayer][jNeuron], 
                        mutationRate, mutationLowerBound, mutationUpperBound);
                }
            }
        }

        public void MutateWeights(float mutationRate, float mutationLowerBound, float mutationUpperBound)
        {
            for (var iLayer = 0; iLayer < weights.Length; iLayer++)
            for (var iFrom = 0; iFrom < weights[iLayer].Length; iFrom++)
            for (var iTo = 0; iTo < weights[iLayer][iFrom].Length; iTo++)
            {
                weights[iLayer][iFrom][iTo] = MutationLogicForSingleValue(weights[iLayer][iFrom][iTo],
                    mutationRate, mutationLowerBound, mutationUpperBound);
            }

            MutateRecurrentWeights(mutationRate, mutationLowerBound, mutationUpperBound);
        }

        private void MutateRecurrentWeights(float mutationRate, float mutationLowerBound, float mutationUpperBound)
        {
            foreach (var weightArray in recurrentWeights)
            {
                if (weightArray.Length > 0)
                {
                    for (int iWeight = 0; iWeight < weightArray.Length; iWeight++)
                    {
                        weightArray[iWeight] = MutationLogicForSingleValue(weightArray[iWeight], 
                            mutationRate, mutationLowerBound, mutationUpperBound);
                    }
                }
            }
        }

        public static float MutationLogicForSingleValue(float val, float mutationRate,
            float mutationLowerBound, float mutationUpperBound)
        {
            var rand = Sampling.SampleFromUniformRange(0f, 1f);
            if (rand < mutationRate)
            {
                val += Sampling.SampleFromUniformRange(mutationLowerBound, mutationUpperBound);
            }

            return val;
        }

        public string GetDescriptionString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Neural Network " + (isFFN ? "w/o" : "w/")+ " recurrent layers w/ " +
                          $"{this.unitsPerLayer.Length} layers in total:");
            
            for (int i = 0; i < unitsPerLayer.Length; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine($"  > Input layer with {unitsPerLayer[i]} neurons");
                }
                else if (i == unitsPerLayer.Length - 1)
                {
                    sb.AppendLine($"  > Output layer with {unitsPerLayer[i]} neurons");
                }
                else
                {
                    var layerType = isLayerRecurrent[i] ? "Recurrent layer" : "Forward layer";
                    sb.AppendLine($"  > {layerType} (hidden # {i}) with {unitsPerLayer[i]} neurons");
                }
            }

            sb.AppendLine("## Weights:");
            sb.AppendLine(GetWeightString());

            sb.AppendLine("## Biases:");
            sb.AppendLine(GetBiasString());

            if (!isFFN)
            {
                sb.AppendLine("## Recurrent Storage");
                sb.AppendLine(GetRecurrentStorageString());

                sb.AppendLine("## Recurrent Weights");
                sb.AppendLine(GetRecurrentWeightsString());
            }

            return sb.ToString();
        }

        private string GetWeightString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < weights.Length; i++)
            {
                sb.AppendLine($"  weights[{i}]:");
                for (int j = 0; j < weights[i].Length; j++)
                {
                    sb.AppendLine($"    from neuron [{j}]");
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        sb.AppendLine($"      to neuron [{k}] = {weights[i][j][k]}");
                    }
                }
            }

            return sb.ToString();
        }

        private string GetBiasString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < biases.Length; i++)
            {
                sb.AppendLine($"  biases[{i}]:");
                for (int j = 0; j < biases[i].Length; j++)
                {
                    sb.AppendLine($"    [{j}] = {biases[i][j]}");
                }
            }

            return sb.ToString();
        }

        public string GetRecurrentStorageString()
        {
            var sb = new StringBuilder();
            for (int iArena = 0; iArena < nParallelArenas; iArena++)
            {
                sb.AppendLine($"  bot id [{iArena}]: ");
                for (int iBot = 0; iBot < recurrentStorage[iArena].Length; iBot++)
                {
                    sb.AppendLine($"  bot id [{iBot}]: ");
                    for (int jLayer = 0; jLayer < recurrentStorage[iArena][iBot].Length; jLayer++)
                    {
                        sb.AppendLine($"    layerNo [{jLayer}]:");
                        for (int kNeuron = 0; kNeuron < recurrentStorage[iArena][iBot][jLayer].Length; kNeuron++)
                        {
                            sb.AppendLine($"      neuron [{kNeuron}] = {recurrentStorage[iArena][iBot][jLayer][kNeuron]}");
                        }   
                    }
                }    
            }
            
            return sb.ToString();
        }

        public string GetRecurrentWeightsString()
        {
            var sb = new StringBuilder();

            for (int iLayer = 0; iLayer < recurrentWeights.Length; iLayer++)
            {
                sb.AppendLine($"  layerNo [{iLayer}]:");
                for (int jNeuron = 0; jNeuron < recurrentWeights[iLayer].Length; jNeuron++)
                {
                    sb.AppendLine($"    neuron [{jNeuron}] = {recurrentWeights[iLayer][jNeuron]}");
                }
            }
            return sb.ToString();
        }

        public void SetWeights(float[][][] weights)
        {
            this.weights = weights;
        }

        public void SetBiases(float[][] biases)
        {
            this.biases = biases;
        }

        public void SetRecurrentWeights(float[][] recurrentWeights)
        {
            this.recurrentWeights = recurrentWeights;
        }

        public string ToJson()
        {
            var jsonModel = new NeuralNetJSONModel(
                unitsPerLayer,
                isLayerRecurrent,
                neurons,
                biases,
                weights,
                recurrentWeights,
                isFFN,
                nBots,
                calculateFitness,
                id
            );

           return JsonConvert.SerializeObject(jsonModel, Formatting.Indented);
        }
        
        public void AcceptJsonParams(string jsonString)
        {
            var jsonModel = JsonConvert.DeserializeObject<NeuralNetJSONModel>(jsonString);

            if (jsonModel == null) throw new Exception("Something went wrong with the JSON Model (it is null).");
            
            this.AcceptJsonModel(jsonModel);
        }

        public void AcceptJsonModel(NeuralNetJSONModel jsonModel)
        {
            JsonHelpers.CompareNNToJsonShapes(this, jsonModel);

            this.biases = jsonModel.biases;
            this.weights = jsonModel.weights;
            if (this.isFFN)
                this.recurrentWeights = jsonModel.recurrentWeights;
        }
        
        public enum  FitnessCalculation
        {
            AverageFitness,
            MinFitness
        }

        public float GetNormalizedError(bool debug = false)
        {
            var nOutputNodes = unitsPerLayer[unitsPerLayer.Length - 1];

            if (debug)
            {
                Debug.Log($"nOutputNodes = {nOutputNodes}");
                Debug.Log($"cumErrorStorage: {cumErrorCurrentGenStorage.ToArray().ArrayToString()}");
            }

            if (fitnessCalc == FitnessCalculation.AverageFitness)
            {
                var errors = 0f;
                var errorArr = cumErrorCurrentGenStorage.ToArray();
                for (int i = 0; i < errorArr.Length; i++)
                {
                    var unpenalizedError = errorArr[i].Item2 / errorArr[i].Item1 / nOutputNodes;
                    errors += penalizeFitness ? CalcPenalizedError(unpenalizedError, errorArr[i].Item3):
                            unpenalizedError;
                }
                return errors / errorArr.Length;
            }
            else if (fitnessCalc == FitnessCalculation.MinFitness)
            {
                var maxError = cumErrorCurrentGenStorage.Select(x => 
                    penalizeFitness ? 
                    CalcPenalizedError(x.Item2 / x.Item1  / nOutputNodes, x.Item3) :
                    x.Item2 / x.Item1 / nOutputNodes
                    ).Max();
                return maxError;
            }
            else
            {
                throw new Exception($"Unknown fitnessCalc: {fitnessCalc.ToString()}");
            }
        }

        public (int, float, float)[] GetEvaluationStorageTuples()
        {
            return cumErrorCurrentGenStorage.ToArray();
        }

        public static float CalcPenalizedError(float error, float correctOpinionPercentage, float maxPenaltyFactor = 2f)
        {
            if (error <= 0)
                Debug.Log($"ARGH! Error is smaller than 0: {error}");
            if (error >= 1)
                Debug.Log($"ARGH! Error is bigger than 1: {error}");
            Assert.IsTrue(error >= 0 && error <= 1);
            var fitness = 1 - error;
            var factor = (1/maxPenaltyFactor) + correctOpinionPercentage * (1 - 1 / maxPenaltyFactor);
            var penalizedFitness = factor * fitness;
            var penalizedError = 1 - penalizedFitness;

            return penalizedError;
        }
    }

    public class NetworkPair : IComparable<NetworkPair>
    {
        public NetworkPair(NeuralNetwork decisionNetwork, NeuralNetwork predictionNetwork, string id)
        {
            this.decisionNetwork = decisionNetwork;
            this.predictionNetwork = predictionNetwork;
            this.id = id;
        }

        public NetworkPair(NetworkPairJSONModel jsonModel, int nParallelArenas, 
            NeuralNetwork.FitnessCalculation fitnessCalc, bool penalizeFitness) : this(
            new NeuralNetwork(jsonModel.decisionNet, nParallelArenas, fitnessCalc, penalizeFitness),
            new NeuralNetwork(jsonModel.predictionNet, nParallelArenas, fitnessCalc, penalizeFitness),
            jsonModel.id
            )
        {
        }
        
        public NeuralNetwork decisionNetwork { get; set; }
        public NeuralNetwork predictionNetwork { get; set; }

        public NetworkPair CreateCopy(int nParallelArenas)
        {
            var copyNet =  new NetworkPair(this.decisionNetwork.CreateCopy(nParallelArenas), 
                this.predictionNetwork.CreateCopy(nParallelArenas), 
                this.id+"."+copyCounter);
            copyCounter++;
            return copyNet;
        }

        public string id { get; set; }
        private int copyCounter = 0;

        public float GetNormalizedError()
        {
            return this.predictionNetwork.GetNormalizedError();
        }

        public (int, float, float)[] GetEvaluationStorageTuples()
        {
            return predictionNetwork.GetEvaluationStorageTuples();
        }

        public void Mutate(float mutationRate, float mutationLowerBound, float mutationUpperBound)
        {
            predictionNetwork.MutationWrapper(mutationRate, mutationLowerBound, mutationUpperBound);
            decisionNetwork.MutationWrapper(mutationRate, mutationLowerBound, mutationUpperBound);
        }

        public void ResetForNextGen()
        {
            predictionNetwork.ResetForNextGen();
            decisionNetwork.ResetForNextGen();
        }

        public void ResetForNextRound(float[] currRoundOpinionOutcomes)
        {
            predictionNetwork.ResetForNextRound(currRoundOpinionOutcomes);
            decisionNetwork.ResetForNextRound(currRoundOpinionOutcomes);
        }
        
        public int CompareTo(NetworkPair other)
        {
            if (other == null) 
                return 1;
            if (predictionNetwork.GetNormalizedError() > other.predictionNetwork.GetNormalizedError())            
                return 1;
            else if (predictionNetwork.GetNormalizedError() < other.predictionNetwork.GetNormalizedError())            
                return -1;
            else
                return 0;
        }

        public Opinion AccessNeuralNetworks(BotController botController, int nMaxMessages, NeuralNetLogger logger)
        {
            var botID = botController.index;
            var arenaIndex = botController.homeArena;
            var msgs = botController.opinionHandler.GetCurrentMessages();
            var currentOpinion = botController.opinionHandler.currentOpinion;
            var groundBrightness = botController.lastMeasuredGroundBrightness;

            var decNetInputArr =
                NeuralNetAdapter.CreateDecisionNetInputArr(msgs, nMaxMessages, groundBrightness, currentOpinion);
            var decNetOutput = NeuralNetAdapter.AccessDecisionNetwork(msgs, nMaxMessages, groundBrightness,
                currentOpinion, decisionNetwork, botID, arenaIndex);
            
            var newOpinion = NeuralNetAdapter.DecodeOpinion(decNetOutput[0]);

            var predictionNetInputArr =
                NeuralNetAdapter.CreatePredictionNetInputArr(msgs, nMaxMessages, groundBrightness, newOpinion);
            var predictionOutput = NeuralNetAdapter.AccessPredictionNetwork(msgs, nMaxMessages, groundBrightness,
                newOpinion, predictionNetwork, botID, arenaIndex);
            
            logger.LogNeuralNetCall(botController, this, newOpinion, 
                predictionNetInputArr, predictionOutput, 
                decNetInputArr, decNetOutput
                );

            return newOpinion;
        }
        
    }
}