using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLua;
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
			    				if (!quest.PauseTime.Equals(TimeSpan.Zero)) //If there is pause time in the quest, then skip this quest and remove the tickrate time from the pause time.
			    				{
			    					quest.PauseTime -= TickRate;
			    					continue;
			    				}
			    				if (!quest.player.RunningQuest)
			    					quest.player.RunningQuest = true;
			    				
			    				if (quest.info.Time != 0) //If time limit on quest exists
			    				{
			    					if (DateTime.UtcNow.Subtract(quest.starttime) > TimeSpan.FromSeconds(quest.info.Time)) //Check the start time of the quest with the time limit
			    					{
			    						quest.player.TSPlayer.SendErrorMessage(string.Format("Quest \"{0}\" aborted. Your time limit of {1} seconds is up.", quest.info.Name, quest.info.Time));
		    							quest.running = false;
			    						quest.player.RunningQuest = false;
			    					}
			    				}
			    				if (QTools.IsLoggedIn(quest.player.Index)) //If the player is still logged in
				    			{
					    			quest.EvaluateTrigger(); //Main quest handling, run the current trigger
					    			if (quest.triggers.Count <= 0 && quest.currentTrigger == null)
					    			{
				    					quest.running = false;
					    				quest.player.RunningQuest = false;
					    				quest.player.MyDBPlayer.QuestAttemptData.Find(x => x.QuestName == quest.info.Name).Complete = true;
					    			}
				    			}
				    			else //If the player isn't logged in, end the quest
				    			{
		    						quest.running = false;
				    			}
			    			}
		    			}
	    				catch (NLua.Exceptions.LuaException e)
						{
	    					StringBuilder errorMessage = new StringBuilder();
	    					errorMessage.AppendLine(string.Format("Error in quest system while running quest: Player: {0} QuestName: {1}", quest.player.TSPlayer.Name, quest.path));
	    					errorMessage.AppendLine(e.Message);
	    					errorMessage.AppendLine(e.StackTrace);
	    					TShockAPI.Log.ConsoleError(errorMessage.ToString());
	    					
	    					quest.player.TSPlayer.SendErrorMessage("Your current quest has encountered an exception and had to be stopped.");
	    					
	    					quest.running = false; //Quit quest on exception
						}
		    		}
		    		RunningQuests.RemoveAll(q => q.running == false); //Remove inactive quests
		    		LastExecution = DateTime.UtcNow;
	    		}
    		}
    	}
   	}	
}
