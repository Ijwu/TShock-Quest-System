using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LuaInterface;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting; 

namespace QuestSystemLUA
{
    public class QTools
    {
        public static void RunQuest(object RunQuestOb)
        {
            QFunctions functions = new QFunctions();

            ScriptEngine pyEngine = Python.CreateEngine();
            ScriptScope pyScope = pyEngine.CreateScope();

            pyScope.SetVariable("Functions", functions);

            Lua lua = new Lua();

            //Updated In 1.1
            lua.RegisterFunction("AtXY", functions, functions.GetType().GetMethod("AtXY")); //int x, int y, QPlayer Player
            lua.RegisterFunction("TileEdit", functions, functions.GetType().GetMethod("TileEdit")); //int x, int y, string tile
            lua.RegisterFunction("WallEdit", functions, functions.GetType().GetMethod("WallEdit")); //int x, int y, string wall
            lua.RegisterFunction("DeleteBoth", functions, functions.GetType().GetMethod("DeleteBoth")); //int x, int y
            lua.RegisterFunction("DeleteWall", functions, functions.GetType().GetMethod("DeleteWall")); //int x, int y
            lua.RegisterFunction("DeleteTile", functions, functions.GetType().GetMethod("DeleteTile")); //int x, int y
            lua.RegisterFunction("Sleep", functions, functions.GetType().GetMethod("Sleep")); //int time
            lua.RegisterFunction("Teleport", functions, functions.GetType().GetMethod("Teleport")); //int x, int y, QPlayer Player
            lua.RegisterFunction("ClearKillList", functions, functions.GetType().GetMethod("ClearKillList")); //QPlayer Player
            lua.RegisterFunction("GoCollectItem", functions, functions.GetType().GetMethod("GoCollectItem")); //string itemname, int amount, QPlayer Player
            lua.RegisterFunction("TakeItem", functions, functions.GetType().GetMethod("TakeItem")); //string qname, string iname, int amt, QPlayer Player
            lua.RegisterFunction("GetRegionTilePercentage", functions, functions.GetType().GetMethod("GetRegionTilePercentage")); //string tiletype, string regionname
            lua.RegisterFunction("GetXYTilePercentage", functions, functions.GetType().GetMethod("GetXYTilePercentage")); //string tiletype, int X, int Y, int Width, int Height
            lua.RegisterFunction("GetRegionWallPercentage", functions, functions.GetType().GetMethod("GetRegionWallPercentage")); //string walltype, string regionname
            lua.RegisterFunction("GetXYWallPercentage", functions, functions.GetType().GetMethod("GetXYWallPercentage"));//string walltype, int X, int Y, int Width, int Height
            //Updated In 1.2
            lua.RegisterFunction("Give", functions, functions.GetType().GetMethod("Give")); //string name, QPlayer Player
            lua.RegisterFunction("Kill", functions, functions.GetType().GetMethod("Kill")); //string name, QPlayer Player, int amount = 1
            lua.RegisterFunction("KillNpc", functions, functions.GetType().GetMethod("KillNpc")); //int id
            lua.RegisterFunction("StartQuest", functions, functions.GetType().GetMethod("StartQuest")); //string qname, QPlayer Player
            lua.RegisterFunction("ReadNextChatLine", functions, functions.GetType().GetMethod("ReadNextChatLine")); //QPlayer Player, bool hide = false
            lua.RegisterFunction("SetNPCHealth", functions, functions.GetType().GetMethod("SetNPCHealth")); //int id, int health
            lua.RegisterFunction("Private", functions, functions.GetType().GetMethod("Private")); //string message, QPlayer Player, Color color
            lua.RegisterFunction("Broadcast", functions, functions.GetType().GetMethod("Broadcast")); //string message, Color color
            lua.RegisterFunction("SpawnMob", functions, functions.GetType().GetMethod("SpawnMob")); //string name, int x, int y, int amount = 1
            //Custom; added by Ijwu (Ijwu Version 1)
            lua.RegisterFunction("GetTile", functions, functions.GetType().GetMethod("GetTile")); //int x, int y
            lua.RegisterFunction("SetTile", functions, functions.GetType().GetMethod("SetTile")); //int x, int y, Tile newtile
            lua.RegisterFunction("CheckEmpty", functions, functions.GetType().GetMethod("CheckEmpty")); //int x, int y
            lua.RegisterFunction("BuffPlayer", functions, functions.GetType().GetMethod("BuffPlayer")); //string buffname, QPlayer Player, int time
            lua.RegisterFunction("CheckDay", functions, functions.GetType().GetMethod("CheckDay"));  //none
            lua.RegisterFunction("CheckTime", functions, functions.GetType().GetMethod("CheckTime")); //double time, int range, bool dayTime = true
            lua.RegisterFunction("HealPlayer", functions, functions.GetType().GetMethod("HealPlayer")); //QPlayer Player
            lua.RegisterFunction("SetWire", functions, functions.GetType().GetMethod("SetWire")); //int x, int y, bool wire = true, bool active = false
            lua.RegisterFunction("SetTileType", functions, functions.GetType().GetMethod("SetTileType")); //int x, int y, byte type, short frameX = 0, short frameY = 0
            //Added in Ijwu Version 2 (and some changed in Version 3)
            lua.RegisterFunction("AddParty", functions, functions.GetType().GetMethod("AddParty")); //QPlayer Player, string partyname
            lua.RegisterFunction("PartyHunt", functions, functions.GetType().GetMethod("PartyHunt")); //QPlayer Player, string pty, string npc, int amt = 1
            lua.RegisterFunction("PartyHuntList", functions, functions.GetType().GetMethod("PartyHuntList")); //QPlayer Player, string pty, dynamic hunt (The hunt var is a list. Depending on if you use Python or Lua. It will [hopefully] auto-detect and compensate for either.)
            lua.RegisterFunction("ChangeGroup", functions, functions.GetType().GetMethod("ChangeGroup")); //QPlayer Player, string group
            //Added in Ijwu Version 3
            lua.RegisterFunction("CreateMenu", functions, functions.GetType().GetMethod("CreateMenu")); // QPlayer Player, string title, dynamic menu
            lua.RegisterFunction("DirectoryIterate", functions, functions.GetType().GetMethod("DirectoryIterate")); // string path, string pattern, bool alldir = false // RETURNS JSON FORMATTED STRING
            /*Version 4
             *Changed the BuffPlayer shit to not return a bool. What the hell was that for? 
             *Added a /questr remove command
             */
            /*Version 5
             *When saving names to DB, escape characters should be placed in front of most troublesome characters. (e.g. quotes, escape characters)
             *Added the DirectoryIterate function, is for my own use in a personal project, but I've decided to include it here.
             */

            var parameters = (RunQuestParameters)RunQuestOb;
            QuestPlayerData qdata = null;
            if ((qdata = GetPlayerQuestData(parameters.Quest.Name, parameters.Player)) != null)
                qdata.Attempts++;
            else
            {
                qdata = new QuestPlayerData(parameters.Quest.Name, false, 0);
                parameters.Player.MyDBPlayer.QuestPlayerData.Add(qdata);
                qdata.Attempts++;
            }
            parameters.Player.RunningQuests.Add(parameters.Quest.Name);
            object[] returnvalues = new object[1];
            if (parameters.Quest.FilePath.EndsWith(".lua"))
            {
                try
                {
                    parameters.Player.RunningPython = false;
                    lua["Player"] = parameters.Player;
                    lua["Name"] = parameters.Player.TSPlayer.Name;
                    lua["QName"] = parameters.Quest.Name;
                    lua["Color"] = new Color();
                    returnvalues = lua.DoFile(parameters.Quest.FilePath);

                    if (returnvalues == null || returnvalues[0] == null || (bool)returnvalues[0])
                        qdata.Complete = true;
                    UpdateStoredPlayersInDB();
                    parameters.Player.RunningQuests.Remove(parameters.Quest.Name);
                    parameters.Player.RunningQuestThreads.Remove(parameters);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    parameters.Player.RunningQuests.Remove(parameters.Quest.Name);
                    parameters.Player.RunningQuestThreads.Remove(parameters);
                    UpdateStoredPlayersInDB();
                }
            }
            else
            {
                try
                {
                    parameters.Player.RunningPython = true;
                    pyScope.SetVariable("Player", parameters.Player);
                    pyScope.SetVariable("Name", parameters.Player.TSPlayer.Name);
                    pyScope.SetVariable("QName", parameters.Quest.Name);
                    pyScope.SetVariable("Color", new Color());
                    //Note to self: See if you can get them return values to work here.

                    //Console.WriteLine("Var setting success.");
                    ScriptSource source = pyEngine.CreateScriptSourceFromFile(parameters.Quest.FilePath);
                    //Console.WriteLine("Source retrieval success.");
                    CompiledCode compiled = source.Compile();
                    //Console.WriteLine("Source compilation success.");
                    compiled.Execute(pyScope);
                    //Console.WriteLine("Source execution success.");

                    qdata.Complete = true;
                    UpdateStoredPlayersInDB();
                    parameters.Player.RunningQuests.Remove(parameters.Quest.Name);
                    parameters.Player.RunningQuestThreads.Remove(parameters);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    parameters.Player.RunningQuests.Remove(parameters.Quest.Name);
                    parameters.Player.RunningQuestThreads.Remove(parameters);
                    UpdateStoredPlayersInDB();
                }
            }
        }
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
        public static Quest GetQuestByName(string name)
        {
            foreach (Quest q in QMain.QuestPool)
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
        public static StoredQPlayer GetStoredPlayerByIdentification(QPlayer Player)
        {
            if (Player.TSPlayer.IsLoggedIn)
            {
                for (int i = 0; i < QMain.StoredPlayers.Count; i++)
                    if (QMain.StoredPlayers[i].LoggedInName == Player.TSPlayer.UserAccountName)
                        return QMain.StoredPlayers[i];
            }
            else
            {
                for (int i = 0; i < QMain.StoredPlayers.Count; i++)
                    if (QMain.StoredPlayers[i].LoggedInName == Player.TSPlayer.IP)
                        return QMain.StoredPlayers[i];
            }
            return null;
        }
        public static void UpdateStoredPlayersInDB()
        {
            QMain.SQLWriter.DeleteRow("QuestPlayers", new List<SqlValue>());

            foreach (StoredQPlayer player in QMain.StoredPlayers)
            {
                string name = SanitizeString(player.LoggedInName);
                Console.WriteLine(name);
                List<SqlValue> values = new List<SqlValue>();
                values.Add(new SqlValue("LogInName", "\"" + name + "\""));

                string playerdata = "";

                foreach (QuestPlayerData data in player.QuestPlayerData)
                {
                    string qname = data.QuestName;
                    if (playerdata != "")
                        playerdata = string.Join(":", playerdata, string.Join(",", qname, data.Complete.ToString(), data.Attempts));
                    else
                        playerdata = string.Join(",", data.QuestName, data.Complete.ToString(), data.Attempts);
                }

                values.Add(new SqlValue("QuestPlayerData", "\"" + playerdata + "\""));

                QMain.SQLEditor.InsertValues("QuestPlayers", values);
            }
        }
        public static void UpdateRegionsInDB()
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

                foreach (Quest q in r.Quests)
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
        public static QuestPlayerData GetPlayerQuestData(string name, QPlayer Player)
        {
            foreach (QuestPlayerData data in Player.MyDBPlayer.QuestPlayerData)
            {
                if (data.QuestName == name)
                    return data;
            }
            return null;
        }
        public static int GetQuestsCompleted(List<QuestPlayerData> qdata)
        {
            int completed = 0;
            foreach (QuestPlayerData data in qdata)
            {
                if (data.Complete)
                    completed++;
            }
            return completed;
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
        public static void LoadQuestData()
        {
            QMain.QuestPool = new List<Quest>();
            QMain.QuestRegions = new List<QuestRegion>();
            QMain.StoredPlayers = new List<StoredQPlayer>();
            if (!Directory.Exists("Quests"))
                Directory.CreateDirectory("Quests");
            string[] filePaths = Directory.GetFiles("Quests", "*.lua");
            IEnumerable<string> filePathz = filePaths.Concat(Directory.GetFiles("Quests","*.py")); // Couldn't find the explicit conversion that Visual C# insisted existed. So, I got sloppy.
            foreach (string path in filePathz)
            {
                try
                {
                    string[] configfile = File.ReadAllLines(path.Split('.')[0] + ".cfg");
                    string Name = "";
                    int MaxAttempts = 0;
                    int MinQuestsNeeded = 0;
                    int AmountOfPlayersAtATime = 0;
                    bool endondeath = false;
                    foreach (string line in configfile)
                    {
                        if (line.Trim().StartsWith("Name"))
                            Name = line.Trim().Split(':')[1];
                        if (line.Trim().StartsWith("MaxAttempts"))
                            MaxAttempts = Int32.Parse(line.Trim().Split(':')[1]);
                        if (line.Trim().StartsWith("MinQuestsNeeded"))
                            MinQuestsNeeded = Int32.Parse(line.Trim().Split(':')[1]);
                        if (line.Trim().StartsWith("AmountOfPlayersAtATime"))
                            AmountOfPlayersAtATime = Int32.Parse(line.Trim().Split(':')[1]);
                        if (line.Trim().StartsWith("EndOnDeath"))
                            endondeath = bool.Parse(line.Trim().Split(':')[1]);
                    }
                    QMain.QuestPool.Add(new Quest(Name, path, MinQuestsNeeded, MaxAttempts, AmountOfPlayersAtATime, endondeath));
                }
                catch { }
            }
            for (int i = 0; i < QMain.SQLEditor.ReadColumn("QuestPlayers", "LogInName", new List<SqlValue>()).Count; i++)
            {
                string qname = DesanitizeString(QMain.SQLEditor.ReadColumn("QuestPlayers", "LogInName", new List<SqlValue>())[i].ToString());
                string questdata = QMain.SQLEditor.ReadColumn("QuestPlayers", "QuestPlayerData", new List<SqlValue>())[i].ToString();
                List<QuestPlayerData> playerdata = new List<QuestPlayerData>();
                foreach (string data in questdata.Split(':'))
                {
                    try
                    {
                        if (data != "")
                        {
                            string name = data.Split(',')[0];
                            bool complete = bool.Parse(data.Split(',')[1]);
                            int attempts = int.Parse(data.Split(',')[2]);
                            playerdata.Add(new QuestPlayerData(name, complete, attempts));
                        }
                    }
                    catch { }
                }
                QMain.StoredPlayers.Add(new StoredQPlayer(qname, playerdata));
            }
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
                    List<Quest> quests = new List<Quest>();
                    foreach (string quest in Quests.Split(','))
                    {
                        Quest q = QTools.GetQuestByName(quest);
                        if (q != null)
                            quests.Add(q);
                    }
                    QMain.QuestRegions.Add(new QuestRegion(name, quests, X1, Y1, X2, Y2, Entry, Exit));
                }
                catch { }
            }
        }
        public static bool AbleToRunQuest(Quest q)
        {
            int count = 0;
            foreach (QPlayer player in QMain.Players)
            {
                foreach (string qname in player.RunningQuests)
                {
                    if (qname == q.Name)
                        count++;
                }
            }
            return count == 0 || count < q.AmountOfPlayersAtATime;
        }
        public static RunQuestParameters GetRunningQuestByName(string playername, string questname)
        {
            QPlayer p;
            if ((p = GetPlayerByName(playername)) != null)
            {
                foreach (RunQuestParameters qp in p.RunningQuestThreads)
                {
                    if (qp.Quest.Name == questname)
                        return qp;
                }
            }
            return null;
        }
        public static QuestParty GetQuestPartyByName(string name)
        {
            foreach (QuestParty pty in QMain.QuestParties)
            {
                if (pty.PartyName.ToLower() == name.ToLower())
                {
                    return pty;
                }
            }
            return null;
        }
        public static void MenuCallback(Object sender, ChatAssistant.MenuEventArgs args)
        {
            QPlayer player = GetPlayerByID(args.PlayerID);
            player.MenuOption = args.Selected;
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
    }
}