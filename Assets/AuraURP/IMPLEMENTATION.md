# Aura URP Implementation Summary

## Overview

This document provides a technical overview of how Aura's volumetric lighting has been ported to Unity's Universal Render Pipeline (URP).

## Architecture

### High-Level Flow

```
Camera Rendering Start
    ↓
RenderPipelineManager.beginCameraRendering
    ↓
Aura.UpdateFrustrumURP()
    ↓
Frustum.ComputeData() [Existing compute pipeline]
    ↓
VolumetricLightingPass.Execute()
    ↓
Aura.InternalBeforeComposite()
    ↓
Composite shader blits volumetric fog onto camera target
    ↓
RenderPipelineManager.endCameraRendering
    ↓
Camera Rendering End
```

### Key Components

#### 1. VolumetricLightingFeature (ScriptableRendererFeature)
- **Location**: `Assets/AuraURP/Runtime/VolumetricLightingFeature.cs`
- **Purpose**: Integrates Aura into URP's rendering pipeline
- **Responsibilities**:
  - Creates and manages VolumetricLightingPass
  - Creates composite material from shader
  - Retrieves Aura component from camera
  - Enqueues render pass when Aura is active

#### 2. VolumetricLightingPass (ScriptableRenderPass)
- **Location**: Nested in VolumetricLightingFeature.cs
- **Purpose**: Executes per-frame volumetric rendering
- **Render Event**: `RenderPassEvent.AfterRenderingTransparents`
- **Responsibilities**:
  - Configures depth input requirement
  - Calls Aura's per-frame update methods
  - Blits composite shader onto camera color target

#### 3. Aura_URP.cs (Partial Class)
- **Location**: `Assets/AuraURP/Runtime/Aura_URP.cs`
- **Purpose**: URP-specific lifecycle management
- **Responsibilities**:
  - Subscribes to RenderPipelineManager events
  - Manages frustum updates per camera
  - Increments frame counter
  - Provides interface methods for render pass

#### 4. AuraComposite.shader
- **Location**: `Assets/AuraURP/Shaders/AuraComposite.shader`
- **Purpose**: Full-screen shader to composite volumetric fog
- **Technique**:
  - Samples depth buffer
  - Rescales depth to frustum range
  - Samples 3D `Aura_VolumetricLightingTexture`
  - Applies fog formula: `finalColor = backColor * transmission + inscattering`

## Data Flow

### Texture Pipeline

The existing Aura compute pipeline creates and manages two key 3D textures:

1. **Aura_VolumetricDataTexture** (LightingVolumeTextures)
   - Contains raw lighting data (RGB) and density (A)
   - Written by `ComputeDataComputeShader`
   - Set as global in `Frustum.ComputeData()`

2. **Aura_VolumetricLightingTexture** (FogVolumeTexture)
   - Contains accumulated fog after integration
   - Written by `ComputeAccumulationComputeShader`
   - Set as global in `Buffers.CreateBuffers()`
   - **This is what the URP composite shader samples**

### Frame Events

```
Frame N:
  beginCameraRendering
    → UpdateFrustrumURP()
      → Frustum.ComputeData()
        → Dispatch ComputeDataComputeShader
          → Writes to Aura_VolumetricDataTexture
        → Dispatch ComputeAccumulationComputeShader
          → Reads Aura_VolumetricDataTexture
          → Writes to Aura_VolumetricLightingTexture
          → Sets as global texture
  
  VolumetricLightingPass.Execute()
    → InternalBeforeComposite()
      → Updates LightsManager
      → Sets global frame ID
    → Blit with AuraComposite shader
      → Samples Aura_VolumetricLightingTexture
      → Applies fog to camera color
  
  endCameraRendering
    → Increment FrameId
```

## Conditional Compilation

The implementation uses preprocessor directives to maintain compatibility with both pipelines:

```csharp
#if UNITY_PIPELINE_URP
    // URP-specific code
#endif

#if !UNITY_PIPELINE_URP
    // Built-in pipeline code (e.g., OnRenderImage)
#endif
```

The `UNITY_PIPELINE_URP` symbol is automatically defined when:
- URP package version 14.0.0+ is installed
- Defined via `AuraURP.Runtime.asmdef` version defines

Users can also manually add it to Player Settings > Scripting Define Symbols.

## Key Design Decisions

### 1. Reuse Existing Compute Pipeline
**Decision**: Keep the existing `Frustum.ComputeData()` compute shader pipeline intact.

**Rationale**: 
- Minimizes changes to proven, working code
- Reduces risk of introducing bugs
- Compute shaders work identically in Built-in and URP
- 3D texture management is pipeline-agnostic

### 2. Partial Classes
**Decision**: Use partial classes to separate URP code from Built-in code.

**Rationale**:
- Clean code organization
- Easier maintenance
- Clear separation of concerns
- Both pipelines can coexist in the same assembly

### 3. Global Textures
**Decision**: Continue using global shader textures (`Shader.SetGlobalTexture`).

**Rationale**:
- Already implemented and working
- Simplifies shader access
- No need to pass textures through command buffers
- Consistent with existing architecture

### 4. Simple Composite Shader
**Decision**: Create a minimal, focused composite shader for URP.

**Rationale**:
- Easier to debug and maintain
- Clear separation from Built-in pipeline's PostProcessShader
- Uses URP's HLSL includes and conventions
- Focused on MVP functionality

## Limitations and Future Work

### Current MVP Limitations

1. **No Shadow Integration**
   - URP shadow atlas not accessed
   - Lights don't cast volumetric shadows
   - Future: Access URP's `_MainLightShadowmapTexture`

2. **No Cookie Support**
   - Light cookies don't affect volumetrics
   - Future: Access URP's cookie textures

3. **No Per-Light URP Data**
   - Not using URP's `AdditionalLightData`
   - Not reading from URP's light buffers
   - Future: Integrate with URP light system

4. **No VR/XR Support**
   - Single-camera assumption
   - Future: Handle multiplane rendering

### Future Enhancement Paths

#### Phase 2: Shadow Integration
```csharp
// In VolumetricLightingPass.Execute()
var shadowMapTexture = Shader.GetGlobalTexture("_MainLightShadowmapTexture");
cmd.SetGlobalTexture("_AuraMainLightShadowmap", shadowMapTexture);
```

#### Phase 3: Cookie Integration
```csharp
// Access URP's light cookie atlas
var cookieAtlas = Shader.GetGlobalTexture("_MainLightCookieTexture");
// Pass to compute shader
```

#### Phase 4: URP Light Buffer Integration
```csharp
// Read AdditionalLights data
var lightData = renderingData.lightData;
var additionalLights = lightData.additionalLightsCount;
// Pass to Aura's light manager
```

## Testing Strategy

### Unit Testing (Manual)
Follow `Assets/AuraURP/INTEGRATION_TEST.md` for step-by-step verification.

### Visual Verification
1. Basic fog appears
2. Density adjustment works
3. Color adjustment works
4. Anisotropy adjustment works

### Performance Testing
1. Profile with Unity Profiler
2. Check compute shader dispatch times
3. Verify no allocation spikes
4. Compare with Built-in pipeline baseline

## Known Issues

### Issue 1: First Frame May Be Black
**Symptom**: First rendered frame shows no volumetrics.
**Cause**: Textures not initialized before first composite.
**Workaround**: Automatically resolves on second frame.
**Fix**: Pre-initialize textures in InitializeURP().

### Issue 2: Resolution Changes
**Symptom**: Volumetrics may appear incorrect after window resize.
**Cause**: Camera resolution changes not detected in URP path.
**Workaround**: Exit and re-enter play mode.
**Fix**: Add resolution change detection in beginCameraRendering.

## Performance Characteristics

### Expected Performance
- **Compute Time**: ~1-3ms per frame (same as Built-in)
- **Composite Time**: ~0.1-0.3ms per frame
- **Memory**: ~20-50MB for textures (resolution dependent)

### Optimization Opportunities
1. Async compute dispatch (URP supports it)
2. Temporal reprojection refinement
3. Variable rate resolution
4. Half-precision texture formats where applicable

## Dependencies

### Required Packages
- `com.unity.render-pipelines.universal` >= 14.0.0
- `com.unity.render-pipelines.core` >= 14.0.0

### Required Unity Version
- Unity 2022.3 LTS or newer

### Hardware Requirements
- Compute shader support (Shader Model 5.0)
- 3D texture support
- Texture2DArray support

## References

### URP Documentation
- [ScriptableRendererFeature](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/api/UnityEngine.Rendering.Universal.ScriptableRendererFeature.html)
- [ScriptableRenderPass](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/api/UnityEngine.Rendering.Universal.ScriptableRenderPass.html)

### Aura Original Architecture
- Frustum-based volumetric lighting
- Bart Wronski's volumetric fog technique
- Sebastien Hillaire's integration formula

## Contributors

This URP port maintains the original Aura architecture by Raphaël Ernaelsten while adapting it for modern render pipelines.
