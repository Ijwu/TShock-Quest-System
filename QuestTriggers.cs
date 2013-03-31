using System;
using System.IO.Streams;
using QuestSystemLUA;
using Terraria;
using TShockAPI;
using System.Collections.Generic;
using Hooks;
using System.IO;
using System.ComponentModel;
using LuaInterface;

//TODO: Debug and fix these triggers: All region percentage triggers, ReadNextChatLine, RetrieveItem.

namespace Triggers
{	
	public class Hunt : Trigger
	{
		public Dictionary<string, int> toBeKilled = new Dictionary<string, int>();
		public QPlayer player;
		
		public Hunt(QPlayer Player, string type, int amount=1)
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
//			NpcHooks.StrikeNpc += OnStrike;
		}
		 
		public override bool Update()
		{
			if (toBeKilled.Count == 0)
				return true;
			return false;
		}
		
		public override void onComplete()
		{
//			NpcHooks.StrikeNpc -= OnStrike;
			NetHooks.GetData -= OnGetData;
		}
		
//		public void OnStrike(NpcStrikeEventArgs args)
//		{
//			Console.WriteLine("NPC Strike {0} {1}.", args.Damage, args.Npc.life);
//			if (args.Npc.life - args.Damage <= 0)
//			{
//				if (toBeKilled.ContainsKey(args.Npc.name))
//					toBeKilled[args.Npc.name] -= 1;
//				
//				if (toBeKilled[args.Npc.name] <= 0)
//					toBeKilled.Remove(args.Npc.name);
//			}
//			
//		}
		
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
		
		public override bool Update()
		{
			player.TSPlayer.GiveItem(item.type, item.name, item.width, item.height, item.stack, item.prefix);
			return true;
		}
	}
	
	public class RetrieveItem : Trigger
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
		
		public override bool Update()
		{
			if (toBeCollected.Count == 0)
			{
				return true;
			}
			return false;
		}
		
		public RetrieveItem(QPlayer player, string type, int amount=1)
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
		//NOTE: May need redoing
		private void checkItemDrops(GetDataEventArgs args)
		{
			if (args.MsgID == PacketTypes.ItemDrop)
			{
				using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
	            {
		            var reader = new BinaryReader(data);
		            var id = reader.ReadInt16();
		            var posx = reader.ReadSingle();
		            var posy = reader.ReadSingle();
		            var velx = reader.ReadSingle();
		            var vely = reader.ReadSingle();
		            var stack = reader.ReadByte();
		            var prefix = reader.ReadByte();
		            var type = reader.ReadInt16();
		            
		            var item = new Item();
		            item.SetDefaults(type);
		            
		            if (toBeCollected.ContainsKey(item.name))
		            {
		            	toBeCollected[item.name] -= stack;
		            	
		            	if (toBeCollected[item.name] < 0)
		            	{
		            		if (Math.Abs(toBeCollected[item.name]) > 1)
		            		    player.TSPlayer.SendInfoMessage(string.Format("Returning {0} {1}s", Math.Abs(toBeCollected[item.name]), item.name));
	                        else
	                            player.TSPlayer.SendInfoMessage(string.Format("Returning {0} {1}", Math.Abs(toBeCollected[item.name]), item.name));
	                        player.TSPlayer.GiveItem(item.type, item.name, item.width, item.width, Math.Abs(toBeCollected[item.name]));
	                        toBeCollected.Remove(item.name);
	                        args.Handled = true;
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
		
		public override bool Update()
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
		
		public override bool Update()
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
		
		public override bool Update()
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
	
	public class DeleteBoth : Trigger
	{
		public int x, y;
		
		public DeleteBoth(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		
		public override bool Update()
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
		
		public override bool Update()
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
		
		public override bool Update()
		{
			Main.tile[x, y].active = false;
            Main.tile[x, y].skipLiquid = true;
            Main.tile[x, y].liquid = 0;
            QTools.UpdateTile(x, y);
            return true;
		}
	}
	
	public class Pass : Trigger
	{		
		public override bool Update()
		{
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
		
		public override bool Update()
		{
            return player.TSPlayer.Teleport(x, y + 3);
		}
	}
	
	public class SpawnMob : Trigger
	{
		public string name;
		public int x, y, amount;
		
		public override bool Update()
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
	
	public class ReadNextChatLine : Trigger
	{
		public QPlayer player;
		public string Message;
		public bool hideMessage;
		
		public override void Initialize()
		{
			//NOTE: DEBUG
			ServerHooks.Chat += onChat;
			Console.WriteLine("Chat init");
		}
		
		public ReadNextChatLine(QPlayer player, bool hideMsg = false)
		{
			this.player = player;
			this.hideMessage = hideMsg;
		}
		
		public override void onComplete()
		{
			ServerHooks.Chat -= onChat;
			Console.WriteLine("Chat end");
		}
		
		public override bool Update()
		{
			Console.WriteLine("{0}", Message != null ? "true" : "false");
			if (Message != null)
				return true;
			
			return false;
		}
		
		public void onChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
		{
			Console.WriteLine("{0} || {1} || {2} || {3}", text, ply, player.Index, Message);
			
			if (ply == player.Index)
			{
				Message = text;
				if (hideMessage)
					e.Handled = true;
			}
			
		}
	}
	
	public class Gather : Trigger
	{
		public Item item;
		public int amount;
		private int count = 0;
		public QPlayer player;		
		
		public Gather(QPlayer player, string item, int amount=1)
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
		
		public override bool Update()
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
		public int percentage = 0;
		
		public GetRegionTilePercentage(string tiletype, string regionname)
		{
			this.tiletype = tiletype;
			this.regionname = regionname;
		}
		
		public override bool Update()
		{
			int amountofmatchedtiles = 0;
            int totaltilecount = 0;
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
                percentage = ((amountofmatchedtiles / totaltilecount) * 100);
            return true;
		}
	}
	
	public class GetXYTilePercentage : Trigger
	{
		public string tiletype;
		public int X, Y, Width, Height;
		public int percentage = 0;
		public GetXYTilePercentage(string tiletype, int X, int Y, int Width, int Height)
		{
			this.tiletype = tiletype;
			this.X = X;
			this.Y  = Y;
			this.Width = Width;
			this.Height = Height;
			
		}
		
		public override bool Update()
		{
			int amountofmatchedtiles = 0;
            int totaltilecount = 0;
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
		public int percentage;
		
		public GetRegionWallPercentage(string walltype, string regionname)
		{
			this.regionname = regionname;
			this.walltype = walltype;
		}
		
		public override bool Update()
		{
			int amountofmatchedwalls = 0;
            int totalwallcount = 0;
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
		public int percentage = 0;
		
		public GetXYWallPercentage(string walltype, int X, int Y, int Width, int Height)
		{
			this.walltype = walltype;
			this.X = X;
			this.Y  = Y;
			this.Width = Width;
			this.Height = Height;
		}
		
		public override bool Update()
		{
			int amountofmatchedwalls = 0;
            int totalwallcount = 0;
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
		
		public override bool Update()
		{
			TShock.Utils.Broadcast(message, color);
			return true;
		}
	}
	
	public class Private : Trigger
	{
		public string message;
		public Color color;
		public QPlayer player;
		
		public Private(string message, QPlayer player, Color color)
		{
			this.message = message;
			this.color = color;
			this.player = player;
		}
		
		public override bool Update()
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
		
		public override bool Update()
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
}