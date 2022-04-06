using System.Linq;
using Assets;

using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;


namespace Tests
{
    public class MessageStorageTests
    {
        [Test]
        public void TestDuplicates()
        {
            var msg1 = new OpinionMessage("bee1", Opinion.Black, 0f);
            var msg2 = new OpinionMessage("bee1", Opinion.White, 1f);
            var msg3 = new OpinionMessage("bee2", Opinion.White, 2f);

            var ms = new Assets.MessageStorage(5);
            ms.EnqueueNewMessage(msg1);
            ms.EnqueueNewMessage(msg2);
            ms.EnqueueNewMessage(msg3);
            
            Assert.AreEqual(2, ms.GetMessageArray().Length);
            Assert.AreEqual(Opinion.Black, ms.GetMessageArray()[0].opinion);
            Assert.AreEqual("bee1", ms.GetMessageArray()[0].sender);
            Assert.AreEqual("bee2", ms.GetMessageArray()[1].sender);
        }

        [Test]
        public void TestQueueSize()
        {
            var ms = new Assets.MessageStorage(4);
            OpinionMessage[] msgs = new OpinionMessage[5];
            for (int i = 0; i < 5; i++) // i in [0, 1, ..., 4]
            {
                msgs[i] = new OpinionMessage("bee" + i, Opinion.Black, (float) i);
                ms.EnqueueNewMessage(msgs[i]);
            }
            
            Assert.AreEqual("bee1", ms.GetMessageArray()[0].sender);
            Assert.AreEqual("bee4", ms.GetMessageArray()[3].sender);
        }

    }
}