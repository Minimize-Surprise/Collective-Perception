using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets
{
    public static class Misc
    {
        public static float mod(float a,float b)
        {
            return a - b * Mathf.Floor(a / b);
        }

        public static float VectorToArenaOrientation(Vector3 vectorToMeasure)
        {
            return Vector3.SignedAngle(Vector3.forward, vectorToMeasure, Vector3.up);
        }

        public static Vector3 RotateVectorInArenaSpace(Vector3 vectorToRotate, float rotationAngle)
        {
            return Quaternion.Euler(0, rotationAngle, 0) * vectorToRotate;
        }
        
        /// <summary>
        /// Populate an array with a value:
        /// Example:
        /// <code>
        /// var a = new int[100].Populate(1)
        /// </code>
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] Populate<T>(this T[] arr, T value ) {
            // From here: https://stackoverflow.com/a/1014015/5604353
            for ( int i = 0; i < arr.Length;i++ ) {
                arr[i] = value;
            }
            return arr;
        }

        public static string ArrayToString<T>(this T[] arr)
        {
            return String.Join("; ", arr.Select(x => x.ToString()));
        }
        
        public static float ArrayAbsDifference (this float[] arr1, float[] arr2)
        {
            var res = 0f;
            for (int i = 0; i < arr1.Length; i++)
            {
                res += Mathf.Abs(arr1[i] - arr2[i]);
            }

            return res;
        }

        public static int ArrayMinIndex(this float[] arr)
        {
            var pos = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < arr[pos])
                    pos = i;
            }

            return pos;
        }


        public static T[] Shuffle<T>(T[] arr)
        {
            // Knuth shuffle algorithm :: courtesy of Wikipedia :)
            for (int t = 0; t < arr.Length; t++ )
            {
                T tmp = arr[t];
                int r = Random.Range(t, arr.Length);
                arr[t] = arr[r];
                arr[r] = tmp;
            }

            var retArr = new T[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                retArr[i] = arr[i];
            }

            return retArr;
        }

        public static int[] GetRandomIndices(int arrayLength, int sampleSize)
        {
            var indicesArray = Enumerable.Range(0, arrayLength-1).ToArray();
            var shuffledInd = Misc.Shuffle(indicesArray);

            var selectedIndices = new int[sampleSize];
            for (int i = 0; i < sampleSize; i++)
            {
                selectedIndices[i] = shuffledInd[i];
            }

            return selectedIndices;
        }

        /// <summary>
        /// Returns the cumulative sum array's index where the value lays between the index and the
        /// subsequent element.
        /// </summary>
        /// <param name="cumSumArr">Cumulative sum index (first element has to be 0!)</param>
        /// <param name="val">Value to be ranked in the cumsum array</param>
        /// <returns></returns>
        public static int CumSumFindIndex(this float[] cumSumArr, float val)
        {
            for (int j = 0; j < cumSumArr.Length - 1; j++)
            {
                if (val >= cumSumArr[j] && val < cumSumArr[j + 1])
                {
                    return j;
                }
            }

            return -1;
        }

        public static bool IsArrayShapeIdentical<T>(this T[][][] arr1, T[][][] arr2)
        {
            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i].Length != arr2[i].Length) return false;

                for (int j = 0; j < arr1[i].Length; j++)
                {
                    if (arr1[i][j].Length != arr2[i][j].Length)
                        return false;
                }
            }

            return true;
        }
        
        public static bool IsArrayShapeIdentical<T>(this T[][] arr1, T[][] arr2)
        {
            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i].Length != arr2[i].Length) return false;
            }

            return true;
        }

        public static int[] ArrayGetSortedIndices<T>(this T[] arr)
        {
            var sortedIndices = arr
                .Select((x, i) => new KeyValuePair<T, int>(x, i))
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToArray();

            return sortedIndices;
        }

        public static T[][] CopyArray2D<T>(this T[][] arr)
        {
            return arr.Select(x => x.ToArray()).ToArray();
        }

        public static T[][][] CopyArray3D<T>(this T[][][] arr)
        {
            return arr.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray();
        }
        
        public static string GetArgByName(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}