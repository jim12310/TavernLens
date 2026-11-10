using System;

namespace TavernLens
{
    public static class StatisticsCredit
    {
        public const string Name = "Firestone public statistics · used with permission";
        public const string Url = "https://www.firestoneapp.com/";
        // Preserve retrieval provenance separately from the statistics' origin.
        // Cached releases must receive the same corrected attribution as fresh data.
        public static void Apply(Meta meta)
        {
            if (meta == null) return;
            bool known = (meta.source ?? "").IndexOf("Firestone", StringComparison.OrdinalIgnoreCase) >= 0
                || (meta.source ?? "").IndexOf("Battlegrounds Buddy", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!known) return;
            if ((meta.url ?? "").StartsWith("https://bgbuddy.gg/", StringComparison.OrdinalIgnoreCase) && String.IsNullOrEmpty(meta.retrievalUrl))
                meta.retrievalUrl = meta.url;
            meta.source = Name;
            meta.url = Url;
        }
    }
}
