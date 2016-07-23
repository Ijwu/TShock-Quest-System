﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using NLua;
using TShockAPI;
using Terraria;

namespace QuestSystemLUA
{
	public class Quest
	{
		public QPlayer player;
		public QuestInfo info;
		public LinkedList<Trigger> triggers = new LinkedList<Trigger>();
		public List<Trigger> completedTriggers = new List<Trigger>();
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
//			if (currentTrigger != null)
//				Console.WriteLine("Trigger type: {0}", currentTrigger.GetType().Name);
			if (currentTrigger.Update(this))
			{
				currentTrigger.onComplete();
				
				currentTrigger.Callback.Call(new Object[]{currentTrigger});
				completedTriggers.Add(currentTrigger);
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
			if (this.currentTrigger != null)
			{
				this.currentTrigger.onComplete();
				this.currentTrigger = null;
			}
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
				if (this.player.CurQuestRegion != null)
					lua["Region"] = this.player.CurQuestRegion;
				
				lua.RegisterFunction("Add", this, this.GetType().GetMethod("Add"));
				lua.RegisterFunction("Prioritize", this, this.GetType().GetMethod("Prioritize"));
				lua.RegisterFunction("Enqueue", this, this.GetType().GetMethod("Enqueue"));
				lua.RegisterFunction("ClearQueue", this, this.GetType().GetMethod("ClearQueue"));
				
				lua.DoFile(this.path);
				this.player.TSPlayer.SendInfoMessage(string.Format("Quest {0} has started.", this.info.Name));
				
				if (triggers.Count == 0)
					throw new NLua.Exceptions.LuaScriptException(string.Format("The script for the quest \"{0}\" never enqueues any triggers. Quests must enqueue at least one trigger.", this.info.Name), this.info.Path);
				
				running = true;
				
				NextTrigger();
			}
			catch (Exception e)
			{
				System.Text.StringBuilder errorMessage = new System.Text.StringBuilder();
				errorMessage.AppendLine(string.Format("Error in quest system while loading quest: Player: {0} Quest: {1}", this.player.TSPlayer.Name, this.path));
				errorMessage.AppendLine(e.Message);
				errorMessage.AppendLine(e.StackTrace);
				Log.ConsoleError(errorMessage.ToString());
				if (e.InnerException != null)
				{
					Log.ConsoleError("Inner Exception:");
					Log.ConsoleError(e.InnerException.ToString());
				}
				player.TSPlayer.SendErrorMessage("Your quest has failed to load due to an error in the script or the system. Please report this to a server administrator.");
			}
		}
	}
}