using System;
using TShockAPI;
using Terraria;
using System.Collections.Generic;

namespace QuestSystemLUA
{	
	public class QPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public Item[] Inventory { get { return TSPlayer.TPlayer.inventory; } }
        public StoredQPlayer MyDBPlayer;
        public bool IsLoggedIn = false;
        public Vector2 LastTilePos = Vector2.Zero;
        public string CurQuestRegionName { get; set; }
        public QuestRegion CurQuestRegion { get; set; }
        public bool RunningQuest = false;
        public bool InMenu = false;
        public Quest CurrentQuest;
        public Menu QuestMenu;

        public QPlayer(int index)
        {
            Index = index;
        }
    }
	
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