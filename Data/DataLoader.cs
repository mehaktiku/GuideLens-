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
            return list;
        }

        // Convenience: load from 'JsonData/CincinnatiData.json' relative to content root


        public static Task<List<Recommendation>> LoadFromContentRootAsync(string contentRootPath)
        {
            var path = Path.Combine(contentRootPath, "JsonData","CincinnatiData.json");
            return LoadAllAsync(path);
        }

        private static async Task<List<Recommendation>> LoadAllAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}