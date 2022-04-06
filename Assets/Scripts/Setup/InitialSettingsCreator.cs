using System;
using System.Collections.Generic;
using System.Text;
using Assets;
using ExperimentHandler;
using Newtonsoft.Json;
using Setup;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


public abstract class InitialSettingsCreator
{
    protected readonly ExperimentType experimentType;
    
    protected readonly int nBots;
    protected readonly int minX;
    protected readonly int maxX;
    protected readonly int minY;
    protected readonly int maxY;

    protected readonly float taskDifficulty;
    protected readonly int nCellsX;
    protected readonly int nCellsY;
    protected readonly Opinion dominatingOpinion;
    protected readonly float correctInitializationPercentage;

    protected readonly float botDiameter;

    public InitialSettingsCreator(int nBots, int minX, int maxX, int minY, int maxY,
        float taskDifficulty, int nCellsX, int nCellsY, Opinion dominatingOpinion,
        float correctInitializationPercentage,
        float botDiameter, ExperimentType experimentType)
    {
        this.experimentType = experimentType;
        this.nBots = nBots;
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        this.taskDifficulty = taskDifficulty;
        this.nCellsX = nCellsX;
        this.nCellsY = nCellsY;
        this.dominatingOpinion = dominatingOpinion;
        this.correctInitializationPercentage = correctInitializationPercentage;
        this.botDiameter = botDiameter;
    }

    public InitialSettingsCreator(Parameters master) : this(master.beeCountPerArena,
        master.minX, master.maxX, master.minY, master.maxY,
        master.taskDifficulty, master.nCellsX, master.nCellsY, master.dominatingColor,
        master.correctOpinionInitializationPercentage, master.beeWidth,
        master.experimentType)
    {
    }

    protected StartSetting CreateSingleStartSetting(string id)
    {
        return CreateSingleStartSetting(id, nBots, minX, maxX, minY, maxY,
            taskDifficulty, nCellsX, nCellsY, dominatingOpinion,
            correctInitializationPercentage,  botDiameter
        );
    }
    
    public static StartSetting CreateSingleStartSetting(string id, int nBots, int minX, int maxX, int minY, int maxY,
        float taskDifficulty, int nTilesX, int nTilesY, Opinion dominatingOpinion,
        float correctInitializationPercentage,
        float botDiameter)
    {
        var tiles = ArenaCreator.CreateColorArrayForDifficulty(
            nTilesX, nTilesY, taskDifficulty, dominatingOpinion);
        var opinions =
            BotCreator.CreateOpinionArrayForInitializationRatio(nBots, dominatingOpinion,
                correctInitializationPercentage);

        var positions = CreateNonCollidingBotPositions(nBots,
            minX, maxX, minY, maxY, botDiameter);

        return new StartSetting(id, tiles, positions, opinions);
    }

    private static BotPosition[] CreateNonCollidingBotPositions(int nBots, int minX, int maxX,
        int minY, int maxY, float botDiameter)
    {
        var posList = new List<BotPosition>();

        for (int i = 0; i < nBots; i++)
        {
            posList.Add(AddCollisionfreeBotPosition(posList,
                minX, maxX, minY, maxY, botDiameter));
        }

        return posList.ToArray();
    }

    private static BotPosition AddCollisionfreeBotPosition(List<BotPosition> posList, 
        int minX, int maxX, int minY, int maxY, float botDiameter)
    {
        var xLower = minX + botDiameter / 2;
        var xUpper = maxX - botDiameter / 2;
        var yLower = minY + botDiameter / 2;
        var yUpper = maxY - botDiameter / 2;

        var tries = 0;
        while (tries < 200)
        {
            var x = Sampling.SampleFromUniformRange(xLower, xUpper);
            var y = Sampling.SampleFromUniformRange(yLower, yUpper);
            var rot = Sampling.SampleFromUniformRange(0f, 360f);
            var newPos = new BotPosition(x, y, rot);
            if (!CheckForCollision(newPos, posList, botDiameter))
            {
                return newPos;
            }

            tries++;
        }

        throw new Exception($"Did not find a collision free bot position after {tries} tries.");
    }

    private static bool CheckForCollision(BotPosition newPos, List<BotPosition> posList, float botDiameter)
    {
        foreach (var position in posList)
        {
            if (position.CollidesWith(newPos, botDiameter))
                return true;
        }

        return false;
    }
    
    protected StartSetting ImportFirstStartSettingFromMasterGOs(Parameters master)
    {
        var bots = master.monas;

        var positions = new List<BotPosition>();
        var opinions = new List<Opinion>();
        foreach (var bot in bots)
        {
            var bc = bot.GetComponent<BeeClust>();
            opinions.Add(bc.botController.opinionHandler.currentOpinion);

            var transform = bc.transform;
            var position = transform.position;
            var pos = new BotPosition(
                position.x,
                position.z,
                transform.rotation.eulerAngles.y
            );
            
            positions.Add(pos);
        }
        
        var (tiles,tex) = ArenaCreator.TiledTextureWrapper(master.nCellsX, master.nCellsY, master.taskDifficulty, master.dominatingColor,
            master.arenaWidth, master.arenaHeight);
        master.SetFloorTexture(tex);
        
        return new StartSetting("initial", tiles, positions.ToArray(), opinions.ToArray());
    }

    public abstract InitialSettingsJsonModel ToJsonModel();
    
    public String ToJsonString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
    
    public String ToJsonStringViaModel()
    {
        return JsonConvert.SerializeObject(ToJsonModel(), Formatting.Indented);
    }

    public static InitialSettingsJsonModel FromJson(string jsonString)
    {
        var obj = JsonConvert.DeserializeObject<InitialSettingsJsonModel>(jsonString);
        return obj;
    }

    public void CompareParamsWithMaster(Parameters master)
    {
        if (master.experimentType != this.experimentType || 
            master.experimentType != this.experimentType )
        {
            master.run = false;
            throw new Exception($"Mismatch between master.experimentType = {master.experimentType} and imported json experimentType = {experimentType}." +
                                $" Applying json parameter.");
        }
        
        if (master.beeCountPerArena != nBots)
        {
            Debug.Log($"Mismatch between master.beeCount = {master.beeCountPerArena} and imported json nBots = {nBots}." +
                      $" Applying json parameter.");
            master.beeCountPerArena = nBots;
        }
        
        if (master.minX != minX)
        {
            Debug.Log($"Mismatch between master.minX = {master.minX} and imported json minX = {minX}." +
                      $" Applying json parameter.");
            master.minX = minX;
        }
        
        if (master.maxX != maxX)
        {
            Debug.Log($"Mismatch between master.maxX = {master.maxX} and imported json maxX = {maxX}." +
                      $" Applying json parameter.");
            master.maxX = maxX;
        }
        
        if (master.minY != minY)
        {
            Debug.Log($"Mismatch between master.minY = {master.minY} and imported json minY = {minY}." +
                      $" Applying json parameter.");
            master.minY = minY;
        }
        
        if (master.maxY != maxY)
        {
            Debug.Log($"Mismatch between master.maxY = {master.maxY} and imported json maxY = {maxY}." +
                      $" Applying json parameter.");
            master.maxY = maxY;
        }
        
        if (Math.Abs(master.taskDifficulty - taskDifficulty) > 0.005f)
        {
            Debug.Log($"Mismatch between master.taskDifficulty = {master.taskDifficulty} and imported json taskDifficulty = {taskDifficulty}." +
                      $" Applying json parameter.");
            master.taskDifficulty = taskDifficulty;
        }
        
        if (master.nCellsX != nCellsX)
        {
            Debug.Log($"Mismatch between master.nCellsX = {master.maxY} and imported json nCellsX = {nCellsX}." +
                      $" Applying json parameter.");
            master.nCellsX = nCellsX;
        }
        
        if (master.nCellsY != nCellsY)
        {
            Debug.Log($"Mismatch between master.nCellsY = {master.nCellsY} and imported json nCellsY = {nCellsY}." +
                      $" Applying json parameter.");
            master.nCellsY = nCellsY;
        }
        
        if (master.dominatingColor != dominatingOpinion)
        {
            Debug.Log($"Mismatch between master.dominatingColor = {master.dominatingColor} and imported json dominatingOpinion = {dominatingOpinion}." +
                      $" Applying json parameter.");
            master.maxY = maxY;
        }
        
        if (Math.Abs(master.correctOpinionInitializationPercentage - correctInitializationPercentage) > 0.005f)
        {
            Debug.Log($"Mismatch between master.correctOpinionInitializationPercentage = {master.correctOpinionInitializationPercentage} and imported json correctInitializationPercentage = {correctInitializationPercentage}." +
                      $" Applying json parameter.");
            master.correctOpinionInitializationPercentage = correctInitializationPercentage;
        }
        
        if (Math.Abs(master.beeWidth - botDiameter) > 0.005f)
        {
            Debug.Log($"Mismatch between master.beeWidth = {master.beeWidth} and imported json botRadius = {botDiameter}." +
                      $" Applying json parameter.");
            master.beeWidth = botDiameter;
        }
    }
    
    public abstract StartSetting GetVeryFirstStartSetting();
}

public class EvaluationSettingsCreator : InitialSettingsCreator
{
    public StartSetting[] startSettings;

    public EvaluationSettingsCreator(int nBots, int minX, int maxX, int minY, int maxY,
        float taskDifficulty, int nCellsX, int nCellsY, Opinion dominatingOpinion,
        float correctInitializationPercentage, float botDiameter) :
        base(nBots, minX, maxX, minY, maxY,
            taskDifficulty, nCellsX, nCellsY, dominatingOpinion,
            correctInitializationPercentage, botDiameter, ExperimentType.EvaluationExperiment)
    {
    }

    public EvaluationSettingsCreator(Parameters master) : base(master)
    {
    }

    /*
     * We need:
     * - Tiles, positions, opinions
     * --- for each of nSimulationRounds
     */

    public void InitStartSettings(int nRounds, Parameters master)
    {
        var startSettingsList = new List<StartSetting>();
        for (var i = 0; i < nRounds; i++)
        {
            startSettingsList.Add(CreateSingleStartSetting($"#{i}"));
        }

        this.startSettings = startSettingsList.ToArray();
    }

    public override StartSetting GetVeryFirstStartSetting()
    {
        return startSettings[0];
    }
    
    public override InitialSettingsJsonModel ToJsonModel()
    {
        var model = new InitialSettingsJsonModel(nBots,minX, maxX, minY, maxY, taskDifficulty, nCellsX, nCellsY, 
            dominatingOpinion, correctInitializationPercentage, botDiameter);
        model.MakeEvaluationSettings(startSettings);

        return model;
    }

    public void SetStartSettingsFromJson(StartSetting[] startSettings)
    {
        this.startSettings = startSettings;
    }
}

public class EvolutionSettingsCreator : InitialSettingsCreator
{
    public StartSetting[,] startSettings;

    public EvolutionSettingsCreator(int nBots, int minX, int maxX, int minY, int maxY,
        float taskDifficulty, int nCellsX, int nCellsY, Opinion dominatingOpinion,
        float correctInitializationPercentage, float botDiameter) :
        base(nBots, minX, maxX, minY, maxY,
            taskDifficulty, nCellsX, nCellsY, dominatingOpinion,
            correctInitializationPercentage, botDiameter,
            ExperimentType.EvolutionExperiment)
    {
    }

    public EvolutionSettingsCreator(Parameters master) : base(master)
    {
    }

    /*
     * We need:
     * - Tiles, positions, opinions
     * --- for each of nSimulationRounds
     * --- for each of nGenerations
     */
    public void InitStartSettings(int nGenerations, int nEvalRounds, Parameters master)
    {
        startSettings = new StartSetting[nGenerations, nEvalRounds];
        
        for (int i = 0; i < nGenerations; i++)
        {
            for (int j = 0; j < nEvalRounds; j++)
            {
                startSettings[i, j] = CreateSingleStartSetting($"#[{i},{j}]");    
            }
        }
    }

    public override StartSetting GetVeryFirstStartSetting()
    {
        return startSettings[0, 0];
    }

    public override InitialSettingsJsonModel ToJsonModel()
    {
        var model = new InitialSettingsJsonModel(nBots,minX, maxX, minY, maxY, taskDifficulty, nCellsX, nCellsY, 
            dominatingOpinion, correctInitializationPercentage, botDiameter);
        model.MakeEvolutionSettings(startSettings);
        return model;
    }

    public void SetStartSettingsFromJson(StartSetting[,] startSettings)
    {
        this.startSettings = startSettings;
    }
}

[System.Serializable]
public class StartSetting
{
    public StartSetting(string id, Opinion[,] tiles, BotPosition[] positions, Opinion[] opinions)
    {
        this.tiles = tiles;
        this.positions = positions;
        this.opinions = opinions;
        this.id = id;
    }

    public readonly Opinion[,] tiles;
    public readonly BotPosition[] positions;
    public readonly Opinion[] opinions;
    [SerializeField]
    public readonly string id;

    public new string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Start Setting (id: {id}) for {positions.Length} bots:");
        sb.AppendLine($"> Tiles:");
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            sb.Append($"  {i}: ");
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                sb.Append($"{tiles[i, j]} | ");
            }

            sb.Append(Environment.NewLine);
        }

        sb.AppendLine($"> Positions: {positions.ArrayToString()}");
        sb.AppendLine($"> Opinions: {opinions.ArrayToString()}");

        return sb.ToString();
    }
}

public class BotPosition
{
    public BotPosition(float x, float y, float rot)
    {
        this.x = x;
        this.y = y;
        this.rot = rot;
    }

    public readonly float x;
    public readonly float y;
    public readonly float rot;

    public bool CollidesWith(BotPosition otherPos, float botRadius)
    {
        var dist = Mathf.Sqrt(Mathf.Pow(x - otherPos.x, 2) + Mathf.Pow(y - otherPos.y, 2));
        return dist <= botRadius;
    }

    public Vector3 GetPosVector3()
    {
        return new Vector3(x, 0.15f, y);
    }

    public Quaternion GetRotQuat()
    {
        return Quaternion.Euler(new Vector3(0, rot, 0));
    }

    public override string ToString()
    {
        return $"BotPos: x:{x:F2}, y:{y:F2}, rot:{rot:F2}°";
    }
}