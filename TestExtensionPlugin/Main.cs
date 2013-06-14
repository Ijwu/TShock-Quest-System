using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Terraria;
using Hooks;
using TShockAPI;
using System.ComponentModel;
using System.IO; 
using QuestSystemLUA;

namespace Extension
{
    [APIVersion(1, 12)]
    public class Extension : TerrariaPlugin
    {        
        public override string Name
        {
            get { return "Trigger Extension Test"; }
        }
        public override string Author
        {
            get { return "Ijwu."; }
        }
        public override string Description
        {
            get { return "You can make shit happen, yo!"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        
        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
            }
            base.Dispose(disposing);
        }
        public void OnInitialize()
        {
        	QMain.TriggerHandler.RegisterTrigger(new NewBroadcast());
        	QMain.TriggerHandler.RegisterTrigger(typeof(NewBroadcast2).GetConstructors()[0]);
        	QMain.TriggerHandler.RegisterTrigger(typeof(NewBroadcast3));
        }          
        public Extension(Main game)
            : base(game)
        {
        }
    }
    
    public class NewBroadcast : Trigger
    {
    	public override bool Update()
    	{
    		TShock.Utils.Broadcast("Text has been displayed.", Color.SeaGreen);
    		return true;
    	}
    }
    
    public class NewBroadcast2 : Trigger
    {
    	public override bool Update()
    	{
    		TShock.Utils.Broadcast("Text has been displayed.", Color.Sienna);
    		return true;
    	}
    }
    
    public class NewBroadcast3 : Trigger
    {
    	public override bool Update()
    	{
    		TShock.Utils.Broadcast("Text has been displayed.", Color.Tomato);
    		return true;
    	}
    }
}