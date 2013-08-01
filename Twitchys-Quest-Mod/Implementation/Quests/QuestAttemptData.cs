using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using NLua;
using TShockAPI;
using Terraria;

namespace QuestSystemLUA
{
	public class QuestAttemptData //TODO: Add number of quest attempts here.
    {
        public string QuestName;
        public bool Complete = false;
        public DateTime LastAttempt;

        public QuestAttemptData(string name, bool Complete, DateTime LastAttempt)
        {
            QuestName = name;
            this.Complete = Complete;
            this.LastAttempt = LastAttempt;
        }
    }
}