using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LuaInterface;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace QuestSystemLUA
{
    public class QThreadable
    {
    	public List<Quest> RunningQuests = new List<Quest>();
    	public DateTime LastExecution = DateTime.UtcNow;
    	public static TimeSpan TickRate = new TimeSpan(0,0,0,0,1); //1 milliseconds
    	public void QuestHandler()
    	{
    		while (QMain.Running)
    		{
    			if (DateTime.UtcNow.Subtract(LastExecution) > TickRate)
    			{
		    		foreach (Quest quest in RunningQuests)
		    		{
		    			try
		    			{
			    			if (quest.running)
			    			{
			    				if (!quest.player.RunningQuest)
			    					quest.player.RunningQuest = true;
			    				
			    				if (quest.info.Time != 0)
			    				{
			    					if (DateTime.UtcNow.Subtract(quest.starttime) > TimeSpan.FromSeconds(quest.info.Time))
			    					{
			    						quest.player.TSPlayer.SendErrorMessage(string.Format("Quest \"{0}\" aborted. Your time limit of {1} seconds is up.", quest.info.Name, quest.info.Time));
		    							quest.running = false;
			    						quest.player.RunningQuest = false;
			    					}
			    				}
			    				if (QTools.IsLoggedIn(quest.player.Index))
				    			{
					    			quest.evaluateTrigger();
					    			if (quest.triggers.Count <= 0 && quest.currentTrigger == null)
					    			{
				    					quest.running = false;
					    				quest.player.RunningQuest = false;
					    				quest.player.MyDBPlayer.QuestAttemptData.Find(x => x.QuestName == quest.info.Name).Complete = true;
					    			}
				    			}
				    			else
				    			{
		    						quest.running = false;
				    			}
			    			}
		    			}
	    				catch (LuaException e)
						{
	    					StringBuilder errorMessage = new StringBuilder();
	    					errorMessage.AppendLine(string.Format("Error in quest system while running quest: Player: {0} QuestName: {1}", quest.player.TSPlayer.Name, quest.path));
	    					errorMessage.AppendLine(e.Message);
	    					errorMessage.AppendLine(e.StackTrace);
	    					TShockAPI.Log.ConsoleError(errorMessage.ToString());
	    					
	    					quest.player.TSPlayer.SendErrorMessage("Your current quest has encountered an exception and had to be stopped.");
	    					
	    					quest.running = false;
						}
		    		}
		    		RunningQuests.RemoveAll(q => q.running == false);
		    		LastExecution = DateTime.UtcNow;
	    		}
    		}
    	}
   	}	
}
