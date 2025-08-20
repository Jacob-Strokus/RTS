# Age of Empires 4 Control Scheme Implementation

This document describes the complete Age of Empires 4 control scheme implementation for Frontier Ages RTS game.

## Overview
We have implemented a professional RTS input management system based on the complete Age of Empires 4 control scheme. The system provides industry-standard controls that will feel familiar to RTS players.

## Core Components

### RTSInputManager.cs
Main input management system that handles all keyboard shortcuts, mouse controls, and hotkeys. Automatically detects when present and takes precedence over legacy input handling.

### SelectionManager.cs (Enhanced)
Enhanced with API methods for professional selection management:
- Control group management
- Multi-unit selection
- Box selection with additive modifier
- Entity type filtering

### MinimalHelpOverlay.cs (Updated)
Adaptive help system that shows different control schemes based on whether RTSInputManager is present.

## Complete Control Reference

### Selection & Basic Controls
| Key | Action |
|-----|--------|
| Left Click | Select unit |
| Double Left Click | Select all visible units of same type |
| Ctrl + Left Click | Add/remove unit from selection |
| Left Click + Drag | Box select groups of units |
| Right Click | Contextual order (move/attack) |
| Right Click + Drag | Facing move order |
| ESC | Cancel/deselect/game menu |

### Unit Management
| Key | Action |
|-----|--------|
| Ctrl + A | Select all units on screen |
| Ctrl + Shift + A | Select all units |
| Tab | Cycle through selected units forward |
| Ctrl + Tab | Cycle through selected units reverse |
| Delete (hold) | Delete selected units/buildings |
| Insert | Toggle team-based or unique player colors |

### Control Groups
| Key | Action |
|-----|--------|
| 0-9 | Select control group |
| Ctrl + 0-9 | Set control group to selected units |
| Shift + 0-9 | Add selected units to control group |

### Quick Selection Hotkeys
| Key | Action |
|-----|--------|
| F1 | Select all Military Production Buildings |
| F2 | Select all Economy Buildings |
| F3 | Select all Research Buildings |
| F4 | Select all Landmarks, Wonders, and Capital Town Centers |
| H | Cycle through Town Centers |
| Ctrl + H | Focus on Capital Town Center |
| . (Period) | Cycle through idle economy workers |
| , (Comma) | Cycle through idle military units |
| Ctrl + . | Select all idle workers |
| Ctrl + , | Select all idle military units |

### Resource Worker Management
| Key | Action |
|-----|--------|
| Ctrl + Shift + V | Select all villagers |
| Ctrl + Shift + R | Return all villagers to work |
| Ctrl + Shift + C | Select all military units |
| Ctrl + F | Cycle through villagers gathering food |
| Ctrl + W | Cycle through villagers gathering wood |
| Ctrl + G | Cycle through villagers gathering gold |
| Ctrl + S | Cycle through villagers gathering stone |

### Camera Controls
| Key | Action |
|-----|--------|
| Alt + Mouse | Rotate camera by holding the hotkey |
| [ | Rotate camera 45 degrees counter-clockwise |
| ] | Rotate camera 45 degrees clockwise |
| Backspace | Reset camera (rotation first press, zoom second press) |
| F5 | Focus on selected units |
| Home | Follow selected unit |
| Arrow Keys | Pan camera (left/right/up/down) |

### Building & Production
| Key | Action |
|-----|--------|
| B | Toggle building placement mode |
| Period/Comma | Cycle building types (when in build mode) |
| Y | Access secondary UI panel |
| T | Quick train unit |
| Shift + [production] | Queue 5 units of that type |
| P | Set rally point at mouse cursor |
| O | Clear rally point |

### Game Controls
| Key | Action |
|-----|--------|
| F10 | Open game menu |
| F11 | Toggle game time display |
| Pause | Pause simulation (single-player) |
| F8 | Quick save |
| F9 | Quick load |

### Communication
| Key | Action |
|-----|--------|
| Enter | Team chat |
| Shift + Enter | All/Global chat |
| Tab (in chat) | Swap between team and all chat |
| Page Up | Scroll chat messages (older) |
| Page Down | Scroll chat messages (newer) |
| F6 | Toggle players and tribute panel |
| Spacebar | Focus on last event |

### Ping System
| Key | Action |
|-----|--------|
| Ctrl + E + Click | Send notify ping |
| Ctrl + R + Click | Send attack ping |
| Ctrl + T + Click | Send defend ping |

## Implementation Features

### Professional Control Groups
- 10 control groups (0-9) with persistent storage
- Smart group management with add/remove functionality
- Visual feedback and group selection cycling

### Advanced Selection System
- Multi-type unit selection with cycling
- Screen-based and global unit selection
- Building type filtering and quick selection
- Idle unit management and cycling

### Camera System Integration
- Smooth camera rotation and panning
- Focus and follow functionality
- Reset and zoom management
- Professional RTS camera behavior

### Context-Sensitive Controls
- Building mode with type cycling
- Rally point management
- Production queue management
- Context-aware help system

### Backward Compatibility
- Automatic detection of RTSInputManager presence
- Falls back to legacy controls when RTSInputManager not present
- Seamless integration with existing SimBootstrap system

## Testing the Implementation

### Current Test Build
The PlacementTest build now includes the complete Age of Empires 4 control scheme:

1. **Launch the game**: The latest build includes RTSInputManager
2. **Press F1**: Toggle help to see the new control reference
3. **Test selection**: Use left-click, drag, and Ctrl+A for selection
4. **Test control groups**: Select units and use Ctrl+1 to set, then press 1 to select
5. **Test building**: Press B to enter build mode, use Period/Comma to cycle types
6. **Test camera**: Use [ and ] to rotate camera, arrows to pan
7. **Test rally points**: Press P to set rally, O to clear

### Key Improvements
- **Professional Feel**: Controls match industry standards from Age of Empires 4
- **Muscle Memory**: Familiar to RTS players
- **Comprehensive Coverage**: All major RTS control categories implemented
- **Future-Ready**: Extensible system for additional features

## Next Steps

### Immediate Testing
1. Verify all hotkeys work as expected
2. Test control group persistence
3. Validate camera controls
4. Check building placement cycling

### Future Enhancements
1. Implement building-specific hotkeys (military buildings, etc.)
2. Add unit type quick-select keys
3. Implement advanced formation controls
4. Add replay/observer controls

The system provides a solid foundation for professional RTS gameplay with industry-standard controls that will feel natural to experienced RTS players.
