<h1 align="center">🉐 Freelancer UTF Editor</h1>

<p align="center">
  <b>Editor and analysis tool for geometry and resources (UTF / CMP / 3DB) for Freelancer.</b>
</p>

<p align="center">
  <img src="https://shields.dvurechensky.pro/badge/Freelancer-UTF%20Editor-blue">
  <img src="https://shields.dvurechensky.pro/badge/.NET-3.5-purple">
  <img src="https://shields.dvurechensky.pro/badge/DirectX-9-green">
</p>

---

<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Language: </strong>
  
  <a href="./README.ru.md" style="color: #F5F752; margin: 0 10px;">
    🇷🇺 Russian
  </a>
  | 
  <span style="color: #0891b2; margin: 0 10px;">
    ✅ 🇺🇸 English (current)
  </span>
</div>

---

- [Dependencies](#dependencies)
- [Build](#build)
- [Installation](#installation)
- [Major Changes](#major-changes)
  - [Geometry Pipeline](#geometry-pipeline)
  - [Texture System](#texture-system)
  - [Model Validation \& Safety](#model-validation--safety)
  - [VMeshData Refactor](#vmeshdata-refactor)
  - [Rendering \& Debug](#rendering--debug)
  - [Asset Pipeline](#asset-pipeline)
  - [Data Processing](#data-processing)
  - [Engine-Level Improvements](#engine-level-improvements)
- [Authors \& Credits](#authors--credits)
  - [Core Contributors](#core-contributors)
  - [Additional Credits](#additional-credits)

---

> [!NOTE]
> This project is part of the **Lizerium** ecosystem and belongs to the following direction:
>
> - [`Lizerium.Tools.Structs`](https://github.com/Lizerium/Lizerium.Tools.Structs)
>
> If you are looking for related engineering and utility tools, start there.

---

> [!CAUTION]
> Also explore [Managed .NET wrapper for the Autodesk FBX SDK 2015](ArcManagedFBX/README.ru.md). It remained unchanged.

---

## Dependencies

For proper operation of the model viewer, DirectX installation may be required:

- DirectX End-User Runtime Web Installer (`dxwebsetup.exe`)  
  https://www.microsoft.com/en-us/download/details.aspx?id=35

---

## Build

Requirements:

- Visual Studio 2008 C# Express
- DirectX SDK (August 2007)

---

## Installation

Required:

- DirectX 9 (End-User Runtime)
- .NET Framework 3.5 (Client Profile)

---

> [!NOTE]
> This project is based on the collective work of the Freelancer community and continues to evolve through accumulated experience and reverse engineering of file formats.

## Major Changes

Key changes in the current release.  
See [CHANGELOG.md](./CHANGELOG.md) for the full list.

---

### Geometry Pipeline

A full geometry processing pipeline has been implemented:

- normal recalculation
- tangent / binormal generation
- mesh structure normalization

> [!IMPORTANT]
> Proper support for normal mapping and smoothing is now ensured at the geometry level.

---

### Texture System

The texture system has been reworked:

- import of `.dds` and `.tga`
- MIP level support
- updating existing textures without duplication
- export with `manifest.json` generation

> [!TIP]
> Centralized management of texture resources is now supported.

---

### Model Validation & Safety

Model structure validation added:

- vertex range checks
- triangle index validation
- VMesh / WireData consistency

Critical issues fixed:

- out-of-bounds access
- handling of `null` nodes
- corrupted data processing

> [!IMPORTANT]
> Invalid data no longer causes application crashes.

---

### VMeshData Refactor

Format handling has been reworked:

- FVF logic migrated to bitmask
- `TVertex` structure simplified
- `GetRawData()` calculation redesigned

> [!WARNING]
> Changes may be incompatible with older binary data.

---

### Rendering & Debug

Geometry analysis tools added:

- normals visualization (`DrawNormals`)
- UI toggle
- render pipeline control

> [!TIP]
> A visual debug mode for geometry and lighting analysis is now available.

---

### Asset Pipeline

Texture search mechanism reworked:

- depth limitation removed
- search now continues up to filesystem root

> [!IMPORTANT]
> Fixes cases where textures could not be found due to directory structure.

---

### Data Processing

Data processing operations added:

- model scaling
- hardpoints import / export (THN)
- bulk replace
- wireframe rebuild

---

### Engine-Level Improvements

Core system improvements:

- `NodeChanged` synchronization
- automatic CRC updates
- improved stability when working with binary structures and invalid data

---

## Authors & Credits

### Core Contributors

- [`Adoxa`](https://adoxa.altervista.org/freelancer/index.html) — fixes and improvements (2.1, 3.0)
- [`Cannon`](https://github.com/Cannon) — development of version 2.0, model viewer
- [`FriendlyFire`](https://github.com/Friendly0Fire) — hardpoint support and fixes
- [`w0dk4`](https://github.com/w0dk4) — fixes and support for normal mapping

---

### Additional Credits

> [!TIP]
> This project continues the work started by Colin Sanby and Mario "HCl" Brito. Original versions and updates can be found at: http://the-starport.net

- Mario "HCl" Brito — UTF (CMP) structures, original `utf_edit` source code
- Colin Sanby — CMP Exporter, SUR Importer, SurDump.exe
- Anton — FLModelTool
- LancerSolurus — SUR structure information
- Martin Baker — matrix and Euler angle conversions  
  http://www.euclideanspace.com/

---
