using Assets;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class InitialSettingsTests
    {
        [Test]
        public void InitialSettingsTest()
        {
            var icsEvo = new EvolutionSettingsCreator(5, 0, 5, 0, 5, 0.5f, 5, 5, Opinion.Black, 0.4f, 0.7f / 2);
            icsEvo.InitStartSettings(2, 3, null);
            Debug.Log(icsEvo.GetVeryFirstStartSetting().ToString());
            Debug.Log($"startSettings dimensions: {icsEvo.startSettings.GetLength(0)} x {icsEvo.startSettings.GetLength(1)}");
            
            var icsEval = new EvaluationSettingsCreator(5, 0, 5, 0, 5, 0.5f, 5, 5, Opinion.Black, 0.4f, 0.7f / 2);
            icsEval.InitStartSettings(3, null);
            Debug.Log(icsEval.GetVeryFirstStartSetting().ToString());
            Debug.Log($"icsEval: Created {icsEval.startSettings.Length} start settings");
        }
        
        [Test]
        public void InitialSettingsJsonTest()
        {
            var icsEvo = new EvolutionSettingsCreator(5, 0, 5, 0, 5, 0.5f, 5, 5, Opinion.Black, 0.4f, 0.7f / 2);
            icsEvo.InitStartSettings(2, 3, null);
            var jsonStr = icsEvo.ToJsonStringViaModel();
            //Debug.Log(jsonStr);

            var icsJsonModel = InitialSettingsCreator.FromJson(jsonStr);

            var real = icsJsonModel.ToRealInitialSettingsCreator();
        }
    }
}