using System.Text.Json;

namespace AircraftExplorer.Tours;

public static class TourLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<TourDefinition> LoadAllFromDirectory(string directoryPath)
    {
        var tours = new List<TourDefinition>();

        if (!Directory.Exists(directoryPath))
            return tours;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var tour = JsonSerializer.Deserialize<TourDefinition>(json, JsonOptions);
                if (tour is not null)
                    tours.Add(tour);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skipping tour {file}: {ex.Message}");
            }
        }

        return tours;
    }
}
