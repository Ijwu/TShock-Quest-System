using System;
using System.Collections.Generic;
using System.Reflection;

namespace QuestSystemLUA
{
	
	public static class QuestMenuBuilder
	{
		public static void ShowMenu(int index, Quest q)
		{
			Menu.CreateMenu(index, string.Format("{0} quest progress.", q.info.Name), BuildMenu(index, q), QTools.EmptyCallback);
		}
		
		private static List<MenuItem> BuildMenu(int index, Quest q)
		{
			List<MenuItem> returnList = new List<MenuItem>();
			Quest realTimeQuest = QTools.GetRunningQuest(index, q);
			List<MenuItem> incompleteItems = BuildIncompleteItems(realTimeQuest);
			incompleteItems.Reverse();
			returnList.AddRange(BuildCompletedItems(realTimeQuest));
			returnList.Add(BuildCurrentTrigger(realTimeQuest));
			returnList.AddRange(incompleteItems);
			return returnList;
		}	
		
		private static List<MenuItem> BuildCompletedItems(Quest q)
		{
			List<MenuItem> returnList = new List<MenuItem>();
			foreach (Trigger trig in q.completedTriggers)
			{
				if (trig.RepresentInMenu)
				{
					string itemString = string.Format("Complete: {0}: {1}", trig.GetType().Name, trig.Progress());
					returnList.Add(new MenuItem(itemString, 0, false, false, trig.MenuColor));
				}
			}
			return returnList;
		}
		
		private static MenuItem BuildCurrentTrigger(Quest q)
		{
			if (q.currentTrigger.RepresentInMenu)
			{
				string itemString = string.Format("> Current: {0}: {1} <", q.currentTrigger.GetType().Name, q.currentTrigger.Progress());
				return new MenuItem(itemString, 0, false, false, q.currentTrigger.MenuColor);
			}
			return null;
		}
		
		private static List<MenuItem> BuildIncompleteItems(Quest q)
		{
			List<MenuItem> returnList = new List<MenuItem>();
			foreach (Trigger trig in q.triggers)
			{
				if (trig.RepresentInMenu)
				{
					string itemString = string.Format("Incomplete: {0}: {1}", trig.GetType().Name, trig.Progress());
					returnList.Add(new MenuItem(itemString, 0, false, false, trig.MenuColor));
				}
			}
			return returnList;
		}
	}
}