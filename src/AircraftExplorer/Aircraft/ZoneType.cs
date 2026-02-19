using System.Text.Json.Serialization;

namespace AircraftExplorer.Aircraft;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoneType
{
    Cockpit,
    ForwardCabin,
    MidCabin,
    AftCabin,
    ForwardGalley,
    AftGalley,
    ForwardLavatory,
    AftLavatory,
    Vestibule,
    Aisle,
    SeatSection,
    CargoHold,
    ForwardCargoHold,
    AftCargoHold,
    BulkCargoHold,
    CargoAccess,
    Nose,
    ForwardFuselage,
    MidFuselage,
    AftFuselage,
    Tail,
    LeftWing,
    RightWing,
    LeftEngine,
    RightEngine,
    LeftMainGear,
    RightMainGear,
    NoseGear,
    VerticalStabilizer,
    HorizontalStabilizer,
    APU
}
