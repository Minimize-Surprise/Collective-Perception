using System;
using System.Linq;
using Assets;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Tests
{
    public class SamplingTests
    {
        [Test]
        public void ExponentialDistTest()
        {
            var seed = 42;
            Random.InitState(seed);

            var mean = 40;
            Debug.Log($"Sampling from exponential with seed {seed} and mean {mean}:");

            var nSamples = 1000;
            float[] results = new float[nSamples];

            float empiricalMean = 0f;
            
            for (int i = 0; i < nSamples; i++)
            {
                results[i] = Sampling.SampleFromExponentialDistribution(mean);
                empiricalMean += results[i];
            }
            Debug.Log($"Results: {String.Join("; ",results.Select(x => x.ToString("F2")))}");
            Debug.Log($"Empirical mean: {empiricalMean / nSamples}");
        }
    }
}