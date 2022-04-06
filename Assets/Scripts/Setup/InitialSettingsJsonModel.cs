using System.IO;
using Assets;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Setup
{
    public class InitialSettingsJsonModel
    {
        public string type;
        public StartSetting[] startSettingsEval;
        public StartSetting[,] startSettingsEvo;
        
        public readonly int nBots;
        public readonly int minX;
        public readonly int maxX;
        public readonly int minY;
        public readonly int maxY;

        public readonly float taskDifficulty;
        public readonly int nTilesX;
        public readonly int nTilesY;
        public readonly Opinion dominatingOpinion;
        public readonly float correctInitializationPercentage;

        public readonly float botRadius;
        
        public InitialSettingsJsonModel(
            int nBots, int minX, int maxX, int minY, int maxY,
            float taskDifficulty, int nTilesX, int nTilesY, Opinion dominatingOpinion,
            float correctInitializationPercentage, float botRadius)
        {
            this.nBots = nBots;
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.taskDifficulty = taskDifficulty;
            this.nTilesX = nTilesX;
            this.nTilesY = nTilesY;
            this.dominatingOpinion = dominatingOpinion;
            this.correctInitializationPercentage = correctInitializationPercentage;
            this.botRadius = botRadius;
        }

        [JsonConstructor]
        public InitialSettingsJsonModel(
            int nBots, int minX, int maxX, int minY, int maxY,
            float taskDifficulty, int nTilesX, int nTilesY, Opinion dominatingOpinion,
            float correctInitializationPercentage, float botRadius,
            string type, [CanBeNull] StartSetting[,] startSettingsEvo, 
            [CanBeNull] StartSetting[] startSettingsEval 
        ) : this(nBots, minX, maxX, minY, maxY,
            taskDifficulty, nTilesX, nTilesY, dominatingOpinion,
            correctInitializationPercentage, botRadius)
        {
            this.type = type;
            this.startSettingsEvo = startSettingsEvo;
            this.startSettingsEval = startSettingsEval;
        }

        public void MakeEvolutionSettings(StartSetting[,] startSettings)
        {
            this.type = "Evolution";
            this.startSettingsEvo = startSettings;
        }

        public void MakeEvaluationSettings(StartSetting[] startSettings)
        {
            this.type = "evaluation";
            this.startSettingsEval = startSettings;
        }
        
        public static InitialSettingsJsonModel ConstructFromJsonPath(string jsonPath)
        {
            StreamReader reader = new StreamReader(jsonPath);
            var jsonString = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<InitialSettingsJsonModel>(jsonString);
        }

        public InitialSettingsCreator ToRealInitialSettingsCreator()
        {
            InitialSettingsCreator ics = null;
            if (type.ToLower() == "evolution")
            {
                ics = new EvolutionSettingsCreator(
                    nBots, minX, maxX, minY, maxY,
                    taskDifficulty, nTilesX, nTilesY, dominatingOpinion,
                    correctInitializationPercentage, botRadius
                );
                ((EvolutionSettingsCreator) ics).SetStartSettingsFromJson(this.startSettingsEvo);
            } else if (type.ToLower() == "evaluation")
            {
                ics = new EvaluationSettingsCreator(
                    nBots, minX, maxX, minY, maxY,
                    taskDifficulty, nTilesX, nTilesY, dominatingOpinion,
                    correctInitializationPercentage, botRadius
                );
                ((EvaluationSettingsCreator) ics).SetStartSettingsFromJson(this.startSettingsEval);
            }

            return ics;
        }
    }
}