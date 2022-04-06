using System;
using System.Globalization;
using System.IO;
using ExperimentHandler;
using UnityEngine;

public class FileLogger
{
    private string filePath;
    
    private StreamWriter sw;
    private bool isOpened = false;

    public FileLogger(string filePath)
    {
        this.filePath = filePath;
    }

    public void Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.filePath));
        this.sw = new StreamWriter(this.filePath, true);
        this.isOpened = true;
    }

    public virtual void Close()
    {
        this.sw.Close();
    }

    public void AddLine(string line)
    {
        if (!this.isOpened)
            Open();
        sw.WriteLine(line);
    }

    public void ForceFlush()
    {
        sw.Flush();
    }

    public void AddKeyValueEntry(string key, string value)
    {
        value = value.Contains(",") ? value.Replace(",", ".") : value;
        AddLine(key + " : " + value);
    }
}


public class ParameterLogger : FileLogger
{
    public ParameterLogger(string filePath) : base(filePath)
    {
    }

    public void WriteParamLogWrapper(Parameters master)
    {
        var propList = new[]
        {
            "startTimeID", "runID", 
            "nParallelArenas", "simTime", "sequentialSimRounds", "arenaWidth", "arenaHeight", "beeCountPerArena", "beeLength", 
            "beeWidth", "beeSpeed", "beeTurn", "senseRange", "minX", "maxX", "minY", "maxY",
            "communicationRange", "nCellsX", "nCellsY", "taskDifficulty", "dominatingColor",
            "correctOpinionInitializationPercentage", "decisionRule", "fitnessCalc", "penalizeFitness", "randomSeed",
            "loadInitialSettings", "initialSettingsJsonPath","loadNetworkJson", "networkJsonPath"
        };

        AddKeyValueEntry("timeScaleInitial", Time.timeScale.ToString(CultureInfo.InvariantCulture));
        AddKeyValueEntry("isBatchMode", Application.isBatchMode.ToString());
        
        foreach (var prop in propList)
        {
            AddKeyValueEntry(prop, GetFieldValue(master, prop).ToString());
        }

        if (master.experimentType == ExperimentType.EvolutionExperiment)
        {
            var evoPropList = new[]
            {
                "PopulationSize", "NGenerations", "RunsPerFitnessEval", "MutationProb"
            };
            
            foreach (var prop in evoPropList)
            {
                AddKeyValueEntry("evolutionHandler."+prop, 
                    GetFieldValue(master.experimentHandler, prop).ToString());    
            }
        }

        Close();
    }

    public static object GetFieldValue(object src, string propName)
    {
        return src.GetType().GetField(propName).GetValue(src);
    }
}
