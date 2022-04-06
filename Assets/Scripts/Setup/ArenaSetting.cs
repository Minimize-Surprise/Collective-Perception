using System;
using System.Text;
using UnityEngine;

namespace Setup
{
    [System.Serializable]
    public class ArenaSetting
    {
        public StartSetting startSetting;

        public ArenaSetting(StartSetting startSetting)
        {
            this.startSetting = startSetting;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (startSetting != null)
                sb.Append(" - " +startSetting.id);
            return sb.ToString();
        }
    }

    [System.Serializable]
    public class EvolutionArenaSetting : ArenaSetting
    {
        [SerializeField]
        public int generationIndex;
        [SerializeField]
        public int evalRunIndex;
        [SerializeField]
        public int populationIndex;

        [SerializeField]
        public bool inversedTiles;

        public EvolutionArenaSetting(StartSetting startSetting, int generationIndex, int evalRunIndex, int populationIndex,
            bool inversedTiles) : base(startSetting)
        {
            this.generationIndex = generationIndex;
            this.evalRunIndex = evalRunIndex;
            this.populationIndex = populationIndex;
            this.inversedTiles = inversedTiles;
        }

        public override string ToString()
        {
            var s = $" - {generationIndex} - {populationIndex} - {evalRunIndex} - {inversedTiles}";
            return base.ToString() + s;
        }
    }
}