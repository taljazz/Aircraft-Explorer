# Aircraft Explorer

A screen-reader-accessible Windows application for exploring Boeing 737 and 777 aircraft using spatial audio, 3D navigation, and an extensive education system. Designed for blind and visually impaired users, with full support for NVDA, JAWS, and other screen readers via the Tolk library.

## Features

### 3D Aircraft Navigation
- **Interior exploration** — Walk through the cabin, cockpit, galleys, and lavatories step by step
- **Exterior walk-around** — Circle the aircraft at ground level or climb to inspect wings, engines, and tail surfaces
- **Exterior grid view** — Jump between major exterior zones quickly
- **Vertical movement** — Use Page Up/Down to explore wings, stabilizers, and the vertical fin at their actual height

### Spatial Audio
- **Positional tones** — Pitch reflects your vertical height (200Hz at ground level to 800Hz at the top)
- **Directional panning** — Movement tones pan in the direction you walk, giving spatial orientation
- **Component beacons** — A pulsing tone guides you toward nearby components, getting faster as you approach
- **Arrival indicator** — A fast triple tone plays when you reach a component
- **Zone transition chimes** — Ascending or descending two-note chimes when crossing zone boundaries
- **Boundary warnings** — A low tone when you reach the edge of the navigable area

### Flight Hardware Support
- Connect a yoke, joystick, or throttle quadrant via USB
- Axis feedback with zone-based announcements for pitch, roll, yaw, and throttle
- Button mapping for Select (trigger) and Back (secondary button)
- Supported via SharpDX DirectInput

### Component Interactions
- Press **Enter** at a component for multi-step interaction walkthroughs
- Engine startup sequences, flap extension stages, landing gear retraction, cockpit instrument scans, door operations, and more
- 473 interaction steps across all 7 aircraft with vivid, sensory-rich descriptions

### Structural Shape Announcements
- In exterior mode, hear announcements as you approach structural edges
- Wing tips, leading and trailing edges, stabilizer tips, fin top, engine inlet and exhaust, nose tip, tail cone
- Helps build a mental model of the aircraft's 3D shape while walking around

### Quiz System
- Press **K** near any component to start a location-based quiz
- 216 multiple-choice questions drawn from nearby education topics
- Audio feedback — ascending chime for correct, descending buzz for incorrect
- Explanations after each answer with running score tracking

### Guided Tours
- Three beacon-guided tours accessible from the main menu:
  - **Pre-Flight Walk-Around** (12 exterior stops) — inspect the aircraft like a pilot
  - **Cabin Safety Equipment** (9 interior stops) — emergency exits, slides, oxygen, life vests
  - **Cockpit Systems** (10 interior stops) — sit in the captain's seat and learn every instrument
- Audio beacon guides you to each stop with narration on arrival

### Education System
- 148 education topics covering flight controls, cockpit instruments, engines, landing gear, cabin systems, safety equipment, and more
- Topics are context-aware — press **I** near a component to see relevant topics
- Deduplicated topic lists when multiple nearby components share the same topic
- Screen-reader-friendly topic reader window (press **R**) with a read-only text view
- Topics cover both common aviation concepts and aircraft-specific details

### Jump to Component
- Press **J** to open a list of all components in the current area
- Interior mode shows only interior components; exterior modes show only exterior components
- Select a component and instantly jump to its location

## Aircraft

Seven Boeing variants are included, each with detailed 3D component layouts, zones, and education content:

| Aircraft | Type | Passengers | Range |
|----------|------|-----------|-------|
| 737-800 | Narrow-body | 189 | 2,935 nmi |
| 737-900 | Narrow-body (stretched) | 220 | 2,950 nmi |
| 777-200 | Wide-body | 314 | 5,240 nmi |
| 777-200ER | Wide-body (extended range) | ~300 | 7,065 nmi |
| 777-200LR | Wide-body (Worldliner) | ~250 | 9,395 nmi |
| 777-300 | Wide-body (stretched) | 550 | 5,955 nmi |
| 777-300ER | Wide-body (stretched, extended range) | 370 | 7,370 nmi |

## Keyboard Controls

| Key | Action |
|-----|--------|
| Arrow Keys | Move forward, backward, left, right |
| Page Up / Page Down | Move up / down (vertical) |
| Enter | Select / interact with component |
| Escape | Go back |
| I | Show education topics for nearby components |
| K | Start a quiz on nearby components |
| R | Open selected topic in a readable text window |
| J | Jump to a component from a list |
| T | Toggle between exterior walk-around and grid view |
| C | Announce current position and surroundings |
| H / F1 | Help for the current mode |
| \- / = | Decrease / increase tone volume |
| Q | Quit |

## Requirements

- Windows 10 or later
- A screen reader (NVDA, JAWS, or similar) for speech output
- Optional: USB flight yoke, joystick, or throttle for flight control mode
- .NET 8.0 Runtime only needed if building from source (the release download is self-contained)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project src/AircraftExplorer
```

## Dependencies

- [OpenTK](https://opentk.net/) — OpenAL audio for spatial tones and beacons
- [SharpDX.DirectInput](https://github.com/sharpdx/SharpDX) — Flight hardware input
- [Tolk](https://github.com/ndarilek/tolk) — Screen reader abstraction layer (NVDA, JAWS, etc.)
- Microsoft.Extensions.Configuration / DependencyInjection — App configuration and DI

## License

This project is provided as-is for educational purposes.
