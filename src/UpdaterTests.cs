using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace TavernLens
{
    public static class UpdaterTests
    {
        public static int Run()
        {
            int count = 0;
            Action<bool, string> check = (ok, name) =>
            {
                if (!ok)
                    throw new Exception("Updater test: " + name);
                count++;
            };
            check(AutoUpdater.IsNewer("1.0.7", "1.0.6"), "new release detected");
            check(!AutoUpdater.IsNewer("1.0.6", "1.0.6"), "same release ignored");
            check(!AutoUpdater.IsNewer("bad", "1.0.6"), "invalid version ignored");
            string file = Path.Combine(Path.GetTempPath(), "TavernLens-updater-test.bin");
            File.WriteAllText(file, "verified update");
            string digest;
            using (var stream = File.OpenRead(file))
            using (var sha = SHA256.Create())
                digest = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            AutoUpdater.Verify(file, digest);
            check(File.Exists(file), "matching checksum accepted");
            bool rejected = false;
            try
            {
                AutoUpdater.Verify(file, new string ('0', 64));
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            check(rejected && !File.Exists(file), "bad checksum rejected and removed");
            // GitHub release parsing
            string hash = new string ('a', 64);
            Func<string, string, string, bool, string> release = (tag, url, dig, pre) => "{\"tag_name\":\"" + tag + "\",\"draft\":false,\"prerelease\":" + (pre ? "true" : "false") + ",\"body\":\"notes\",\"assets\":[" + "{\"name\":\"TavernLens-Setup.exe\",\"browser_download_url\":\"" + url + "\",\"digest\":\"" + dig + "\"}]}";
            string good = "https://github.com/jim12310/TavernLens/releases/download/v1.0.13/TavernLens-Setup.exe";
            var m = AutoUpdater.FromGitHub(release("v1.0.13", good, "sha256:" + hash, false));
            check(m != null && m.version == "1.0.13" && m.url == good && m.sha256 == hash, "GitHub release parsed with digest");
            check(AutoUpdater.FromGitHub(release("v1.0.13", good, "sha256:" + hash, true)) == null, "prerelease ignored");
            check(AutoUpdater.FromGitHub(release("v1.0.13", "https://example.com/TavernLens-Setup.exe", "sha256:" + hash, false)) == null, "foreign download host rejected");
            check(AutoUpdater.FromGitHub(release("v1.0.13", good, "", false)) == null, "release without digest rejected");
            check(AutoUpdater.FromGitHub(release("nightly", good, "sha256:" + hash, false)) == null, "non-version tag rejected");
            // Remote compatibility merge
            Func<string, string, string[], Compatibility> compat = (ver, refVer, builds) => new Compatibility
            {
                engine = "@firestone-hs/simulate-bgs-battle",
                version = ver,
                referenceVersion = refVer,
                status = "experimental-visible-state",
                testedCardBuild = builds[builds.Length - 1],
                testedCardBuilds = new List<string>(builds),
                reviewedAt = "2026-11-01"
            };
            var local = compat("1.1.774", "3.0.219", new[] { "253932", "255790" });
            var merged = RemoteCompatibility.Merge(local, compat("1.1.774", "3.0.219", new[] { "253932", "255790", "256100" }));
            check(merged != null && merged.testedCardBuilds.Contains("256100") && merged.testedCardBuild == "256100", "approved build adopted");
            check(RemoteCompatibility.Merge(local, compat("1.1.780", "3.0.219", new[] { "256100" })) == null, "approval for another simulator version ignored");
            check(RemoteCompatibility.Merge(local, compat("1.1.774", "3.0.220", new[] { "256100" })) == null, "approval for other reference data ignored");
            check(RemoteCompatibility.Merge(local, compat("1.1.774", "3.0.219", new[] { "255790" })) == null, "nothing new means no change");
            check(RemoteCompatibility.Merge(local, compat("1.1.774", "3.0.219", new[] { "../evil" })) == null, "non-numeric build ignored");
            // Messages when odds are withheld
            check(CombatRunner.Explain("Error: New card build: simulator needs a compatibility update").StartsWith("Odds paused"), "untested build explained");
            check(CombatRunner.Explain("Error: Simulator card data is stale").Contains("refreshes"), "stale data explained");
            check(CombatRunner.Explain("TypeError: boom") == "Simulation failed; no odds available", "unexpected errors stay generic");
            // Stale art cleanup
            string data = Path.Combine(Path.GetTempPath(), "TavernLens-art-test-" + Guid.NewGuid().ToString("N"));
            foreach (var dir in new[]
            {
                @"art\verified-v2\100",
                @"art\verified-v2\200",
                @"art\portraits\100",
                @"art\portraits\200",
                @"art\100",
                @"art\verified-v2\keep-me"
            }

            )
            {
                Directory.CreateDirectory(Path.Combine(data, dir));
                File.WriteAllText(Path.Combine(data, dir, "x.png"), "12345");
            }

            long freed = ArtCleanup.Prune(data, "200");
            check(!Directory.Exists(Path.Combine(data, @"art\verified-v2\100")) && !Directory.Exists(Path.Combine(data, @"art\portraits\100")) && !Directory.Exists(Path.Combine(data, @"art\100")), "old build art removed");
            check(Directory.Exists(Path.Combine(data, @"art\verified-v2\200")) && Directory.Exists(Path.Combine(data, @"art\portraits\200")), "current build art kept");
            check(Directory.Exists(Path.Combine(data, @"art\verified-v2\keep-me")), "non-build folders untouched");
            check(freed == 15, "freed bytes reported");
            check(ArtCleanup.Prune(data, "") == 0 && Directory.Exists(Path.Combine(data, @"art\verified-v2\200")), "unknown current build removes nothing");
            Directory.Delete(data, true);
            return count;
        }
    }
}
