using System.IO;
using NUnit.Framework;
using Setup;
using UnityEngine;

namespace Tests
{
    public class ParamsImporterTest
    {
        [Test]
        public void BasicTest()
        {
            var yamlPath = Path.Combine(Application.dataPath, "StreamingAssets", "master_params.yaml");
            var importer = new ParamsImporter(yamlPath, null);
            var arr = importer.YamlFileToKeyValueArr();

            Debug.Log("Found these key-value pairs in the config yaml:");
            foreach (var tuple in arr)
            {
                Debug.Log($"Key: {tuple.Item1} - Value: {tuple.Item2}");
            }
        }
    }
}