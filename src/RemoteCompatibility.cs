using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace TavernLens
{
    public class Compatibility
    {
        public string engine, version, referenceVersion, testedCardBuild, status, reviewedAt;
        public List<string> testedCardBuilds = new List<string>();
    }

    // Lets combat odds resume after a game patch without a reinstall. The repository's simulator/compatibility.json
    // lists the card builds that passed the combat and Duos tests. Approvals are only adopted when they were made for
    // exactly the simulator and reference-data versions installed here, so an approval can never apply to other code.
    public static class RemoteCompatibility
    {
        const string Url = "https://raw.githubusercontent.com/jim12310/TavernLens/main/simulator/compatibility.json";
        static string LocalFile
        {
            get
            {
                return Path.Combine(Store.Root, "simulator", "compatibility.json");
            }
        }

        public static void Sync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                string remoteJson;
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "TavernLens/" + AutoUpdater.CurrentVersion;
                    remoteJson = client.DownloadString(Url);
                }

                var local = Store.Json().Deserialize<Compatibility>(File.ReadAllText(LocalFile));
                var remote = Store.Json().Deserialize<Compatibility>(remoteJson);
                var merged = Merge(local, remote);
                if (merged == null)
                    return;
                string temp = LocalFile + ".tmp";
                File.WriteAllText(temp, Store.Json().Serialize(merged));
                File.Copy(temp, LocalFile, true);
                File.Delete(temp);
            }
            catch (Exception)
            {
            // Offline or unexpected content: keep the installed list.
            }
        }

        // Returns the local list extended with remotely approved builds, or null when nothing should change.
        public static Compatibility Merge(Compatibility local, Compatibility remote)
        {
            if (local == null || remote == null)
                return null;
            if (remote.engine != local.engine || remote.version != local.version || remote.referenceVersion != local.referenceVersion)
                return null;
            var added = (remote.testedCardBuilds ?? new List<string>()).Where(b => !String.IsNullOrEmpty(b) && b.All(char.IsDigit) && !local.testedCardBuilds.Contains(b)).ToList();
            if (added.Count == 0)
                return null;
            var result = new Compatibility
            {
                engine = local.engine,
                version = local.version,
                referenceVersion = local.referenceVersion,
                status = local.status,
                testedCardBuilds = local.testedCardBuilds.Concat(added).ToList(),
                testedCardBuild = added.Last(),
                reviewedAt = remote.reviewedAt ?? local.reviewedAt,
            };
            return result;
        }
    }
}
