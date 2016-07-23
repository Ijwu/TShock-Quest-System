using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using NLua;
using TShockAPI;
using Terraria;

namespace QuestSystemLUA
{
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
}