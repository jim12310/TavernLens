using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TavernLens
{
    // Removes cached card art for game builds other than the current one. The app only reads
    // data/art/verified-v2/<build> and data/art/portraits/<build>, so older build folders are never read again
    // and otherwise accumulate roughly 350 MB per patch. Anything removed by mistake is fetched again on demand.
    public static class ArtCleanup
    {
        public static long Prune(string dataDir, string currentBuild)
        {
            if (string.IsNullOrEmpty(currentBuild) || !currentBuild.All(char.IsDigit))
                return 0;
            long freed = 0;
            string art = Path.Combine(dataDir, "art");
            foreach (var parent in new[]
            {
                Path.Combine(art, "verified-v2"),
                Path.Combine(art, "portraits"),
                art
            }

            )
            {
                if (!Directory.Exists(parent))
                    continue;
                foreach (var dir in Directory.GetDirectories(parent))
                {
                    string name = Path.GetFileName(dir);
                    if (name.Length == 0 || !name.All(char.IsDigit) || name == currentBuild)
                        continue;
                    try
                    {
                        long size = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                        Directory.Delete(dir, true);
                        freed += size;
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }

            return freed;
        }

        public static void PruneInBackground(string currentBuild)
        {
            Task.Run(() =>
            {
                try
                {
                    Prune(Store.Data, currentBuild);
                }
                catch (Exception)
                {
                }
            });
        }
    }
}
