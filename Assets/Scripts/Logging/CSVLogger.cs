using System;
using ExperimentHandler;
using MinimalSurprise;
using Newtonsoft.Json;
using Setup;
using UnityEngine;
using SysDebug = System.Diagnostics.Debug;

namespace Assets.Logging
{
    public class CSVLogger : FileLogger
    {
        private string[] colNames;
        private bool headerWrittenOut = false;
        private string sep = ";";
        public CSVLogger(string filePath, string[] colNames) : base(filePath)
        {
            this.colNames = colNames;
        }
        
        public void AddLine(string[] vals)
        {
            if (!headerWrittenOut)
                WriteHeaders();
                
                
            SysDebug.Assert(vals != null, nameof(vals) + " != null");
            SysDebug.Assert(vals.Length == colNames.Length, "csv writer: len of cols and vals different!");
            string s = String.Join(sep, vals);
            base.AddLine(s);
        }

        private void WriteHeaders()
        {
            base.AddLine(String.Join(sep, colNames));
            this.headerWrittenOut = true;
        }
    }

    public class NeuralNetLogger : CSVLogger
    {
        private Parameters masterParams;
        
        public NeuralNetLogger(string filePath, Parameters masterParams) : base(filePath, 
            new[] {"time", "run", "arenaIndex", "botIndex", 
                "networkID",
                "currentOpinion", "newOpinion", 
                "predNetInput", "predNetOutput", "decNetInput", "decNetOutput"})
        {
            this.masterParams = masterParams;
        }

        public void LogNeuralNetCall(BotController botController, NetworkPair networkPair, Opinion newOpinion,
            float[] predNetInput, float[] predNetOutput,
            float[] decNetInput, float[] decNetOutput)
        {
            float time = Time.time - masterParams.currentRoundStartTime;
            
            AddLine(new string[]
            {
                time.ToString("F2"),
                masterParams.currentSimulationRound.ToString(),
                botController.homeArena.ToString(),
                botController.index.ToString(),
                networkPair.id,
                botController.opinionHandler.currentOpinion.ToString(),
                newOpinion.ToString(),
                JsonConvert.SerializeObject(predNetInput),
                JsonConvert.SerializeObject(predNetOutput),
                JsonConvert.SerializeObject(decNetInput),
                JsonConvert.SerializeObject(decNetOutput)
            });
        }
    }

    public class PositionOpinionLogger : CSVLogger
    {
        private Parameters masterParams;
        private BeeClust[,] beeScripts;
        public PositionOpinionLogger(string filePath, Parameters masterParams) : base(filePath, 
            new []{"run", "time", "botname", "arenaIndex", "botIndex", "x", "y", "opinion"})
        {
            this.masterParams = masterParams;

            this.beeScripts = new BeeClust[masterParams.monas.GetLength(0),
                masterParams.monas.GetLength(1)];
            for (int i = 0; i < masterParams.monas.GetLength(0); i++)
            {
                for (int j = 0; j < masterParams.monas.GetLength(1); j++)
                {
                    beeScripts[i, j] = masterParams.monas[i, j].GetComponent<BeeClust>();
                }
            }
        }
        
        public void WriteLine()
        {
            float time = Time.time - masterParams.currentRoundStartTime;
            for (int iArena = 0; iArena < masterParams.monas.GetLength(0); iArena++)
            {
                if (!masterParams.arenas[iArena].activeSelf) continue;
                for (int jBot = 0; jBot < masterParams.monas.GetLength(1); jBot++)
                {
                    var mona = masterParams.monas[iArena, jBot];
                    var localPosition = mona.transform.localPosition;
                
                    NullCheckBeeScript(iArena, jBot);
                
                    AddLine(new []
                    {
                        masterParams.currentSimulationRound.ToString(),
                        time.ToString("F2"), 
                        mona.name,
                        iArena.ToString(),
                        jBot.ToString(),
                        localPosition.x.ToString("F2"),
                        localPosition.y.ToString("F2"),
                        beeScripts[iArena, jBot].botController.opinionHandler.currentOpinion.ToString()
                    });    
                }
            }
        }

        private void NullCheckBeeScript(int iArena, int jBot)
        {
            if (beeScripts[iArena, jBot] == null)
            {
                beeScripts[iArena, jBot] = masterParams.monas[iArena, jBot].GetComponent<BeeClust>();
            }
        }
    }

    public class ArenaSettingLogger : CSVLogger
    {
        private ExperimentType experimentType;
        
        public ArenaSettingLogger(string filePath,
            ExperimentType experimentType) : base(filePath,
            experimentType == ExperimentType.EvolutionExperiment?
            new[]
            {
                "round", "arenaIndex", "startSettingID", 
                "generationIndex", "populationIndex", "evalRunIndex", 
                "tilesInversed"
            } :
            new []{ "round", "arenaIndex", "startSettingID"}
            )
        {
            this.experimentType = experimentType;
        }

        public void LogArenaSetting(int round, int arenaIndex, ArenaSetting arenaSetting)
        {
            if (experimentType == ExperimentType.EvolutionExperiment)
            {
                EvolutionArenaSetting evoSetting = (EvolutionArenaSetting) arenaSetting;
                AddLine(new [] {
                        round.ToString(), arenaIndex.ToString(), evoSetting.startSetting.id,
                        evoSetting.generationIndex.ToString(), evoSetting.populationIndex.ToString(),
                        evoSetting.evalRunIndex.ToString(),
                        evoSetting.inversedTiles.ToString()
                    }
                );    
            } else if (experimentType == ExperimentType.EvaluationExperiment)
            {
                AddLine(new[]
                {
                    round.ToString(), arenaIndex.ToString(), arenaSetting.startSetting.id
                });
            }
        }
    }
}