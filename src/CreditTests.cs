using System;
namespace TavernLens
{
    public static class CreditTests
    {
        public static int Run()
        {
            var old = new Meta { source="Battlegrounds Buddy / Firestone community data", url="https://bgbuddy.gg/heroes?game_mode=duo", build="256589" };
            StatisticsCredit.Apply(old);
            if (old.source != StatisticsCredit.Name || old.url != StatisticsCredit.Url) throw new Exception("Cached statistics attribution was not corrected");
            if (old.retrievalUrl != "https://bgbuddy.gg/heroes?game_mode=duo" || old.build != "256589") throw new Exception("Retrieval provenance changed");
            StatisticsCredit.Apply(old);
            if (old.retrievalUrl != "https://bgbuddy.gg/heroes?game_mode=duo") throw new Exception("Repeated normalization lost provenance");
            var unknown = new Meta { source="Other source",url="https://example.com/" };
            StatisticsCredit.Apply(unknown);
            if (unknown.source != "Other source") throw new Exception("Unknown source incorrectly relabelled as Firestone");
            return 4;
        }
    }
}
