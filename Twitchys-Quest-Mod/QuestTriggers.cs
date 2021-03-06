﻿using System;
using System.IO.Streams;
using QuestSystemLUA;
using Terraria;
using TShockAPI;
using System.Collections.Generic;
using Hooks;
using System.IO;
using System.ComponentModel;
using LuaInterface;

namespace Triggers
{		
	public class HuntMob : Trigger
	{
		public Dictionary<string, int> toBeKilled = new Dictionary<string, int>();
		public QPlayer player;
		
		public HuntMob(QPlayer Player, string type, int amount=1)
		{
			player = Player;
			addNPC(type, amount);
		}
		
		public void addNPC(string type, int amount=1)
		{
			List<NPC> npc = TShock.Utils.GetNPCByIdOrName(type);
			if (npc.Count == 1) 
			{
				toBeKilled.Add(npc[0].name, amount);
			}
			else
			{
				throw new FormatException("More than one or no NPCs matched to name or ID.");
			}
		}
		
		public override void Initialize()
		{
			NetHooks.GetData += OnGetData;
		}
		 
		public override bool Update(Quest q)
		{
			if (toBeKilled.Count == 0)
				return true;
			return false;
		}
		
		public override void onComplete()
		{
			NetHooks.GetData -= OnGetData;
		}
		
		public void OnGetData(GetDataEventArgs args)
		{
			if (args.Msg.whoAmI == player.Index)
			{
				if (args.MsgID == PacketTypes.NpcStrike)
				{
					MemoryStream data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length);
					int npcID = data.ReadInt16();
					int damage = data.ReadInt16();
					if (Main.npc[npcID].life - damage <= 0)
					{
						if (toBeKilled.ContainsKey(Main.npc[npcID].name))
							toBeKilled[Main.npc[npcID].name] -= 1;
					
						if (toBeKilled[Main.npc[npcID].name] <= 0)
							toBeKilled.Remove(Main.npc[npcID].name);	
					}
				}
			}
		}
	}
	
	public class Reward : Trigger
	{
		private QPlayer player;
		private Item item;
		
		public Reward(QPlayer player, string item, int stack=1, int prefix=0)
		{
			List<Item> items = TShock.Utils.GetItemByIdOrName(item);
			if (items.Count == 1) 
			{
				this.item = items[0];
				this.player = player;
			}
			else
			{
				throw new FormatException("More than one or no items matched to name or ID.");
			}
			this.item.prefix = (byte)prefix;
			this.item.stack = stack;
		}
		
		public override bool Update(Quest q)
		{
			player.TSPlayer.GiveItem(item.type, item.name, item.width, item.height, item.stack, item.prefix);
			return true;
		}
	}
	
	
	public class GiveUpItem : Trigger
	{
		private Dictionary<string, int> toBeCollected = new Dictionary<string,int>();
		private QPlayer player;
		
		public override void Initialize()
		{
			NetHooks.GetData += checkItemDrops;
		}
		
		public override void onComplete()
		{
		 	NetHooks.GetData -= checkItemDrops;
		}
		
		public override bool Update(Quest q)
		{
			if (toBeCollected.Count == 0)
			{
				return true;
			}
			return false;
		}
		
		public GiveUpItem(QPlayer player, string type, int amount=1)
		{
			this.player = player;
			addItem(type, amount);
		}
		
		
		public void addItem(string type, int amount=1)
		{
			List<Item> item = TShock.Utils.GetItemByIdOrName(type);
			if (item.Count == 1) 
			{
				toBeCollected.Add(item[0].name, amount);
			}
			else
			{
				throw new FormatException("More than one or no items matched to name or ID.");
			}
		}
		
		private void checkItemDrops(GetDataEventArgs args)
		{
			if (args.MsgID == PacketTypes.ItemDrop)
			{
				if (args.Handled)
					return;
				
				using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
	            {
		            Int16 id = data.ReadInt16();
		            float posx = data.ReadSingle();
		            float posy = data.ReadSingle();
		            float velx = data.ReadSingle();
		            float vely = data.ReadSingle();
		            int stack = data.ReadByte();
		            int prefix = data.ReadByte();
		            Int16 type = data.ReadInt16();
		            
		            Item item = new Item();
		            item.SetDefaults(type);
		         	
		            if (id == 0)
		            	return;
		            
		            if (toBeCollected.ContainsKey(item.name))
		            {
		            	toBeCollected[item.name] -= stack;
		            	
		            	if (toBeCollected[item.name] < 0)
		            	{
		            		if (Math.Abs(toBeCollected[item.name]) > 1)
		            		    player.TSPlayer.SendInfoMessage(string.Format("Returning {0} {1}s", Math.Abs(toBeCollected[item.name]), item.name));
	                        else
	                            player.TSPlayer.SendInfoMessage(string.Format("Returning {0} {1}", Math.Abs(toBeCollected[item.name]), item.name));
	                        args.Handled = true;
	                        player.TSPlayer.GiveItem(item.type, item.name, item.width, item.width, Math.Abs(toBeCollected[item.name]));
	                        toBeCollected.Remove(item.name);
		            	}
		            	else if (toBeCollected[item.name] > 0)
		            	{
		            		 if (Math.Abs(toBeCollected[item.name]) > 1)
	                            player.TSPlayer.SendInfoMessage(string.Format("Drop another {0} {1}s, to continue", Math.Abs(toBeCollected[item.name]), item.name));
	                        else
	                            player.TSPlayer.SendInfoMessage(string.Format("Drop {0} {1}, to continue", Math.Abs(toBeCollected[item.name]), item.name));
	                        args.Handled = true;
		            	}
	            		else
	                    {
	                        if (stack > 1)
	                            player.TSPlayer.SendInfoMessage(string.Format("You dropped {0} {1}s", stack, item.name));
	                        else
	                            player.TSPlayer.SendInfoMessage(string.Format("You dropped {0} {1}", stack, item.name));
	
	                        toBeCollected.Remove(item.name);
	                        args.Handled = true;
	                    }
		            }
		            args.Handled = true;
	            }
			}
		}
	}
	
	
	public class AtXY : Trigger
	{
		public int x, y, radius;
		public QPlayer player;
		public Rectangle playerRect {get {return new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1);}}
		public Rectangle targetRect {get {return new Rectangle(x, y, radius, radius);}}
		
		public AtXY(QPlayer player, int x, int y, int radius = 1)
		{
			this.player = player;
			this.x = x;
			this.y = y;
			this.radius = radius;
		}
		
		public override bool Update(Quest q)
		{
			return playerRect.Intersects(targetRect);
		}
	}
	
	public class TileEdit : Trigger
	{
		public int x, y;
		public string name;
		
		public TileEdit(int x, int y, string tile)
		{
			this.x = x;
			this.y = y;
			this.name = tile;
		}
		
		public override bool Update(Quest q)
		{
			byte type;
			
			if (QTools.GetTileTypeFromName(name, out type))
            {
                if (type < 253)
                {
                    Main.tile[x, y].type = (byte)type;
                    Main.tile[x, y].active = true;
                    Main.tile[x, y].liquid = 0;
                    Main.tile[x, y].skipLiquid = true;
                    Main.tile[x, y].frameNumber = 0;
                    Main.tile[x, y].frameX = -1;
                    Main.tile[x, y].frameY = -1;
                }
                else if (type == 253)
                {
                    Main.tile[x, y].active = false;
                    Main.tile[x, y].skipLiquid = false;
                    Main.tile[x, y].lava = false;
                    Main.tile[x, y].liquid = 255;
                    Main.tile[x, y].checkingLiquid = false;
                }
                else if (type == 254)
                {
                    Main.tile[x, y].active = false;
                    Main.tile[x, y].skipLiquid = false;
                    Main.tile[x, y].lava = true;
                    Main.tile[x, y].liquid = 255;
                    Main.tile[x, y].checkingLiquid = false;
                }
                if ((Main.tile[x, y].type == 53) || (Main.tile[x, y].type == 253) || (Main.tile[x, y].type == 254))
                    WorldGen.SquareTileFrame(x, y, false);
                QTools.UpdateTile(x, y);
                return true;
            }
            else
            {
                throw new Exception("Invalid Tile Name");
            }
		}
	}
	
	public class WallEdit : Trigger
	{
		public int x, y;
		public string name;
		
		public WallEdit(int x, int y, string wall)
		{
			this.x = x;
			this.y = y;
			this.name = wall;
		}
		
		public override bool Update(Quest q)
		{
			byte type;

            if (QTools.GetWallTypeFromName(name, out type))
            {
                if (type < 255)
                {
                    Main.tile[x, y].wall = (byte)type;
                }
                QTools.UpdateTile(x, y);
                return true;
            }
            else
            {
                throw new Exception("Invalid Wall Name");
            }
        }
	}
	
	public class Delay : Trigger
	{
		TimeSpan DelayTime;
		
		public Delay(int seconds)
		{
			DelayTime = new TimeSpan(0,0,seconds);
		}
		public override bool Update(Quest q)
		{
			q.PauseTime += DelayTime;
			return true;
		}
	}
	
	public class DeleteTileWall : Trigger
	{
		public int x, y;
		
		public DeleteTileWall(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		
		public override bool Update(Quest q)
		{
			Main.tile[x, y].active = false;
            Main.tile[x, y].wall = 0;
            Main.tile[x, y].skipLiquid = true;
            Main.tile[x, y].liquid = 0;
            QTools.UpdateTile(x, y);
            return true;
		}
	}
	
	public class DeleteWall : Trigger
	{
		public int x, y;
		
		public DeleteWall(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		
		public override bool Update(Quest q)
		{
			Main.tile[x, y].wall = 0;
            QTools.UpdateTile(x, y);
            return true;
		}
	}
	
	public class DeleteTile : Trigger
	{
		public int x, y;
		
		public DeleteTile(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		
		public override bool Update(Quest q)
		{
			Main.tile[x, y].active = false;
            Main.tile[x, y].skipLiquid = true;
            Main.tile[x, y].liquid = 0;
            QTools.UpdateTile(x, y);
            return true;
		}
	}
	
	public class Teleport : Trigger
	{
		QPlayer player;
		int x, y;
		
		public Teleport(int x, int y, QPlayer player)
        {
			this.x = x;
			this.y = y;
			this.player = player;
        }
		
		public override bool Update(Quest q)
		{
            return player.TSPlayer.Teleport(x, y + 3);
		}
	}
	
	public class SpawnMob : Trigger
	{
		public string name;
		public int x, y, amount;
		
		public override bool Update(Quest q)
		{
			List<NPC> npcs = TShock.Utils.GetNPCByIdOrName(name);
			if (npcs.Count == 1)
			{
				NPC npc = npcs[0];
				for (int i = 0; i < amount; i++)
      			{
	                TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, x, y, 1, 1);
        		}
				return true;
			}
			else
			{
				throw new FormatException("More than one or no NPCs matched to name or ID.");
			}
		}
		
		public SpawnMob(string name, int x, int y, int amount = 1)
        {
			this.x = x;
			this.y = y;
			this.amount = amount;
			this.name = name;
        }
	}
	
	public class SpawnModdedMob : Trigger //TODO: Make this work, kay?
	{
		public string name;
		public int x, y, amount;
		public LuaTable mods;
		
		public override bool Update(Quest q)
		{			
			List<NPC> npcs = TShock.Utils.GetNPCByIdOrName(name);
			if (npcs.Count == 1)
			{
				NPC npc = npcs[0];
				for (int i = 0; i < amount; i++)
      			{
					int sx;
					int sy;
					TShock.Utils.GetRandomClearTileWithInRange(x, y, 5, 5, out sx, out sy);
					int index = NPC.NewNPC(sx, sy, npc.type);
					Main.npc[index].SetDefaults(npc.type);
					Main.npc[index].netDefaults(npc.type);
					SetModdedMob(ref Main.npc[index]);
					NetMessage.SendData(23, -1, -1, "", index, 0f, 0f, 0f, 0);
        		}
				return true;
			}
			else
			{
				throw new FormatException("More than one or no NPCs matched to name or ID.");
			}
		}
		
		private void SetModdedMob(ref NPC returned)
		{
			if (mods["name"] != null)
			{
				returned.name = mods["name"].ToString();
				returned.displayName = mods["name"].ToString();
			}
			
			if (mods["maxlife"] != null)
			{
				returned.lifeMax = (int)mods["maxlife"];
				returned.life = (int)mods["maxlife"];
			}
			
			if (mods["scale"] != null)
			{
				returned.scale = float.Parse(mods["scale"].ToString());
			}
		}
		
		public SpawnModdedMob(string name, int x, int y, int amount = 1, LuaTable mods = null)
        {
			this.x = x;
			this.y = y;
			this.amount = amount;
			this.name = name;
			this.mods = mods;
        }
	}
	
	public class ReadNextChatLine : Trigger
	{
		public QPlayer player;
		public string Message;
		public bool hideMessage;
		
		public override void Initialize()
		{
			ServerHooks.Chat += onChat;
		}
		
		public ReadNextChatLine(QPlayer player, bool hideMsg = false)
		{
			this.player = player;
			this.hideMessage = hideMsg;
		}	
		
		public override void onComplete()
		{
			ServerHooks.Chat -= onChat;
		}
		
		public override bool Update(Quest q)
		{
			if (Message != null)
				return true;
			
			return false;
		}
		
		public void onChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
		{			
			if (ply == player.Index)
			{
				Message = text;
				if (hideMessage)
					e.Handled = true;
			}
			
		}
	}
	
	public class GatherItem : Trigger
	{
		public Item item;
		public int amount;
		private int count = 0;
		public QPlayer player;		
		
		public GatherItem(QPlayer player, string item, int amount=1)
		{
			this.player = player;
			this.amount = amount;
			
			List<Item> items = TShock.Utils.GetItemByIdOrName(item);
			if (items.Count == 1)
			{
				this.item = items[0];
				foreach(Item slot in player.Inventory)
				{
					if (slot != null)
						if (slot.name.ToLower() == this.item.name.ToLower())
							count -= slot.stack;
				}
			}
			else
			{
				throw new FormatException("More than one or no items matched to name or ID.");
			}
		}
		
		public override bool Update(Quest q)
		{
			int buffer = count;
            try
            {
                foreach (Item slot in player.Inventory)
                {
                    if (slot != null)
                        if (slot.name.ToLower() == item.name.ToLower())
                            count += slot.stack;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
            
            if (count >= amount)
            {
            	return true;
            }
            
            count = buffer;
            return false;
		}
	}
	
	public class GetRegionTilePercentage : Trigger
	{
		public string tiletype;
		public string regionname;
		public float percentage = 0;
		
		public GetRegionTilePercentage(string tiletype, string regionname)
		{
			this.tiletype = tiletype;
			this.regionname = regionname;
		}
		
		public override bool Update(Quest q)
		{
			float amountofmatchedtiles = 0;
            float totaltilecount = 0;
            TShockAPI.DB.Region r = TShock.Regions.ZacksGetRegionByName(regionname);
            byte type;
            if (QTools.GetTileTypeFromName(tiletype, out type))
            {
                if (r != null)
                {
                    for (int i = r.Area.X; i < (r.Area.X + r.Area.Width); i++)
                    {
                        for (int j = r.Area.Y; j < (r.Area.Y + r.Area.Height); j++)
                        {
                            if (Main.tile[i, j].active && Main.tile[i, j].type == type )
                                amountofmatchedtiles++;
                            totaltilecount++;
                        }
                    }
                }
            }
            if (totaltilecount != 0)
            	percentage = (amountofmatchedtiles/totaltilecount)*100;
            return true;
		}
	}
	
	public class GetXYTilePercentage : Trigger
	{
		public string tiletype;
		public int X, Y, Width, Height;
		public float percentage = 0;
		public GetXYTilePercentage(string tiletype, int X, int Y, int Width, int Height)
		{
			this.tiletype = tiletype;
			this.X = X;
			this.Y  = Y;
			this.Width = Width;
			this.Height = Height;
			
		}
		
		public override bool Update(Quest q)
		{
			float amountofmatchedtiles = 0;
            float totaltilecount = 0;
            byte type;
            if (QTools.GetTileTypeFromName(tiletype, out type))
            {
                for (int i = X; i < (X + Width); i++)
                {
                    for (int j = Y; j < (Y + Height); j++)
                    {
                        if (Main.tile[i, j].active && Main.tile[i, j].type == type)
                            amountofmatchedtiles++;
                        totaltilecount++;
                    }
                }
            }
            if (totaltilecount != 0)
            	percentage = ((amountofmatchedtiles / totaltilecount) * 100);
            return true;
		}
	}
	
	public class GetRegionWallPercentage : Trigger
	{
		public string regionname;
		public string walltype;
		public float percentage;
		
		public GetRegionWallPercentage(string walltype, string regionname)
		{
			this.regionname = regionname;
			this.walltype = walltype;
		}
		
		public override bool Update(Quest q)
		{
			float amountofmatchedwalls = 0;
            float totalwallcount = 0;
            TShockAPI.DB.Region r = TShock.Regions.ZacksGetRegionByName(regionname);
            byte type;
            if (QTools.GetWallTypeFromName(walltype, out type))
            {
                if (r != null)
                {
                    for (int i = r.Area.X; i < (r.Area.X + r.Area.Width); i++)
                    {
                        for (int j = r.Area.Y; j < (r.Area.Y + r.Area.Height); j++)
                        {
                            if (Main.tile[i, j].active && Main.tile[i, j].wall == type)
                                amountofmatchedwalls++;
                            totalwallcount++;
                        }
                    }
                }
            }
            if (totalwallcount != 0)
				percentage = ((amountofmatchedwalls / totalwallcount) * 100);
            return true;
		}
		
	}
	
	public class GetXYWallPercentage : Trigger
	{
		public string walltype;
		public int X, Y, Width, Height;
		public float percentage = 0;
		
		public GetXYWallPercentage(string walltype, int X, int Y, int Width, int Height)
		{
			this.walltype = walltype;
			this.X = X;
			this.Y  = Y;
			this.Width = Width;
			this.Height = Height;
		}
		
		public override bool Update(Quest q)
		{
			float amountofmatchedwalls = 0;
            float totalwallcount = 0;
            byte type;
            if (QTools.GetWallTypeFromName(walltype, out type))
            {
                for (int i = X; i < (X + Width); i++)
                {
                    for (int j = Y; j < (Y + Height); j++)
                    {
                        if (Main.tile[i, j].wall == type)
                            amountofmatchedwalls++;
                        totalwallcount++;
                    }
                }
            }
            if (totalwallcount != 0)
				percentage = ((amountofmatchedwalls / totalwallcount) * 100);
            return true;
		}
		
	}
	
	public class Broadcast : Trigger
	{
		public string message;
		public Color color;
		
		public Broadcast(string message, Color color)
		{
			this.message = message;
			this.color = color;
		}
		
		public override bool Update(Quest q)
		{
			TShock.Utils.Broadcast(message, color);
			return true;
		}
	}
	
	public class MessagePlayer : Trigger
	{
		public string message;
		public Color color;
		public QPlayer player;
		
		public MessagePlayer(string message, QPlayer player, Color color)
		{
			this.message = message;
			this.color = color;
			this.player = player;
		}
		
		public override bool Update(Quest q)
		{
            player.TSPlayer.SendMessage(message, color);
            return true;
		}
	}
	
	public class BuffPlayer : Trigger
	{
		public QPlayer player;
		public string buffname;
		public int time;
		
		public BuffPlayer(QPlayer player, string buffname, int time)
		{
			this.player = player;
			this.buffname = buffname;
			this.time = time;
		}
		
		public override bool Update(Quest q)
		{
			List<int> buffs = TShock.Utils.GetBuffByName(buffname);
            if (buffs.Count == 1)
            {
                player.TSPlayer.SetBuff(buffs[0], time*60);
                return true;
            }
            else
			{
				throw new FormatException("More than one or no buffs matched to the name provided.");
			}
		}
	}
	
	public class CallFunction : Trigger
	{
		LuaFunction func;
		LuaTable args;
		
		public override bool Update(Quest q)
		{
			func.Call(new object[]{args});
			return true;
		}
		
		public CallFunction(LuaFunction func, LuaTable args)
		{
			this.func = func;
			this.args = args;
		}
	}
	
	public class ChangePlayerGroup : Trigger
	{
		string targetGroup;
		TSPlayer player;
		
		public ChangePlayerGroup(QPlayer player, string targetGroup)
		{
			this.player = player.TSPlayer;
			if (TShock.Groups.GroupExists(targetGroup))
			{
				this.targetGroup = targetGroup;
			}
			else
			{
				throw new FormatException(string.Format("The group {0} does not exist.", targetGroup));
			}
		}
		
		public override bool Update(Quest q)
		{
			TShock.Users.SetUserGroup(TShock.Users.GetUserByName(player.Name), targetGroup);
			return true;
		}
	}
		
}