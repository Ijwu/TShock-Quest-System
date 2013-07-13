using NLua;

namespace QuestSystemLUA
{
	public abstract class Trigger
	{
		public bool RepresentInMenu = false;
		public Color MenuColor = Color.White;
		public LuaFunction Callback = QMain.utilityInterpreter.LoadString("return", "blankCallback");
		public virtual void Initialize() {}
		public virtual bool Update(Quest q) {return true;}
		public virtual void onComplete() {}
		public virtual string Progress() {return "";}
	}
}