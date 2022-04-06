using MinimalSurprise;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Assets.Logging
{
    public class JSONExporter : FileLogger
    {
        public JSONExporter(string filePath) : base(filePath)
        {
        }

        private bool arrayOpened = false;
        
        public void AddGenerationSummary(EvolutionLogJSONModel evolutionLogJsonModel)
        {
            var str = arrayOpened ? ",\n" : "[\n";
            arrayOpened = true;
            str += JsonConvert.SerializeObject(evolutionLogJsonModel, Formatting.Indented);
            AddLine(str);
        }

        public override void Close()
        {
            if (arrayOpened) AddLine("]");
            base.Close();
        }
    }
}