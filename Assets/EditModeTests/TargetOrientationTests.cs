using Assets;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class TargetOrientationTests
    {


        [Test]
        public void TestAnglesBetweenVectors()
        {
            
            Debug.Log($"identical: {Misc.VectorToArenaOrientation(new Vector3(0, 0,1 ))}");
            Debug.Log($"x one: {Misc.VectorToArenaOrientation( new Vector3(1, 0, 0 ))}");
            Debug.Log($"reverse: { Misc.VectorToArenaOrientation(new Vector3(0, 0, -1 ))}");
            Debug.Log($"x minus one: {Misc.VectorToArenaOrientation(new Vector3(-1, 0, 0 ))}");
            Debug.Log($"Half left: {Misc.VectorToArenaOrientation(new Vector3(-1, 0, 1))}");
        }

        [Test]
        public void TestRotations()
        {
            var baseVec = Vector3.forward;
            
            Debug.Log( $"Forward base rotate 90°, outcome: {Misc.RotateVectorInArenaSpace(baseVec, 90f)}");
            Debug.Log( $"Forward base rotate -45°, outcome: {Misc.RotateVectorInArenaSpace(baseVec, -45f)}");
            Debug.Log( $"Forward base rotate 270°, outcome: {Misc.RotateVectorInArenaSpace(baseVec, 270f)}");

            var backVec = Vector3.back;
            Debug.Log( $"Backward base rotate 90°, outcome: {Misc.RotateVectorInArenaSpace(backVec, 90f)}");
            Debug.Log( $"Backward base rotate -45°, outcome: {Misc.RotateVectorInArenaSpace(backVec, -45f)}");
            Debug.Log( $"Backward base rotate 270°, outcome: {Misc.RotateVectorInArenaSpace(backVec, 270f)}");
        }
        
    }
}