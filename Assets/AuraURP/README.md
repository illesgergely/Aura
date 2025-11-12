# Aura URP Integration

This directory contains the Universal Render Pipeline (URP) integration for Aura volumetric lighting.

## Requirements

- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP) 14.x or newer
- A Universal Render Pipeline Asset configured in Project Settings

## Setup Instructions

### 1. Install URP Package

If you haven't already, install the URP package:
1. Open Window > Package Manager
2. Find "Universal RP" and install it (version 14.x recommended)
3. Create a URP Asset: Right-click in Project > Create > Rendering > Universal Render Pipeline > Pipeline Asset (Forward Renderer)
4. Set this asset in Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings

### 2. Configure the Renderer

1. Locate your Forward Renderer asset (created automatically with the URP Asset, or create one manually)
2. In the Inspector, add a new Renderer Feature: Click "Add Renderer Feature" > "Volumetric Lighting Feature"
3. Assign the Composite Shader: In the feature settings, set "Composite Shader" to `Hidden/Aura/Composite`

### 3. Add Aura Component to Camera

1. Select your Camera GameObject
2. Add Component > Aura > Aura Main Component
3. Assign the required shader references:
   - Compute Maximum Depth Compute Shader
   - Process Occlusion Map Shader
   - Compute Data Compute Shader
   - Compute Accumulation Compute Shader
   - Post Process Shader (not used in URP, but must be assigned for compatibility)
   - Blue Noise Textures Array

### 4. Define URP Scripting Symbol

To enable URP-specific code in Aura:
1. Go to Edit > Project Settings > Player
2. Under "Other Settings" > "Script Compilation" > "Scripting Define Symbols"
3. Add `UNITY_PIPELINE_URP` to the list
4. Click Apply

## How It Works

### Architecture

The URP integration uses:
- **VolumetricLightingFeature**: A ScriptableRendererFeature that integrates into URP's rendering pipeline
- **VolumetricLightingPass**: A ScriptableRenderPass that executes after transparent rendering
- **Aura_URP.cs**: Partial class containing URP-specific lifecycle code
- **AuraComposite.shader**: URP-compatible shader for compositing volumetric results

### Render Pipeline Flow

1. `RenderPipelineManager.beginCameraRendering` → Updates Frustum data
2. `VolumetricLightingPass.Execute` → Dispatches compute shaders and composites result
3. `RenderPipelineManager.endCameraRendering` → Increments frame counter

### Differences from Built-in Pipeline

- No `OnRenderImage` callback (URP uses Render Features instead)
- Uses `CommandBuffer` for all rendering operations
- Integrates with URP's camera color target directly
- Lifecycle managed through `RenderPipelineManager` events

## Current Limitations (MVP)

This is a minimal viable product (MVP) release. The following features are **not yet implemented**:

- ❌ URP shadow atlas integration (shadows disabled)
- ❌ Cookie texture integration (cookies disabled)  
- ❌ Per-light data from URP's light buffers
- ❌ VR/XR multi-plane support
- ❌ Advanced performance optimizations (async compute)

The current implementation provides:
- ✅ Basic volumetric fog/density
- ✅ Anisotropy
- ✅ Blue noise dithering
- ✅ Compute shader-based accumulation
- ✅ Additive compositing onto camera target

## Troubleshooting

### Volumetric lighting not appearing

1. Verify URP is active (check Graphics settings)
2. Ensure `UNITY_PIPELINE_URP` is defined in Scripting Define Symbols
3. Check that VolumetricLightingFeature is added to your Forward Renderer
4. Verify the Composite Shader is assigned in the feature settings
5. Ensure Aura component is enabled and has all required references assigned

### Compilation errors

- If you see errors about missing URP types, ensure the URP package is installed
- If you see duplicate symbol errors, check that `UNITY_PIPELINE_URP` is properly defined

### Performance issues

- Reduce the frustum resolution in Aura settings
- Disable occlusion culling if not needed
- Adjust the far clip plane distance

## Future Enhancements

Planned for future updates:
- Shadow and cookie integration via URP shadow atlas
- Per-light data from URP light buffers
- Temporal reprojection improvements
- VR/XR support
- Async compute optimizations

## Contributing

If you encounter issues or want to contribute improvements, please visit:
https://github.com/illesgergely/Aura
