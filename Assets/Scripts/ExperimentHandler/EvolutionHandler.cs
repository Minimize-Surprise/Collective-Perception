using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets;
using Assets.Logging;
using ExperimentHandler;
using MinimalSurprise;
using Setup;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EvolutionHandler : AbstrExperimentHandler
{
    public EvolutionHandler(Parameters masterParams) : base(masterParams)
    {
        PopulationSize = master.evoPopulationSize == -1 ? DefaultPopulationSize : master.evoPopulationSize;
        NGenerations = master.evoNGenerations == -1 ? DefaultNGenerations : master.evoNGenerations;
        RunsPerFitnessEval = master.evoRunsPerFitnessEval == -1 ? DefaultRunsPerFitnessEval : master.evoRunsPerFitnessEval;
        MutationProb = master.evoMutationProb < 0 ? DefaultMutationProb : master.evoMutationProb;
    }

    public EvolutionHandler(bool debugMode) : base(null)
    {
        debug = debugMode;
    }

    private bool debug = false;

    private int currentGen, currentEvalRun, currentPopulationIndex;
    
    public readonly int PopulationSize; //  Test: 4 - Real: 50;
    public readonly int NGenerations; // Test: 3 - Real: 100;
    public readonly int RunsPerFitnessEval;
    
    // >> TEST
    #if UNITY_EDITOR
    private const int DefaultPopulationSize = 4; //  Test: 4 - Real: 50;
    private const int DefaultNGenerations = 3; // Test: 3 - Real: 100;
    private const int DefaultRunsPerFitnessEval = 2;// Test: 2 - Real: 3;
    
    // >> REAL
    #else
    private const int DefaultPopulationSize = 50; //  Test: 4 - Real: 50;
    private const int DefaultNGenerations = 100; // Test: 3 - Real: 100;
    private const int DefaultRunsPerFitnessEval = 3;// Test: 2 - Real: 3;
    #endif
    
    private bool tilesInversed = false;

    public float MutationProb;
    
    private const int Elitism = 1;
    private const float DefaultMutationProb = 0.1f;
    private const float MutationLowerBound = -0.4f;
    private const float MutationUpperBound = 0.4f;
    
    private const int NStoreNetworks = 5;

    private Opinion[,] currentTiles;
    
    private NetworkPair[] networkPairs;
    private NetworkPair[] currentRoundNetworkPairs;

    private JSONExporter historyJSONExporter;
    private NeuralNetLogger nnLogger;

    private float lastGenerationBestFitness = -1;
    private bool generationTransitionPlanned = false;

    public override void Initialize()
    {
        base.Initialize();
        var nRounds = 2 * PopulationSize * NGenerations * RunsPerFitnessEval;
        if (!debug)
        {
            master.sequentialSimRounds = nRounds;
            master.parallelSimRounds = CalcParallelSimRounds(master.nParallelArenas);
        }
            
        
        if (debug)
            Debug.Log("Initializing Evolution Handler.");
        currentGen = 0;
        GenerateInitialNeuralNetPopulation();
    }

    private int CalcParallelSimRounds(int nParallelArenas)
    {
        var parallelRoundsPerGeneration = Mathf.CeilToInt((float) (2 * PopulationSize * RunsPerFitnessEval) / nParallelArenas);
        return parallelRoundsPerGeneration * NGenerations;
    }

    protected override void InitNewInitialSettingsCreator()
    {
        if (master == null) return;
        this.initialSettingsCreator = new EvolutionSettingsCreator(master);
        ((EvolutionSettingsCreator)initialSettingsCreator).InitStartSettings(NGenerations, RunsPerFitnessEval, master);
        SaveInitialSettingsJSON();
    }

    public override void InitLogs()
    {
        var filePath = master.fileDir + "/" + master.GetStartTimeID() + "_evolution_history.json";
        historyJSONExporter = new JSONExporter(filePath);
        historyJSONExporter.Open();
        
        filePath = master.fileDir + "/" + master.GetStartTimeID() + "_neural_net_log.csv";
        nnLogger = new NeuralNetLogger(filePath, master);
        nnLogger.Open();
        
        filePath = master.fileDir + "/" + master.GetStartTimeID() + "_arena_settings.csv";
        arenaSettingLogger = new ArenaSettingLogger(filePath, ExperimentType.EvolutionExperiment);
        arenaSettingLogger.Open();
    }
        
    public override void CloseLogs()
    {
        historyJSONExporter.Close();
        nnLogger.Close();
        arenaSettingLogger.Close();
    }
    
    public override void FlushLogs()
    {
        historyJSONExporter.ForceFlush();
        nnLogger.ForceFlush();
    }
    

    public override void IntervalWrapper()
    {
        master.positionLogger.WriteLine();
        
        master.currentSimulationStepsInRound++;
        if(master.currentSimulationStepsInRound > master.simulationStepsPerRound)
        {
            // We get here if the current round is over.
            master.CancelInvoke();
            if (master.run)
            {
                var simulationRoundsLeft = master.currentSimulationRound < master.parallelSimRounds - 1;
                if (simulationRoundsLeft)
                {
                    master.StartNextRound();
                }
                else
                {
                    if (generationTransitionPlanned)
                    {
                        ResetNetworksForNewRound();
                        PerformGenerationTransition();
                    }
                    master.ExitExperiment();
                }
            }
        }

        if (!master.run)
        {
            master.CancelInvoke();
        }
    }
    
    public void ProgressInEvolutionLogic()
    {
        if (!tilesInversed)
        {
            StartInverseTileRound();
        }
        else if (tilesInversed)
        {
            StartNextEvaluationRound();
        }
    }

    private void ResetNetworksForNewRound()
    {
        var firstRoundReset = master != null ? master.arenaSettings.Length == 0 : false;
        var currRoundOpinionOutcomes = firstRoundReset ? new float[0] : GetFinalCorrectOpinionPercentageFromMaster();
        for (int i = 0; i < networkPairs.Length; i++)
        {
            networkPairs[i].ResetForNextRound(currRoundOpinionOutcomes);
        }
    }
    
    private float[] GetFinalCorrectOpinionPercentageFromMaster()
    {
        if (debug)
            return new[] {0f, 0f, 0f};
        var arr = new float[master.nBlackOpinion.Length];

        for (int i = 0; i < master.nBlackOpinion.Length; i++)
        {
            if ((EvolutionArenaSetting) master.arenaSettings[i] == null) continue;
            
            var realDomColor = ((EvolutionArenaSetting) master.arenaSettings[i]).inversedTiles
                ? master.dominatingColor == Opinion.Black ? Opinion.White : Opinion.Black
                : master.dominatingColor;
            
            arr[i] = (float) master.nBlackOpinion[i] / (float) master.beeCountPerArena;
            if (realDomColor == Opinion.White)
                arr[i] = 1 - arr[i];
        }

        return arr;
    }

    /*
     * >> LOOP NESTING OVERVIEW
     * Generations (StartNewGeneration, increments generation counter, resets all below)
     *  - Populations (StartNextPopulationIndex, increment Population counter, reset all below)
     *  - eval rounds (StartNextEvalRound, increment eval round, reset all below)
     *  - initial / reversed (Start inversed tile setting, inverese the bool)
     */
    
    private void StartInverseTileRound()
    {
        tilesInversed = true;
        Debug.Log("  > Starting INVERSE Tile Round");
    }
    
    private void StartNextEvaluationRound()
    {
        tilesInversed = false;
        IncrementCurrentEvalRun();
    }

    private void IncrementCurrentEvalRun()
    {
        currentEvalRun++;
        Debug.Log($"  > Current evaluation run {currentEvalRun} / {RunsPerFitnessEval} finished.");
        if (currentEvalRun >= RunsPerFitnessEval)
        {
            StartNextPopulationIndex();
            IncrementCurrentPopulationIndex();
        }
    }
    
    private void StartNextPopulationIndex()
    {
        tilesInversed = false;
        currentEvalRun = 0;
    }

    private void IncrementCurrentPopulationIndex()
    {
        currentPopulationIndex++;
        Debug.Log($"  > Current population ID {currentPopulationIndex} / {PopulationSize} ID finished.");
        if (currentPopulationIndex >= PopulationSize)
        {
            currentPopulationIndex = 0;
            IncrementCurrentGenerationIndex();
        }
    }
    
    private void PerformGenerationTransition()
    {
        if (debug)
            Debug.Log(">> Generation Transition");
        EvalCurrentGeneration();
        CreateChildGeneration();
    }

    private void IncrementCurrentGenerationIndex()
    {
        generationTransitionPlanned = true;
        currentGen++;
        Debug.Log($"  > Current generation {currentGen} / {NGenerations} finished (population index overflow)");
        if (currentGen >= NGenerations)
        {
            if (!debug)
            {
                generationTransitionPlanned = true; // Final Evaluation
            }
                
            else
                Debug.Log("  > End (no more simulation rounds).");
        }
    }
    
    private void CreateChildGeneration()
    {
        if (debug)
        {
            Debug.Log("Creating child generation.");
            return;
        }

        var nextGenNetworks = new NetworkPair[PopulationSize];
        var cumErrors = GetNormalizedErrors();
        
        // Perform Elitism
        var minIndices = cumErrors.ArrayGetSortedIndices();
        for (int i = 0; i < Elitism; i++)
        {
            nextGenNetworks[i] = networkPairs[minIndices[i]].CreateCopy(master.nParallelArenas);
        }

        // Perform Proportionate Selection
        var propSelectedInd = ProportionateSelectionIndices(cumErrors, new int[]{}, PopulationSize - Elitism);

        for (int i = Elitism; i < nextGenNetworks.Length; i++)
            nextGenNetworks[i] = networkPairs[propSelectedInd[i-Elitism]].CreateCopy(master.nParallelArenas);

        Debug.Log(">>> This gen's performance: " + networkPairs.Select(x => x.id).ToArray().ArrayToString() + Environment.NewLine + networkPairs.Select(x => x.GetNormalizedError()).ToArray().ArrayToString());
        networkPairs = nextGenNetworks;
        Debug.Log(">>> Next gen network pairs: " + networkPairs.Select(x => x.id).ToArray().ArrayToString());
        
        // Mutate & Reset initial values
        for (int i = 0; i < networkPairs.Length; i++)
        {
            if (i >= Elitism)
                networkPairs[i].Mutate(MutationProb, MutationLowerBound, MutationUpperBound);
            networkPairs[i].ResetForNextGen();
        }
    }

    public static int[] ProportionateSelectionIndices(float[] errorVals, int[] ignoreIndices, int nSelect)
    {
        var (indArray, cumSumArray) = PrepareIndexCumSumArrays(errorVals, ignoreIndices);
        var fitnessSum = cumSumArray[cumSumArray.Length - 1];
        var selectIndices = new List<int>();

        for (int i = 0; i < nSelect; i++)
        {
            var r = Sampling.SampleFromUniformRange(0, fitnessSum);
            var cumSumIndex = cumSumArray.CumSumFindIndex(r);
            selectIndices.Add(indArray[cumSumIndex]);
        }

        return selectIndices.ToArray();
    }

    public static (int[], float[]) PrepareIndexCumSumArrays(float[] errorVals, int[] ignoreIndices)
    {
        var indices = new List<int>();
        var cumSumFitness = new List<float>();
        cumSumFitness.Add(0f);
        var fitnessSum = 0f;

        for (int i = 0; i < errorVals.Length; i++)
        {
            if (!ignoreIndices.Contains(i))
            {
                var fitnessVal = 1 - errorVals[i];
                indices.Add(i);
                fitnessSum += fitnessVal;
                cumSumFitness.Add(fitnessSum);
            }
        }
        
        return (indices.ToArray(), cumSumFitness.ToArray());
    }

    private void EvalCurrentGeneration()
    {
        if (debug)
        {
            Debug.Log("Evaluating & logging current generation.");
            return;
        }
        
        var netList = networkPairs.ToList();
        // Sort ascending (small to large errors)
        netList.Sort();
        
        var sortedNormalizedErrors = netList.Select(x => x.GetNormalizedError()).ToArray();
        var sortedFitness = sortedNormalizedErrors.Select(x => 1 - x).ToArray();
        //Debug.Log("Current (sorted) normalized errors: " + sortedNormalizedErrors.ArrayToString());
        var sortedIDs = netList.Select(x => x.id).ToArray();
        var sortedEvalRunTuples = netList.Select(x => x.GetEvaluationStorageTuples()).ToArray();

        var netsToStore = new ArraySegment<NetworkPair>(netList.ToArray(), 0, Mathf.Min(NStoreNetworks, netList.Count)).ToArray();

        lastGenerationBestFitness = 1 - netList.First().GetNormalizedError();
        
        var jsonModel = new EvolutionLogJSONModel(currentGen, lastGenerationBestFitness, sortedFitness,  sortedIDs, sortedEvalRunTuples, netsToStore);
        historyJSONExporter.AddGenerationSummary(jsonModel);
    } 

    private float[] GetNormalizedErrors()
    {
        return networkPairs.Select(x => x.GetNormalizedError()).ToArray();
    }

    private void GenerateInitialNeuralNetPopulation()
    {
        networkPairs = new NetworkPair[PopulationSize];
        var nBees = debug ? 5 : master.beeCountPerArena;
        var nParallelArenas = debug ? 3 : master.nParallelArenas;
        for (int iPop = 0; iPop < PopulationSize; iPop++)
        {
            networkPairs[iPop] = new NetworkPair(
                new NeuralNetwork(NeuralNetwork.ActionNetTopology, nBees, nParallelArenas, $"decNet#{iPop}", 
                    master != null? master.fitnessCalc: NeuralNetwork.FitnessCalculation.AverageFitness,
                    master.penalizeFitness),
                new NeuralNetwork(NeuralNetwork.PredictionNetTopology, NeuralNetwork.PredictionNetRecurrentLayers, 
                    nBees, nParallelArenas,true, $"predNet#{iPop}", 
                    master != null? master.fitnessCalc: NeuralNetwork.FitnessCalculation.AverageFitness,
                    master.penalizeFitness),
                $"#{iPop}");
        }
    }
    
    public override string GetCurrentInfoString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mode: <b>Evolution</b>");
        sb.AppendLine($"Current generation: {currentGen+1} / {NGenerations}");
        sb.AppendLine($"Current NN pair ID: {currentPopulationIndex+1} / {PopulationSize}");
        sb.AppendLine($"Current eval no: {currentEvalRun+1} / {RunsPerFitnessEval}");
        var tileStatus = tilesInversed ? "inversed" : "initial";
        sb.AppendLine($"Current tile status: {tileStatus}");

        if (Math.Abs(lastGenerationBestFitness - (-1)) > 0.005)
            sb.AppendLine($"Last generation's best fitness: {lastGenerationBestFitness:F2}");
            
        return sb.ToString();
    }

    public override Opinion AccessNeuralNetworks(BotController botController)
    {
        var popIndex = ((EvolutionArenaSetting) master.arenaSettings[botController.homeArena]).populationIndex;
        var currentNet = networkPairs[popIndex];
        return currentNet.AccessNeuralNetworks(botController, master.botParams.maxMsgQueueSize, nnLogger);
    }

    private EvolutionArenaSetting RequestNextSetting()
    {
        if (currentGen >= NGenerations)
        {
            return null;
        } 
            
        if (debug)
        {
            return new EvolutionArenaSetting(null, currentGen, currentEvalRun, currentPopulationIndex, tilesInversed);
        }
        else
        {
            var startSetting = ((EvolutionSettingsCreator) initialSettingsCreator).startSettings[currentGen, currentEvalRun];
            return new EvolutionArenaSetting(startSetting, currentGen, currentEvalRun, currentPopulationIndex, tilesInversed);    
        }
    }

    public override ArenaSetting[] GetNextRoundArenaSettings()
    {
        var nArenas = debug ? 3 : master.nParallelArenas;
        
        ResetNetworksForNewRound(); // needs to happen before generation transition (so errors are stored & accessible)
        if (generationTransitionPlanned)
        {
            PerformGenerationTransition();
            generationTransitionPlanned = false;
        }

        var arenaSettings = new EvolutionArenaSetting[nArenas];
        var thisRoundGeneration = -1;
        
        for (int i = 0; i < nArenas; i++)
        {
            var nextSetting = RequestNextSetting();
            if (nextSetting == null) break;
            var noGenerationChangeInSetting = (thisRoundGeneration == -1) || nextSetting.generationIndex == thisRoundGeneration;
            if (noGenerationChangeInSetting)
            {
                if (thisRoundGeneration == -1)
                    thisRoundGeneration = nextSetting.generationIndex;
                
                arenaSettings[i] = nextSetting;
                ProgressInEvolutionLogic();
            }
            else
            {
                generationTransitionPlanned = true;
                arenaSettings[i] = null;
            }
        }

        return arenaSettings;
    }
}