using System.Text.Json;
using GuideLens.Models;

namespace GuideLens.Data
{
    public static class DataLoader
    {
        // Load from a given file path
        public static List<Recommendation> LoadAll(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException("Data file not found", jsonFilePath);

            var json = File.ReadAllText(jsonFilePath);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<Recommendation>>(json, opts) ?? new List<Recommendation>();

            // Precompute lowercase helper fields to speed up case-insensitive substring searches
            foreach (var r in list)
            {
                r.NameLower = r.Name?.ToLowerInvariant() ?? string.Empty;
                r.TheBestOfferLower = r.TheBestOffer?.ToLowerInvariant() ?? string.Empty;
                r.NoteTipLower = r.NoteTip?.ToLowerInvariant() ?? string.Empty;
            }

            return list;
        }

        // Convenience: load from 'JsonData/CincinnatiData.json' relative to content root
        public static List<Recommendation> LoadFromContentRoot(string contentRootPath)
        {
            var path = Path.Combine(contentRootPath, "JsonData", "CincinnatiData.json");
            return LoadAll(path);
        }
    }
}