using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Hooks;
using TShockAPI;
using TShockAPI.DB;

namespace QuestSystemLUA
{
    public class QCommands
    {
        public static void GetCoords(CommandArgs args)
        {
            int x = (int)args.Player.X / 16;
            int y = (int)args.Player.Y / 16;
            args.Player.SendMessage(string.Format("X: {0}, Y: {1}", x, y), Color.Magenta);
        }
        public static void ListQuest(CommandArgs args)
        {
        	QPlayer player = QTools.GetPlayerByID(args.Player.Index);
        	QuestRegion qr = player.CurQuestRegion;
        	if (player.CurQuestRegion == null)
        	{
        		args.Player.SendErrorMessage("You are not currently in a quest region.");
        		return;
        	}
        	string returnString = "Quests available in region: ";
        	foreach(QuestInfo q in qr.Quests)
        	{
        		returnString += q.Name + ", ";
        	}
        	args.Player.SendInfoMessage(returnString);
        }
        public static void StartQuest(CommandArgs args)
        {
        	QPlayer player = QTools.GetPlayerByID(args.Player.Index);
        	QuestRegion qr = player.CurQuestRegion;
        	if (player.RunningQuest)
        	{
        		player.TSPlayer.SendMessage("You have a quest running. You may only run one quest at a time.", Color.Red);
        		return;
        	}
        	if (player.IsLoggedIn)
        	{
        		Rectangle ply = new Rectangle(args.Player.TileX, args.Player.TileY, 1, 1);
        		if (ply.Intersects(qr.Area))
        		{
        			foreach (QuestInfo q in qr.Quests)
        			{
        				if (q.Name.ToLower() == args.Parameters[0].ToLower())
	        			{
        					if (!(q.Permission == ""))
        					{
	        					if (!player.TSPlayer.Group.HasPermission(q.Permission))
	        					{
	        						player.TSPlayer.SendErrorMessage("You do not have permission to start this quest.");
		        					return;
	        					}
        					}
	        				if (QTools.IntervalLeft(player, q) == 0)
	                        {
	                        	lock(QMain.ThreadClass.RunningQuests)
	                        		QMain.ThreadClass.RunningQuests.Add(new Quest(player, q));
	                        	
	                        	QuestAttemptData lastAttempt = player.MyDBPlayer.QuestAttemptData.Find(x => x.QuestName == q.Name);
	                        	if (lastAttempt != null)
	                        		lastAttempt.LastAttempt = DateTime.UtcNow;
	                        	else
	                        	{
	                        		QuestAttemptData attempt = new QuestAttemptData(q.Name, false, DateTime.UtcNow);
	                        		player.MyDBPlayer.QuestAttemptData.Add(attempt);
	                        	}
	                        	return;
	                        }
	        				else
	        				{
	        					player.TSPlayer.SendErrorMessage(string.Format("You may not start the quest \"{0}\" for another {1} seconds.", q.Name, QTools.IntervalLeft(player, q).ToString()));
	        					return;
	        				}
        				}
        			}
    			player.TSPlayer.SendErrorMessage("This quest is non-existant.");
				return;
        		}
        	}
        	else
        	{
        		player.TSPlayer.SendErrorMessage("You are not Logged in.");
        	}
        }
        public static void QuestRegion(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "define":
                        {
                            if (args.Parameters.Count > 1)
                            {
                                if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                                {
                                    string qregionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
                                    foreach (QuestRegion qr in QMain.QuestRegions)
                                    {
                                    	if (qr.Name == qregionName)
                                    	{
                                    		args.Player.SendErrorMessage("A quest region with this name already exists. Choose another name.");
                                    		return;
                                    	}
                                    }
                                    QMain.QuestRegions.Add(new QuestRegion(qregionName, new List<QuestInfo>(), x, y, width + x, height + y, "Entered Quest Region: " + qregionName, "Left Quest Region: " + qregionName));
                                    args.Player.SendMessage(string.Format("Added new Quest Region: \"{0}\"", qregionName), Color.Yellow);
                                    QTools.SaveRegions();
                                }
                                else
                                {
                                    args.Player.SendMessage("Points not set up yet", Color.Red);
                                }
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /questr define [name]", Color.Red);
                            break;
                        }
                    case "add":
                        {
                            if (args.Parameters.Count > 2)
                            {
                                string rName = args.Parameters[1];
                                string qName = args.Parameters[2];
                                QuestInfo q = QTools.GetQuestByName(qName);
                                QuestRegion r = QTools.GetRegionByName(rName);
                                if (r != null && q != null)
                                {
                                    args.Player.SendMessage(string.Format("Added Quest: \"{0}\" to the Quest Region: \"{1}\"", q.Name, r.Name), Color.Yellow);
                                    if (!r.Quests.Contains(q))
                                    {
	                                    r.Quests.Add(q);
	                                    QTools.SaveRegions();
                                    }
                                    else
                                    	args.Player.SendErrorMessage(string.Format("Quest region \"{0}\" already contains quest \"{1}\"", r.Name, q.Name));
                                }
                                else if (r == null)
                                    args.Player.SendMessage("Invalid Quest Region Name", Color.Red);
                                else if (q == null)
                                    args.Player.SendMessage("Invalid Quest Name", Color.Red);
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /questr add [RegionName] [QuestName]", Color.Red);
                            break;
                        }
                    case "remove":
                        {
                            if (args.Parameters.Count > 2)
                            {
                                string rName = args.Parameters[1];
                                string qName = args.Parameters[2];
                                QuestInfo q = QTools.GetQuestByName(qName);
                                QuestRegion r = QTools.GetRegionByName(rName);
                                if (r != null && q != null && r.Quests.Contains(q))
                                {
                                    args.Player.SendMessage(string.Format("Removed Quest: \"{0}\" from the Quest Region: \"{1}\"", q.Name, r.Name), Color.Yellow);
                                    r.Quests.Remove(q);
                                    QTools.SaveRegions();
                                }
                                else if (r == null)
                                    args.Player.SendMessage("Invalid Quest Region Name", Color.Red);
                                else if (q == null || !r.Quests.Contains(q))
                                    args.Player.SendMessage("Invalid Quest Name or this region does not contain this quest", Color.Red);
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /questr remove [RegionName] [QuestName]", Color.Red);
                            break;
                        }
                    case "delete":
                        {
                            if (args.Parameters.Count > 1)
                            {
                                string questregionName = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                foreach (QuestRegion qr in QMain.QuestRegions)
                                {
                                    if (qr.Name == questregionName)
                                    {
                                        QMain.QuestRegions.Remove(qr);
                                        break;
                                    }
                                }
                                QTools.SaveRegions();
                                args.Player.SendMessage("Quest Region: " + questregionName + " deleted", Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Invalid syntax! Proper syntax: /questr delete [region]", Color.Red);
                            break;
                        }
                    case "list":
                        {
                            const int pagelimit = 15;
                            const int perline = 5;
                            int page = 0;
                            if (args.Parameters.Count > 1)
                            {
                                if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                                {
                                    args.Player.SendMessage(string.Format("Invalid page number: {0}", page), Color.Red);
                                    return;
                                }
                                page--;
                            }
                            if (QMain.QuestRegions.Count == 0)
                            {
                                args.Player.SendMessage("There are currently no Quest Regions defined.", Color.Red);
                                return;
                            }
                            int pagecount = QMain.QuestRegions.Count / pagelimit;
                            if (page > pagecount)
                            {
                                args.Player.SendMessage(string.Format("Page number exceeds pages (Page {0}/{1})", page + 1, pagecount + 1), Color.Red);
                                return;
                            }
                            args.Player.SendMessage(string.Format("Current Quest Regions (Page {0}/{1}):", page + 1, pagecount + 1), Color.Green);
                            var nameslist = new List<string>();
                            for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < QMain.QuestRegions.Count; i++)
                            {
                                nameslist.Add(QMain.QuestRegions[i].Name);
                            }
                            var names = nameslist.ToArray();
                            for (int i = 0; i < names.Length; i += perline)
                            {
                                args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                            }
                            if (page < pagecount)
                            {
                                args.Player.SendMessage(string.Format("Type /questr list {0} for more Quest Regions.", (page + 2)), Color.Yellow);
                            }
                            break;
                        }
                	default:
                		{
                			args.Player.SendErrorMessage("Invalid sub-command. Valid sub-commands: define, add, remove, delete, list.");
                			break;
                		}
                }
            }
        }
        public static void LoadQuestData(CommandArgs args)
        {
        	QTools.LoadPlayers();
        	QTools.LoadQuests();
        	QTools.LoadRegions();
            args.Player.SendMessage("Successfully reloaded QuestSystem data.", Color.Yellow);
        }
        public static void GiveQuest(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                QPlayer ply;
                if ((ply = QTools.GetPlayerByName(args.Parameters[0])) != null)
                {
                    QuestInfo q;
                    if ((q = QTools.GetQuestByName(args.Parameters[1])) != null)
                    {
                    	lock(QMain.ThreadClass.RunningQuests)
                    		QMain.ThreadClass.RunningQuests.Add(new Quest(ply, q));
                    }
                    else
                        args.Player.SendMessage("Quest does not exist!", Color.Red);
                }
                else
                    args.Player.SendMessage("Player does not exist!", Color.Red);
            }
        }
        public static void ForceQuestOnAllPlayers(CommandArgs args)
        {
        	QuestInfo q = QTools.GetQuestByName(args.Parameters[0]);

        	foreach (QPlayer ply in QMain.Players)
        	{
        		foreach (Quest quest in QMain.ThreadClass.RunningQuests)
        		{
        			if (quest.player.Index == ply.Index)
        				lock(QMain.ThreadClass.RunningQuests)
        					QMain.ThreadClass.RunningQuests.Remove(quest);
        			ply.TSPlayer.SendErrorMessage("Your quest was removed by server staff.");
        		}
        		lock(QMain.ThreadClass.RunningQuests)
        			QMain.ThreadClass.RunningQuests.Add(new Quest(ply, q));
        		ply.TSPlayer.SendInfoMessage(string.Format("New quest: \"{0}\" forced by admin.", q.Name));
        	}
        }
    }
}
