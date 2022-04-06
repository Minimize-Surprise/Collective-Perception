using System.IO;
using UnityEngine;
using UnityEditor;
using Assets;

#if UNITY_EDITOR
public class BeeClustSettings : EditorWindow

{
    //Arena Settings
    bool init = true;
    int height;
    int width;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Mesh mesh;
    string tempPath = "";
    //string phePath = "";
    string obsPath = "";
    public int[,] obsEntries;
    Texture2D tempTexture;
    public int[,] tempEntries;
    Texture2D pheTexture;

    //Bee Settings
    int beeCount = 1;
    float beeWidth = 0.8f;
    float beeLength = 0.8f;
    float beeSpeed = 2.0f;
    float beeTurn = 50.0f;
    float senseRange = 1.0f;
    float maxW = 60.0f;
    float theta = 1000.0f;
    int minX;
    int maxX;
    int minY;
    int maxY;
    int digitCount;
    System.DateTime startDateTime;

    //Sim Settings
    float simTime;
    int totalSimulationRounds = 1;
    string fileDir;

    Parameters master;
    int tab = 0;
    int oldtab;

    [MenuItem("Tools/BeeClust Settings")]
    public static void ShowWindow()
    {
        EditorWindow firstWindow = GetWindow<BeeClustSettings>("BeeClust Settings");
        firstWindow.Focus();
        firstWindow.minSize = new Vector2(380f, 400f);
    }

    void OnGUI()
    {

        while (init)
        {
            master = GameObject.Find("Master").GetComponent<Parameters>();
            height = master.arenaHeight;
            width = master.arenaWidth;
            obsPath = master.obsPath;
            tempPath = master.tempPath;
            simTime = master.simTime;
            totalSimulationRounds = master.sequentialSimRounds;
            fileDir = master.fileDir;
            //phePath = master.phePath;
            init = false;
        }

        tab = GUILayout.Toolbar(tab, new string[] { "Build Arena", "Generate Bees", "Simulation Settings" });
  

        switch (tab)
        {
            case 0:
                BuildArena();
                break;
            case 1:
                GenerateBees();
                break;
            case 2:
                SimConfig();
                break;
        }
        if (tab!=oldtab)
        { 
            GUIUtility.keyboardControl = 0;
        }
        oldtab = tab;
    }

    void BuildArena()
    {
        GUILayout.Label("Input Arena Dimensions", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        height = EditorGUILayout.IntField(new GUIContent("Width", "along the y-axis"), height);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        width = EditorGUILayout.IntField(new GUIContent("Length", "along the x-axis"), width);
        GUILayout.Label("units");
        GUILayout.EndHorizontal();

        GUILayout.Label("", EditorStyles.boldLabel);

        GUILayout.Label(new GUIContent("Select Files", "Seperate terms with ',' delimiter"), EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        obsPath = EditorGUILayout.TextField(new GUIContent("Obstacle Array", "Array should be of the same dimensions as the arena"), obsPath);
        if (GUILayout.Button("Browse"))
        {
            obsPath = EditorUtility.OpenFilePanel("Browse", "", "txt");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        tempPath = EditorGUILayout.TextField(new GUIContent("Temperature Array", "Array should be ten times the dimensions of the arena"), tempPath);
        if (GUILayout.Button("Browse"))
        {
            tempPath = EditorUtility.OpenFilePanel("Browse", "", "txt");
        }
        GUILayout.EndHorizontal();

        /*
        GUILayout.BeginHorizontal();
        phePath = EditorGUILayout.TextField(new GUIContent("Pheromone Array", "Array should be ten times the dimensions of the arena"), phePath);
        if (GUILayout.Button("Browse"))
        {
            phePath = EditorUtility.OpenFilePanel("Browse", "", "txt");
        }
        GUILayout.EndHorizontal();
        */

        GUILayout.Label("", EditorStyles.boldLabel);

        if (GUILayout.Button("Build"))
        {
            if (height < 1.0f || width < 1.0f)
            {
                EditorUtility.DisplayDialog("Invalid Size", "The minimum dimensions are 1x1.", "OK");
                return;
            }
            if (obsPath != "")
            {
                if (!CheckArrayDimensions(obsPath, width, height))
                {
                    EditorUtility.DisplayDialog("Incorrect Dimensions", "The Obstacle Array and Arena Dimensions do not match", "OK");
                    return;
                }
            }
            if (tempPath != "")
            {
                if (!CheckArrayDimensions(tempPath, width * 10, height * 10))
                {
                    EditorUtility.DisplayDialog("Incorrect Dimensions", "The Temperature Array and Arena Dimensions do not match", "OK");
                    return;
                }
            }
            ClearBees();
            ClearTestArea();
            CreateArena();
            if (obsPath != "")
            {
                GenerateObstacles();
            }



            if (tempPath != "")
            {
                BuildTempTexture();
            }
            else
            {
                Opinion[,] cellArray = ArenaCreator.CreateColorArrayForDifficulty(10, 10, 0.5d, Opinion.Black);
                ArenaCreator.CreateTiledTexture(width, height, 10, 10, cellArray);
                //BuildTempTextureNULL();
            }
            master = GameObject.Find("Master").GetComponent<Parameters>();
            master.arenaHeight = height;
            master.arenaWidth = width;
            master.obsPath = obsPath;
            master.tempPath = tempPath;
            //master.phePath = phePath;
            GameObject camera = GameObject.Find("Main Camera");
            camera.transform.position = new Vector3((float)width / 2.0f, Mathf.Max(width, height) * 1.10f, (float)height / 2.0f);
        }
    }
    
    void ClearTestArea()
    {
        if (GameObject.Find("Arena") != null)
        {
            DestroyImmediate(GameObject.Find("Arena"));
        }
        if (GameObject.Find("Obstacles") != null)
        {
            DestroyImmediate(GameObject.Find("Obstacles"));
        }
    }

    void ClearBees()
    {
        GameObject[] monas = GameObject.FindGameObjectsWithTag("MONA");
        foreach (GameObject mona in monas)
        {
            DestroyImmediate(mona);
        }
    }

    void CreateArena()
    {
        GameObject arena = new GameObject("Arena");

        GameObject floor = CreateFloor();

        GameObject westWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        westWall.name = "West Wall";
        westWall.transform.position = new Vector3(-0.25f, 1.0f, (float)height / 2.0f);
        westWall.transform.localScale = new Vector3(0.5f, 2.0f, height);
        westWall.AddComponent<Rigidbody>();
        westWall.GetComponent<Rigidbody>().useGravity = false;

        GameObject eastWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eastWall.name = "East Wall";
        eastWall.transform.position = new Vector3((float)width + 0.25f, 1.0f, (float)height / 2.0f);
        eastWall.transform.localScale = new Vector3(0.5f, 2.0f, height);
        eastWall.AddComponent<Rigidbody>();
        eastWall.GetComponent<Rigidbody>().useGravity = false;

        GameObject northWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        northWall.name = "North Wall";
        northWall.transform.position = new Vector3((float)width / 2.0f, 1.0f, (float)height + 0.25f);
        northWall.transform.localScale = new Vector3(width + 1.0f, 2.0f, 0.5f);
        northWall.AddComponent<Rigidbody>();
        northWall.GetComponent<Rigidbody>().useGravity = false;

        GameObject southWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        southWall.name = "South Wall";
        southWall.transform.position = new Vector3((float)width / 2.0f, 1.0f, -0.25f);
        southWall.transform.localScale = new Vector3((float)width + 1.0f, 2.0f, 0.5f);
        southWall.AddComponent<Rigidbody>();
        southWall.GetComponent<Rigidbody>().useGravity = false;

        floor.transform.SetParent(arena.transform);
        eastWall.transform.SetParent(arena.transform);
        westWall.transform.SetParent(arena.transform);
        northWall.transform.SetParent(arena.transform);
        southWall.transform.SetParent(arena.transform);
    }

    public static bool CheckArrayDimensions(string address, int length, int width)
    {
        StreamReader reader = new StreamReader(address);
        string fullData = reader.ReadToEnd();
        int i = 0, j = 0;
        int maxJ = 0;
        int[,] entries = new int[width,length];
        foreach (var row in fullData.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(','))
            {
                if (int.TryParse(col, out int result))
                {
                    if (j >= length || i >= width)
                    {
                        reader.Close();
                        return false;
                    }
                    entries[i, j] = int.Parse(col);
                    j++;
                }
            }
            i++;
            if(maxJ < j)
            {
                maxJ = j;
            }
        }
        if(i - 1 != width || maxJ != length)
        {
            return false;
        }
        reader.Close();
        return true;
    }

    void GenerateObstacles()
    {
        StreamReader reader = new StreamReader(obsPath);
        string fullData = reader.ReadToEnd();
        int i = 0, j = 0;
        obsEntries = new int[height, width];
        foreach (var row in fullData.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(','))
            {
                if (int.TryParse(col, out int result))
                {
                    obsEntries[i, j] = int.Parse(col);
                    j++;
                }
            }
            i++;
        }

        reader.Close();

        GameObject obstacles = new GameObject("Obstacles");
        for (int a = 0; a < height; a++)
        {
            for(int b = 0; b < width; b++)
            {
                if(obsEntries[a, b] == 1)
                {
                    GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obstacle.name = "Obstacle";
                    obstacle.transform.parent = obstacles.transform;
                    obstacle.transform.position = new Vector3((float)b + 0.5f, 0.5f, (float)height - 1 - a + 0.5f);
                }
            }
        }
    }

    GameObject CreateFloor()
    {
        GameObject go = new GameObject("Floor");
        MeshFilter mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer mr = go.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        mesh = new Mesh();
        go.GetComponent<MeshFilter>().mesh = mesh;
        (go.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = mesh;
        CreateShape();
        UpdateMesh();
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Default");
        return go;
    }

    void BuildTempTexture()
    {
        StreamReader reader = new StreamReader(tempPath);
        string fullData = reader.ReadToEnd();
        int i = 0, j = 0;
        tempEntries = new int[height * 10, width * 10];
        foreach (var row in fullData.Split('\n'))
        {
            j = 0;
            foreach (var col in row.Split(','))
            {
                if (int.TryParse(col, out int result))
                {
                    tempEntries[i, j] = int.Parse(col);
                    j++;
                }
            }
            i++;
        }

        reader.Close();

        tempTexture = new Texture2D(width * 10, height * 10, TextureFormat.ARGB32, false);

        for (int z = 0; z < height * 10; z++)
        {
            for (int x = 0; x < width * 10; x++)
            {
                tempTexture.SetPixel(x, z, Color.Lerp(Color.blue, Color.red, (float)tempEntries[height * 10 - z - 1, x] / 255.0f));
            }
        }
        tempTexture.Apply();
        AssetDatabase.CreateAsset(tempTexture, "Assets/Resources/Textures/tempTexture.asset");
    }

    void BuildTempTextureNULL()
    {
        tempEntries = new int[height * 10, width * 10];
        tempTexture = new Texture2D(width * 10, height * 10, TextureFormat.ARGB32, false);

        for (int z = 0; z < height * 10; z++)
        {
            for (int x = 0; x < width * 10; x++)
            {
                tempTexture.SetPixel(x, z, Color.Lerp(Color.blue, Color.red, 0));
            }
        }
        tempTexture.Apply();
        AssetDatabase.CreateAsset(tempTexture, "Assets/Resources/Textures/tempTexture.asset");
    }

    void CreateShape()
    {
        vertices = new Vector3[4];

        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, 0, height);
        vertices[2] = new Vector3(width, 0, 0);
        vertices[3] = new Vector3(width, 0, height);

        triangles = new int[] { 0, 1, 2, 2, 1, 3 };

        uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;  

        mesh.RecalculateNormals();
    }

    void GenerateBees()
    {
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Bees"))
        {
            ClearBees();
        }
        GUILayout.Label("", EditorStyles.boldLabel);


        master = GameObject.Find("Master").GetComponent<Parameters>();
        GameObject prefab = Resources.Load<GameObject>("Prefabs/BEE");
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
        beeTurn = EditorGUILayout.FloatField("Turn Speed of Bee", beeTurn);
        GUILayout.Label("degrees/sec");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        senseRange = EditorGUILayout.FloatField("Sensing Range of Bee", senseRange);
        GUILayout.Label("units (from centre)");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        maxW = EditorGUILayout.FloatField("Maximum Wait Time (w)", maxW);
        GUILayout.Label("seconds");
        GUILayout.EndHorizontal();

        theta = EditorGUILayout.FloatField("Theta Value", theta);

        GUILayout.BeginHorizontal();
        beeCount = EditorGUILayout.IntField("Number of Bees", beeCount);
        GUILayout.Label("bees");
        GUILayout.EndHorizontal();

        GUILayout.Label("", EditorStyles.boldLabel);

        GUILayout.Label("Determine Bee Placement", EditorStyles.boldLabel);
        minX = EditorGUILayout.IntField("Minimum X Limit", minX);
        maxX = EditorGUILayout.IntField("Maximum X Limit", maxX);
        minY = EditorGUILayout.IntField("Minimum Y Limit", minY);
        maxY = EditorGUILayout.IntField("Maximum Y Limit", maxY);
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate"))
        {
            startDateTime = System.DateTime.Now;

            digitCount = (int)Mathf.Floor(Mathf.Log10(beeCount) + 1);

            if (minX < 0 || minY < 0 || maxX > master.arenaWidth || maxY > master.arenaHeight)
            {
                EditorUtility.DisplayDialog("Invalid Placement", "The Bees are outside the arena. Check the Master Parameters for dimensions", "OK");
                return;
            }

            for (int i = 0; i < beeCount; i++)
            {
                string id = i.ToString().PadLeft(digitCount, '0');
                string name = "Bee" + id;
                int x = Random.Range(minX, maxX);
                int y = Random.Range(minY, maxY);
                Collider[] occupied = Physics.OverlapBox(new Vector3(x + 0.5f, 0.5f, y + 0.5f), new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity);
                if (occupied.Length == 0)
                {
                    GameObject bee = (GameObject)Instantiate(prefab, new Vector3(x + 0.5f, 0.15f, y + 0.5f), Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
                    bee.transform.localScale = new Vector3(beeWidth, 0.15f, beeLength);
                    bee.name = name;
                    var botParams = new BotParams(senseRange, beeSpeed, beeTurn, i, 1);
                    botParams.SetBeeClustParams(maxW, theta);
                    bee.GetComponent<BeeClust>().SetBotParams(botParams);
                }   
                else
                {
                    i--;
                }
                System.TimeSpan duration = System.DateTime.Now - startDateTime;
                if(duration.TotalSeconds >= 10.0f)
                {
                    EditorUtility.DisplayDialog("Error in bee placement", "The area may be too small for the number of bees", "OK");
                    return;
                }
            }
            GameObject[] monas = GameObject.FindGameObjectsWithTag("MONA");
            master = GameObject.Find("Master").GetComponent<Parameters>();
            master.beeCountPerArena = beeCount;
            master.beeWidth = beeWidth;
            master.beeLength = beeLength;
            master.beeSpeed = beeSpeed;
            master.beeTurn = beeTurn;
            master.senseRange = senseRange;
            master.minX = minX;
            master.minY = minY;
            master.maxX = maxX;
            master.maxY = maxY;
            master.digitCount = digitCount;
            
        }
    }

    void SimConfig()
    {
        GUILayout.Label("Configure Simulation", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        simTime = EditorGUILayout.FloatField("Duration of Simulation", simTime);
        GUILayout.Label("seconds");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        totalSimulationRounds = EditorGUILayout.IntField("Number of Iterations", totalSimulationRounds);
        GUILayout.Label("iterations");
        GUILayout.EndHorizontal();
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
            master.sequentialSimRounds = totalSimulationRounds;
            master.fileDir = fileDir;
            
            //master.fileName = fileName;
        }
    }
}

#endif
