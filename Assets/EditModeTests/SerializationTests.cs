using System;
using MinimalSurprise;
using NUnit.Framework;

namespace Tests
{
    public class SerializationTests
    {
        [Test]
        public void JSONPackageTest()
        {
            var nParallelArenas = 1;
            var nn = new NeuralNetwork(new[] {1, 2, 1}, 1, nParallelArenas, "testnet1", NeuralNetwork.FitnessCalculation.AverageFitness, false);
            var jsonString = nn.ToJson();
            nn.AcceptJsonParams(jsonString);
            
            var nn2 = new NeuralNetwork(new[] {1, 2, 2,1}, 2, nParallelArenas+1, "testnet2", NeuralNetwork.FitnessCalculation.AverageFitness, false);
            var changedJsonString = nn2.ToJson();
            Assert.Throws<FormatException>(() => nn.AcceptJsonParams(changedJsonString));
        }
    }
}