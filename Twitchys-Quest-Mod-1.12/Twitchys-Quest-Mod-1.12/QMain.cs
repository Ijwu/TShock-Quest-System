﻿using System;
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
using Triggers;

namespace QuestSystemLUA
{
    [APIVersion(1, 13)]
    public class QMain : TerrariaPlugin
    {        
        public override string Name
        {
            get { return "Quest System"; }
        }
        public override string Author
        {
            get { return "Created by Twitchy."; }
        }
        public override string Description
        {
            get { return "You can make shit happen, yo!"; }
        }
        public override Version Version
        {
        	get { return new Version(2,1); }
        }
        
        public static QThreadable ThreadClass = new QThreadable();
        public static Thread QuestHandler;
        public static bool Running;
        
        public static List<QPlayer> Players = new List<QPlayer>();
        public static List<StoredQPlayer> LoadedPlayers = new List<StoredQPlayer>();
        
        public static List<QuestInfo> PossibleQuests = new List<QuestInfo>();
        public static List<QuestRegion> QuestRegions = new List<QuestRegion>();
        
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        
        public static Lua utilityInterpreter = new Lua();
        
        public static TriggerRegistry TriggerHandler = new TriggerRegistry();

        public override void Initialize()
        {
            TypesList.SetupTypes();
            ServerHooks.Join += OnJoin;
            ServerHooks.Leave += OnLeave;
            GameHooks.Initialize += OnInitialize;
            GameHooks.Update += OnUpdate;
//            TShockAPI.Hooks.GeneralHooks.ReloadEvent += QCommands.LoadQuestData;
        }
        protected override void Dispose(bool disposing)
        {
        	Running = false;
        	QuestHandler.Abort();
            if (disposing)
            {
                ServerHooks.Join -= OnJoin;
                ServerHooks.Leave -= OnLeave;
                GameHooks.Initialize -= OnInitialize;
                GameHooks.Update -= OnUpdate;
//                TShockAPI.Hooks.GeneralHooks.ReloadEvent -= QCommands.LoadQuestData;
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
            Commands.ChatCommands.Add(new Command("usequest", QCommands.ListQuest, "listquests"));
            Commands.ChatCommands.Add(new Command("usequest", QCommands.StartQuest, "startquest"));
            Commands.ChatCommands.Add(new Command("questregion", QCommands.QuestRegion, "questr"));
            Commands.ChatCommands.Add(new Command("reloadqdata", QCommands.LoadQuestData, "reloadquestdata"));
            Commands.ChatCommands.Add(new Command("giveq", QCommands.GiveQuest, "giveq"));
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

            QTools.LoadQuests();
            QTools.LoadRegions();
            QTools.LoadPlayers();
                        
            Running = true;
            
            TriggerHandler.InitializeRegistry();
            
            QuestHandler = new Thread(ThreadClass.QuestHandler);
            QuestHandler.Start();
        }          
        public QMain(Main game)
            : base(game)
        {
            Order = -10;
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
                            StoredQPlayer splayer = new StoredQPlayer(player.TSPlayer.UserAccountName, new List<QuestAttemptData>());
                            player.MyDBPlayer = splayer;
                            QTools.SavePlayers();
                        }
                        player.IsLoggedIn = true;
                    }
                    foreach (QuestRegion qr in QuestRegions)
                    {
                    	if (qr.InArea(player.TSPlayer.TileX, player.TSPlayer.TileY))
                    	{
                    		if (player.CurQuestRegionName != qr.Name)
                    		{
                    			player.CurQuestRegionName = qr.Name;
                    			player.CurQuestRegion = qr;
                    			player.TSPlayer.SendMessage(qr.MessageOnEntry, Color.Magenta);
                    		}
                    	}
                    	else if(player.CurQuestRegionName == qr.Name && !qr.InArea((int)player.LastTilePos.X, (int)player.LastTilePos.Y))
                    	{
                    		player.CurQuestRegion = null; 
                    		player.CurQuestRegionName = "";
                    		player.TSPlayer.SendMessage(qr.MessageOnExit, Color.Magenta);
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
                        Players.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}