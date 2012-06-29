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
        public static void HitCoords(CommandArgs args)
        {
            var player = QTools.GetPlayerByID(args.Player.Index);
            player.AwaitingHitCoords = true;
            args.Player.SendMessage("Hit a Tile/Wall to get its Coords", Color.Magenta);
        }
        public static void ListQuest(CommandArgs args)
        {
            QPlayer Player = QTools.GetPlayerByID(args.Player.Index);
            if (Player.IsLoggedIn)
            {
                Rectangle ply = new Rectangle((int)args.Player.X / 16, (int)args.Player.Y / 16, 1, 1);
                string availquests = "Available Quests: ";
                foreach (QuestRegion qr in QMain.QuestRegions)
                {
                    if (ply.Intersects(qr.Area))
                    {
                        foreach (Quest q in qr.Quests)
                        {
                            QuestPlayerData data = QTools.GetPlayerQuestData(q.Name, Player);

                            if (QTools.AbleToRunQuest(q) && (q.MinQuestsNeeded == 0 || q.MinQuestsNeeded <= QTools.GetQuestsCompleted(Player.MyDBPlayer.QuestPlayerData)) && (q.MaxAttemps == 0 || data == null || QTools.GetPlayerQuestData(q.Name, Player).Attempts < q.MaxAttemps))
                            {
                                availquests = availquests + q.Name + ", ";
                            }
                        }
                    }
                }

                if (availquests != "Available Quests: ")
                {
                    availquests = availquests.Substring(0, availquests.Length - 2);
                    Player.TSPlayer.SendMessage(availquests, Color.Magenta);
                    Player.TSPlayer.SendMessage("Use /startquest [Quest Name], to begin that quest", Color.Magenta);
                }
                else
                    Player.TSPlayer.SendMessage("No Available Quests", Color.Red);
            }
            else
                Player.TSPlayer.SendMessage("You are not Logged in", Color.Red);
        }
        public static void StartQuest(CommandArgs args)
        {
            QPlayer Player = QTools.GetPlayerByID(args.Player.Index);
            if (Player.IsLoggedIn)
            {
                Rectangle ply = new Rectangle((int)args.Player.X / 16, (int)args.Player.Y / 16, 1, 1);
                bool questfound = false;

                foreach (QuestRegion qr in QMain.QuestRegions)
                {
                    if (ply.Intersects(qr.Area))
                    {
                        foreach (Quest q in qr.Quests)
                        {
                            QuestPlayerData data = QTools.GetPlayerQuestData(q.Name, Player);

                            if (QTools.AbleToRunQuest(q) && (q.MinQuestsNeeded == 0 || q.MinQuestsNeeded <= QTools.GetQuestsCompleted(Player.MyDBPlayer.QuestPlayerData)) && q.Name.ToLower() == args.Parameters[0].ToLower() && (q.MaxAttemps == 0 || data == null || QTools.GetPlayerQuestData(q.Name, Player).Attempts < q.MaxAttemps))
                            {
                                questfound = true;
                                break;
                            }
                        }
                    }
                    if (questfound)
                        break;
                }

                if (questfound)
                {
                    if (!Player.NewQuest(QTools.GetQuestByName(args.Parameters[0])))
                        Player.TSPlayer.SendMessage("Quest already running.", Color.Red);
                }
                else
                    Player.TSPlayer.SendMessage("Quest not found.", Color.Red);
            }
            else
                Player.TSPlayer.SendMessage("You are not Logged in", Color.Red);
        }
        public static void QuestRegion(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "name":
                        {
                            var player = QTools.GetPlayerByID(args.Player.Index);
                            args.Player.SendMessage("Hit a block to get the name of the Quest Region", Color.Yellow);
                            player.AwaitingQRName = true;
                            break;
                        }
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
                                    QMain.QuestRegions.Add(new QuestRegion(qregionName, new List<Quest>(), x, y, width + x, height + y, "Entered Quest Region: " + qregionName, "Left Quest Region: " + qregionName));
                                    args.Player.SendMessage(string.Format("Added new Quest Region: \"{0}\"", qregionName), Color.Yellow);
                                    QTools.UpdateRegionsInDB();
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
                                Quest q = QTools.GetQuestByName(qName);
                                QuestRegion r = QTools.GetRegionByName(rName);
                                if (r != null && q != null)
                                {
                                    args.Player.SendMessage(string.Format("Added Quest: \"{0}\" to the Quest Region: \"{1}\"", q.Name, r.Name), Color.Yellow);
                                    r.Quests.Add(q);
                                    QTools.UpdateRegionsInDB();
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
                                QTools.UpdateRegionsInDB();
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
                                    args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
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
                                args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
                                return;
                            }
                            args.Player.SendMessage(string.Format("Current Quest Regions ({0}/{1}):", page + 1, pagecount + 1), Color.Green);
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
                }
            }
        }
        public static void LoadQuestData(CommandArgs args)
        {
            QTools.LoadQuestData();
            args.Player.SendMessage("Successfully Loaded Quest Data!");
        }
        public static void GiveQuest(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                QPlayer ply;
                if ((ply = QTools.GetPlayerByName(args.Parameters[0])) != null)
                {
                    Quest q;
                    if ((q = QTools.GetQuestByName(args.Parameters[1])) != null)
                    {
                        ply.NewQuest(q);
                    }
                    else
                        args.Player.SendMessage("Quest does not exist!", Color.Red);
                }
                else
                    args.Player.SendMessage("Player does not exist!", Color.Red);
            }
        }
        public static void StopQuest(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                var runpara = QTools.GetRunningQuestByName(args.Parameters[0], args.Parameters[1]);

                if (runpara != null)
                    runpara.QThread.Abort();
                else
                    args.Player.SendMessage("Invalid Arguments or Player is not running Quest", Color.Red);
            }
        }
    }
}
