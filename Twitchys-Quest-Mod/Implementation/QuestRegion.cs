using System;
using System.Collections.Generic;
using TShockAPI.DB;

namespace QuestSystemLUA
{
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
}