using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets
{
    public static class BotNameParsing
    {

        public static int ArenaFromBotName(string botName)
        {
            return int.Parse(botName.Split('_')[1]);
        }

        public static int BotIndexFromBotName(string botName)
        {
            return int.Parse(botName.Split('_')[2]);
        }
    }
}