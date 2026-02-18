using AircraftExplorer.Input;

namespace AircraftExplorer.FlightSim;

public interface IFlightControlInterpreter
{
    IReadOnlyList<ControlEffect> InterpretAxisChange(AxisState previousState, AxisState currentState);
}
