using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.Net;
using System.IO.Streams;

namespace QuestSystemLUA
{
    public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

    public static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;
        public static void InitGetDataHandler()
        {
            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.NpcStrike, OnNpcStrike},
                {PacketTypes.Tile, HandleTile},
                {PacketTypes.TileSendSquare, HandleSendTileSquare},
                {PacketTypes.TileKill, HandleTileKill},
                {PacketTypes.ItemDrop, HandleDropItem}
            };
        }
        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }
        private static bool OnNpcStrike(GetDataHandlerArgs args)
        {
            short npcid = args.Data.ReadInt16();
            int damage = args.Data.ReadByte();
            NPC npc = Main.npc[(int)npcid];
            if (npc.life - damage <= 0)
            {
                var player = QTools.GetPlayerByID(args.Player.Index);
                if (player.AwaitingKill && player.KillNames.Contains(npc.name))
                    player.KillNames.Remove(npc.name);
                if (player.RunningQuests.Count > 0)
                {
                    foreach (QuestParty pty in player.CurrentParties)
                    {
                        if (pty.AwaitingKill.Contains(npc.name))
                        {
                            pty.AwaitingKill.Remove(npc.name);
                            if (pty.AwaitingKill.Count == 0)
                            {
                                pty.ObjComplete = true;
                                QMain.QuestParties.Remove(pty);
                            }
                        }
                    }
                }
            }
            return false;
        }
        private static bool HandleTile(GetDataHandlerArgs args)
        {
            byte type = args.Data.ReadInt8();
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            byte tiletype = args.Data.ReadInt8();
            var player = QTools.GetPlayerByID(args.Player.Index);
            player.LastTileHitX = x;
            player.LastTileHitY = y;
            if (player.AwaitingQRName)
            {
                player.AwaitingQRName = false;
                if (QTools.InQuestRegion(x, y) == null)
                    args.Player.SendMessage("Tile is not in any Quest Region", Color.Yellow);
                else
                    args.Player.SendMessage("Quest Region Name: " + QTools.InQuestRegion(x, y), Color.Yellow);
            }
            if (player.AwaitingHitCoords)
            {
                player.TSPlayer.SendMessage("X:" + x + ", Y:" + y);
                args.Player.SendTileSquare(x, y);
                player.AwaitingHitCoords = false;
                return true;
            }
            return false;
        }
        private static bool HandleTileKill(GetDataHandlerArgs args)
        {
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            var player = QTools.GetPlayerByID(args.Player.Index);
            player.LastTileHitX = x;
            player.LastTileHitY = y;
            if (player.AwaitingQRName)
            {
                player.AwaitingQRName = false;
                if (QTools.InQuestRegion(x, y) == null)
                    args.Player.SendMessage("Tile is not in any Quest Region", Color.Yellow);
                else
                    args.Player.SendMessage("Quest Region Name: " + QTools.InQuestRegion(x, y), Color.Yellow);
            }
            if (player.AwaitingHitCoords)
            {
                player.TSPlayer.SendMessage("X:" + x + ", Y:" + y);
                args.Player.SendTileSquare(x, y);
                player.AwaitingHitCoords = false;
                return true;
            }
            return false;
        }
        private static bool HandleSendTileSquare(GetDataHandlerArgs args)
        {
            short size = args.Data.ReadInt16();
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            var player = QTools.GetPlayerByID(args.Player.Index);
            player.LastTileHitX = x;
            player.LastTileHitY = y;
            if (player.AwaitingHitCoords)
            {
                player.TSPlayer.SendMessage("X:" + x + ", Y:" + y);
                args.Player.SendTileSquare(x, y);
                player.AwaitingHitCoords = false;
                return true;
            }
            return false;
        }
        private static bool HandleLiquidSet(GetDataHandlerArgs args)
        {
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            var player = QTools.GetPlayerByID(args.Player.Index);
            player.LastTileHitX = x;
            player.LastTileHitY = y;
            if (player.AwaitingHitCoords)
            {
                player.TSPlayer.SendMessage("X:" + x + ", Y:" + y);
                args.Player.SendTileSquare(x, y);
                player.AwaitingHitCoords = false;
                return true;
            }
            return false;
        }
        private static bool HandleDropItem(GetDataHandlerArgs args)
        {
            var player = QTools.GetPlayerByID(args.Player.Index);
            var reader = new BinaryReader(args.Data);
            var id = reader.ReadInt16();
            var posx = reader.ReadSingle();
            var posy = reader.ReadSingle();
            var velx = reader.ReadSingle();
            var vely = reader.ReadSingle();
            var stack = reader.ReadByte();
            var prefix = reader.ReadByte();
            var type = reader.ReadInt16();

            /*var itemnamebytes = new byte[args.Data.Length];
            reader.Read(itemnamebytes, 0, (int)(args.Data.Length));
            reader.Close();
            List<byte> finalbytelist = new List<byte>();

            foreach (byte by in itemnamebytes)
            {
                if (by != 0)
                    finalbytelist.Add(by);
            }

            var itemname = System.Text.Encoding.ASCII.GetString(finalbytelist.ToArray());*/
            var item = new Item();
            item.SetDefaults(type);

            foreach (AwaitingItem aitem in player.AwaitingItems)
            {
                if (aitem.AwaitingItemName == item.name)
                {
                    aitem.AwaitingAmount -= stack;

                    if (aitem.AwaitingAmount < 0)
                    {
                        if (Math.Abs(aitem.AwaitingAmount) > 1)
                            player.TSPlayer.SendMessage(string.Format("Returning {0} {1}s", Math.Abs(aitem.AwaitingAmount), item.name));
                        else
                            player.TSPlayer.SendMessage(string.Format("Returning {0} {1}", Math.Abs(aitem.AwaitingAmount), item.name));

                        player.TSPlayer.GiveItem(item.type, item.name, item.width, item.width, Math.Abs(aitem.AwaitingAmount));
                        player.AwaitingItems.Remove(aitem);
                        return true;
                    }
                    else if (aitem.AwaitingAmount > 0)
                    {
                        if (Math.Abs(aitem.AwaitingAmount) > 1)
                            player.TSPlayer.SendMessage(string.Format("Drop another {0} {1}s, to continue", Math.Abs(aitem.AwaitingAmount), item.name));
                        else
                            player.TSPlayer.SendMessage(string.Format("Drop {0} {1}, to continue", Math.Abs(aitem.AwaitingAmount), item.name));
                        return true;
                    }
                    else
                    {
                        if (stack > 1)
                            player.TSPlayer.SendMessage(string.Format("You dropped {0} {1}s", stack, item.name));
                        else
                            player.TSPlayer.SendMessage(string.Format("You dropped {0} {1}", stack, item.name));

                        player.AwaitingItems.Remove(aitem);
                        return true;
                    }
                }
            }

            return false;
        }
        private static bool OnKillMe(GetDataHandlerArgs args)
        {

            return false;
        }
    }
}