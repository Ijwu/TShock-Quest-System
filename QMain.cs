using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Terraria;
using MySql.Data.MySqlClient;
using Hooks;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using LuaInterface;
using System.IO;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting; 

namespace QuestSystemLUA
{
    [APIVersion(1, 12)]
    public class QMain : TerrariaPlugin
    {        
        public override string Name
        {
            get { return "QuestPluginLUA"; }
        }
        public override string Author
        {
            get { return "Created by Twitchy."; }
        }
        public override string Description
        {
            get { return ""; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public static List<QPlayer> Players = new List<QPlayer>();
        public static List<Quest> QuestPool = new List<Quest>();
        public static List<StoredQPlayer> StoredPlayers = new List<StoredQPlayer>();
        public static List<QuestRegion> QuestRegions = new List<QuestRegion>();
        public static List<QuestParty> QuestParties = new List<QuestParty>();
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static Lua lua = new Lua();
        public static ScriptEngine pyEngine = Python.CreateEngine();
        public static ScriptScope pyScope = pyEngine.CreateScope();

        public override void Initialize()
        {
            TypesList.SetupTyps();
            ServerHooks.Join += OnJoin;
            ServerHooks.Leave += OnLeave;
            NetHooks.GetData += GetData;
            GameHooks.Initialize += OnInitialize;
            GameHooks.Update += OnUpdate;
            ServerHooks.Chat += OnChat;

            GetDataHandlers.InitGetDataHandler();     
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerHooks.Join -= OnJoin;
                ServerHooks.Leave -= OnLeave;
                NetHooks.GetData -= GetData;
                GameHooks.Initialize -= OnInitialize;
                GameHooks.Update -= OnUpdate;
                ServerHooks.Chat -= OnChat;
            }
            base.Dispose(disposing);
        }
        public void OnInitialize()
        {
            Main.ignoreErrors = true;
            Main.rand = new Random();

            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            Commands.ChatCommands.Add(new Command(QCommands.GetCoords, "getcoords"));
            Commands.ChatCommands.Add(new Command(QCommands.HitCoords, "hitcoords"));
            Commands.ChatCommands.Add(new Command("usequest", QCommands.ListQuest, "listquests"));
            Commands.ChatCommands.Add(new Command("usequest", QCommands.StartQuest, "startquest"));
            Commands.ChatCommands.Add(new Command("questregion", QCommands.QuestRegion, "questr"));
            Commands.ChatCommands.Add(new Command("reloadqdata", QCommands.LoadQuestData, "reloadquestdata"));
            Commands.ChatCommands.Add(new Command("giveq", QCommands.GiveQuest, "giveq"));
            Commands.ChatCommands.Add(new Command("stopquest", QCommands.StopQuest, "stopquest"));
			Commands.ChatCommands.Add(new Command("forcequestonall", QCommands.ForceQuestOnAllPlayers, "forcequest"));             
            
            var table = new SqlTable("QuestPlayers",
                 new SqlColumn("LogInName", MySqlDbType.VarChar, 50) { Unique = true, Length = 50},
                 new SqlColumn("QuestPlayerData", MySqlDbType.Text)
             );
            SQLWriter.EnsureExists(table);

            table = new SqlTable("QuestRegions",
                new SqlColumn("RegionName", MySqlDbType.VarChar, 50) { Unique = true, Length = 50},
                new SqlColumn("X1", MySqlDbType.Int32),
                new SqlColumn("Y1", MySqlDbType.Int32),
                new SqlColumn("X2", MySqlDbType.Int32),
                new SqlColumn("Y2", MySqlDbType.Int32),
                new SqlColumn("Quests", MySqlDbType.Text),
                new SqlColumn("EntryMessage", MySqlDbType.Text),
                new SqlColumn("ExitMessage", MySqlDbType.Text)
            );
            SQLWriter.EnsureExists(table);

            QTools.LoadQuestData();

            QFunctions functions = new QFunctions();
            pyScope.SetVariable("Functions", functions);

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
            lua.RegisterFunction("DirectoryIterate", functions, functions.GetType().GetMethod("DirectoryIterate")); // string path, string pattern, bool alldir = false RETURNS JSON FORMATTED STRING
            //Added in Ijwu Version 4
            lua.RegisterFunction("RunFileDirectly", functions, functions.GetType().GetMethod("RunFileDirectly")); // string path
            lua.RegisterFunction("RunTShockCommand", functions, functions.GetType().GetMethod("RunTShockCommand")); //string name, string[] parameters
        }
        public QMain(Main game)
            : base(game)
        {
            Order = -10;
        }
        public void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
        {
            if (e.Handled)
                return;

            var player = QTools.GetPlayerByID(ply);
            if (player.AwaitingChat)
            {
                player.LastChatMessage = text;
                player.AwaitingChat = false;

                if (player.HideChat)
                    e.Handled = true;
            }            
        }
        public void OnUpdate()
        {
            lock (Players)
            {
                foreach (QPlayer player in Players)
                {
                    if (!player.IsLoggedIn && player.TSPlayer.IsLoggedIn)
                    {
                        player.MyDBPlayer = QTools.GetStoredPlayerByIdentification(player);

                        if (player.MyDBPlayer == null)
                        {
                            StoredQPlayer splayer = new StoredQPlayer(player.TSPlayer.UserAccountName,
                                                                      new List<QuestPlayerData>());
                            StoredPlayers.Add(splayer);
                            player.MyDBPlayer = splayer;
                            QTools.UpdateStoredPlayersInDB();
                        }

                        player.IsLoggedIn = true;
                    }

                    if (player.LastTilePos != new Vector2(player.TSPlayer.TileX, player.TSPlayer.TileY))
                    {
                        bool inhouse = false;
                        foreach (QuestRegion qr in QuestRegions)
                        {

                            if (qr.InArea(player.TSPlayer.TileX, player.TSPlayer.TileY))
                            {
                                inhouse = true;
                                if (player.CurQuestRegionName != qr.Name)
                                {
                                    player.CurQuestRegionName = qr.Name;
                                    player.CurQuestRegion = qr;
                                    player.InHouse = true;

                                    if (qr.MessageOnEntry != "")
                                    {
                                        player.TSPlayer.SendMessage(qr.MessageOnEntry, Color.Magenta);
                                    }
                                }
                            }
                        }
                        QuestRegion intersection = player.CurQuestRegion;
                        if (!inhouse && player.InHouse)
                        {
                            if (intersection.MessageOnExit != "")
                            {
                                player.TSPlayer.SendMessage(intersection.MessageOnExit, Color.Magenta);
                            }
                            player.CurQuestRegionName = "";
                            player.InHouse = false;
                        }
                    }
                    player.LastTilePos = new Vector2(player.TSPlayer.TileX, player.TSPlayer.TileY);     
                }
            }
        }
        public void OnJoin(int who, HandledEventArgs e)
        {
            QPlayer player = new QPlayer(who);

            lock (Players)
                Players.Add(player);
        }
        public void OnLeave(int ply)
        {
            lock (Players)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == ply)
                    {
                        Players[i].RunningQuests.Clear();
                        Players[i].RunningQuestThreads.Clear();
                        Players.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        private void GetData(GetDataEventArgs e)
        {
            PacketTypes type = e.MsgID;
            var player = TShock.Players[e.Msg.whoAmI];

            if (player == null)
            {
                e.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                e.Handled = true;
                return;
            }

            using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        e.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}