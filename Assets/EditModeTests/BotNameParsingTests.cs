using Assets;
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class BotNameParsingTests
    {

        [Test]
        public void BotNameParsingTest()
        {
            (string, int, int)[] botnames = new (string, int, int)[]
            {
                ("Bee_1_03", 1, 3),
                ("Bee_0_12", 0, 12),
                ("Bee_12_01", 12, 1),
            };

            foreach (var name in botnames)
            {
                Assert.AreEqual(
                    name.Item2,
                    BotNameParsing.ArenaFromBotName(name.Item1)
                    );
                Assert.AreEqual(
                    name.Item3,
                    BotNameParsing.BotIndexFromBotName(name.Item1)
                );
            }
        }
    }
}