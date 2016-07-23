using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TShockAPI;
namespace QuestSystemLUA
{
    public enum MenuStatus
    {        
        ForceExit = -1,
        Exit = 0,
        Select = 1,
        Input = 2
    }
    public class MenuEventArgs : HandledEventArgs
    {
        public MenuEventArgs(List<MenuItem> contents, int playerID) : this(contents, playerID, -1, MenuStatus.Exit) { }
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection) : this(contents, playerID, selection, MenuStatus.Select) { }
        public MenuEventArgs(List<MenuItem> contents, int playerID, int selection, MenuStatus status)
        {
            this.Data = contents;
            this.Selected = selection;
            this.PlayerID = playerID;
            this.Status = status;
        }
        public int Selected;
        public MenuStatus Status;
        public List<MenuItem> Data;
        public int PlayerID;
    }
    public class MenuItem
    {
        public String Text;
        public Color Color;
        public int Value;
        public bool Selectable;
        public bool Writable;
        public String Input;
        public MenuItem(MenuItem menuitem)
        {
            this.Text = menuitem.Text;
            this.Value = menuitem.Value;
            this.Selectable = menuitem.Selectable;
            this.Writable = menuitem.Writable;
            this.Color = menuitem.Color;
            this.Input = menuitem.Input;
        }
        public MenuItem(String text) : this(text, -1) { }
        public MenuItem(String text, int value) : this(text, value, true, false) { }
        public MenuItem(String text, int value, Color color) : this(text, value, true, false, color) { }
        public MenuItem(String text, int value, bool selectable, Color color) : this(text, value, selectable, false, color) { }
        public MenuItem(String text, int value, bool selectable, bool writable) : this(text, value, selectable, writable, Color.White) { }
        public MenuItem(String text, int value, bool selectable, bool writable, Color color)
        {
            this.Text = text;
            this.Color = color;
            this.Value = value;
            this.Selectable = selectable;
            this.Writable = writable;
            this.Input = "";
        }
    }
    public class Menu
    {
        public int PlayerID;
        public string title;
        public List<MenuItem> contents;
        public int index = 0;
        public bool header = true;
        public QMain.MenuAction MenuActionHandler;

        public Menu(int playerid, String title, List<MenuItem> contents, QMain.MenuAction del = null)
        {
            this.contents = contents;
            this.PlayerID = playerid;
            this.title = title;
            
            if (del != null)
            	this.MenuActionHandler = del;
            
            QPlayer target = QMain.Players[QMain.Players.IndexOf(QTools.GetPlayerByID(playerid))];
            
            if (target != null)
            	target.InMenu = true;
            
            this.DisplayMenu();
        }
        public void DisplayMenu()
        {
            TSPlayer player = TShock.Players[this.PlayerID];
            if (player != null)
            {
                int j = -2;
                if (this.header)
                    player.SendData(PacketTypes.ChatText, String.Format("{0}: (Move: [up,down] - Select: [spacebar] - Exit: [up+down])", this.title), 255, Color.DarkSalmon.R, Color.DarkSalmon.G, Color.DarkSalmon.B, 1);
                else
                    j = -3;
                for (int i = j; i <= 3; i++)
                {
                    if (i == 0)
                    {
                        if (this.contents[this.index].Writable)
                            player.SendData(PacketTypes.ChatText, (this.contents[this.index + i].Text.Contains("@0")) ? this.contents[this.index + i].Text.Replace("@0", String.Format(">{0}<", this.contents[this.index + i].Input)) : String.Format("{0} >{1}<", this.contents[this.index].Text, this.contents[this.index].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                        else if (this.contents[this.index].Selectable)
                            player.SendData(PacketTypes.ChatText, String.Format("> {0} <", this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input)), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                        else
                            player.SendData(PacketTypes.ChatText, this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input), 255, this.contents[this.index].Color.R, this.contents[this.index].Color.G, this.contents[this.index].Color.B, 1);
                    }
                    else if (this.index + i < 0 || this.index + i >= this.contents.Count)
                        player.SendData(PacketTypes.ChatText, "", 255, 0f, 0f, 0f, 1);
                    else
                        player.SendData(PacketTypes.ChatText, this.contents[this.index + i].Text.Replace("@0", this.contents[this.index + i].Input), 255, this.contents[this.index + i].Color.R, this.contents[this.index + i].Color.G, this.contents[this.index + i].Color.B, 1);
                }
            }
        }
        public void MoveDown()
        {
            if (this.index + 1 < this.contents.Count)
            {
                this.index++;
                this.DisplayMenu();
            }
        }
        public void MoveUp()
        {
            if (this.index - 1 >= 0)
            {
                this.index--;
                this.DisplayMenu();
            }
        }
        public void Close(bool force = false)
        {
            try
            {
            	QPlayer player = QTools.GetPlayerByID(this.PlayerID);
                if (player != null)
                {
                    MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, -1, (force)?MenuStatus.ForceExit:MenuStatus.Exit);
                    if (this.MenuActionHandler != null)
                        this.MenuActionHandler(this, args);
                    if (force || !args.Handled)
                    {
                        player.InMenu = false;
                        player.QuestMenu = null;
                    }
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }
        public void Select()
        {
            if (this.contents[this.index].Selectable)
            {
                MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, this.index, MenuStatus.Select);
                if (this.MenuActionHandler != null)
                    this.MenuActionHandler(this, args);
            }
        }
        public void OnInput(String text)
        {
            var player = QMain.Players[this.PlayerID];
            if (player != null)
            {
                string oldinput = this.contents[this.index].Input;
                this.contents[this.index].Input = text;
                MenuEventArgs args = new MenuEventArgs(this.contents, this.PlayerID, this.index, MenuStatus.Input);
                if (this.MenuActionHandler != null)
                    this.MenuActionHandler(this, args);
                if (!args.Handled)
                    DisplayMenu();
                else
                    this.contents[this.index].Input = oldinput;
            }
        }
        public MenuItem GetItemByValue(int value)
        {
            foreach (MenuItem item in this.contents)
            {
                if (item.Value == value)
                    return item;
            }
            return null;
        }

        public static Menu CreateMenu(int playerID, string title, List<MenuItem> data, QMain.MenuAction callback)
        {
        	QPlayer player = QTools.GetPlayerByID(playerID);
            try
            {
                if (player != null && !player.InMenu)
                {
                    player.QuestMenu = new Menu(player.Index, title, data, callback);
                    player.InMenu = true;
                    return player.QuestMenu;
                }
            }
            catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
            return null;
        }
    }
}