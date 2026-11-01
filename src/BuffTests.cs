using System;
using System.Drawing;

namespace TavernLens
{
    public static class BuffTests
    {
        static int count;
        static void Check(bool ok)
        {
            if (!ok)
                throw new Exception("Buff counter regression");
            count++;
        }

        public static int Run()
        {
            count = 0;
            var s = new MatchState
            {
                Stream = @"C:\Logs\Power.log",
                Started = true,
                Mode = "GT_BATTLEGROUNDS"
            };
            var p = s.E(2);
            p.Tags["CARDTYPE"] = "PLAYER";
            p.Tags["PLAYER_ID"] = "1";
            p.Tags["CONTROLLER"] = "1";
            Check(BuffValues.Read(s).Gems == "— / —");
            Check(BuffValues.Read(s).ActiveChips().Count == 0); // nothing buffed yet: no counters shown
            p.Tags["BACON_BLOODGEMBUFFATKVALUE"] = "0";
            p.Tags["BACON_BLOODGEMBUFFHEALTHVALUE"] = "0";
            Check(!BuffValues.Read(s).GemActive); // base +1/+1 Blood Gems are not shown
            p.Tags["BACON_OLD_GOD_ATTACK"] = "1";
            p.Tags["BACON_OLD_GOD_HEALTH"] = "1";
            Check(!BuffValues.Read(s).DeityActive); // a fresh 1/1 Deity is not shown
            p.Tags["BACON_OLD_GOD_ATTACK"] = "3";
            p.Tags["BACON_OLD_GOD_HEALTH"] = "2";
            Check(BuffValues.Read(s).Deity == "+2 / +1" && BuffValues.Read(s).ActiveChips().Count == 1); // Joyous: Deity +2/+1
            p.Tags.Remove("BACON_OLD_GOD_ATTACK");
            p.Tags.Remove("BACON_OLD_GOD_HEALTH");
            p.Tags["BACON_BLOODGEMBUFFATKVALUE"] = "2";
            p.Tags["BACON_BLOODGEMBUFFHEALTHVALUE"] = "5";
            Check(BuffValues.Read(s).Gems == "+3 / +6");
            Check(BuffValues.Read(s).Gems == "+3 / +6");
            var e = s.E(30);
            e.CardId = "BG26_159pe";
            e.Tags["ATTACHED"] = "2";
            e.Tags["TAG_SCRIPT_DATA_NUM_1"] = "4";
            e.Tags["TAG_SCRIPT_DATA_NUM_2"] = "1";
            Check(BuffValues.Read(s).Gems == "+5 / +6");
            e.Tags["ATTACHED"] = "3";
            Check(BuffValues.Read(s).Gems == "+3 / +6");
            p.Tags["TAVERN_SPELL_ATTACK_INCREASE"] = "3";
            p.Tags["TAVERN_SPELL_HEALTH_INCREASE"] = "6";
            Check(BuffValues.Read(s).Spells == "+3 / +6");
            var both = BuffValues.Read(s).ActiveChips();
            Check(both.Count == 2 && both[0].Key == ChipKind.Gem && both[1].Key == ChipKind.Spell);
            s.Mode = "GT_BATTLEGROUNDS_DUO";
            Check(BuffValues.Read(s).Spells == "+3 / +6");
            s.Complete = true;
            Check(BuffValues.Read(s).Gems == "— / —");
            s.Complete = false;
            s.Feed("D 12:00:00 PowerTaskList.DebugPrintPower() - CREATE_GAME");
            Check(BuffValues.Read(s).Spells == "— / —");
            using (var image = new Bitmap(640, 90))
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(24, 23, 29));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                BuffCounters.DrawChip(g, new Rectangle(10, 10, 198, 64), ChipKind.Gem, "+3 / +6");
                BuffCounters.DrawChip(g, new Rectangle(220, 10, 198, 64), ChipKind.Spell, "+3 / +6");
                BuffCounters.DrawChip(g, new Rectangle(430, 10, 198, 64), ChipKind.Deity, "+180 / +176");
                image.Save(System.IO.Path.Combine(Store.Root, "Buff-Counters.png"));
            }

            return count;
        }
    }
}
