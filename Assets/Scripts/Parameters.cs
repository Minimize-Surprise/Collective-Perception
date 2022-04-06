using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using Assets;
using Assets.Logging;
using ExperimentHandler;
using MinimalSurprise;
using Setup;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

public class Parameters : MonoBehaviour
{
    public int arenaHeight;
    public int arenaWidth;
    public string obsPath;
    public string tempPath;
    //public string phePath;
    public int[,] tempEntries;
    public bool run = false;
    public int beeCountPerArena;
    public float beeWidth;
    public float beeLength;
    public float beeSpeed;
    public float beeTurn;
    public float senseRange;
    public float communicationRange;
    public int minX;
    public int maxX;
    public int minY;
    public int maxY;
    public int digitCount;
    public float simTime;
    public int sequentialSimRounds;
    public int parallelSimRounds;
    public string fileDir;
    public Text infoText;
    string positionLogPath;
    //public string fileName;
    public int toggle = 1;
    public int currentSimulationRound = 0;
    public float currentRoundStartTime;
    public float simulationStepInterval = 1;
    public int simulationStepsPerRound;
    public int currentSimulationStepsInRound = 0;
    public GameObject[,] monas;

    public string startTimeID = "";
    public string runID = "not set";

    public int nParallelArenas;
    
    public int nCellsX, nCellsY;
    public float taskDifficulty;
    public Opinion dominatingColor;

    public float correctOpinionInitializationPercentage;
    public int[] nBlackOpinion;

    private Renderer[] floorRenderers;

    public BotParams botParams;

    public readonly CentralCommunicator communicationModule = new CentralCommunicator();
    
    public GizmoManager gizmoManager = new GizmoManager();

    public PositionOpinionLogger positionLogger;

    public int randomSeed;
    public DecisionRule decisionRule;

    public bool loadNetworkJson;
    public string networkJsonPath;

    public AbstrExperimentHandler experimentHandler;
    public ExperimentType experimentType;
    
    public bool loadInitialSettings;
    public string initialSettingsJsonPath;

    public Text arenaSettingsText;
    public ArenaSetting[] arenaSettings;
    public GameObject[] arenas;
    public NeuralNetwork.FitnessCalculation fitnessCalc;
    
    public int evoPopulationSize = -1; 
    public int evoNGenerations = -1;
    public int evoRunsPerFitnessEval = -1;
    public float evoMutationProb = -1f;
    public bool penalizeFitness = true;

    public bool configFromArgument = false;

    void Awake()
    {
        #if !UNITY_EDITOR
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        #endif
    
        experimentType = ((decisionRule == DecisionRule.MajorityRule || decisionRule == DecisionRule.VoterModel)
            ? ExperimentType.EvaluationExperiment :experimentType);
        HandleYAMLConfigImport();

        var sb = new StringBuilder();
        sb.Append($">> MasterParameters awakes | random seed: {randomSeed}");
        Random.InitState(randomSeed);

        monas = CreateMona2DArray();
        arenas = CreateArenaArray();
        sb.Append($" | Found {monas.Length} bots.");

        communicationModule.Initialize(this);
        
        parallelSimRounds = CalcParallelSimRounds(sequentialSimRounds, nParallelArenas);
        Debug.Log(sb.ToString());
    }

    private void HandleYAMLConfigImport()
    {
        var pathFromArgs = Misc.GetArgByName("-params");

        var path = Path.Combine(Application.dataPath, "StreamingAssets", "master_params.yaml"); 
        if (pathFromArgs != null) {
            path = Path.Combine(Application.dataPath, "StreamingAssets", pathFromArgs);
            configFromArgument = true;
        }
        
        if (!File.Exists(path))
        {
            Debug.LogError("Config file not found under path: " + path);
            Application.Quit();
        }
        
        var importer = new ParamsImporter(path, this);
        importer.ImportLogicWrapper();
    }

    private GameObject[,] CreateMona2DArray()
    {
        var allMonas = GameObject.FindGameObjectsWithTag("MONA");
        var monas2d = new GameObject[nParallelArenas, allMonas.Length / nParallelArenas];
        foreach (var bot in allMonas)
        {
            var arenaIndex = BotNameParsing.ArenaFromBotName(bot.name);//bcScript.botController.homeArena;
            var botIndex = BotNameParsing.BotIndexFromBotName(bot.name); //bcScript.botController.index;
            monas2d[arenaIndex, botIndex] = bot;
        }

        return monas2d;
    }

    private GameObject[] CreateArenaArray()
    {
        var arenasByTag = GameObject.FindGameObjectsWithTag("arena");
        var arenaArr = new GameObject[nParallelArenas];
        for (int iArena = 0; iArena < arenasByTag.Length; iArena++)
        {
            var ind = int.Parse(arenasByTag[iArena].name.Split('_')[1]);
            arenaArr[ind] = arenasByTag[iArena];
        }

        return arenaArr;
    }

    public static int CalcParallelSimRounds(int seqRounds, int nParallelArenas)
    {
        return Mathf.CeilToInt((float) seqRounds / (float) nParallelArenas);
    }

    private void OnDestroy()
    {
        CloseLogs();
    }

    private void CloseLogs()
    {
        positionLogger?.Close();
        experimentHandler?.CloseLogs();
    }

    private void FlushLogs()
    {
        positionLogger.ForceFlush();
        experimentHandler.FlushLogs();
    }
    
    public string GetStartTimeID()
    {
        if (startTimeID == "")
        {
            startTimeID = System.DateTime.Now.ToString("yy-MM-dd_HH-mm-ss"); 
        }

        return startTimeID;
    }

    // Start is called before the first frame update
    void Start()
    {
        #if !UNITY_EDITOR
        var sb = new StringBuilder();
        sb.AppendLine($"🚀 Starting experiment at `{GetStartTimeID()}`");
        sb.AppendLine($"RunID: `{runID}`");
        sb.AppendLine($"Config from argument: `{configFromArgument}`");
        StartCoroutine(TelegramBot.SendMessageCoroutine(sb.ToString()));
        #endif

        simulationStepsPerRound = (int)(simTime / simulationStepInterval);

        HandleBatchMode();
        
        InitExperimentHandler();
        
        InitializeLogs();
        StartNextRound(false); // Initialize FIRST ever round
        run = true;
    }

    private void HandleBatchMode()
    {
        if (Application.isBatchMode)
        {
            var batchTimeScale = 150f;
            Debug.Log($"Application ws started in batch mode. Setting timescale to {batchTimeScale}.");
            Time.timeScale = batchTimeScale;
        }
    }

    private void InitExperimentHandler()
    {
        if (decisionRule == DecisionRule.MajorityRule ||
            decisionRule == DecisionRule.VoterModel ||
            experimentType == ExperimentType.EvaluationExperiment)
        {
            experimentHandler = new EvaluationHandler(this);    
        }
        else
        {
            experimentHandler = new EvolutionHandler(this);
        }

        experimentHandler.Initialize();
    }

    private void InitializeLogs()
    {
        WriteParameterLog();
        InitializePositionLog();
        this.experimentHandler.InitLogs();
    }
    
    private void WriteParameterLog()
    {
        string pathParametersLog = fileDir + "/" + GetStartTimeID() + "_Parameters" + ".txt";
        var paramLog = new ParameterLogger(pathParametersLog);
        paramLog.WriteParamLogWrapper(this);
    }

    internal void SetFloorTexture(Texture2D tileTexture)
    {
        for (int i = 0; i < nParallelArenas; i++)
        {
            SetFloorTextureInSingleArena(tileTexture, i);
        }
    }

    internal void SetFloorTextureInSingleArena(Texture2D tileTexture, int arenaIndex)
    {
        FloorRenderersNullCheck();
        floorRenderers[arenaIndex].material.mainTexture = tileTexture;
        floorRenderers[arenaIndex].material.SetColor("_Color", Color.white);
    }

    internal void FloorRenderersNullCheck()
    {
        if (floorRenderers == null)
        {
            floorRenderers = new Renderer[nParallelArenas];
            var floors = GameObject.FindGameObjectsWithTag("floor");
            foreach (var floor in floors)
            {
                var floorInd = int.Parse(floor.transform.parent.name.Split('_')[1]);
                floorRenderers[floorInd] = floor.GetComponent<Renderer>();
            }
        }
    }

    private void InitializePositionLog()
    {
        positionLogPath = fileDir + "/" + GetStartTimeID() + "_PositionsOpinions" + ".csv";
        positionLogger = new PositionOpinionLogger(positionLogPath, this);
        positionLogger.Open();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (run)
        {
            UpdateCurrentInfoString();
        }
        HandleButtonsForTimescaling();
    }
    
    private static void HandleButtonsForTimescaling()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 5;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 10;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Time.timeScale = 30;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Time.timeScale = 50;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Time.timeScale = 75;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Time.timeScale = 100;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(0);
        }
    }

    private void UpdateCurrentInfoString()
    {
        var s = new StringBuilder();
        s.AppendLine($"Decision Rule: {decisionRule}");
        if (decisionRule == DecisionRule.MinimizeSurprise)
            s.AppendLine("Network parameters " + (loadNetworkJson? "Imported from JSON" : "Newly created"));
        
        s.AppendLine($"Elapsed time overall {Time.time:F2}");
        s.AppendLine("Parallel run: " + (currentSimulationRound + 1) + " / " + parallelSimRounds);
        s.AppendLine($"   over sequential iterations {(currentSimulationRound*nParallelArenas)+1}...{(currentSimulationRound+1)*nParallelArenas} / {sequentialSimRounds}");
        s.AppendLine("Elapsed time in current run: " +
             (Time.time - currentRoundStartTime).ToString("F2") + " sec / " + simulationStepsPerRound + " sec");
        s.AppendLine();
        s.AppendLine(experimentHandler.GetCurrentInfoString());
        s.AppendLine("Initial round settings: " + (loadInitialSettings? "Imported from JSON" : "Newly created"));
        s.AppendLine();
        s.AppendLine("Time scale: " + Time.timeScale);
        s.AppendLine();
        s.AppendLine("Task difficulty: " + taskDifficulty.ToString("F2"));
        s.AppendLine("Correct initialization percentage: " + correctOpinionInitializationPercentage);
        s.AppendLine("Correct opinion: " + dominatingColor);
        s.AppendLine();
        s.AppendLine("Current opinion distribution:");
        s.AppendLine("Number of bots per arena: " + beeCountPerArena);
        for (int i = 0; i < nParallelArenas; i++)
        {
            var nBlack = nBlackOpinion[i];
            var nWhite = beeCountPerArena - nBlack;
            s.AppendLine($"- Arena # {i}: Black: {nBlack} ({(float) nBlack / beeCountPerArena * 100:F1}%) - White: {nWhite} ({(float) nWhite / beeCountPerArena * 100:F1}%)");    
        }
        
        infoText.text = s.ToString();
    }

    public void ExitExperiment()
    {
        Debug.Log("Experiment End (no more simulation rounds).", this);
        #if !UNITY_EDITOR
        var sb = new StringBuilder();
        sb.AppendLine($"🏁 Experiment finished at `{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}`");
        sb.AppendLine($"RunID: `{runID}`");
        sb.AppendLine($"(start was at `{GetStartTimeID()}`)");
        StartCoroutine(TelegramBot.SendMessageCoroutine(sb.ToString()));
        #endif
        run = false;
        Application.Quit();
    }
    
    void IntervalWrapper()
    {
        experimentHandler.IntervalWrapper();
    }
    
    public void UpdateCurrentOpinionDistribution(Opinion newChangedOpinion, int arenaIndex)
    {
        int deltaBlack = newChangedOpinion == Opinion.Black ? 1 : -1;
        nBlackOpinion[arenaIndex] += deltaBlack;
    }

    private int[] InitNBlackOpinionArr()
    {
        var blackPercentage = (dominatingColor == Opinion.Black)
            ? correctOpinionInitializationPercentage
            : (1 - correctOpinionInitializationPercentage);
        
        return (new int[nParallelArenas]).Populate(
                Mathf.RoundToInt(beeCountPerArena * blackPercentage)
            );
    }

    public void StartNextRound(bool progressSimRound = true)
    {
        if (progressSimRound)
        {
            currentSimulationRound++;
            currentSimulationStepsInRound = 0;
            currentRoundStartTime = Time.time;
            
            FlushLogs();
        }
        
        arenaSettings = experimentHandler.GetNextRoundArenaSettings();
        nBlackOpinion = InitNBlackOpinionArr();
        if (arenaSettings == null) return;
        for (int iArena = 0; iArena < nParallelArenas; iArena++)
        {
            var arenaSetting = arenaSettings[iArena];
            if (arenaSetting == null)
            {
                ArenaSetActive(iArena, false);
            }
            else
            {
                if (!arenas[iArena].activeSelf) ArenaSetActive(iArena, true);
                BotCreator.ReinitializeBotsForNewRoundInArena(this, arenaSetting.startSetting , iArena);
                if (experimentType == ExperimentType.EvolutionExperiment &&
                    ((EvolutionArenaSetting) arenaSetting).inversedTiles)
                    ArenaCreator.ReinitializeTilesForInversedRoundInArena(this, arenaSetting.startSetting, iArena);
                else
                {
                    ArenaCreator.ReinitializeTilesForNewRoundInArena(this, arenaSetting.startSetting, iArena);
                }
            }
        }

        LogArenaSettings();
        RefreshArenaSettingsInfoText();
        InvokeRepeating("IntervalWrapper", 0.0f, 1.0f);
    }

    private void LogArenaSettings()
    {
        for (int i = 0; i < arenaSettings.Length; i++)
        {
            if (arenaSettings[i] != null) // might be deactivated
            {
                experimentHandler.arenaSettingLogger.LogArenaSetting(currentSimulationRound, i, arenaSettings[i]);    
            }
        }
    }

    private void RefreshArenaSettingsInfoText()
    {
        var sb = new StringBuilder();
        if (experimentType == ExperimentType.EvolutionExperiment)
            sb.AppendLine("<i>Order: (start setting, gen, pop, eval run, inversed)</i>");
        else
            sb.AppendLine("<i>Order: (start setting)</i>");
        
        
        for (int i = 0; i < nParallelArenas; i++)
        {
            var stateString = arenaSettings[i] == null ? "deactivated" : arenaSettings[i].ToString(); 
            sb.AppendLine($"Arena #{i}: {stateString}");
        }
        arenaSettingsText.text = sb.ToString();
    }

    private void ArenaSetActive(int arenaIndex, bool activeValue)
    {
        arenas[arenaIndex].SetActive(activeValue);
    }

    public Opinion AccessNeuralNetworks(BotController botController)
    {
        return experimentHandler.AccessNeuralNetworks(botController);
    }
}
