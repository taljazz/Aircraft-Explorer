namespace AircraftExplorer.Aircraft;

public class AircraftRegistry
{
    private readonly List<AircraftModel> _aircraft;

    public AircraftRegistry(string dataDirectoryPath)
    {
        _aircraft = AircraftLoader.LoadAllFromDirectory(dataDirectoryPath);
    }

    public IReadOnlyList<AircraftModel> GetAll() => _aircraft;

    public AircraftModel? GetById(string id)
    {
        return _aircraft.Find(a => a.Id == id);
    }

    public int Count => _aircraft.Count;
}
