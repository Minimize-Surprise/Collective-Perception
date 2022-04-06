using UnityEngine;
using UnityEditor;
using Assets;
using ExperimentHandler;
using MinimalSurprise;
using UnityEngine.Assertions;

#if UNITY_EDITOR
public class CollectivePerceptionSettings : EditorWindow
#else
public class CollectivePerceptionSettings : Object
#endif
{
    //Arena Settings
    private bool init = true;
    public int arenaHeight = 40;
    public int arenaWidth = 40;

    // Collective Perception Ground Settings
    public int nCellsX, nCellsY;
    public float taskDifficulty;
    public Opinion dominatingColor;

    //Bee Settings
    public GameObject beePrefab;
    public int beeCount = 20;
    public float beeWidth = 0.7f;
    public float beeLength = 0.7f;
    public float beeSpeed = 1.0f;
    public float senseRange = 0.8f;
    public float communicationRange = 7.0f;
    public int minX;
    public int maxX;
    public int minY;
    public int maxY;
    private int digitCount;
    private System.DateTime startDateTime;
    
    // Parallel Environment Settings
    public int nParallelArenas;
    
    // Collective perception settings
    public float correctOpinionInitializationPercentage;
    public int maxMsgQueueSize = 4;
    public float g = 10f;
    public float sigma = 10f;

    //Sim Settings
    private float simTime;
    private int simCount = 1;
    private string fileDir;
    private DecisionRule decisionRule;

    private bool penalizeFitness;

    private NeuralNetwork.FitnessCalculation fitnessCalc;

    private ExperimentType experimentType;
    
    private bool loadNetworkJson;
    private string networkJsonPath;

    private bool loadInitialSettings;
    private string initialSettingsJsonPath;

    private int randomSeed = 42;

    internal Parameters master;
    private int tab = 0;
    private int oldtab;

    public CollectivePerceptionSettings()
    {
        //botCreator = new BotCreator();
        SetDefaultValues();
    }

    #if UNITY_EDITOR
    [MenuItem("Tools/Collective Perception Settings")] 
    public static void ShowWindow()
    {
        EditorWindow firstWindow = GetWindow<CollectivePerceptionSettings>("Collective Perception Settings");
        firstWindow.Focus();
        firstWindow.minSize = new Vector2(380f, 400f);
    }

    void OnGUI()
    {
        while (init)
        {
            master = GameObject.Find("Master").GetComponent<Parameters>();
            arenaHeight = master.arenaHeight;
            arenaWidth = master.arenaWidth;
            simTime = master.simTime;
            simCount = master.sequentialSimRounds;
            fileDir = master.fileDir;

            nCellsX = master.nCellsX;
            nCellsY = master.nCellsY;
            taskDifficulty = master.taskDifficulty;
            dominatingColor = master.dominatingColor;

            randomSeed = master.randomSeed;
            decisionRule = master.decisionRule;
            experimentType = master.experimentType;
            fitnessCalc = master.fitnessCalc;

            loadNetworkJson = master.loadNetworkJson;
            networkJsonPath = master.networkJsonPath;

            loadInitialSettings = master.loadInitialSettings;
            initialSettingsJsonPath = master.initialSettingsJsonPath;
            
            nParallelArenas = master.nParallelArenas;

            penalizeFitness = master.penalizeFitness;

            //botCreator.masterParams = master;
            
            init = false;
        }

        tab = GUILayout.Toolbar(tab, new string[] { "Build Arena", "Generate Bees", "Simulation Settings" });
        switch (tab)
        {
            case 0:
                ShowBuildArenaTab();
                break;
            case 1:
                ShowGenerateBeesTab();
                break;
            case 2:
                ShowSimConfigTab();
                break;
        }
        if (tab!=oldtab)
        { 
            GUIUtility.keyboardControl = 0;
        }
        oldtab = tab;
    }
    
    void ShowBuildArenaTab()
    {
        // Parallel Simulations
        GUILayout.Label("Parallel Simulation Settings", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        nParallelArenas = EditorGUILayout.IntField(new GUIContent("Number of parallel arenas", "for parallel simulation execution"), nParallelArenas);
        GUILayout.Label("arenas");
        GUILayout.EndHorizontal();
        GUILayout.Label("", EditorStyles.boldLabel);

        // Arena Dimensions
        GUILayout.Label("Input Arena Dimensions", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        arenaHeight = EditorGUILayout.IntField(new GUIContent("Width", "along the y-axis"), arenaHeight);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        arenaWidth = EditorGUILayout.IntField(new GUIContent("Length", "along the x-axis"), arenaWidth);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();
        
        // Build Button
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Build arena objects"))
        {
            HandleBuildButtonPress();
        }
        
        // B/W tile setup
        GUILayout.Label("", EditorStyles.boldLabel);
        GUILayout.Label("BW Tile Setup", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        nCellsX = EditorGUILayout.IntField(new GUIContent("X Number of cells", "along the x-axis"), nCellsX);
        GUILayout.Label("cells");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        nCellsY = EditorGUILayout.IntField(new GUIContent("Y Number of cells", "along the y-axis"), nCellsY);
        GUILayout.Label("cells");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        taskDifficulty = EditorGUILayout.Slider(new GUIContent("Task difficulty ρ", "percentage of dominated color tiles"), taskDifficulty, 0f, 0.5f);
        GUILayout.Label("");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        dominatingColor = (Opinion) EditorGUILayout.EnumPopup(new GUIContent("Dominating color", "predominant tile color"), dominatingColor);
        GUILayout.Label("");
        GUILayout.EndHorizontal();
        
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Create black/white tiles"))
        {
            HandleCreateTilesButton();
        }
        
        GUILayout.Label("\nYou need to recreate the arena or tiles to accept parameter changes.", EditorStyles.miniLabel);
    }
    #endif

    private void HandleCreateTilesButton()
    {
        var tilesArr = ArenaCreator.TiledTextureWrapper(nCellsX, nCellsY, taskDifficulty, dominatingColor, arenaWidth, arenaHeight);
        TileCreationSaveParams();
    }

    private void TileCreationSaveParams()
    {
        master.nCellsX = nCellsX;
        master.nCellsY = nCellsY;
        master.taskDifficulty = taskDifficulty;
        master.dominatingColor = dominatingColor;
    }

    public Opinion GetDominatingColor()
    {
        Assert.AreEqual(this.dominatingColor, master.dominatingColor);
        return this.dominatingColor;
    }

    public Opinion GetNonDominatingColor()
    {
        Assert.AreEqual(this.dominatingColor, master.dominatingColor);
        if (this.dominatingColor == Opinion.Black)
        {
            return Opinion.White;
        }
        else
        {
            return Opinion.Black;
        }
    }

    void HandleBuildButtonPress()
    {
        if (arenaHeight < 1.0f || arenaWidth < 1.0f)
        {
            #if UNITY_EDITOR
            EditorUtility.DisplayDialog("Invalid Size", "The minimum dimensions are 1x1.", "OK");
            #endif
            return;
        }
        BotCreator.ClearBees();
        ArenaCreator.ClearTestArea();
        ArenaCreator.CreateArenas(nParallelArenas, arenaWidth,arenaHeight);
        
        master = GameObject.Find("Master").GetComponent<Parameters>();
        master.arenaHeight = arenaHeight;
        master.arenaWidth = arenaWidth;
        master.nParallelArenas = nParallelArenas;

        var posVec = new Vector3((float) arenaWidth / 2.0f, Mathf.Max(arenaWidth, arenaHeight) * 1.10f,
            (float) arenaHeight / 2.0f);
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = posVec;

        var cameraHome = GameObject.Find("Main Camera Home");
        cameraHome.transform.position = posVec;

        var cameraCurrHome = GameObject.Find("Main Camera Current Home");
        cameraCurrHome.transform.position = posVec;
    }

#if UNITY_EDITOR
    void ShowGenerateBeesTab()
    {
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Bees"))
        {
            BotCreator.ClearBees();
        }
        GUILayout.Label("", EditorStyles.boldLabel);


        master = GameObject.Find("Master").GetComponent<Parameters>();
        beePrefab = Resources.Load<GameObject>("Prefabs/BEE");
        GUILayout.Label("Input Bee Parameters", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        beeWidth = EditorGUILayout.FloatField("Width of Bee", beeWidth);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        beeLength = EditorGUILayout.FloatField("Length of Bee", beeLength);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        beeSpeed = EditorGUILayout.FloatField("Forward Speed of Bee", beeSpeed);
        GUILayout.Label("units/sec");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        senseRange = EditorGUILayout.FloatField("Sensing Range of Bee", senseRange);
        GUILayout.Label("units (from centre)");
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        communicationRange = EditorGUILayout.FloatField("Communication Range", communicationRange);
        GUILayout.Label("units (from centre)");
        GUILayout.EndHorizontal();

        maxMsgQueueSize = EditorGUILayout.IntField("Max. Number of messages in Queue", maxMsgQueueSize);
        sigma = EditorGUILayout.FloatField("Sigma (exploration state mean duration)", sigma);
        g = EditorGUILayout.FloatField("g (dissemination state mean duration w/o feedback)", g);
        
        GUILayout.BeginHorizontal();
        correctOpinionInitializationPercentage = EditorGUILayout.Slider("Correctly initialized opinions", correctOpinionInitializationPercentage, 0, 1);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        beeCount = EditorGUILayout.IntField("Number of Bees", beeCount);
        GUILayout.Label("bees");
        GUILayout.EndHorizontal();

        GUILayout.Label("", EditorStyles.boldLabel);

        GUILayout.Label("Determine Bee Placement", EditorStyles.boldLabel);
        minX = EditorGUILayout.IntSlider("Minimum X Limit", minX, 0, maxX);
        maxX = EditorGUILayout.IntSlider("Maximum X Limit", maxX, 1, arenaWidth);
        minY = EditorGUILayout.IntSlider("Minimum Y Limit", minY, 0, maxY);
        maxY = EditorGUILayout.IntSlider("Maximum Y Limit", maxY, 1, arenaHeight);
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate"))
        {
            HandleGenerateBeesButton();
        }
        
        GUILayout.Label("\nYou need to regenerate the robots to accept parameter changes.", EditorStyles.miniLabel);
    }
    #endif

    private void SetDefaultValues()
    {
        beeCount = 30;
        maxX = arenaWidth;
        maxY = arenaHeight;
        correctOpinionInitializationPercentage = 0.5f;
    }

    private void HandleGenerateBeesButton()
    {
        if (minX < 0 || minY < 0 || maxX > master.arenaWidth || maxY > master.arenaHeight)
        {
            #if UNITY_EDITOR
            EditorUtility.DisplayDialog("Invalid Placement", "The Bees are outside the arena. Check the Master Parameters for dimensions", "OK");
            #endif
            return;
        }

        BotCreator.CreateBots(this);
        
        GameObject[] monas = GameObject.FindGameObjectsWithTag("MONA");
        master = GameObject.Find("Master").GetComponent<Parameters>();
        
        BeeGenerationSaveParams();
    }
    
    private void BeeGenerationSaveParams()
    {
        master.beeCountPerArena = beeCount;
        master.beeWidth = beeWidth;
        master.beeLength = beeLength;
        master.beeSpeed = beeSpeed;
        master.senseRange = senseRange;
        master.communicationRange = communicationRange;
        master.minX = minX;
        master.minY = minY;
        master.maxX = maxX;
        master.maxY = maxY;
        master.digitCount = digitCount;
        master.correctOpinionInitializationPercentage = correctOpinionInitializationPercentage;
    }
    
    #if UNITY_EDITOR
    void ShowSimConfigTab()
    {
        GUILayout.Label("Decision-Making Settings", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        decisionRule = (DecisionRule) EditorGUILayout.EnumPopup(new GUIContent("Decision Rule", "Decision rule to use by bots"), decisionRule);
        GUILayout.EndHorizontal();

        if (decisionRule == DecisionRule.MinimizeSurprise)
        {
            GUILayout.BeginHorizontal();
            experimentType = (ExperimentType) EditorGUILayout.EnumPopup(new GUIContent("Experiment Type", "Evolution or evaluation experiment"), experimentType);
            GUILayout.EndHorizontal();
        }

        if (decisionRule == DecisionRule.MinimizeSurprise &&
            experimentType == ExperimentType.EvolutionExperiment)
        {
            GUILayout.Label("", EditorStyles.boldLabel);
            GUILayout.Label("Minimize Surprise Evolution Settings", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            fitnessCalc = (NeuralNetwork.FitnessCalculation) EditorGUILayout.EnumPopup(new GUIContent("Fitness Calculation", "How to aggregate fitness for evaluation rounds?"), fitnessCalc);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            penalizeFitness = EditorGUILayout.Toggle(new GUIContent("Penalize Fitness", "Apply fitness penalty according to final correct opinion distribution?"), penalizeFitness);
            GUILayout.EndHorizontal();
        }
        
        
        GUILayout.Label("", EditorStyles.boldLabel);
        GUILayout.Label("Import Settings", EditorStyles.boldLabel);
        // InitialSettings Import
        loadInitialSettings = EditorGUILayout.Toggle("Restore initial settings?", loadInitialSettings);
        if (loadInitialSettings)
        {
            initialSettingsJsonPath = EditorGUILayout.TextField(new GUIContent("Load initial settings from", "Choose json file to load for initial settings"), initialSettingsJsonPath);
            if (GUILayout.Button("Browse"))
            {
                initialSettingsJsonPath =
                    EditorUtility.OpenFilePanelWithFilters("Choose initial settings json.", "Assets/04 Config",new [] { "JSON files","json"});
            }
        }
        
        // Neural Network Import
        if (decisionRule == DecisionRule.MinimizeSurprise && experimentType == ExperimentType.EvaluationExperiment)
        {
            loadNetworkJson = EditorGUILayout.Toggle("Load JSON network pair", loadNetworkJson);
            if (loadNetworkJson)
            {
                networkJsonPath = EditorGUILayout.TextField(new GUIContent("Load network from", "Choose json file to load for network pair"), networkJsonPath);
                if (GUILayout.Button("Browse"))
                {
                    networkJsonPath =
                        EditorUtility.OpenFilePanelWithFilters("Choose network pair json.", "Assets/04 Config",new [] { "JSON files","json"});
                }
            }
        }

        GUILayout.Label("", EditorStyles.boldLabel);
        GUILayout.Label("General Settings", EditorStyles.boldLabel);
        randomSeed = EditorGUILayout.IntField("Random seed", randomSeed);
        
        GUILayout.Label("", EditorStyles.boldLabel);
        GUILayout.Label("Simulation Configuration", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        simTime = EditorGUILayout.FloatField("Duration of Simulation", simTime);
        GUILayout.Label("seconds");
        GUILayout.EndHorizontal();

        if (experimentType == ExperimentType.EvaluationExperiment || 
            decisionRule == DecisionRule.MajorityRule ||
            decisionRule == DecisionRule.VoterModel
            )
        {
            GUILayout.BeginHorizontal();
            simCount = EditorGUILayout.IntField("Number of Iterations", simCount);
            GUILayout.Label("iterations");
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        fileDir = EditorGUILayout.TextField(new GUIContent("Save file to", "Array should be of the same dimensions as the arena"), fileDir);
        if (GUILayout.Button("Browse"))
        {
            fileDir = EditorUtility.SaveFolderPanel("Save to folder", "", "");
        }
        GUILayout.EndHorizontal();
        //fileName = EditorGUILayout.TextField("File Name", fileName);


        if (GUILayout.Button("Save Settings"))
        {
            master = GameObject.Find("Master").GetComponent<Parameters>();
            master.simTime = simTime;
            master.sequentialSimRounds = simCount;
            master.fileDir = fileDir;
            master.randomSeed = randomSeed;
            master.decisionRule = decisionRule;

            master.experimentType = experimentType;
            master.fitnessCalc = fitnessCalc;
            master.penalizeFitness = penalizeFitness;

            master.loadNetworkJson = loadNetworkJson;
            master.networkJsonPath = networkJsonPath;

            master.loadInitialSettings = loadInitialSettings;
            master.initialSettingsJsonPath = initialSettingsJsonPath;

            //master.fileName = fileName;
        }
        
        GUILayout.Label("\nYou need to save the settings to accept parameter changes.", EditorStyles.miniLabel);
    }
    #endif
}