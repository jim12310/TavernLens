using System;
using System.Collections.Generic;

namespace TavernLens
{
    // Hero badge order across the reveal animation, the pick and a reroll (pattern taken from real Solo and Duos logs).
    public static class HeroOrderTests
    {
        public static int Run()
        {
            int count = 0;
            Action<bool, string> check = (ok, name) =>
            {
                if (!ok)
                    throw new Exception("Hero order test: " + name);
                count++;
            };
            var s = new MatchState();
            s.Entities[2] = new Entity
            {
                Id = 2,
                Tags = new Dictionary<string, string>
                {
                    {
                        "CARDTYPE",
                        "PLAYER"
                    },
                    {
                        "PLAYER_ID",
                        "1"
                    },
                    {
                        "CONTROLLER",
                        "1"
                    }
                }
            };
            Action<int, string, string, int> hero = (id, card, zone, pos) => s.Entities[id] = new Entity
            {
                Id = id,
                CardId = card,
                Tags = new Dictionary<string, string>
                {
                    {
                        "CONTROLLER",
                        "1"
                    },
                    {
                        "ZONE",
                        zone
                    },
                    {
                        "ZONE_POSITION",
                        pos.ToString()
                    }
                }
            };
            s.Choices.AddRange(new[] { "A", "B", "C", "D" });
            s.ChoicePositions["A"] = 1;
            s.ChoicePositions["B"] = 2;
            s.ChoicePositions["C"] = 3;
            s.ChoicePositions["D"] = 4;
            hero(101, "A", "HAND", 3);
            hero(102, "B", "HAND", 1);
            hero(103, "C", "HAND", 2);
            hero(104, "D", "HAND", 4);
            var order = HeroChoices.Order(s, null);
            check(string.Join(",", order) == "B,C,A,D", "live hand positions decide the order while all heroes are in hand");
            hero(103, "C", "SETASIDE", 0);
            check(string.Join(",", HeroChoices.Order(s, order)) == "B,C,A,D", "order held while a hero is mid-animation");
            hero(103, "C", "HAND", 2);
            // Pick A: it leaves the hand, so its position falls back to the original offer (1) and collides with B.
            hero(101, "A", "PLAY", 0);
            hero(104, "D", "HAND", 3);
            check(string.Join(",", HeroChoices.Order(s, order)) == "B,C,A,D", "badges stay above the same heroes after the pick");
            // A reroll replaces D with E: a different set of heroes, so the order is recomputed.
            hero(101, "A", "HAND", 3);
            hero(104, "D", "SETASIDE", 0);
            s.Choices.Remove("D");
            s.Choices.Add("E");
            hero(105, "E", "HAND", 4);
            check(string.Join(",", HeroChoices.Order(s, order)) == "B,C,A,E", "reroll re-sorts with the new hero");
            return count;
        }
    }
}
