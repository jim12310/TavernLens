using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TavernLens
{
    public class UpdateManifest
    {
        public string version, url, sha256, notes;
    }

    public class GitHubRelease
    {
        public string tag_name, body;
        public bool draft, prerelease;
        public List<GitHubAsset> assets = new List<GitHubAsset>();
    }

    public class GitHubAsset
    {
        public string name, browser_download_url, digest;
    }

    // Finds and installs updates. The latest GitHub release is the primary source (GitHub publishes a SHA-256 digest
    // for every release asset); the website manifest is a fallback when GitHub can't be reached.
    public static class AutoUpdater
    {
        public const string CurrentVersion = "1.0.12";
        const string ReleaseApi = "https://api.github.com/repos/jim12310/TavernLens/releases/latest";
        const string ReleaseDownloadPrefix = "https://github.com/jim12310/TavernLens/releases/download/";
        const string ManifestUrl = "https://tavernlens.pages.dev/update.json";
        const string InstallerName = "TavernLens-Setup.exe";
        static bool checking;
        public static async void Check(Main owner)
        {
            if (checking || Program.DiagnosticMode)
                return;
            checking = true;
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                var manifest = await Task.Run(() => FindUpdate());
                if (manifest == null || !IsNewer(manifest.version, CurrentVersion) || !Uri.IsWellFormedUriString(manifest.url, UriKind.Absolute) || !manifest.url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return;
                owner.SetUpdateStatus("DOWNLOADING UPDATE…");
                string installer = Path.Combine(Path.GetTempPath(), "TavernLens-Setup-" + manifest.version + ".exe");
                await Task.Run(() =>
                {
                    using (var client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "TavernLens/" + CurrentVersion;
                        client.DownloadFile(manifest.url, installer);
                    }

                    Verify(installer, manifest.sha256);
                });
                owner.SetUpdateStatus("UPDATE READY · RESTARTING…");
                Process.Start(new ProcessStartInfo(installer, "/S /FROMAPP") { UseShellExecute = true });
                owner.ExitForUpdate();
            }
            catch (Exception e)
            {
                owner.SetUpdateStatus("UPDATE CHECK FAILED · " + e.Message);
            }
            finally
            {
                checking = false;
            }
        }

        static UpdateManifest FindUpdate()
        {
            try
            {
                var fromGitHub = FromGitHub(Download(ReleaseApi, "application/vnd.github+json"));
                if (fromGitHub != null)
                    return fromGitHub;
            }
            catch (WebException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }

            return Store.Json().Deserialize<UpdateManifest>(Download(ManifestUrl, null));
        }

        static string Download(string url, string accept)
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "TavernLens/" + CurrentVersion;
                if (accept != null)
                    client.Headers[HttpRequestHeader.Accept] = accept;
                return client.DownloadString(url);
            }
        }

        // Converts the GitHub "latest release" response into an update, or null when it isn't usable.
        // Only the project's own release downloads with a published SHA-256 digest are accepted.
        public static UpdateManifest FromGitHub(string json)
        {
            var release = Store.Json().Deserialize<GitHubRelease>(json);
            if (release == null || release.draft || release.prerelease || String.IsNullOrEmpty(release.tag_name))
                return null;
            var asset = (release.assets ?? new List<GitHubAsset>()).FirstOrDefault(a => a.name == InstallerName);
            if (asset == null || String.IsNullOrEmpty(asset.browser_download_url) || !asset.browser_download_url.StartsWith(ReleaseDownloadPrefix, StringComparison.Ordinal))
                return null;
            if (String.IsNullOrEmpty(asset.digest) || !asset.digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                return null;
            string version = release.tag_name.TrimStart('v', 'V');
            Version parsed;
            if (!Version.TryParse(version, out parsed))
                return null;
            return new UpdateManifest
            {
                version = version,
                url = asset.browser_download_url,
                sha256 = asset.digest.Substring(7),
                notes = release.body
            };
        }

        public static bool IsNewer(string available, string installed)
        {
            Version remote, current;
            return Version.TryParse(available, out remote) && Version.TryParse(installed, out current) && remote > current;
        }

        public static void Verify(string file, string expected)
        {
            if (String.IsNullOrWhiteSpace(expected) || expected.Trim().Length != 64)
                throw new InvalidDataException("Update checksum is missing.");
            string actual;
            using (var stream = File.OpenRead(file))
            using (var hash = SHA256.Create())
                actual = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (!String.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }

                throw new InvalidDataException("The downloaded update did not pass verification.");
            }
        }
    }
}
