using Assets;
using Assets.Logging;
using MinimalSurprise;
using Setup;
using UnityEngine;

namespace ExperimentHandler
{
    public class EvaluationHandler : AbstrExperimentHandler
    {
        public EvaluationHandler(Parameters masterParams) : base(masterParams)
        {
        }

        private NetworkPair networkPair;
        private NeuralNetLogger nnLogger;

        public override void Initialize()
        {
            base.Initialize();
            if (master.decisionRule == DecisionRule.MinimizeSurprise)
            {
                InitNetworkPair();
            }
        }
        
        public override void InitLogs()
        {
            if (master.decisionRule == DecisionRule.MinimizeSurprise)
            {
                var filePath = master.fileDir + "/" + master.GetStartTimeID() + "_neural_net_log.csv";
                nnLogger = new NeuralNetLogger(filePath, master);
                nnLogger.Open();
            }
            
            var path = master.fileDir + "/" + master.GetStartTimeID() + "_arena_settings.csv";
            arenaSettingLogger = new ArenaSettingLogger(path, ExperimentType.EvaluationExperiment);
            arenaSettingLogger.Open();
            
        }
        
        public override void CloseLogs()
        {
            if (master.decisionRule == DecisionRule.MinimizeSurprise)
            {
                nnLogger.Close();    
            }
            arenaSettingLogger.Close();
        }
    
        public override void FlushLogs()
        {
            if (master.decisionRule == DecisionRule.MinimizeSurprise)
            {
                nnLogger.ForceFlush();    
            }
        }

        protected override void InitNewInitialSettingsCreator()
        {
            this.initialSettingsCreator = new EvaluationSettingsCreator(master);
            ((EvaluationSettingsCreator) initialSettingsCreator).InitStartSettings(master.sequentialSimRounds, master);
            SaveInitialSettingsJSON();
        }

        private void InitNetworkPair()
        {
            if (master.loadNetworkJson)
            {
                var jsonNetworkPair = NetworkPairJSONModel.ConstructFromJsonPath(master.networkJsonPath);
                networkPair = new NetworkPair(jsonNetworkPair, master.nParallelArenas, 
                    master.fitnessCalc, master.penalizeFitness);
                Debug.Log($">> Successfully loaded neural net from path: {master.networkJsonPath}");
            }
            else
            {
                networkPair = new NetworkPair(
                    new NeuralNetwork(NeuralNetwork.ActionNetTopology, master.beeCountPerArena, master.nParallelArenas, "eval_dec_net", 
                        master.fitnessCalc, master.penalizeFitness),
                    new NeuralNetwork(NeuralNetwork.PredictionNetTopology, NeuralNetwork.PredictionNetRecurrentLayers,
                        master.beeCountPerArena, master.nParallelArenas, true, "eval_pred_net", master.fitnessCalc, master.penalizeFitness),
                    "eval_netpair");
            }
        }

        public override void IntervalWrapper()
        {
            master.positionLogger.WriteLine();
        
            master.currentSimulationStepsInRound++;
            if(master.currentSimulationStepsInRound > master.simulationStepsPerRound)
            {
                master.CancelInvoke();
                var simulationRoundsLeft = master.currentSimulationRound < master.parallelSimRounds - 1;
                if (simulationRoundsLeft)
                {
                    StartNextSimulationRound();
                }
                else
                {
                    master.ExitExperiment();
                }
            }
        }

        public override string GetCurrentInfoString()
        {
            return "Mode: <b>Evaluation</b>";
        }

        public override Opinion AccessNeuralNetworks(BotController botController)
        {
            return this.networkPair.AccessNeuralNetworks(botController, master.botParams.maxMsgQueueSize, nnLogger);
        }

        public override ArenaSetting[] GetNextRoundArenaSettings()
        {
            var arenaSettings = new ArenaSetting[master.nParallelArenas];

            var startSettings = ((EvaluationSettingsCreator) initialSettingsCreator).startSettings;
            
            for (int iArena = 0; iArena < master.nParallelArenas; iArena++)
            {
                var settingIndex = master.currentSimulationRound * master.nParallelArenas + iArena;
                if (settingIndex >= startSettings.Length)
                    arenaSettings[iArena] = null;
                else
                    arenaSettings[iArena] = new ArenaSetting(startSettings[settingIndex]);
            }
            
            return arenaSettings;
        }

        private void StartNextSimulationRound()
        {
            Debug.Log($"Starting next simulation round (# {master.currentSimulationRound})", master);
            master.StartNextRound();
        }
        
    }
}