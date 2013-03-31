using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TShockAPI;
using Terraria;
using TShockAPI.DB;

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
        public int LastTileHitX { get; set; }
        public int LastTileHitY { get; set; }

        public QPlayer(int index)
        {
            Index = index;
        }
    }
	public class QuestRegion : Region
    {
        public string MessageOnEntry;
        public string MessageOnExit;
        public List<QuestInfo> Quests = new List<QuestInfo>();

        public QuestRegion(string name, List<QuestInfo> quests, int x1, int y1, int x2, int y2, string entry, string exit)
        {
            this.Name = name;
            Quests = quests;
            this.Area = new Rectangle(x1, y1, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            MessageOnEntry = entry;
            MessageOnExit = exit;
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