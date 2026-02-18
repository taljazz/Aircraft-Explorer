namespace AircraftExplorer.Config;

public class AppSettings
{
    public NavigationSettings Navigation { get; set; } = new();
    public SpeechSettings Speech { get; set; } = new();
    public string AircraftDataPath { get; set; } = "Data/Aircraft";
    public string EducationDataPath { get; set; } = "Data/Education";
}

public class NavigationSettings
{
    public int StepSize { get; set; } = 1;
    public double ComponentProximityRadius { get; set; } = 2.0;
    public bool AnnounceZoneChanges { get; set; } = true;
    public bool AnnounceNearbyComponents { get; set; } = true;
}

public class SpeechSettings
{
    public int VerbosityLevel { get; set; } = 2;
    public int DebounceMilliseconds { get; set; } = 500;
    public double AxisThreshold { get; set; } = 0.15;
}
