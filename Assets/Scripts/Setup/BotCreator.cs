using System;
using System.Linq;
using Assets;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BotCreator
{
    public static void CreateBots(CollectivePerceptionSettings cps)
    {
        var digitCount = (int)Mathf.Floor(Mathf.Log10(cps.beeCount) + 1);
        
        var initialPos = CreateInitialPositions(cps.beeCount, cps.minX, cps.maxX, cps.minY, cps.maxY, cps.beeWidth);
        
        for (int iArena = 0; iArena < cps.nParallelArenas; iArena++)
        {
            var parentArena = GameObject.Find("Arena_" + iArena);
            for (int i = 0; i < cps.beeCount; i++)
            {
                string id = i.ToString().PadLeft(digitCount, '0');
                string name = "Bee_" + iArena + "_" + id;
                GameObject bee = (GameObject)Object.Instantiate(cps.beePrefab, parentArena.transform, false);
                //bee.transform.localPosition = veryFirstStartSetting.positions[i].GetPosVector3();
                bee.transform.localPosition = initialPos[i];
                bee.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));//veryFirstStartSetting.positions[i].GetRotQuat();
                bee.transform.localScale = new Vector3(cps.beeWidth, 0.15f, cps.beeLength);
                bee.name = name;
                var botParams = new BotParams(cps.senseRange, cps.beeSpeed, 0, i, iArena);
                botParams.SetOpinionParams(Opinion.Black, cps.maxMsgQueueSize, cps.g, cps.sigma);
                bee.GetComponent<BeeClust>().SetBotParams(botParams);
                cps.master.botParams = botParams;
            }
        }
    }

    private static Vector3[] CreateInitialPositions(int beeCount, int minX, int maxX, int minY, int maxY, float beeWidth)
    {
        var arr = new Vector3[beeCount];
        var xStart = minX + beeWidth * 0.75f;
        var yStart = minY + beeWidth * 0.75f;
        
        var currX = xStart;
        var currY = yStart;
        for (int i = 0; i < beeCount; i++)
        {
            arr[i] = new Vector3(currX, 0.15f, currY);
            currX += 1.25f * beeWidth;
            if (currX > maxX - 0.75f * beeWidth)
            {
                currX = xStart;
                currY += 1.25f * beeWidth;
                if (currY > maxY - 0.75f * beeWidth)
                {
                    throw new Exception("Not enough space to align bots");
                }
            }
        }

        return arr;
    }

    public static void ReinitializeBotsForNewRound(Parameters masterParams, StartSetting currentStartSetting)
    {
        for (int iArena = 0; iArena < masterParams.monas.GetLength(0); iArena++)
        {
            ReinitializeBotsForNewRoundInArena(masterParams, currentStartSetting, iArena);
        }
    }

    public static void ReinitializeBotsForNewRoundInArena(Parameters masterParams, StartSetting currentStartSetting,
        int arenaIndex)
    {
        for (int jBot = 0; jBot < masterParams.monas.GetLength(1); jBot++)
        {
            var bot = masterParams.monas[arenaIndex, jBot];
            var script = bot.GetComponent<BeeClust>();
            var botId = script.botController.index;

            var pos = currentStartSetting.positions[botId].GetPosVector3();
            var rot = currentStartSetting.positions[botId].GetRotQuat();
            
            script.Reinitialize(pos, rot, currentStartSetting.opinions[botId]);   
        }
    }


    public static void ClearBees()
    {
        GameObject[] monas = GameObject.FindGameObjectsWithTag("MONA");
        foreach (GameObject mona in monas)
        {
            Object.DestroyImmediate(mona);
        }
    }

    public static Opinion[] CreateOpinionArrayForInitializationRatio(int nBees, Opinion dominatingColor, float correctInitPercentage)
    {
        var opinionArr = new Opinion[nBees];
        var nonDominatingColor = dominatingColor == Opinion.Black ? Opinion.White : Opinion.Black;

        for (int i = 0; i < nBees; i++)
        {
            opinionArr[i] = nonDominatingColor;
        }

        var nCorrect = Mathf.RoundToInt(nBees * correctInitPercentage);
        var selectedIndices = Misc.GetRandomIndices(nBees, nCorrect);
        
        foreach (var index in selectedIndices)
        {
            opinionArr[index] = dominatingColor;
        }
        
        return opinionArr;
    }

    public static Opinion[] CreateOpinionArrayForInitializationRatio(CollectivePerceptionSettings cps)
    {
        var nBees = cps.beeCount;
        var domCol = cps.GetDominatingColor();
        var correctInitPercentage = cps.correctOpinionInitializationPercentage;

        return CreateOpinionArrayForInitializationRatio(nBees, domCol, correctInitPercentage);
    }
    
    
}
