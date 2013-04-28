using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using LuaInterface;

namespace QuestSystemLUA
{
	public abstract class Trigger
	{
		public LuaFunction Callback = new Lua().LoadString("return", "blankCallback");
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
		
		public Quest(QPlayer player, QuestInfo info) 
		{
			this.player = player;
			this.info = info;
			loadQuest();
		}
		
		public void nextTrigger()
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
		public void evaluateTrigger()
		{
			if (currentTrigger.Update())
			{
				currentTrigger.onComplete();
				
				currentTrigger.Callback.Call(new Object[]{currentTrigger});
				nextTrigger();
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
		
		public void loadQuest()
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
				
				lua.DoFile(this.path);
				this.player.TSPlayer.SendInfoMessage(string.Format("Quest {0} has started.", this.info.Name));
				
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
}
