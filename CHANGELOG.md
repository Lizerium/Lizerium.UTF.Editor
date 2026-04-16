<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Language: </strong>
  
  <a href="CHANGELOG.ru.md" style="color: #F5F752; margin: 0 10px;">
    🇷🇺 Russian
  </a>
  | 
  <span style="color: #0891b2; margin: 0 10px;">
    ✅ 🇺🇸 English (current)
  </span>
</div>

### Version 4.0

# Changes / Changelog

## UI / Form

- Increased form `ClientSize`
- Expanded `TableLayoutPanel`
- Repositioned `OK` button
- `RichTextBox`:
  - set to `ReadOnly = true`
  - replaced `Dock = Fill` → `Anchor` (responsive resizing)
  - increased size

- Increased `Label` width
- Removed `FormBorderStyle = FixedDialog`

---

## Stability / Safety

- Added `null` check before reading:

  ```csharp
  if (node != null)
  ```

* `Tag` is now read only if the node exists
* Fixed potential `NullReferenceException`

> [!IMPORTANT]
> Crashes caused by missing nodes and invalid data have been eliminated.

---

## Binary Parsing Fixes

### GetString

- Added bounds protection:

  ```csharp
  maxLength = Math.Max(0, Math.Min(maxLength, data.Length - startIndex));
  ```

- Fixed possible `ArgumentOutOfRangeException`

- Stabilized behavior for corrupted data

> [!CAUTION]
> Previously, invalid binary data could cause application crashes.

---

## Texture System

### Import

- Added:
  - `ImportTexturesAllDds`
  - `ImportTexturesDdsAndTga`

- Support for `.dds` and `.tga`

- Updating existing textures without duplication

### Processing

- Name normalization (`NormalizeNodeName`)

- MIP-level handling:
  - `ExtractMipLevelName`
  - `GetMipLevel`

- Texture grouping

### Export

- `ExportAllTextures`
- `manifest.json` generation
- Config merge support

> [!TIP]
> Enables centralized texture resource management.

---

## DDS Handling

- Added `DDS_SIZE_LIMIT`
- Fully rewritten:

  ```csharp
  private Bitmap[] ReadDDS(byte[] data)
  ```

> [!IMPORTANT]
> Fixed issues with loading corrupted DDS files.

---

## Geometry Pipeline

### Normals / Tangents

- Added:
  - `DuplicateVerticesAndCalcNormalsAndTangents`
  - `ComputeTangentBasis`
  - `DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries`

### Helper Structures

- `Vec2`, `Vec3`
- `VertexNeighbours`
- `NormalGroup`
- `VerticeEdges`

> [!TIP]
> Provides proper tangent generation for normal mapping.

---

## Mesh Structure

- `MakeSureMeshesAreSequential`

Ensures:

- sequential vertex layout
- no overlaps
- correct offsets

---

## WireData Rebuild

- Recalculated:
  - `VertexOffset`
  - `MaxVertNoPlusOne`
  - `NoVertices`

- Rebinding of indices

---

## Model Validation

- Added:

  ```csharp
  VerifyModelData()
  ```

Validates:

- vertex ranges
- triangle indices
- `VMeshRef`
- `VWireData`

> [!WARNING]
> Invalid models are now detected before use.

---

## Hardpoints / THN

- Added:
  - `ImportHardpointsFromTHN`
  - `ExportHardpointsToTHN`
  - `BuildImportedHardpoints`

---

## Scaling

- `RescaleModel`

Supports:

- `VMeshData`
- `VMeshRef`
- `Hardpoints`

---

## Node System

- Improved `NodeChanged`:
  - automatic CRC update
  - hardpoint synchronization
  - observer notification

---

## Replace System

- `ReplaceAll`

Supports:

- node names
- content
- partial / exact match

---

## Rendering / Debug

### Normals Visualization

- Added:
  - `DrawNormals()`

- Uses:
  - `DrawUserPrimitives(LineList)`

### UI Toggle

- `displayVerticeNormalsToolStripMenuItem`
- Trigger:

  ```csharp
  CheckedChanged → Render()
  ```

> [!TIP]
> Enables visual analysis of geometry and lighting.

---

## Render Pipeline

- Added override:

  ```csharp
  device.SetTextureStageState(0, TextureStage.ColorOperation, SelectArg2);
  ```

- Texture disabling in debug mode

---

## Texture Search

### BEFORE

- Limited to 3 parent levels

### AFTER

- Traverses up to root:

  ```csharp
  while (...)
      Directory.GetParent(...)
  ```

> [!IMPORTANT]
> Textures are now reliably found regardless of directory depth.

---

## VMeshData Changes

### TVertex

- Removed:
  - `TangentW`

---

### FVF Processing

- Replaced `switch` with bitmask logic (`if`)
- Usage:

  ```csharp
  FlexibleVertexFormat & D3DFVF_*
  ```

---

### Supported FVF

- Updated supported formats
- Removed unused ones
- Added missing ones

> [!WARNING]
> May introduce incompatibility with older binary data.

---

### Vertex Parsing

- Added flags:

  ```csharp
  hasNormals
  hasDiffuse
  fvfTexCount
  ```

- Parsing via:

  ```csharp
  switch(fvfTexCount)
  ```

---

### Buffer Size Calculation

- Rewritten `GetRawData()`
- Now depends on:
  - `hasNormals`
  - `hasDiffuse`
  - `fvfTexCount`

---

## Data Validation

- Added checks for:
  - empty `parent`
  - out-of-bounds access
  - `child == ""`
  - `data == null`

> [!CAUTION]
> Invalid data is now handled safely instead of causing crashes.

---

### Version 3.0

- Improved hardpoint editing
- Various fixes and improvements

---

### Version 2.1

- Reads model materials from parent directories
- Automatically expands root node
- Displays `.3db` and `.sph` models
- Shows model scale as a numeric value
- UI improvements:
  - removed shortcut keys from context menu
  - added accelerator keys
  - added rename button

- Ability to define model center point

---

### Version 2.0

- Added animation channel editor
- Automatic CRC update for `VMeshRef` when renaming `VMeshData`
- Added live-updating model viewer for `Pris / Rev / Fix`
- Added `VMeshRef` editor
- Added yaw/pitch/roll interpreter for Fix (limited accuracy)

---

## Planned Improvements

- Full support for all FVF formats
- Improved Euler angle conversions
- Add `.tga` / `.dds` viewer
- Add hardpoint viewer/editor
- Improve mouse-based center movement
