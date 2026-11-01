using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TavernLens
{
    public enum ChipKind
    {
        Gem,
        Spell,
        Deity,
        Beetle,
        Tavern,
        Elemental,
        Undead
    }

    public class BuffValues
    {
        public int? GemAttack, GemHealth, SpellAttack, SpellHealth, DeityAttack, DeityHealth;
        public int? BeetleAttack, BeetleHealth, TavernAttack, TavernHealth, ElementalAttack, ElementalHealth, UndeadAttack;
        // A counter only appears once it has been buffed: Blood Gems above their base +1/+1, the Deity (buffed by
        // Aberrations) above its starting 1/1, and every other counter above +0.
        public bool GemActive
        {
            get
            {
                return (GemAttack ?? 1) > 1 || (GemHealth ?? 1) > 1;
            }
        }

        public bool SpellActive
        {
            get
            {
                return Positive(SpellAttack, SpellHealth);
            }
        }

        public bool DeityActive
        {
            get
            {
                return Positive(DeityAttack, DeityHealth);
            }
        }

        public bool BeetleActive
        {
            get
            {
                return Positive(BeetleAttack, BeetleHealth);
            }
        }

        public bool TavernActive
        {
            get
            {
                return Positive(TavernAttack, TavernHealth);
            }
        }

        public bool ElementalActive
        {
            get
            {
                return Positive(ElementalAttack, ElementalHealth);
            }
        }

        public bool UndeadActive
        {
            get
            {
                return Positive(UndeadAttack, null);
            }
        }

        static bool Positive(int? a, int? h)
        {
            return (a ?? 0) > 0 || (h ?? 0) > 0;
        }

        public string Gems
        {
            get
            {
                return Pair(GemAttack, GemHealth);
            }
        }

        public string Spells
        {
            get
            {
                return Pair(SpellAttack, SpellHealth);
            }
        }

        public string Deity
        {
            get
            {
                return Pair(DeityAttack, DeityHealth);
            }
        }

        public string Undead
        {
            get
            {
                return UndeadAttack.HasValue ? "+" + UndeadAttack.Value + " ATK" : "—";
            }
        }

        public List<KeyValuePair<ChipKind, string>> ActiveChips()
        {
            var chips = new List<KeyValuePair<ChipKind, string>>();
            Action<bool, ChipKind, string> add = (active, kind, text) =>
            {
                if (active)
                    chips.Add(new KeyValuePair<ChipKind, string>(kind, text));
            };
            add(GemActive, ChipKind.Gem, Gems);
            add(SpellActive, ChipKind.Spell, Spells);
            add(DeityActive, ChipKind.Deity, Deity);
            add(BeetleActive, ChipKind.Beetle, Pair(BeetleAttack, BeetleHealth));
            add(TavernActive, ChipKind.Tavern, Pair(TavernAttack, TavernHealth));
            add(ElementalActive, ChipKind.Elemental, Pair(ElementalAttack, ElementalHealth));
            add(UndeadActive, ChipKind.Undead, Undead);
            return chips;
        }

        static string Pair(int? a, int? h)
        {
            return (a.HasValue ? "+" + a.Value : "—") + " / " + (h.HasValue ? "+" + h.Value : "—");
        }

        public static BuffValues Read(MatchState state)
        {
            var result = new BuffValues();
            var player = state.Player;
            if (!state.Started || state.Complete || !state.Battlegrounds || player == null)
                return result;
            // These are cumulative values, not deltas. Replaying a tag must never add it twice.
            var gems = Enchantments(state, player, "BG26_159pe");
            result.GemAttack = Gem(player, "BACON_BLOODGEMBUFFATKVALUE", gems, "TAG_SCRIPT_DATA_NUM_1");
            result.GemHealth = Gem(player, "BACON_BLOODGEMBUFFHEALTHVALUE", gems, "TAG_SCRIPT_DATA_NUM_2");
            result.SpellAttack = Value(player, "TAVERN_SPELL_ATTACK_INCREASE");
            result.SpellHealth = Value(player, "TAVERN_SPELL_HEALTH_INCREASE");
            // The Deity's current stats; it starts at 1/1, so the buff from Aberrations is the amount above that.
            var deityAttack = Value(player, "BACON_OLD_GOD_ATTACK");
            var deityHealth = Value(player, "BACON_OLD_GOD_HEALTH");
            result.DeityAttack = deityAttack.HasValue && deityAttack.Value > 0 ? (int? )(deityAttack.Value - 1) : null;
            result.DeityHealth = deityHealth.HasValue && deityHealth.Value > 0 ? (int? )(deityHealth.Value - 1) : null;
            // Beetle buffs live in two unnamed player tags; the client prints them as numbers. Identified from real logs:
            // Forest Rover adds +2/+1 and Ravaging Scorpid +4/+4 to exactly these tags.
            result.BeetleAttack = Value(player, "3874");
            result.BeetleHealth = Value(player, "3924");
            // "Minions/Elementals in the Tavern have +X/+Y this game" are running totals on enchantments attached to the
            // player (attack in script data 1, health in 2). The game sometimes swaps in a fresh copy holding the same
            // total, so the largest live value is used rather than a sum.
            var tavern = Enchantments(state, player, "BG_ShopBuff");
            result.TavernAttack = Largest(tavern, "TAG_SCRIPT_DATA_NUM_1");
            result.TavernHealth = Largest(tavern, "TAG_SCRIPT_DATA_NUM_2");
            var elemental = Enchantments(state, player, "BG_ShopBuff_Elemental");
            result.ElementalAttack = Largest(elemental, "TAG_SCRIPT_DATA_NUM_1");
            result.ElementalHealth = Largest(elemental, "TAG_SCRIPT_DATA_NUM_2");
            // Undead get Attack only, held on the "Undead Bonus Attack" player enchantment.
            var undead = Largest(Enchantments(state, player, "BG25_011pe"), "TAG_SCRIPT_DATA_NUM_1");
            var undeadTag = Value(player, "BACON_UNDEAD_ATTACK_BONUS");
            result.UndeadAttack = undead.HasValue && undeadTag.HasValue ? Math.Max(undead.Value, undeadTag.Value) : undead ?? undeadTag;
            return result;
        }

        static Entity[] Enchantments(MatchState state, Entity player, string cardId)
        {
            return state.Entities.Values.Where(e => e.CardId == cardId && e.N("ATTACHED") == player.Id && e.S("ZONE") != "GRAVEYARD" && e.S("ZONE") != "REMOVEDFROMGAME").ToArray();
        }

        static int? Largest(Entity[] enchants, string script)
        {
            int? best = null;
            foreach (var e in enchants)
            {
                var v = Value(e, script);
                if (v.HasValue)
                    best = best.HasValue ? Math.Max(best.Value, v.Value) : v;
            }

            return best;
        }

        static int? Value(Entity e, string tag)
        {
            int n;
            return e.Tags.ContainsKey(tag) && Int32.TryParse(e.Tags[tag], out n) && n >= 0 ? (int? )n : null;
        }

        static int? Gem(Entity player, string tag, Entity[] enchants, string script)
        {
            int? bonus = Value(player, tag);
            var fromEnchant = Largest(enchants, script);
            if (fromEnchant.HasValue)
                bonus = bonus.HasValue ? Math.Max(bonus.Value, fromEnchant.Value) : fromEnchant;
            return bonus.HasValue ? (int? )(1 + bonus.Value) : null;
        }
    }

    public class BuffCounters : Floating
    {
        readonly Main main;
        BuffValues values = new BuffValues();
        List<KeyValuePair<ChipKind, string>> chips = new List<KeyValuePair<ChipKind, string>>();
        string signature = "";
        const int ChipSpacing = 205, RowSpacing = 70, PerRow = 4;
        public BuffCounters(Main m)
        {
            main = m;
            Text = "Buff counters";
            Size = new Size(288, 50);
            BackColor = Color.FromArgb(1, 2, 3);
            TransparencyKey = BackColor;
            PassThrough(true);
        }

        public void Follow(Rectangle game)
        {
            var s = main.State;
            if (!s.Battlegrounds || !s.Started || s.Complete || s.Phase == "Hero selection" || main.Scenes.InLobby || main.Scenes.Loading)
            {
                Hide();
                return;
            }

            values = BuffValues.Read(s);
            chips = values.ActiveChips();
            if (chips.Count == 0)
            {
                Hide();
                return;
            }

            string next = s.Key + "|" + string.Join(";", chips.Select(c => c.Key + "=" + c.Value));
            // Up to four chips per row; more wrap onto a row above so the panel grows upwards from the same spot.
            int cols = Math.Min(PerRow, chips.Count), rows = (chips.Count + PerRow - 1) / PerRow;
            int width = Math.Min(144 * cols, game.Width / 2), height = 50 * rows;
            Place(new Rectangle(game.Left + (int)(game.Width * .16), game.Top + (int)(game.Height * .79) + 50 - height, width, height));
            if (next != signature)
            {
                signature = next;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (chips.Count == 0)
                return;
            int cols = Math.Min(PerRow, chips.Count), rows = (chips.Count + PerRow - 1) / PerRow;
            using (var image = new Bitmap(ChipSpacing * cols, RowSpacing * rows))
            {
                using (var g = Graphics.FromImage(image))
                {
                    g.Clear(BackColor);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    for (int i = 0; i < chips.Count; i++)
                        DrawChip(g, new Rectangle(2 + (i % PerRow) * ChipSpacing, 2 + (i / PerRow) * RowSpacing, 197, 64), chips[i].Key, chips[i].Value);
                }

                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(image, ClientRectangle);
            }
        }

        static Color Accent(ChipKind kind)
        {
            switch (kind)
            {
                case ChipKind.Spell:
                    return Color.FromArgb(198, 112, 247);
                case ChipKind.Deity:
                    return Color.FromArgb(118, 214, 140);
                case ChipKind.Beetle:
                    return Color.FromArgb(92, 196, 214);
                case ChipKind.Tavern:
                    return Color.FromArgb(240, 212, 112);
                case ChipKind.Elemental:
                    return Color.FromArgb(255, 122, 72);
                case ChipKind.Undead:
                    return Color.FromArgb(176, 186, 206);
                default:
                    return Color.FromArgb(245, 174, 85);
            }
        }

        static string Label(ChipKind kind)
        {
            switch (kind)
            {
                case ChipKind.Spell:
                    return "TAVERN SPELL +";
                case ChipKind.Deity:
                    return "DEITY · ABERRATIONS";
                case ChipKind.Beetle:
                    return "BEETLES";
                case ChipKind.Tavern:
                    return "TAVERN MINIONS";
                case ChipKind.Elemental:
                    return "TAVERN ELEMENTALS";
                case ChipKind.Undead:
                    return "UNDEAD ATTACK";
                default:
                    return "BLOOD GEM";
            }
        }

        public static void DrawChip(Graphics g, Rectangle r, ChipKind kind, string value)
        {
            using (var path = new GraphicsPath())
            {
                int rad = 52;
                path.AddArc(r.X, r.Y, rad, rad, 90, 180);
                path.AddArc(r.Right - rad, r.Y, rad, rad, 270, 180);
                path.CloseFigure();
                using (var b = new SolidBrush(Color.FromArgb(38, 34, 35)))
                    g.FillPath(b, path);
                using (var p = new Pen(Color.FromArgb(14, 12, 13), 2))
                    g.DrawPath(p, path);
            }

            var circle = new Rectangle(r.X + 3, r.Y + 3, 46, 46);
            var accent = Accent(kind);
            using (var b = new LinearGradientBrush(circle, accent, Color.FromArgb(55, 20, 43), 60))
                g.FillEllipse(b, circle);
            using (var p = new Pen(accent, 2))
                g.DrawEllipse(p, circle);
            DrawIcon(g, r, kind);
            using (var f = Theme.Font(13, true))
                TextRenderer.DrawText(g, value, f, new Rectangle(r.X + 54, r.Y + 16, r.Width - 57, 28), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            using (var f = Theme.Font(7.5f, true))
                TextRenderer.DrawText(g, Label(kind), f, new Rectangle(r.X, r.Y + 51, r.Width, 13), accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }

        static void DrawIcon(Graphics g, Rectangle r, ChipKind kind)
        {
            int x = r.X, y = r.Y;
            switch (kind)
            {
                case ChipKind.Deity:
                {
                    // An eye with tentacles below it, for the Old God Deity that Aberrations buff.
                    var eye = new Rectangle(x + 11, y + 13, 30, 18);
                    using (var b = new SolidBrush(Color.FromArgb(28, 70, 40)))
                        g.FillEllipse(b, eye);
                    using (var p = new Pen(Color.FromArgb(205, 255, 214), 2))
                    {
                        g.DrawEllipse(p, eye);
                        g.DrawBezier(p, x + 17, y + 31, x + 13, y + 37, x + 19, y + 41, x + 15, y + 45);
                        g.DrawBezier(p, x + 26, y + 32, x + 24, y + 38, x + 28, y + 42, x + 26, y + 47);
                        g.DrawBezier(p, x + 35, y + 31, x + 39, y + 37, x + 33, y + 41, x + 37, y + 45);
                    }

                    using (var b = new SolidBrush(Color.FromArgb(232, 255, 236)))
                        g.FillEllipse(b, new Rectangle(x + 21, y + 17, 10, 10));
                    using (var b = new SolidBrush(Color.FromArgb(20, 40, 26)))
                        g.FillEllipse(b, new Rectangle(x + 24, y + 20, 4, 4));
                    break;
                }

                case ChipKind.Spell:
                {
                    using (var p = new Pen(Color.FromArgb(255, 226, 255), 3))
                    {
                        g.DrawLine(p, x + 17, y + 35, x + 31, y + 17);
                        g.DrawLine(p, x + 31, y + 10, x + 31, y + 23);
                        g.DrawLine(p, x + 25, y + 16, x + 38, y + 16);
                        g.DrawLine(p, x + 36, y + 31, x + 36, y + 40);
                        g.DrawLine(p, x + 32, y + 35, x + 40, y + 35);
                    }

                    break;
                }

                case ChipKind.Beetle:
                {
                    // A beetle seen from above: shell split down the middle, head and six legs.
                    using (var p = new Pen(Color.FromArgb(214, 248, 255), 2))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            g.DrawLine(p, x + 17, y + 20 + i * 7, x + 10, y + 17 + i * 9);
                            g.DrawLine(p, x + 35, y + 20 + i * 7, x + 42, y + 17 + i * 9);
                        }
                    }

                    using (var b = new SolidBrush(Color.FromArgb(24, 74, 86)))
                        g.FillEllipse(b, new Rectangle(x + 15, y + 15, 22, 28));
                    using (var b = new SolidBrush(Color.FromArgb(214, 248, 255)))
                        g.FillEllipse(b, new Rectangle(x + 20, y + 8, 12, 9));
                    using (var p = new Pen(Color.FromArgb(214, 248, 255), 2))
                    {
                        g.DrawEllipse(p, new Rectangle(x + 15, y + 15, 22, 28));
                        g.DrawLine(p, x + 26, y + 16, x + 26, y + 42);
                    }

                    break;
                }

                case ChipKind.Tavern:
                {
                    // A tankard: the tavern's own minions.
                    using (var b = new SolidBrush(Color.FromArgb(92, 62, 22)))
                        g.FillRectangle(b, new Rectangle(x + 13, y + 18, 20, 23));
                    using (var p = new Pen(Color.FromArgb(255, 238, 180), 2))
                    {
                        g.DrawRectangle(p, new Rectangle(x + 13, y + 18, 20, 23));
                        g.DrawArc(p, new Rectangle(x + 27, y + 22, 13, 13), -90, 180);
                        g.DrawLine(p, x + 19, y + 22, x + 19, y + 37);
                        g.DrawLine(p, x + 26, y + 22, x + 26, y + 37);
                    }

                    using (var b = new SolidBrush(Color.FromArgb(255, 250, 230)))
                    {
                        g.FillEllipse(b, new Rectangle(x + 11, y + 11, 11, 10));
                        g.FillEllipse(b, new Rectangle(x + 18, y + 9, 11, 11));
                        g.FillEllipse(b, new Rectangle(x + 25, y + 11, 10, 10));
                    }

                    break;
                }

                case ChipKind.Elemental:
                {
                    // A flame.
                    using (var flame = new GraphicsPath())
                    {
                        flame.AddBezier(x + 26, y + 8, x + 32, y + 18, x + 40, y + 24, x + 38, y + 34);
                        flame.AddBezier(x + 38, y + 34, x + 36, y + 44, x + 16, y + 45, x + 14, y + 34);
                        flame.AddBezier(x + 14, y + 34, x + 13, y + 26, x + 20, y + 22, x + 21, y + 15);
                        flame.AddBezier(x + 21, y + 15, x + 24, y + 19, x + 25, y + 21, x + 26, y + 8);
                        flame.CloseFigure();
                        using (var b = new SolidBrush(Color.FromArgb(255, 150, 60)))
                            g.FillPath(b, flame);
                        using (var p = new Pen(Color.FromArgb(255, 236, 170), 2))
                            g.DrawPath(p, flame);
                    }

                    using (var b = new SolidBrush(Color.FromArgb(255, 236, 150)))
                        g.FillEllipse(b, new Rectangle(x + 21, y + 28, 10, 12));
                    break;
                }

                case ChipKind.Undead:
                {
                    // A skull.
                    using (var b = new SolidBrush(Color.FromArgb(232, 236, 244)))
                    {
                        g.FillEllipse(b, new Rectangle(x + 12, y + 9, 28, 25));
                        g.FillRectangle(b, new Rectangle(x + 17, y + 30, 18, 11));
                    }

                    using (var b = new SolidBrush(Color.FromArgb(40, 42, 54)))
                    {
                        g.FillEllipse(b, new Rectangle(x + 17, y + 18, 7, 8));
                        g.FillEllipse(b, new Rectangle(x + 28, y + 18, 7, 8));
                        g.FillPolygon(b, new[] { new Point(x + 26, y + 27), new Point(x + 23, y + 32), new Point(x + 29, y + 32) });
                    }

                    using (var p = new Pen(Color.FromArgb(40, 42, 54), 1.5f))
                    {
                        g.DrawLine(p, x + 22, y + 34, x + 22, y + 41);
                        g.DrawLine(p, x + 26, y + 34, x + 26, y + 41);
                        g.DrawLine(p, x + 30, y + 34, x + 30, y + 41);
                    }

                    break;
                }

                default:
                {
                    var pts = new[]
                    {
                        new Point(x + 23, y + 8),
                        new Point(x + 39, y + 25),
                        new Point(x + 29, y + 43),
                        new Point(x + 12, y + 35),
                        new Point(x + 13, y + 17)
                    };
                    using (var b = new SolidBrush(Color.FromArgb(181, 23, 47)))
                        g.FillPolygon(b, pts);
                    using (var p = new Pen(Color.FromArgb(255, 142, 153), 2))
                    {
                        g.DrawPolygon(p, pts);
                        g.DrawLine(p, pts[0], pts[3]);
                        g.DrawLine(p, pts[0], pts[2]);
                    }

                    break;
                }
            }
        }
    }
}
