using Assets.Logging;
using NUnit.Framework;

namespace Tests
{
    public class LoggerTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void FileLoggerTest()
        {
            FileLogger fl = new FileLogger("Assets/03 Results/test.txt");
            fl.Open();

            for (int i = 0; i < 1000; i++)
            {
                //fl.AddLine("Line "+i);
                fl.AddKeyValueEntry("my key", i.ToString());
            }
            
            fl.Close();
        }
        
        [Test]
        public void CSVLoggerTest()
        {
            var csvL = new CSVLogger("Assets/03 Results/test.csv", new []{"string", "int", "float"});
            csvL.Open();
            csvL.AddLine(new []{"hi", 1.ToString(), 1.3f.ToString("F2")});

            csvL.Close();
        }
    }
}
