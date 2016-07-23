using System;
using TShockAPI;
using Terraria;
using System.Collections.Generic;

namespace QuestSystemLUA
{	
	public class StoredQPlayer
    {
        public string LoggedInName;
        public List<QuestAttemptData> QuestAttemptData = new List<QuestAttemptData>();

        public StoredQPlayer(string name, List<QuestAttemptData> playerdata)
        {
            LoggedInName = name;
            QuestAttemptData = playerdata;
        }
    }
}