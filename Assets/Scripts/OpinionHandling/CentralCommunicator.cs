using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets
{
    public class OpinionMessage
    {
        public string sender;
        public Opinion opinion;
        public float timestamp;

        public OpinionMessage(string sender, Opinion opinion, float timestamp)
        {
            this.sender = sender;
            this.opinion = opinion;
            this.timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"Msg from {sender} at {timestamp:F2}: {opinion}";
        }
    }
    
    [Serializable]
    public class CentralCommunicator
    {
        public float commRadius;
        
        private Parameters masterParams;
        
        private Dictionary<string, BeeClust>[] beeScripts; 

        public void Initialize(Parameters masterParams)
        {
            this.masterParams = masterParams;
            beeScripts = this.CreateScriptDict();
            this.commRadius = masterParams.communicationRange;
        }

        private Dictionary<string, BeeClust>[] CreateScriptDict()
        {
            var dictArr = new Dictionary<string, BeeClust>[masterParams.nParallelArenas];
            for (int i = 0; i < masterParams.nParallelArenas; i++)
                dictArr[i] = new Dictionary<string, BeeClust>();
            
            for (int iArena = 0; iArena < masterParams.monas.GetLength(0); iArena++)
            {
                for (int jBot = 0; jBot < masterParams.monas.GetLength(1); jBot++)
                {
                    var bot = masterParams.monas[iArena, jBot];
                    dictArr[iArena].Add(bot.name, bot.GetComponent<BeeClust>());
                }
            }
            
            return dictArr;
        }

        public void SendOpinion(GameObject senderGO, Opinion sentOpinion)
        {
            var msg = new OpinionMessage(senderGO.name, sentOpinion, Time.time);
            
            var pos = senderGO.transform.position;
            var neighborNames = GetNeighborNames(pos, senderGO.name);
            
            foreach (var botName in neighborNames)
            {
                beeScripts[BotNameParsing.ArenaFromBotName(botName)]
                    [botName].ReceiveOpinionMessage(msg);
            }
        }

        private IEnumerable<string> GetNeighborNames(Vector3 pos, String requesterName)
        {
            var botLayerId = 8;
            int layerMask = 1 << botLayerId;
            var neigh = Physics.OverlapSphere(pos, commRadius, layerMask);
            var neighNames = neigh.Where(x => x.gameObject.CompareTag("MONA") && x.gameObject.name != requesterName).
                Select(x => x.gameObject.name);
            return neighNames;
        }

        public OpinionMessage[] QueryNeighborOpinions(GameObject requesterGO)
        {
            var pos = requesterGO.transform.position;
            var neighNames = GetNeighborNames(pos, requesterGO.name);

            var neighOpinionMessages = neighNames.Select(x =>
                 new OpinionMessage(x, beeScripts[BotNameParsing.ArenaFromBotName(requesterGO.name)][x].QueryOpinion(), Time.time)
            );

            return neighOpinionMessages.ToArray();
        }
    }
    
    
}