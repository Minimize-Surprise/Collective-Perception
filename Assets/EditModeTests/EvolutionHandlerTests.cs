using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class EvolutionHandlerTests
    {
        
        [Test]
        public void EvolutionHandlerTest()
        {
            var eh = new EvolutionHandler(true);
            
            eh.Initialize();

            for (int i = 0; i < 70; i++)
            {
                Debug.Log($"### ROUND {i} ###");
                Debug.Log(eh.GetCurrentInfoString());
                
                Debug.Log("/// Prepping next round:");
                eh.ProgressInEvolutionLogic();
                Debug.Log("");
            }
        }
        
        
        [Test]
        public void ParallelEvolutionHandlerTest()
        {
            var eh = new EvolutionHandler(true);
            
            eh.Initialize();

            for (int i = 0; i < 20; i++)
            {
                Debug.Log($"### ROUND {i} ###");
                Debug.Log("-- Evolution Logic Progress:");
                var currStartSettings = eh.GetNextRoundArenaSettings();
                Debug.Log("-- Created Arena Settings:");
                for (int j = 0; j < currStartSettings.Length; j++)
                {
                    Debug.Log($"> Arena {j}: {(currStartSettings[j] == null? null : currStartSettings[j].ToString())}");
                }
            }
        }

        [Test]
        public void ProportionateSelectionTest()
        {
            var fitness = new[] {0.9f, 0.8f, 0.7f, 0.6f, 0.5f};
            var errors = fitness.Select(x => 1 - x).ToArray();
            var ignoreInd = new int[] { }; // Elitism individuals are NOT ignored
            var nSelect = 10000;

            var ind = EvolutionHandler.ProportionateSelectionIndices(errors, ignoreInd, nSelect);

            Assert.AreEqual(nSelect, ind.Length);

            var counts = (new int[errors.Length]).Populate(0);

            foreach (var i in ind)
            {
                counts[i]++;
            }

            var percArr = new float[counts.Length];
            for (int i = 0; i < counts.Length; i++)
            {
                percArr[i] = (float) counts[i] / (float) nSelect;
            }

            Debug.Log($"Sampled Counts: {counts.ArrayToString()}");
            Debug.Log($"Sampled Percentages: {percArr.ArrayToString()}");

            
            var targets = new float[fitness.Length];
            var notIgnoreSum = 0f;
            for (int i = 0; i < fitness.Length; i++)
            {
                if (!ignoreInd.Contains(i))
                    notIgnoreSum += fitness[i];
            }
            
            for (int i = 0; i < fitness.Length; i++)
            {
                if (ignoreInd.Contains(i))
                    targets[i] = 0f;
                else
                    targets[i] = fitness[i] / notIgnoreSum;
            }
            Debug.Log($"Target Percentages: {targets.ArrayToString()}");

            // Should be within 5% tolerance
            for (int i = 0; i < targets.Length; i++)
            {
                Assert.IsTrue(Math.Abs(targets[i] - percArr[i]) < 0.05f);
            }
        }

        [Test]
        public void ElitismTest()
        {
            var errors = new[] { 0.9f, 0.8f, 0.5f, 0.6f};

            var sortedIndices = errors.ArrayGetSortedIndices();
            
            Debug.Log("Sorted array indices: " + sortedIndices.ArrayToString());

            //List<int> B = sorted.Select(x => x.Key).ToList();
            //List<int> idx = sorted.Select(x => x.Value).ToList();

        }
    }
}