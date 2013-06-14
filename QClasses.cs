using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using LuaInterface;
using TShockAPI;
using Terraria;
using TShockAPI.DB;

namespace QuestSystemLUA
{
	public abstract class Trigger
	{
		public LuaFunction Callback = QMain.utilityInterpreter.LoadString("return", "blankCallback");
		public virtual void Initialize() {}
		public virtual bool Update() {return true;}
		public virtual void onComplete() {}
	}
	
	public class QuestInfo
	{
		public string Path;
		public string Name;
		public string QuestLine;
		public int PlaceInLine;
		public bool Required;
		public string Permission;
		public int Time;
		public TimeSpan Interval;
		
		public QuestInfo(string name, string path, string questline, int placeInLine, string permission, bool required, int time, TimeSpan interval)
		{
			this.Name = name;
			this.Path = path;
			this.QuestLine = questline;
			this.PlaceInLine = placeInLine;
			this.Required = required;
			this.Permission = permission;
			this.Time = time;
			this.Interval = interval;
		}
	}
	
	public class Quest
	{
		public QPlayer player;
		public QuestInfo info;
		public LinkedList<Trigger> triggers = new LinkedList<Trigger>();
		public Trigger currentTrigger {get; protected set;}
		public bool running;
		public string path {get {return this.info.Path;}}
		public DateTime starttime = DateTime.UtcNow;
		public TimeSpan PauseTime;
		
		public Quest(QPlayer player, QuestInfo info) 
		{
			this.player = player;
			this.info = info;
			LoadQuest();
		}
		
		public void NextTrigger()
		{
			if (triggers.Count == 0)
			{
				currentTrigger = null;
			}
			else
			{
				currentTrigger = triggers.Last.Value;
				triggers.RemoveLast();
				currentTrigger.Initialize();
			}
		}
		public void EvaluateTrigger()
		{
			if (currentTrigger.Update())
			{
				currentTrigger.onComplete();
				
				currentTrigger.Callback.Call(new Object[]{currentTrigger});
				NextTrigger();
			}
		}
		
		public void Add(Trigger trigger)
		{
			triggers.AddFirst(trigger);
		}
		
		public void Prioritize(Trigger trigger)
		{
			triggers.AddLast(trigger);
		}
		
		public void Enqueue(LuaTable triggers)
		{
			IEnumerator enumerator = triggers.Values.GetEnumerator();
			List<Trigger> trigs = new List<Trigger>();
			while(enumerator.MoveNext())
			{
				trigs.Add((Trigger)enumerator.Current);
			}
			trigs.Reverse();
			foreach(Trigger trig in trigs)
			{
				Prioritize(trig);
			}
		}
		
		public void ClearQueue()
		{
			this.triggers = new LinkedList<Trigger>();
			this.currentTrigger = null;
		}
		
		public void Pause(int seconds)
		{
			PauseTime += new TimeSpan(0, 0, seconds);
		}
		
		public void LoadQuest()
		{
			try
			{
				Lua lua = new Lua();
				QMain.TriggerHandler.SetupScope(lua, this);
				lua["Quest"] = this;
				lua["Player"] = this.player;
				lua["Color"] = new Color();
				
				lua.RegisterFunction("Add", this, this.GetType().GetMethod("Add"));
				lua.RegisterFunction("Prioritize", this, this.GetType().GetMethod("Prioritize"));
				lua.RegisterFunction("Enqueue", this, this.GetType().GetMethod("Enqueue"));
				lua.RegisterFunction("ClearQueue", this, this.GetType().GetMethod("ClearQueue"));
				lua.RegisterFunction("Pause", this, this.GetType().GetMethod("Pause"));
				
				lua.DoFile(this.path);
				this.player.TSPlayer.SendInfoMessage(string.Format("Quest {0} has started.", this.info.Name));
				
				if (triggers.Count == 0)
					throw new LuaScriptException(string.Format("The script for the quest \"{0}\" never enqueues any triggers. Quests must enqueue at least one trigger.", this.info.Name), this.info.Path);
				
				running = true;
				
				currentTrigger = triggers.Last.Value;
				currentTrigger.Initialize();
				triggers.RemoveLast();
			}
			catch (Exception e)
			{
				System.Text.StringBuilder errorMessage = new System.Text.StringBuilder();
				errorMessage.AppendLine(string.Format("Error in quest system while loading quest: Player: {0} QuestName: {1}", this.player.TSPlayer.Name, this.path));
				errorMessage.AppendLine(e.Message);
				errorMessage.AppendLine(e.StackTrace);
				TShockAPI.Log.ConsoleError(errorMessage.ToString());
			}
		}
	}
	
	public class QPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public Item[] Inventory { get { return TSPlayer.TPlayer.inventory; } }
        public StoredQPlayer MyDBPlayer;
        public bool IsLoggedIn = false;
        public Vector2 LastTilePos = Vector2.Zero;
        public string CurQuestRegionName { get; set; }
        public QuestRegion CurQuestRegion { get; set; }
        public bool RunningQuest = false;
        public int LastTileHitX { get; set; }
        public int LastTileHitY { get; set; }

        public QPlayer(int index)
        {
            Index = index;
        }
    }
	public class QuestRegion : Region
    {
        public string MessageOnEntry;
        public string MessageOnExit;
        public List<QuestInfo> Quests = new List<QuestInfo>();

        public QuestRegion(string name, List<QuestInfo> quests, int x1, int y1, int x2, int y2, string entry, string exit)
        {
            this.Name = name;
            Quests = quests;
            this.Area = new Rectangle(x1, y1, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            MessageOnEntry = entry;
            MessageOnExit = exit;
        }
    }
	public class StoredQPlayer
    {
        public string LoggedInName;
        public List<QuestAttemptData> QuestAttemptData = new List<QuestAttemptData>();

        public StoredQPlayer(string name, List<QuestAttemptData> playerdata)
        {
            LoggedInName = name;
            QuestAttemptData = playerdata;
        }
    }
    public class QuestAttemptData //TODO: Add number of quest attempts here.
    {
        public string QuestName;
        public bool Complete = false;
        public DateTime LastAttempt;

        public QuestAttemptData(string name, bool Complete, DateTime LastAttempt)
        {
            QuestName = name;
            this.Complete = Complete;
            this.LastAttempt = LastAttempt;
        }
    }
}
