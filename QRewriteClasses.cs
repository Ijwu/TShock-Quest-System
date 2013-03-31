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
		
		public void loadQuest()
		{
			try
			{
				Lua lua = new Lua();
				QTools.SetupScope(lua, this);
				lua["Quest"] = this;
				lua["Player"] = this.player;
				lua.RegisterFunction("Add", this, this.GetType().GetMethod("Add"));
				lua.RegisterFunction("Prioritize", this, this.GetType().GetMethod("Prioritize"));
				lua.DoFile(this.path);
				this.player.TSPlayer.SendInfoMessage(string.Format("Quest {0} has started.", this.info.Name));
				running = true;
				currentTrigger = triggers.Last.Value;
				currentTrigger.Initialize();
				triggers.RemoveLast();
			}
			catch (Exception e)
			{
				ConsoleColor colorbuffer = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Error in quest system while loading quest: Player: {0} QuestName: {1}", player.TSPlayer.Name, this.path);
				Console.WriteLine(e.Message);
				Console.ForegroundColor = colorbuffer;
			}
		}
	}
}
