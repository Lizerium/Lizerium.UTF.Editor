<h1 align="center">ArcManagedFBX</h1>

<p align="center">
  Managed .NET wrapper for the Autodesk FBX SDK 2015
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

## Overview

**ArcManagedFBX** is a managed Common Language Runtime (CLR) wrapper for the Autodesk FBX SDK 2015.

It allows developers using **C#** or any other **.NET-supported language** to work with the FBX SDK without relying on low-level platform invocation (P/Invoke).

---

## Features

- Import and export FBX scenes
- Modify:
  - cameras
  - nodes
  - attributes
- High-level access to FBX SDK functionality
- Designed for integration with MonoGame pipelines

---

## Integration

This wrapper is intended to be used with:

- [Arcane Dreams MonoGame Fork](https://github.com/arcanedreams/MonoGame)

It enables deeper inspection and processing of FBX assets within the MonoGame content pipeline.

---

## Requirements

- Visual Studio 2012 (Update 4)  
  https://www.microsoft.com/en-gb/download/details.aspx?id=39305

- Autodesk FBX SDK 2015  
  https://usa.autodesk.com/adsk/servlet/pc/item?siteID=123112&id=10775847

---

## Use Case

Typical usage:

- Import FBX files
- Access detailed scene data
- Process geometry and metadata
- Integrate into game engines or asset pipelines

> [!NOTE]
> This project focuses on providing managed access to FBX data rather than replacing the FBX SDK itself.

---

## Credits

- Original inspiration:  
  https://github.com/returnString/ManagedFBX

---
