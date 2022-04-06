using System;
using System.Linq;
using Assets;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ArenaCreator {
    public static void ClearTestArea()
    {
        var arenas = GameObject.FindGameObjectsWithTag("arena");
        for (int i = 0; i < arenas.Length; i++)
        {
            Object.DestroyImmediate(arenas[i]);
        }
    }
    
    public static void CreateArenas(int nArenas, int arenaWidth, int arenaHeight)
    {
        var xOffset = 1.5f * (float) arenaWidth;

        for (int iArena = 0; iArena < nArenas; iArena++)
        {
            var currXOffset = xOffset * iArena;
            
            GameObject arena = new GameObject("Arena_"+iArena);
            arena.transform.position = new Vector3(currXOffset, 0, 0);
            arena.tag = "arena";

            // TODO: Handle mesh
            var (floor, mesh) = CreateFloor(currXOffset, arenaWidth, arenaHeight);
            floor.transform.SetParent(arena.transform);
            floor.tag = "floor";

            GameObject westWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westWall.transform.SetParent(arena.transform);
            westWall.name = "West Wall";
            westWall.transform.localPosition = new Vector3(-0.25f, 1.0f, (float)arenaHeight / 2.0f);
            westWall.transform.localScale = new Vector3(0.5f, 2.0f, arenaHeight);
            westWall.AddComponent<Rigidbody>();
            westWall.GetComponent<Rigidbody>().useGravity = false;
            westWall.GetComponent<Rigidbody>().isKinematic = true; // so the walls never fly away..

            GameObject eastWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastWall.transform.SetParent(arena.transform);
            eastWall.name = "East Wall";
            eastWall.transform.localPosition = new Vector3((float)arenaWidth + 0.25f, 1.0f, (float)arenaHeight / 2.0f);
            eastWall.transform.localScale = new Vector3(0.5f, 2.0f, arenaHeight);
            eastWall.AddComponent<Rigidbody>();
            eastWall.GetComponent<Rigidbody>().useGravity = false;
            eastWall.GetComponent<Rigidbody>().isKinematic = true;

            GameObject northWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northWall.transform.SetParent(arena.transform);
            northWall.name = "North Wall";
            northWall.transform.localPosition = new Vector3((float)arenaWidth / 2.0f, 1.0f, (float)arenaHeight + 0.25f);
            northWall.transform.localScale = new Vector3(arenaWidth + 1.0f, 2.0f, 0.5f);
            northWall.AddComponent<Rigidbody>();
            northWall.GetComponent<Rigidbody>().useGravity = false;
            northWall.GetComponent<Rigidbody>().isKinematic = true;

            GameObject southWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southWall.transform.SetParent(arena.transform);   
            southWall.name = "South Wall";
            southWall.transform.localPosition = new Vector3((float)arenaWidth / 2.0f , 1.0f, -0.25f);
            southWall.transform.localScale = new Vector3((float)arenaWidth + 1.0f, 2.0f, 0.5f);
            southWall.AddComponent<Rigidbody>();
            southWall.GetComponent<Rigidbody>().useGravity = false;
            southWall.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
    
    static (GameObject, Mesh) CreateFloor(float currXOffset, int arenaWidth, int arenaHeight)
    {
        GameObject go = new GameObject("Floor");
        MeshFilter mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer mr = go.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        var mesh = new Mesh();
        go.GetComponent<MeshFilter>().mesh = mesh;
        (go.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = mesh;
        var (vertices, triangles, uvs) =  CreateFloorShape(arenaWidth, arenaHeight, currXOffset);
        UpdateMesh(mesh, vertices, triangles, uvs);
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Default");
        return (go, mesh);
    }
    
    static (Vector3[] vertices, int[] triangles, Vector2[] uvs) CreateFloorShape(int arenaWidth, int arenaHeight, float currXOffSet)
    {
        var vertices = new Vector3[]
        {
            new Vector3(0 + currXOffSet, 0, 0),
            new Vector3(0 + currXOffSet, 0, arenaHeight),
            new Vector3(arenaWidth + currXOffSet, 0, 0),
            new Vector3(arenaWidth + currXOffSet, 0, arenaHeight)
        };
        
        var triangles = new int[] { 0, 1, 2, 2, 1, 3 };

        var uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };
        
        return (vertices, triangles, uvs);
    }

    static void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;  

        mesh.RecalculateNormals();
    }

    public static (Opinion[,], Texture2D) TiledTextureWrapper(int nCellsX, int nCellsY, float taskDifficulty, Opinion dominatingColor, 
        int arenaWidth, int arenaHeight)
    {
        Opinion[,] cellArray = CreateColorArrayForDifficulty(nCellsX,nCellsY, taskDifficulty, dominatingColor);
        var tex = CreateTiledTexture(arenaWidth, arenaHeight, nCellsX,nCellsY, cellArray);
        return (cellArray, tex);
    }

    public static Texture2D CreateTiledTexture(int arenaWidth, int arenaHeight, int nCellsX, int nCellsY, Opinion[,] cellArray)
    {
        var pixelFactor = 10;
        var arenaPixelHeight = arenaHeight * pixelFactor;
        var arenaPixelWidth = arenaWidth * pixelFactor;
        var tempTexture = new Texture2D(arenaPixelHeight, arenaPixelWidth, TextureFormat.ARGB32, false);

        var cellWidth = arenaPixelWidth / nCellsX;
        var cellHeight = arenaPixelHeight / nCellsY;
        
        for (int x = 0; x < arenaPixelWidth; x++)
        {
            for (int y = 0; y < arenaPixelHeight; y++)
            {
                int cellXIndex = x / cellWidth;
                int cellYIndex = y / cellHeight;
                
                // Handle indices because of integer division rounding
                // (remaining pixels not covered by opinion array get same color as last row / col)
                cellXIndex = Math.Min(cellXIndex, cellArray.GetLength(0) - 1);
                cellYIndex = Math.Min(cellYIndex, cellArray.GetLength(1) - 1);
                
                var cellOpinion = cellArray[cellXIndex, cellYIndex];
                
                tempTexture.SetPixel(y,x, cellOpinion == Opinion.Black ? Color.black : Color.white);
            }
        }
        tempTexture.Apply();
        #if UNITY_EDITOR
        AssetDatabase.CreateAsset(tempTexture, "Assets/Resources/Textures/tileTexture.asset");
        #endif
        return tempTexture;
    }

    public static Opinion[,] CreateColorArrayForDifficulty(int nCellsX, int nCellsY, double taskDifficulty, Opinion predominantColor)
    {
        var tileArray = new Opinion[nCellsX, nCellsY];

        // Fill array with base (predominant) color
        for (var i = 0; i < tileArray.GetLength(0); i++)
        {
            for (var j = 0; j < tileArray.GetLength(1); j++)
            {
                tileArray[i, j] = predominantColor;
            }
        }
    
        // Select indices of tiles to change
        var nTiles = nCellsX * nCellsY;
        var nToColor = Mathf.RoundToInt((float) taskDifficulty * nTiles);

        var selectedIndices = Misc.GetRandomIndices(nTiles, nToColor);

        // Color selected indices in the dominated color
        var dominatedColor = predominantColor == Opinion.Black ? Opinion.White : Opinion.Black;
        foreach (var index in selectedIndices)
        {
            int x = index / tileArray.GetLength(1);
            int y = index % tileArray.GetLength(1);
            tileArray[x, y] = dominatedColor;
        }

        return tileArray;
    }

    public static Opinion[,] InverseTiles(Opinion[,] tileMatrix)
    {
        var outMatrix = new Opinion[tileMatrix.GetLength(0), tileMatrix.GetLength(1)];
        for (int i = 0; i < tileMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < tileMatrix.GetLength(1); j++)
            {
                outMatrix[i, j] = tileMatrix[i, j] == Opinion.Black ? Opinion.White : Opinion.Black;
            }
        }

        return outMatrix;
    }

    public static void ReinitializeTilesForNewRoundInArena(Parameters master, StartSetting currentStartSetting, int arenaIndex)
    {
        var tex = CreateTiledTexture(master.arenaWidth, master.arenaHeight, master.nCellsX, master.nCellsY, currentStartSetting.tiles);
        master.SetFloorTextureInSingleArena(tex, arenaIndex);
    }

    public static void ReinitializeTilesForInversedRoundInArena(Parameters master, StartSetting currentStartSetting, int arenaIndex)
    {
        var inversedTiles = InverseTiles(currentStartSetting.tiles);
        var tex = CreateTiledTexture(master.arenaWidth, master.arenaHeight, master.nCellsX, master.nCellsY, inversedTiles);
        master.SetFloorTextureInSingleArena(tex, arenaIndex);
    }
}
