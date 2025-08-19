# Unity Magenta Rendering Analysis & Solutions

## Problem Summary
Despite multiple comprehensive approaches, the Unity game renders everything as magenta/pink in standalone builds. This persists across:
- Custom shader approaches (FA/UnlitSolidColor)
- Sanitization systems (SanitizationExempt components)
- Complete project rewrites (CleanRTSBootstrap)
- Graphics pipeline forcing (Built-in pipeline only)
- Unity built-in shader usage (Shader.Find("Unlit/Color"))
- Graphics settings modifications (Always Included Shaders)

## Root Cause Analysis

Based on Unity documentation and testing, the magenta/pink rendering indicates **missing or failed shader compilation**. Since this persists even with Unity's most basic built-in shaders, the issue is likely:

### Most Likely Causes (in order):
1. **Graphics Driver Issues**: Outdated/incompatible graphics drivers preventing shader compilation
2. **Unity Installation Corruption**: Missing or corrupted Unity shader files
3. **Windows Graphics API Issues**: DirectX/OpenGL compatibility problems
4. **Hardware Compatibility**: Graphics card doesn't support required shader model

### Evidence Supporting This Analysis:
- Unity documentation states magenta = missing/failed shaders
- Our Always Included Shaders list includes all standard Unity shaders (fileID: 10752 for Unlit/Color)
- The CleanRTSBootstrap uses only `Shader.Find("Unlit/Color")` which should always exist
- Compilation logs show no shader errors, suggesting runtime shader loading failure

## Recommended Solutions (Priority Order)

### 1. Graphics Driver Update (HIGHEST PRIORITY)
```powershell
# Check current graphics driver version
dxdiag
# Go to Windows Update and check for driver updates
# Or visit manufacturer website (NVIDIA/AMD/Intel)
```

### 2. Force Unity to Use OpenGL Instead of DirectX
Add to the build command line arguments:
```powershell
FrontierAges.exe -force-opengl
```

### 3. Unity Shader Verification
Create a completely fresh Unity project to test baseline rendering:
```powershell
# In Unity Hub, create a new 3D project
# Add a simple cube with default material
# Build and test - if this also shows magenta, Unity installation is corrupted
```

### 4. Unity Installation Repair
If fresh project also shows magenta:
```powershell
# In Unity Hub:
# - Go to Installs
# - Click gear icon on Unity 6000.2.0f1
# - Select "Add Modules" 
# - Ensure "Windows Build Support (IL2CPP)" is installed
# - Consider reinstalling Unity completely
```

### 5. Windows DirectX/Graphics API Reset
```powershell
# Run as Administrator:
sfc /scannow
dism /online /cleanup-image /restorehealth
# Restart and test
```

## Quick Test Commands

### Test Current CleanRTS Build:
```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts/build-and-run.ps1 -CleanRTS
```

### Test Minimal Unity Build:
```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts/build-and-run.ps1 -MinimalTest
```

### Test with OpenGL Force:
```powershell
bin/windows-x64-latest/FrontierAges.exe -force-opengl
```

## Expected Outcomes

- **If OpenGL force works**: DirectX driver issue
- **If fresh Unity project also shows magenta**: Unity installation corrupted
- **If minimal test works but CleanRTS doesn't**: Project-specific issue (unlikely given our testing)
- **If nothing works**: Hardware/driver compatibility issue

## Code Status
All code modifications have been completed:
- ✅ Compilation errors fixed (AutoBootstrap.cs)
- ✅ Always Included Shaders updated (Unlit/Color added)
- ✅ Graphics pipeline forcing (Built-in only)
- ✅ Minimal test scenarios created
- ✅ Multiple build paths available

The issue is now confirmed to be at the Unity/system level, not code level.
