using System;
using Assets;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class DecisionMakingTests
    {
        
        [Test]
        public void VoterModelTest()
        {
            var realBlackPerc = 80f; // (4 out of 5)
            var opinionMsgs = new OpinionMessage[]
            {
                new OpinionMessage("bee1", Opinion.Black, 0f),
                new OpinionMessage("bee2", Opinion.Black, 1f),
                new OpinionMessage("bee3", Opinion.Black, 2f),
                new OpinionMessage("bee4", Opinion.Black, 3f),
                new OpinionMessage("bee5", Opinion.White, 4f),
            };

            var blackOutcomes = 0;
            var nRuns = 1000;
            for (int i = 0; i < nRuns; i++)
            {
                var result = DecisionMaking.VoterModelDecision(opinionMsgs, Opinion.Black);

                if (result == Opinion.Black)
                    blackOutcomes++;
            }

            var empiricalBlackPerc = (float) blackOutcomes / (float) nRuns * 100;
            
            Assert.IsTrue(Math.Abs(realBlackPerc - empiricalBlackPerc) < 5f);
            
            Debug.Log($"With {nRuns} and 80% black opinions, we have {blackOutcomes} black outcomes ({empiricalBlackPerc:F2}%)");
        }


        [Test]
        public void MajorityRuleTest()
        {
            var blackMajority = new OpinionMessage[]
            {
                new OpinionMessage("bee1", Opinion.Black, 0f),
                new OpinionMessage("bee2", Opinion.Black, 1f),
            };
            
            Assert.AreEqual(Opinion.Black,
                DecisionMaking.MajorityRuleDecision(blackMajority, Opinion.White));
            
            var whiteMajority = new OpinionMessage[]
            {
                new OpinionMessage("bee1", Opinion.White, 0f),
                new OpinionMessage("bee2", Opinion.White, 1f),
            };
            
            Assert.AreEqual(Opinion.White,
                DecisionMaking.MajorityRuleDecision(whiteMajority, Opinion.Black));
            
            var tieBlackWhite= new OpinionMessage[]
            {
                new OpinionMessage("bee1", Opinion.White, 0f),
                new OpinionMessage("bee2", Opinion.Black, 1f),
            };
            
            Assert.AreEqual(Opinion.White, 
                DecisionMaking.MajorityRuleDecision(tieBlackWhite, Opinion.White));
        }
        
    }
    
    
}