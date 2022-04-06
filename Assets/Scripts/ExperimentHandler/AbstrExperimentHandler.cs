using Assets;
using Assets.Logging;
using Setup;
using UnityEngine;

namespace ExperimentHandler
{
    public abstract class AbstrExperimentHandler
    {

        protected Parameters master;
        public InitialSettingsCreator initialSettingsCreator;
        public ArenaSettingLogger arenaSettingLogger;
        
        public AbstrExperimentHandler(Parameters masterParams)
        {
            this.master = masterParams;
        }

        public abstract void IntervalWrapper();
        public abstract string GetCurrentInfoString();

        public virtual void Initialize()
        {
            this.InitInitialSettings();
        }

        public void InitInitialSettings()
        {
            if (master != null && master.loadInitialSettings)
            {
                var obj = InitialSettingsJsonModel.ConstructFromJsonPath(master.initialSettingsJsonPath);
                Debug.Log($">> Loaded initial settings from config file under path {master.initialSettingsJsonPath}");
                initialSettingsCreator = obj.ToRealInitialSettingsCreator();
                initialSettingsCreator.CompareParamsWithMaster(master);
            }
            else
            {
                InitNewInitialSettingsCreator();
            }
        }

        protected abstract void InitNewInitialSettingsCreator();

        public virtual void InitLogs()
        {
        }
        
        public virtual void CloseLogs()
        {
        }
        
        public virtual void FlushLogs()
        {
        }
        
        public abstract Opinion AccessNeuralNetworks(BotController botController);

        protected void SaveInitialSettingsJSON()
        {
            var filePath = master.fileDir + "/" + master.GetStartTimeID() + "_initial_settings.json";
            var jsonExporter = new JSONExporter(filePath);
            jsonExporter.Open();
            jsonExporter.AddLine(initialSettingsCreator.ToJsonStringViaModel());
            jsonExporter.Close();
        }

        public abstract ArenaSetting[] GetNextRoundArenaSettings();
    }

    public enum ExperimentType
    {
        EvaluationExperiment,
        EvolutionExperiment
    }
}