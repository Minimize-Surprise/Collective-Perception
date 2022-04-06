using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ArenaTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ArenaTestsSimplePasses()
        {

            double[] difficulties = {0.2d, 0.4d, 0.5d};
            foreach (var difficulty in difficulties)
            {
                TestArenaSetting(difficulty, 5, 4);
            }
        }
        
        private static void TestArenaSetting(double rho, int x, int y)
        {
            var array = ArenaCreator.CreateColorArrayForDifficulty(x, y, rho, Opinion.Black);
            PrintArena(array);
            var (blacks, whites) = CountBlackWhite(array);
            Assert.AreEqual(rho, CalcDifficulty(blacks, whites));
        }
        

        private static void PrintArena(Opinion[,] array)
        {
            var rowLength = array.GetLength(0);
            var colLength = array.GetLength(1);
            var arrayString = "";
            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < colLength; j++)
                {
                    arrayString += array[i, j] + " "; //string.Format("{0} ", array[i, j]);
                }
                arrayString += System.Environment.NewLine;
            }
            arrayString = "Created tile array: " + System.Environment.NewLine + arrayString;
            Debug.Log(arrayString);
        }

        private static (int blacks, int whites) CountBlackWhite(Opinion[,] array)
        {
            int blacks = 0, whites = 0;
            foreach (var op in array)
            {
                switch (op)
                {
                    case Opinion.Black:
                        blacks += 1;
                        break;
                    case Opinion.White:
                        whites += 1;
                        break;
                }
            }
            return (blacks, whites);
        }

        private static double CalcDifficulty(int blacks, int whites)
        {
            var total = blacks + whites;
            var min = Math.Min(blacks, whites);
            Debug.Log("blacks: " + blacks + ", whites: " + whites + ", min: " + min);
            return (double) min / (double) total;
        }
    }
}
