# Aura URP Integration Test

This document provides a simple checklist to verify your Aura URP setup is working correctly.

## Prerequisites

Before testing, ensure you have:
- [ ] Unity 2022.3 LTS or newer installed
- [ ] URP package 14.x installed via Package Manager
- [ ] URP Pipeline Asset created and set as active in Graphics settings
- [ ] Forward Renderer asset assigned to the URP Pipeline Asset
- [ ] `UNITY_PIPELINE_URP` defined in Player Settings > Scripting Define Symbols

## Setup Verification

### 1. Verify URP Assets

1. Open Edit > Project Settings > Graphics
2. Confirm "Scriptable Render Pipeline Settings" is set to a URP Pipeline Asset
3. Select the URP Pipeline Asset in the Project window
4. Verify "Renderer List" contains at least one Forward Renderer

### 2. Add Volumetric Lighting Feature

1. Select your Forward Renderer asset
2. In the Inspector, find "Renderer Features"
3. Click "Add Renderer Feature"
4. Select "Volumetric Lighting Feature"
5. In the feature's settings:
   - Set "Composite Shader" to `Hidden/Aura/Composite` (search for "Aura" in the shader picker)

### 3. Setup Camera with Aura

1. Select your Main Camera in the scene
2. Add Component > Aura > Aura Main Component
3. Assign the required references:
   - **Compute Maximum Depth Compute Shader**: `Assets/Aura/Shaders/ComputeShaders/ComputeMaximumDepthComputeShader`
   - **Process Occlusion Map Shader**: `Assets/Aura/Shaders/Shaders/ProcessOcclusionTextureShader`
   - **Compute Data Compute Shader**: `Assets/Aura/Shaders/ComputeShaders/ComputeDataComputeShader`
   - **Compute Accumulation Compute Shader**: `Assets/Aura/Shaders/ComputeShaders/ComputeAccumulationComputeShader`
   - **Post Process Shader**: `Assets/Aura/Shaders/Shaders/PostProcessShader` (required but not used in URP)
   - **Blue Noise Textures Array**: `Assets/Aura/Textures/blueNoise`

## Basic Functionality Test

### Test 1: Basic Fog

1. Enter Play mode
2. You should see a subtle volumetric effect in the camera view
3. If nothing appears, check:
   - Console for errors
   - Aura component is enabled
   - Frustum settings: Density should be > 0 (default is usually 0.01)

### Test 2: Adjust Density

1. Select the Camera with Aura component
2. Expand "Frustum" > "Settings"
3. Increase "Density" to 0.1
4. You should see more prominent fog/volumetric effect

### Test 3: Adjust Color

1. In Frustum Settings, change "Color" to a different color (e.g., blue or red)
2. Change "Color Strength" to 1.0 or higher
3. The volumetric fog should take on the chosen color

### Test 4: Change Anisotropy

1. Adjust "Anisotropy" slider between -1 and 1
2. Values closer to 1 create forward scattering (brighter when looking toward lights)
3. Values closer to -1 create backward scattering (brighter when looking away from lights)

## Troubleshooting

### No volumetric effect visible

**Check 1: Scripting Define Symbol**
- Go to Edit > Project Settings > Player > Other Settings
- Under "Scripting Define Symbols", verify `UNITY_PIPELINE_URP` is present
- Click Apply if you added it

**Check 2: Renderer Feature**
- Select your Forward Renderer asset
- Verify "Volumetric Lighting Feature" is in the list
- Verify "Composite Shader" is assigned

**Check 3: Console Errors**
- Open the Console window
- Look for any Aura-related errors
- Common errors:
  - Missing shader references
  - Missing compute shader references
  - URP package not installed

**Check 4: Frustum Settings**
- Density must be > 0
- "Apply As Post Process" should be checked (default)
- Resolution should be reasonable (default is 160x90x128)

### Compilation errors about missing types

**If you see errors like "The type or namespace name 'Universal' does not exist":**
1. Install URP package via Package Manager
2. Add `UNITY_PIPELINE_URP` to Scripting Define Symbols
3. Wait for Unity to recompile

### Performance issues

**If frame rate is low:**
1. Reduce Frustum Resolution (e.g., 80x45x64)
2. Disable Occlusion Culling if not needed
3. Reduce "Far Clip Plane Distance" in Frustum Settings
4. Disable shadow and cookie features (already disabled in MVP)

## Known Limitations (Current MVP)

The current URP integration is an MVP with these limitations:
- ❌ No URP shadow atlas integration (lights don't cast volumetric shadows)
- ❌ No cookie texture support
- ❌ No per-light data from URP's light system
- ❌ No VR/XR multiplane support

These features are planned for future updates.

## Success Criteria

You have successfully integrated Aura with URP if:
- ✅ No compilation errors
- ✅ Volumetric fog appears in the scene view and game view
- ✅ Adjusting density, color, and anisotropy affects the volumetric appearance
- ✅ Performance is acceptable for your target platform

## Next Steps

Once basic integration is working:
1. Add lights to your scene and observe their contribution to the volumetric effect
2. Create Aura Volumes to locally modify fog density and color
3. Experiment with Frustum settings to achieve your desired look
4. Profile performance and optimize settings for your target platform

## Getting Help

If you encounter issues not covered here:
1. Check the main README.md for general Aura documentation
2. Review Assets/AuraURP/README.md for detailed URP setup info
3. Check the GitHub Issues page for similar problems
4. Open a new issue with:
   - Unity version
   - URP version
   - Console error messages
   - Screenshots if applicable
