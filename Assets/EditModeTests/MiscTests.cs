using System;
using System.Linq;
using Assets;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class MiscTests
    {
        [Test]
        public void ModuloTest()
        {
            var vals =new float[]{365f, -5f};


            foreach (var val in vals)
            {
                Debug.Log($"{val} % 360 = {Misc.mod(val, 360)}");    
            }
        }

        [Test]
        public void MultiDimArrayCheckerTest3D()
        {
            var arr1 = new []
            {
                new [] 
                {
                    new[] {0.1f, -0.3f},
                }, 
                new [] 
                {
                    new[] {0.5f},
                    new[] {0.8f}
                }  
            };
            
            var arr2 = new []
            {
                new [] 
                {
                    new[] {0.1f, -0.3f, 0.4f},
                }, 
                new [] 
                {
                    new[] {0.5f},
                    new[] {0.8f}
                }  
            };
            
            var arr3 = new []
            {
                new [] 
                {
                    new[] {-0.3f, 0.4f},
                }, 
                new [] 
                {
                    new[] {0.2f},
                    new[] {0.4f}
                }  
            };
            
            Assert.IsFalse(arr1.IsArrayShapeIdentical(arr2));
            Assert.IsTrue(arr1.IsArrayShapeIdentical(arr3));
        }
        
        [Test]
        public void MultiDimArrayCheckerTest2D()
        {
            var arr1 = new []
            {
                new [] 
                {
                   0.1f, 0.3f
                }, 
                new [] 
                {
                    0.1f
                }  
            };
            
            var arr2 = new []
            {
                new [] 
                {
                    0.1f, 0.3f
                }, 
                new [] 
                {
                    0.1f, 0.4f
                }  
            };
            
            var arr3 = new []
            {
                new [] 
                {
                    0.2f, 0.4f
                }, 
                new [] 
                {
                    0.3f
                }  
            };
            
            Assert.IsFalse(arr1.IsArrayShapeIdentical(arr2));
            Assert.IsTrue(arr1.IsArrayShapeIdentical(arr3));
        }

        [Test]
        public void ArrayDiffTest()
        {
            var arr1 = new[] {1.0f, 0.0f, 3.0f};
            var arr2 = new[] {2.0f, -4f, -1f};

            var absDiff = arr1.ArrayAbsDifference(arr2);
            Assert.IsTrue(Math.Abs(absDiff - (1f+4f+4f)) < 0.005f);
        }
        
        [Test]
        public void CumSumArrayIndexFindingTest()
        {
            var cumSumArray = new[] {0.0f, 8.2f, 11.4f, 12.8f, 14.0f};
            
            Assert.AreEqual(0,cumSumArray.CumSumFindIndex(0.0f));
            Assert.AreEqual(0,cumSumArray.CumSumFindIndex(0.5f));
            Assert.AreEqual(1,cumSumArray.CumSumFindIndex(10f));
            Assert.AreEqual(3,cumSumArray.CumSumFindIndex(13f));
            Assert.AreEqual(-1,cumSumArray.CumSumFindIndex(100f));
        }

        [Test]
        public void CumSumArrayCreationTest()
        {
            var errorVals = new[] {0.05f, 0.05f,0.1f, 0.2f, 0.3f, 0.4f};
            var ignoreIndices = new[] {0, 1};


            var (indArray, cumSumArray) = EvolutionHandler.PrepareIndexCumSumArrays(errorVals, ignoreIndices);

            Debug.Log("indArray = " + indArray.ArrayToString());
            Debug.Log("cumSumArray = " + cumSumArray.ArrayToString());
        }

        [Test]
        public void Copy2DJaggedArrayTest()
        {
            var arr2d = new[]
            {
                new[] {1f, 2f},
                new[] {0f},
                new[] {1f, 2f, 3f}
            };
            
            var copyArr2D = arr2d.CopyArray2D();

            arr2d[0][0] = 10f;
            Assert.AreEqual(1f, copyArr2D[0][0]);
            Assert.AreEqual(10f, arr2d[0][0]);

            Debug.Log( "original array: " + arr2d.Select(x => x.ArrayToString()).ToArray().ArrayToString());
            Debug.Log( "copied array: " + copyArr2D.Select(x => x.ArrayToString()).ToArray().ArrayToString());
        }
        
        [Test]
        public void Copy3DJaggedArrayTest()
        {
            var arr3d = new[]
            {
                new[]
                {
                    new[] {1f, 2f}, 
                    new [] {1f}
                },
                new []
                {
                    new[] {3f, 4f}
                }
            };
            
            var copyArr3D = arr3d.CopyArray3D();

            arr3d[0][0][0] = 10f;
            Assert.AreEqual(1f, copyArr3D[0][0][0]);
            Assert.AreEqual(10f, arr3d[0][0][0]);

            Debug.Log( "original array: " + arr3d.Select(x => x.Select(y => y.ArrayToString()).ToArray().ArrayToString()).ToArray().ArrayToString());
            Debug.Log( "copied array: " + copyArr3D.Select(x => x.Select(y => y.ArrayToString()).ToArray().ArrayToString()).ToArray().ArrayToString());
        }
        
    }
    
}