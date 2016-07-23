using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLua;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using Triggers;

namespace QuestSystemLUA
{
    public class QTools
    {        
        public static QPlayer GetPlayerByID(int id)
        {
            QPlayer player = null;
            foreach (QPlayer ply in QMain.Players)
            {
                if (ply.Index == id)
                    return ply;
            }
            return player;
        }
        public static QPlayer GetPlayerByName(string name)
        {
            var player = TShock.Utils.FindPlayer(name)[0];
            if (player != null)
            {
                foreach (QPlayer ply in QMain.Players)
                {
                    if (ply.TSPlayer == player)
                        return ply;
                }
            }
            return null;
        }
        public static QuestInfo GetQuestByName(string name)
        {
            foreach (QuestInfo q in QMain.PossibleQuests)
            {                
                if (q.Name.ToLower() == name.ToLower())
                    return q;
            }
            return null;
        }
        public static void UpdateTile(int x, int y)
        {
            x = Netplay.GetSectionX(x);
            y = Netplay.GetSectionY(y);

            foreach(QPlayer ply in QMain.Players)
            {
                Netplay.serverSock[ply.Index].tileSection[x, y] = false;
            }
        }
        public static bool GetTileTypeFromName(string name, out byte type)
        {
            type = 255;

            foreach (KeyValuePair<string, byte> key in TypesList.tileTypeNames)
            {
                if (key.Key.ToLower() == name.ToLower())
                {
                    type = key.Value;
                    return true;
                }
            }

            return false;
        }
        public static bool GetWallTypeFromName(string name, out byte type)
        {
            type = 255;

            foreach (KeyValuePair<string, byte> key in TypesList.wallTypeNames)
            {
                if (key.Key.ToLower() == name.ToLower())
                {
                    type = key.Value;
                    return true;
                }
            }

            return false;
        }
        public static QuestRegion GetRegionByName(string name)
        {
            foreach (QuestRegion q in QMain.QuestRegions)
            {
                if (q.Name == name)
                    return q;
            }
            return null;
        }
        public static QuestAttemptData GetPlayerQuestData(string name, QPlayer Player)
        {
            foreach (QuestAttemptData data in Player.MyDBPlayer.QuestAttemptData)
            {
                if (data.QuestName == name)
                    return data;
            }
            return null;
        }
        public static int GetQuestsCompleted(List<QuestAttemptData> qdata)
        {
            int completed = 0;
            foreach (QuestAttemptData data in qdata)
            {
                if (data.Complete)
                    completed++;
            }
            return completed;
        }
        public static List<string> GetQuestsCompletedByName(List<QuestAttemptData> qdata)
        {
        	List<string> completed = new List<string>();
        	foreach (QuestAttemptData data in qdata)
        	{
        		if (data.Complete)
        			completed.Add(data.QuestName);
        	}
        	return completed;
        }
        public static bool CheckQuestCompletion(string name, List<QuestAttemptData> qdata)
        {
        	foreach (QuestAttemptData data in qdata)
        	{
        		if (data.QuestName == name)
        			return true;
        	}
        	return false;
        }
        public static string InQuestRegion(int x, int y)
        {
            foreach (QuestRegion qr in QMain.QuestRegions)
            {
                if (qr.Area.Intersects(new Rectangle(x, y, 1, 1)))
                {
                    return qr.Name;
                }
            }
            return null;
        }
        public static void SavePlayers()
        {
            QMain.SQLWriter.DeleteRow("QuestPlayers", new List<SqlValue>());
            
            foreach (QPlayer player in QMain.Players)
            {
            	string name = SanitizeString(player.MyDBPlayer.LoggedInName);
            	List<SqlValue> values = new List<SqlValue>();
            	values.Add(new SqlValue("LogInName", "\"" + name + "\""));
            	
            	string playerdata = "";
            	
            	foreach (QuestAttemptData data in player.MyDBPlayer.QuestAttemptData)
            	{
            		string qname = data.QuestName;
            		if (playerdata != "")
            			playerdata = string.Join(";", playerdata, string.Join(",", qname, data.Complete.ToString(), data.LastAttempt.ToString()));
            		else
            			playerdata = string.Join(",", qname, data.Complete.ToString(), data.LastAttempt.ToString());
            	}
            	
            	values.Add(new SqlValue("QuestPlayerData", "\"" + playerdata + "\""));
            	
            	QMain.SQLEditor.InsertValues("QuestPlayers", values);
            }
        }
        public static void SaveRegions()
        {
            QMain.SQLWriter.DeleteRow("QuestRegions", new List<SqlValue>());

            foreach (QuestRegion r in QMain.QuestRegions)
            {
                string rname = r.Name;
                List<SqlValue> values = new List<SqlValue>();
                values.Add(new SqlValue("RegionName", "\"" + rname + "\""));
                values.Add(new SqlValue("X1", "'" + r.Area.X + "'"));
                values.Add(new SqlValue("Y1", "'" + r.Area.Y + "'"));
                values.Add(new SqlValue("X2", "'" + (r.Area.X + r.Area.Width) + "'"));
                values.Add(new SqlValue("Y2", "'" + (r.Area.Y + r.Area.Height) + "'"));
                values.Add(new SqlValue("EntryMessage", "'" + r.MessageOnEntry + "'"));
                values.Add(new SqlValue("ExitMessage", "'" + r.MessageOnExit + "'"));

                string quests = "";

                foreach (QuestInfo q in r.Quests)
                {
                    if (quests != "")
                        quests = string.Join(",", quests, q.Name);
                    else
                        quests = q.Name;
                }

                values.Add(new SqlValue("Quests", "'" + quests + "'"));

                QMain.SQLEditor.InsertValues("QuestRegions", values);
            }
        }
        public static void LoadQuests()
        {
        	QMain.PossibleQuests = new List<QuestInfo>();
        	if (!Directory.Exists("Quests"))
        		Directory.CreateDirectory("Quests");
        	string[] filePaths = Directory.GetFiles("Quests", "*.lua");
        	foreach (string path in filePaths)
        	{
        		try
        		{
        			string[] config = File.ReadAllLines(path.Split('.')[0]+".cfg");
					string name = null;
					string questline = "";
					int placeInLine = 0;
					bool required = false;
					string permission = "";
					int time = 0;
					TimeSpan interval = TimeSpan.Zero;
					foreach (string line in config)
					{
						try
						{
							if (line.Trim().StartsWith("Name"))
								name = line.Trim().Split('=')[1];
							if (line.Trim().StartsWith("Questline"))
	                            questline = line.Trim().Split('=')[1];
	                        if (line.Trim().StartsWith("Place"))
	                            placeInLine = Int32.Parse(line.Trim().Split('=')[1]);
	                        if (line.Trim().StartsWith("Required"))
	                            required = bool.Parse(line.Trim().Split('=')[1]);
	                        if (line.Trim().StartsWith("Permission"))
	                            permission = line.Trim().Split('=')[1];
	                        if (line.Trim().StartsWith("Time"))
	                        	time = Int32.Parse(line.Trim().Split('=')[1]);
	                        if (line.Trim().StartsWith("Interval"))
	                        	interval = TimeSpan.Parse(line.Trim().Split('=')[1]);
						}
						catch (FormatException e)
						{
							ConsoleColor buffer = Console.ForegroundColor;
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Error while loading quest {0}", name != null ? name : "(Error is in loading name)");
							Console.WriteLine(e.Message);
							Console.WriteLine(e.StackTrace);
							Console.ForegroundColor = buffer;
						}
					}
					if (!(name==null)) //Name is required. The rest are optional.
						QMain.PossibleQuests.Add(new QuestInfo(name, path, questline, placeInLine, permission, required, time, interval));
        		}
        		catch {}
        	}
        	
        }
        
        public static void LoadRegions()
        {
        	QMain.QuestRegions = new List<QuestRegion>();
        	for (int i = 0; i < QMain.SQLEditor.ReadColumn("QuestRegions", "RegionName", new List<SqlValue>()).Count; i++)
            {
                try
                {
                    string name = QMain.SQLEditor.ReadColumn("QuestRegions", "RegionName", new List<SqlValue>())[i].ToString();
                    int X1 = int.Parse(QMain.SQLEditor.ReadColumn("QuestRegions", "X1", new List<SqlValue>())[i].ToString());
                    int Y1 = int.Parse(QMain.SQLEditor.ReadColumn("QuestRegions", "Y1", new List<SqlValue>())[i].ToString());
                    int X2 = int.Parse(QMain.SQLEditor.ReadColumn("QuestRegions", "X2", new List<SqlValue>())[i].ToString());
                    int Y2 = int.Parse(QMain.SQLEditor.ReadColumn("QuestRegions", "Y2", new List<SqlValue>())[i].ToString());
                    string Quests = QMain.SQLEditor.ReadColumn("QuestRegions", "Quests", new List<SqlValue>())[i].ToString();
                    string Entry = QMain.SQLEditor.ReadColumn("QuestRegions", "EntryMessage", new List<SqlValue>())[i].ToString();
                    string Exit = QMain.SQLEditor.ReadColumn("QuestRegions", "ExitMessage", new List<SqlValue>())[i].ToString();
                    List<QuestInfo> quests = new List<QuestInfo>();
                    foreach (string quest in Quests.Split(','))
                    {
                        QuestInfo q = QTools.GetQuestByName(quest);
                        if (q != null)
                            quests.Add(q);
                    }
                    QMain.QuestRegions.Add(new QuestRegion(name, quests, X1, Y1, X2, Y2, Entry, Exit));
                }
                catch { }
            }
        }
        public static void LoadPlayers()
        {
        	QMain.LoadedPlayers = new List<StoredQPlayer>();
        	for (int i = 0; i < QMain.SQLEditor.ReadColumn("QuestPlayers", "LogInName", new List<SqlValue>()).Count; i++)
            {
                string qname = DesanitizeString(QMain.SQLEditor.ReadColumn("QuestPlayers", "LogInName", new List<SqlValue>())[i].ToString());
                string questdata = QMain.SQLEditor.ReadColumn("QuestPlayers", "QuestPlayerData", new List<SqlValue>())[i].ToString();
                List<QuestAttemptData> playerdata = new List<QuestAttemptData>();
                foreach (string data in questdata.Split(';'))
                {
                    try
                    {
                        if (data != "")
                        {
                            string name = data.Split(',')[0];
                            bool complete = bool.Parse(data.Split(',')[1]);
                            DateTime lastattempt = DateTime.Parse(data.Split(',')[2]);
                            playerdata.Add(new QuestAttemptData(name, complete, lastattempt));
                        }
                    }
                    catch { }
                }
                QMain.LoadedPlayers.Add(new StoredQPlayer(qname, playerdata));
            }
        }
        public static StoredQPlayer GetStoredPlayerByIdentification(QPlayer player)
        {
        	foreach (StoredQPlayer ply in QMain.LoadedPlayers)
        	{
        		if (ply.LoggedInName == player.TSPlayer.UserAccountName)
        		{
        			return ply;
        		}
        	}
        	return null;
        }
        public static int IntervalLeft(QPlayer player, QuestInfo q)
        {
        	foreach (QuestAttemptData info in player.MyDBPlayer.QuestAttemptData)
        	{
        		if (info.QuestName == q.Name)
        		{
        			return DateTime.UtcNow.Subtract(info.LastAttempt) > q.Interval ? 0 : q.Interval.Seconds - DateTime.UtcNow.Subtract(info.LastAttempt).Seconds;
        		}
        	}
        	return 0;
        }
        public static string SanitizeString(string str)
        {
            str = str.Replace(@"\", @"\\");
            str = str.Replace("\'", @"\'");
            return str;
        }
        public static string DesanitizeString(string str)
        {
            str = str.Replace(@"\\", @"\");
            str = str.Replace(@"\'", "\'");
            return str;
        }
        public static bool IsLoggedIn(int id)
        {
        	foreach (QPlayer ply in QMain.Players)
        	{
        		if (ply.Index == id)
        			return true;
        	}
        	return false;
        }
    }
}