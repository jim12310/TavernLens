using System;
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
        Deity
    }

    public class BuffValues
    {
        public int? GemAttack, GemHealth, SpellAttack, SpellHealth, DeityAttack, DeityHealth;
        // A counter only appears once it has been buffed: Blood Gems above their base +1/+1, Tavern spells above +0/+0,
        // and the Deity (buffed by Aberrations) above its starting 1/1.
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
                return (SpellAttack ?? 0) > 0 || (SpellHealth ?? 0) > 0;
            }
        }

        public bool DeityActive
        {
            get
            {
                return (DeityAttack ?? 0) > 0 || (DeityHealth ?? 0) > 0;
            }
        }

        public string Deity
        {
            get
            {
                return Pair(DeityAttack, DeityHealth);
            }
        }

        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ChipKind, string>> ActiveChips()
        {
            var chips = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ChipKind, string>>();
            if (GemActive)
                chips.Add(new System.Collections.Generic.KeyValuePair<ChipKind, string>(ChipKind.Gem, Gems));
            if (SpellActive)
                chips.Add(new System.Collections.Generic.KeyValuePair<ChipKind, string>(ChipKind.Spell, Spells));
            if (DeityActive)
                chips.Add(new System.Collections.Generic.KeyValuePair<ChipKind, string>(ChipKind.Deity, Deity));
            return chips;
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
            var gems = state.Entities.Values.Where(e => e.CardId == "BG26_159pe" && e.N("ATTACHED") == player.Id && e.S("ZONE") != "GRAVEYARD" && e.S("ZONE") != "REMOVEDFROMGAME").ToArray();
            result.GemAttack = Gem(player, "BACON_BLOODGEMBUFFATKVALUE", gems, "TAG_SCRIPT_DATA_NUM_1");
            result.GemHealth = Gem(player, "BACON_BLOODGEMBUFFHEALTHVALUE", gems, "TAG_SCRIPT_DATA_NUM_2");
            result.SpellAttack = Value(player, "TAVERN_SPELL_ATTACK_INCREASE");
            result.SpellHealth = Value(player, "TAVERN_SPELL_HEALTH_INCREASE");
            // The Deity's current stats; it starts at 1/1, so the buff from Aberrations is the amount above that.
            var deityAttack = Value(player, "BACON_OLD_GOD_ATTACK");
            var deityHealth = Value(player, "BACON_OLD_GOD_HEALTH");
            result.DeityAttack = deityAttack.HasValue && deityAttack.Value > 0 ? (int? )(deityAttack.Value - 1) : null;
            result.DeityHealth = deityHealth.HasValue && deityHealth.Value > 0 ? (int? )(deityHealth.Value - 1) : null;
            return result;
        }

        static int? Value(Entity e, string tag)
        {
            int n;
            return e.Tags.ContainsKey(tag) && Int32.TryParse(e.Tags[tag], out n) && n >= 0 ? (int? )n : null;
        }

        static int? Gem(Entity player, string tag, Entity[] enchants, string script)
        {
            int? bonus = Value(player, tag);
            foreach (var e in enchants)
            {
                var v = Value(e, script);
                if (v.HasValue)
                    bonus = bonus.HasValue ? Math.Max(bonus.Value, v.Value) : v;
            }

            return bonus.HasValue ? (int? )(1 + bonus.Value) : null;
        }
    }

    public class BuffCounters : Floating
    {
        readonly Main main;
        BuffValues values = new BuffValues();
        System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ChipKind, string>> chips = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ChipKind, string>>();
        string signature = "";
        const int ChipSpacing = 205;
        public BuffCounters(Main m)
        {
            main = m;
            Text = "Blood Gem, Tavern spell and Deity bonuses";
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
            int width = Math.Min(144 * chips.Count, game.Width / 2);
            Place(new Rectangle(game.Left + (int)(game.Width * .16), game.Top + (int)(game.Height * .79), width, 50));
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
            using (var image = new Bitmap(ChipSpacing * chips.Count, 70))
            {
                using (var g = Graphics.FromImage(image))
                {
                    g.Clear(BackColor);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    for (int i = 0; i < chips.Count; i++)
                        DrawChip(g, new Rectangle(2 + i * ChipSpacing, 2, 197, 64), chips[i].Key, chips[i].Value);
                }

                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(image, ClientRectangle);
            }
        }

        public static void DrawChip(Graphics g, Rectangle r, ChipKind kind, string value)
        {
            bool spell = kind == ChipKind.Spell;
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
            var accent = kind == ChipKind.Spell ? Color.FromArgb(198, 112, 247) : kind == ChipKind.Deity ? Color.FromArgb(118, 214, 140) : Color.FromArgb(245, 174, 85);
            using (var b = new LinearGradientBrush(circle, accent, Color.FromArgb(55, 20, 43), 60))
                g.FillEllipse(b, circle);
            using (var p = new Pen(accent, 2))
                g.DrawEllipse(p, circle);
            if (kind == ChipKind.Deity)
            {
                // An eye with tentacles below it, for the Old God Deity that Aberrations buff.
                var eye = new Rectangle(r.X + 11, r.Y + 13, 30, 18);
                using (var b = new SolidBrush(Color.FromArgb(28, 70, 40)))
                    g.FillEllipse(b, eye);
                using (var p = new Pen(Color.FromArgb(205, 255, 214), 2))
                {
                    g.DrawEllipse(p, eye);
                    g.DrawBezier(p, r.X + 17, r.Y + 31, r.X + 13, r.Y + 37, r.X + 19, r.Y + 41, r.X + 15, r.Y + 45);
                    g.DrawBezier(p, r.X + 26, r.Y + 32, r.X + 24, r.Y + 38, r.X + 28, r.Y + 42, r.X + 26, r.Y + 47);
                    g.DrawBezier(p, r.X + 35, r.Y + 31, r.X + 39, r.Y + 37, r.X + 33, r.Y + 41, r.X + 37, r.Y + 45);
                }

                using (var b = new SolidBrush(Color.FromArgb(232, 255, 236)))
                    g.FillEllipse(b, new Rectangle(r.X + 21, r.Y + 17, 10, 10));
                using (var b = new SolidBrush(Color.FromArgb(20, 40, 26)))
                    g.FillEllipse(b, new Rectangle(r.X + 24, r.Y + 20, 4, 4));
            }
            else if (!spell)
            {
                var pts = new[]
                {
                    new Point(r.X + 23, r.Y + 8),
                    new Point(r.X + 39, r.Y + 25),
                    new Point(r.X + 29, r.Y + 43),
                    new Point(r.X + 12, r.Y + 35),
                    new Point(r.X + 13, r.Y + 17)
                };
                using (var b = new SolidBrush(Color.FromArgb(181, 23, 47)))
                    g.FillPolygon(b, pts);
                using (var p = new Pen(Color.FromArgb(255, 142, 153), 2))
                {
                    g.DrawPolygon(p, pts);
                    g.DrawLine(p, pts[0], pts[3]);
                    g.DrawLine(p, pts[0], pts[2]);
                }
            }
            else
            {
                using (var p = new Pen(Color.FromArgb(255, 226, 255), 3))
                {
                    g.DrawLine(p, r.X + 17, r.Y + 35, r.X + 31, r.Y + 17);
                    g.DrawLine(p, r.X + 31, r.Y + 10, r.X + 31, r.Y + 23);
                    g.DrawLine(p, r.X + 25, r.Y + 16, r.X + 38, r.Y + 16);
                    g.DrawLine(p, r.X + 36, r.Y + 31, r.X + 36, r.Y + 40);
                    g.DrawLine(p, r.X + 32, r.Y + 35, r.X + 40, r.Y + 35);
                }
            }

            using (var f = Theme.Font(13, true))
                TextRenderer.DrawText(g, value, f, new Rectangle(r.X + 54, r.Y + 16, r.Width - 57, 28), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            using (var f = Theme.Font(7.5f, true))
                TextRenderer.DrawText(g, kind == ChipKind.Spell ? "TAVERN SPELL +" : kind == ChipKind.Deity ? "DEITY · ABERRATIONS" : "BLOOD GEM", f, new Rectangle(r.X, r.Y + 51, r.Width, 13), accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
    }
}
