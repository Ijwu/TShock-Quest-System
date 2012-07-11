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