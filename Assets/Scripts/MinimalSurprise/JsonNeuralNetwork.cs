using System;
using System.IO;
using System.Linq;
using System.Text;
using Assets;
using Newtonsoft.Json;

namespace MinimalSurprise
{
    public class NeuralNetJSONModel
    {
        [JsonConstructor]
        public NeuralNetJSONModel(int[] unitsPerLayer, bool[] isLayerRecurrent, float[][] neurons, float[][] biases,
            float[][][] weights, float[][] recurrentWeights, bool isFFN,
            int nBots, bool calculateFitness, string id)
        {
            this.unitsPerLayer = unitsPerLayer;
            this.isLayerRecurrent = isLayerRecurrent;
            this.neurons = neurons;
            this.biases = biases;
            this.weights = weights;
            this.recurrentWeights = recurrentWeights;
            this.isFFN = isFFN;
            this.nBots = nBots;
            this.calculateFitness = calculateFitness;
            this.id = id;
        }

        public NeuralNetJSONModel(NeuralNetwork nn) : this(nn.unitsPerLayer, nn.isLayerRecurrent, nn.neurons, nn.biases,
            nn.weights, nn.recurrentWeights, nn.isFFN, nn.nBots, nn.calculateFitness, nn.id)
        {
        }
        
        public int[] unitsPerLayer { get; set; }
        public bool[] isLayerRecurrent { get; set;}
        public float[][] neurons { get; set; }
        public float[][] biases { get; set; }
        public float[][][] weights { get; set; }
        public float [][] recurrentWeights { get; set; }
        public bool isFFN { get; set; }
        public int nBots { get; set; }
        public bool calculateFitness { get; set; }
        public string id { get; set; }
    }

    public class NetworkPairJSONModel
    {
        public NeuralNetJSONModel predictionNet;
        public NeuralNetJSONModel decisionNet;
        public string id;

        public NetworkPairJSONModel(NeuralNetwork predictionNet, NeuralNetwork decisionNet, string id)
        {
            this.predictionNet = new NeuralNetJSONModel(predictionNet);
            this.decisionNet = new NeuralNetJSONModel(decisionNet);
            this.id = id;
        }

        public NetworkPairJSONModel(NetworkPair networkPair) : this(networkPair.predictionNetwork, networkPair.decisionNetwork, networkPair.id)
        {
        }

        [JsonConstructor]
        public NetworkPairJSONModel(NeuralNetJSONModel predictionNet, NeuralNetJSONModel decisionNet)
        {
            this.predictionNet = predictionNet;
            this.decisionNet = decisionNet;
        }

        public static NetworkPairJSONModel ConstructFromJsonPath(string jsonPath)
        {
            StreamReader reader = new StreamReader(jsonPath);
            var jsonString = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<NetworkPairJSONModel>(jsonString);
        }

        public static object RawFromString(string jsonString)
        {
            return JsonConvert.DeserializeObject<NetworkPairJSONModel>(jsonString);
        }
    }
    
    public class EvolutionLogJSONModel {
        public EvolutionLogJSONModel(int generationCounter, float bestFitness, float[] completeFitnessVals, string[] completeFitnessIDs, (int, float, float)[][] completeEvalTuples ,NetworkPair[] netsToStore)
        {
            this.generationCounter = generationCounter;
            this.bestFitness = bestFitness;
            this.completeFitnessVals = completeFitnessVals;
            this.completeFitnessIDs = completeFitnessIDs;
            this.completeEvalTuples = completeEvalTuples;
            this.netsToStore = netsToStore.Select(x => new NetworkPairJSONModel(x)).ToArray();
        }

        public int generationCounter { get; set; }
        public float bestFitness { get; set; }
        public float[] completeFitnessVals { get; set; }
        public (int, float, float)[][] completeEvalTuples { get; set; }
        public string[] completeFitnessIDs { get; set; }
        public NetworkPairJSONModel[] netsToStore { get; set; }
    }
    
    public static class JsonHelpers
    {
        public static void CompareNNToJsonShapes(NeuralNetwork nn, NeuralNetJSONModel nnJsonModel)
        {
            if (nnJsonModel.unitsPerLayer.Length != nn.unitsPerLayer.Length ||
                nnJsonModel.isLayerRecurrent.Length != nn.isLayerRecurrent.Length ||
                !nnJsonModel.neurons.IsArrayShapeIdentical(nn.neurons) ||
                !nnJsonModel.biases.IsArrayShapeIdentical(nn.biases) ||
                !nnJsonModel.weights.IsArrayShapeIdentical(nn.weights) ||
                !nnJsonModel.recurrentWeights.IsArrayShapeIdentical(nn.recurrentWeights)||
                nnJsonModel.isFFN != nn.isFFN ||
                nnJsonModel.nBots != nn.nBots)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Shape of imported JSON parameters don't fit this network. (Below: JSONModel vs. NeuralNetwork)");
                sb.AppendLine($"unitsPerLayer equal length? {nnJsonModel.unitsPerLayer.Length} vs. {nn.unitsPerLayer.Length}");
                sb.AppendLine($"isLayerRecurrent equal length? {nnJsonModel.isLayerRecurrent.Length} vs. {nn.isLayerRecurrent.Length}");
                sb.AppendLine($"neurons shape identical? {nnJsonModel.neurons.IsArrayShapeIdentical(nn.neurons)}");
                sb.AppendLine($"biases shape identical? {nnJsonModel.biases.IsArrayShapeIdentical(nn.biases)}");
                sb.AppendLine($"weights shape identical? {nnJsonModel.weights.IsArrayShapeIdentical(nn.weights)}");
                sb.AppendLine($"recurrentWeights shape identical? {nnJsonModel.recurrentWeights.IsArrayShapeIdentical(nn.recurrentWeights)}");
                sb.AppendLine($"isFFN equal? {nnJsonModel.isFFN} vs. {nn.isFFN}");
                sb.AppendLine($"nBots equal? {nnJsonModel.nBots} vs. {nn.nBots}");
                throw new FormatException(sb.ToString());
            }
        }
    }
}