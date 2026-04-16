<h1 align="center">ArcManagedFBX</h1>

<p align="center">
  Управляемый .NET-обёртка для Autodesk FBX SDK 2015
</p>

---

<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Язык: </strong>
  
  <span style="color: #F5F752; margin: 0 10px;">
    ✅ 🇷🇺 Русский (текущий)
  </span>
  | 
  <a href="./README.md" style="color: #0891b2; margin: 0 10px;">
    🇺🇸 English
  </a>
</div>

---

## Обзор

**ArcManagedFBX** — это управляемая обёртка Common Language Runtime (CLR) для Autodesk FBX SDK 2015.

Позволяет разработчикам на **C#** и других языках платформы **.NET** работать с FBX SDK без использования низкоуровневого взаимодействия через platform invocation (P/Invoke).

---

## Возможности

- Импорт и экспорт FBX-сцен
- Модификация:
  - камер
  - узлов (nodes)
  - атрибутов
- Высокоуровневый доступ к функциональности FBX SDK
- Предназначен для интеграции с пайплайнами MonoGame

---

## Интеграция

Данная обёртка предназначена для использования совместно с:

- [Arcane Dreams MonoGame Fork](https://github.com/arcanedreams/MonoGame)

Обеспечивает более глубокий анализ и обработку FBX-ресурсов в рамках content pipeline MonoGame.

---

## Требования

- Visual Studio 2012 (Update 4)  
  https://www.microsoft.com/en-gb/download/details.aspx?id=39305

- Autodesk FBX SDK 2015  
  https://usa.autodesk.com/adsk/servlet/pc/item?siteID=123112&id=10775847

---

## Сценарии использования

Типичные задачи:

- Импорт FBX-файлов
- Доступ к подробным данным сцены
- Обработка геометрии и метаданных
- Интеграция в игровые движки и пайплайны ресурсов

> [!NOTE]
> Проект предназначен для предоставления управляемого доступа к данным FBX и не является заменой самого FBX SDK.

---

## Благодарности

- Исходная идея:  
  https://github.com/returnString/ManagedFBX

---
