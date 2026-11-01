using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TavernLens
{
    // Diagnostic: TavernLens.exe --buff-replay <Power.log> <output.json>
    // Feeds a saved log through the real reader and records the highest value each buff counter reached per game,
    // so counter readings can be checked against real games.
    public static class BuffReplay
    {
        public static void Run(string log, string output)
        {
            var state = new MatchState
            {
                Stream = log
            };
            var games = new List<Dictionary<string, object>>();
            Dictionary<string, int> peaks = null;
            string key = null;
            int lines = 0;
            Action<string, int?> peak = (name, v) =>
            {
                if (v.HasValue && v.Value > 0 && (!peaks.ContainsKey(name) || peaks[name] < v.Value))
                    peaks[name] = v.Value;
            };
            // Shared read access: the game may still be writing this log.
            using (var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
                for (string line; (line = reader.ReadLine()) != null;)
                {
                    state.Feed(line);
                    if (++lines % 200 != 0 || !state.Started)
                        continue;
                    if (state.Key != key)
                    {
                        key = state.Key;
                        peaks = new Dictionary<string, int>();
                        games.Add(new Dictionary<string, object> { { "game", key }, { "mode", state.Mode }, { "peaks", peaks } });
                    }

                    var v = BuffValues.Read(state);
                    peak("bloodGemAttack", v.GemActive ? v.GemAttack : null);
                    peak("bloodGemHealth", v.GemActive ? v.GemHealth : null);
                    peak("tavernSpellAttack", v.SpellAttack);
                    peak("tavernSpellHealth", v.SpellHealth);
                    peak("deityAttack", v.DeityAttack);
                    peak("deityHealth", v.DeityHealth);
                    peak("beetleAttack", v.BeetleAttack);
                    peak("beetleHealth", v.BeetleHealth);
                    peak("tavernAttack", v.TavernAttack);
                    peak("tavernHealth", v.TavernHealth);
                    peak("elementalAttack", v.ElementalAttack);
                    peak("elementalHealth", v.ElementalHealth);
                    peak("undeadAttack", v.UndeadAttack);
                }

            File.WriteAllText(output, Store.Json().Serialize(new { log, lines, games = games.Where(g => ((Dictionary<string, int>)g["peaks"]).Count > 0) }));
        }
    }
}
