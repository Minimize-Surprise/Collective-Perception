using UnityEngine;

namespace Assets
{
    public class Sampling
    {

        
        public static float SampleFromExponentialDistribution(float mean)
        {
            // Source: Argos Code
            // https://github.com/ilpincy/argos3/blob/af64c80b6e8168abb50f59f9c3dcce3d1ce12af0/src/core/utility/math/rng.cpp#L123

            var sampledFromUniform = Random.Range(0f, 1f);
            return -Mathf.Log(sampledFromUniform) * mean;
        }

        public static float SampleFromUniformRange(float lowerBound, float upperBound)
        {
            return Random.Range(lowerBound, upperBound);
        }

        public static bool SampleRandomBool()
        {
            var val = Random.Range(0f, 1f);
            return val > 0.5f;
        }
    }
}