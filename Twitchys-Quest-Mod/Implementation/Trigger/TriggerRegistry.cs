using System;
using Triggers;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NLua;

namespace QuestSystemLUA
{
	public class TriggerRegistry
	{
		private List<ConstructorInfo> registeredTriggers = new List<ConstructorInfo>();
		private Assembly questAssembly = Assembly.GetExecutingAssembly();
		
		internal void InitializeRegistry()
		{
			Type[] definedTypes = questAssembly.GetTypes();
			for (int i=0; i<definedTypes.Length; i++)
			{
				if (definedTypes[i].Namespace == "Triggers")
				{
					registeredTriggers.Add(definedTypes[i].GetConstructors()[0]);
				}
			}
		}
		
		internal void SetupScope(Lua lua, Quest q)
		{
			foreach(ConstructorInfo constructor in registeredTriggers)
			{
				lua.RegisterFunction(constructor.DeclaringType.Name, q, constructor);
			}
		}
		
		public void RegisterTrigger(ConstructorInfo trigger)
		{
			registeredTriggers.Add(trigger);
		}
		
		public void RegisterTrigger(Type trigger)
		{
			registeredTriggers.Add(trigger.GetConstructors()[0]);
		}
		
		public void RegisterTrigger(Trigger trigger)
		{
			registeredTriggers.Add(trigger.GetType().GetConstructors()[0]);
		}
	}
}
