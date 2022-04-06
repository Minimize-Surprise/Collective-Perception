using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Assets;
using ExperimentHandler;
using MinimalSurprise;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Setup
{
    public class ParamsImporter
    {
        private string filePath;
        private Parameters master;
        
        public ParamsImporter(string filePath, Parameters master)
        {
            this.filePath = filePath;
            this.master = master;
        }

        public void ImportLogicWrapper()
        {
            if (!ImportFileExists()) return;

            var arr = YamlFileToKeyValueArr();
            ImportToMaster(arr);
        }
        
        public bool ImportFileExists()
        {
            return File.Exists(filePath);
        }

        public (string, string)[] YamlFileToKeyValueArr()
        {
            var lines = File.ReadAllLines(this.filePath);
            var tuples = new List<(string, string)>();
            for (int i = 0; i < lines.Length; i++)
            {
                var strippedLine = lines[i].Trim();
                if (strippedLine.StartsWith("#") ||
                    strippedLine.Equals("") ||
                    !strippedLine.Contains(":")) continue;

                if (strippedLine.Contains("#"))
                {
                    strippedLine = strippedLine.Split('#')[0];
                }

                var split = strippedLine.Split(':');
                var key = split[0].Trim();
                var val = split[1].Trim();
                
                tuples.Add((key, val));
            }

            return tuples.ToArray();
        }

        public void ImportToMaster((string, string)[] arr)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $">> YAML IMPORT: Found a yaml settings file {this.filePath} and starting master import:");
            var updatedParam = false;
            foreach (var tuple in arr)
            {
                sb.AppendLine($"   Setting master.{tuple.Item1} := {tuple.Item2}");
                try
                {
                    SetFieldValue(master, tuple.Item1, tuple.Item2);
                    updatedParam = true;
                }
                catch (Exception e)
                {
                    Debug.Log(sb.ToString());
                    Debug.LogError($"Exception setting master.{tuple.Item1}: {e}");
                    throw (e);
                }
            }

            if (!updatedParam) sb.AppendLine("   Found no param to be overwritten.");
            Debug.Log(sb.ToString());
        }
        
        public static object GetFieldValue(object src, string propName)
        {
            // Use example: GetFieldValue(master, prop).ToString()
            return src.GetType().GetField(propName).GetValue(src);
        }
        
        public void SetFieldValue(object src, string propName, object value)
        {
            object castValue = new object();
            var strVal = value.ToString();
            
            
            Type t = this.master.GetType();
            FieldInfo fi = t.GetField(propName);
            var typeName = fi.FieldType;

            if (typeName == typeof(string))
            {
                castValue = value.ToString();
            } else if (typeName == typeof(float))
            {
                castValue = float.Parse(value.ToString(), CultureInfo.InvariantCulture);
            } else if (typeName == typeof(int))
            {
                castValue = int.Parse(value.ToString());
            } else if (typeName == typeof(bool))
            {
                castValue = bool.Parse(value.ToString());
            }
            else if (typeName == typeof(Opinion))
            {
                castValue = strVal == "White" ? Opinion.White : Opinion.Black;
            }
            else if (typeName == typeof(ExperimentType))
            {
                castValue = strVal == "EvaluationExperiment" ? ExperimentType.EvaluationExperiment : ExperimentType.EvolutionExperiment;
            } else if (typeName == typeof(DecisionRule))
            {
                if (strVal.Equals("VoterModel"))
                    castValue = DecisionRule.VoterModel;
                else if (strVal.Equals("MajorityRule"))
                    castValue = DecisionRule.MajorityRule;
                else if (strVal.Equals("MinimizeSurprise"))
                    castValue = DecisionRule.MinimizeSurprise;
                else
                    Debug.Log($"Unknown Decision Rule in ParamsImporter: '{strVal}'");
            }
            else if (typeName == typeof(NeuralNetwork.FitnessCalculation))
            {
                castValue = strVal.Equals("AverageFitness")
                    ? NeuralNetwork.FitnessCalculation.AverageFitness : NeuralNetwork.FitnessCalculation.MinFitness;
            }
            else
            {
                Debug.Log($"Unknown type: '{typeName.ToString()}'");
            }
            
            src.GetType().GetField(propName).SetValue(src, castValue);
        }
    }
}