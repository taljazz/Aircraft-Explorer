using System.Text.Json;

namespace AircraftExplorer.Aircraft;

public static class AircraftLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AircraftModel LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<AircraftModel>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize aircraft from {filePath}");
    }

    public static List<AircraftModel> LoadAllFromDirectory(string directoryPath)
    {
        var aircraft = new List<AircraftModel>();

        if (!Directory.Exists(directoryPath))
            return aircraft;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            try
            {
                aircraft.Add(LoadFromFile(file));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skipping {file}: {ex.Message}");
            }
        }

        return aircraft;
    }
}
