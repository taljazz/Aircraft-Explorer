using System.Text.Json.Serialization;

namespace AircraftExplorer.Aircraft;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComponentCategory
{
    ControlSurface,
    CockpitInstrument,
    Engine,
    LandingGear,
    Seat,
    Door,
    Window,
    Galley,
    Lavatory,
    OverheadBin,
    Lighting,
    EmergencyEquipment,
    Structural,
    Avionics,
    APU
}
