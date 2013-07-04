using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;

namespace QuestSystemLUA
{
    public class QNPC
    {
        public static int NewNPC(int X, int Y, int Type, int Start = 0)
        {
            int num = -1;
            for (int i = Start; i < 1000; i++)
            {
                if (!Main.npc[i].active)
                {
                    num = i;
                    break;
                }
            }
            if (num < 0)
            {
                return 1000;
            }
            Main.npc[num] = new NPC();
            Main.npc[num].SetDefaults(Type, -1f);
            Main.npc[num].position.X = (float)(X - Main.npc[num].width / 2);
            Main.npc[num].position.Y = (float)(Y - Main.npc[num].height);
            Main.npc[num].active = true;
            Main.npc[num].timeLeft = (int)((double)750 * 1.25);
            Main.npc[num].wet = Collision.WetCollision(Main.npc[num].position, Main.npc[num].width, Main.npc[num].height);
            return num;
        }
    }
}
