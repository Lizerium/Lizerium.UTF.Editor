<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Язык: </strong>
  
  <span style="color: #F5F752; margin: 0 10px;">
    ✅ 🇷🇺 Русский (текущий)
  </span>
  | 
  <a href="CHANGELOG.md" style="color: #0891b2; margin: 0 10px;">
    🇺🇸 English
  </a>
</div>

### Версия 4.0

# Changes / Changelog

## UI / Form

- Увеличен `ClientSize` формы
- Расширен `TableLayoutPanel`
- Перемещена кнопка `OK`
- `RichTextBox`:
  - `ReadOnly = true`
  - заменён `Dock = Fill` → `Anchor` (адаптивный resize)
  - увеличен размер

- Увеличена ширина `Label`
- Удалён `FormBorderStyle = FixedDialog`

---

## Stability / Safety

- Добавлена проверка `null` перед чтением:

  ```csharp
  if (node != null)
  ```

- Чтение `Tag` выполняется только при существующем узле
- Исправлен потенциальный `NullReferenceException`

> [!IMPORTANT]
> Устранены краши при работе с отсутствующими узлами и некорректными данными.

---

## Binary Parsing Fixes

### GetString

- Добавлена защита от выхода за границы массива:

  ```csharp
  maxLength = Math.Max(0, Math.Min(maxLength, data.Length - startIndex));
  ```

- Устранён `ArgumentOutOfRangeException`
- Стабилизировано поведение при повреждённых данных

> [!CAUTION]
> Ранее возможен краш при чтении битых бинарных блоков.

---

## Texture System

### Import

- Добавлены:
  - `ImportTexturesAllDds`
  - `ImportTexturesDdsAndTga`

- Поддержка `.dds` и `.tga`
- Обновление существующих текстур без дублирования

### Processing

- Нормализация имён (`NormalizeNodeName`)
- Работа с MIP-уровнями:
  - `ExtractMipLevelName`
  - `GetMipLevel`

- Группировка текстур

### Export

- `ExportAllTextures`
- Генерация `manifest.json`
- Поддержка merge конфигурации

> [!TIP]
> Система теперь позволяет централизованно управлять текстурами и их версиями.

---

## DDS Handling

- Добавлен `DDS_SIZE_LIMIT`
- Полностью переписан:

  ```csharp
  private Bitmap[] ReadDDS(byte[] data)
  ```

> [!IMPORTANT]
> Устранены проблемы с загрузкой повреждённых DDS.

---

## Geometry Pipeline

### Normals / Tangents

- Добавлены:
  - `DuplicateVerticesAndCalcNormalsAndTangents`
  - `ComputeTangentBasis`
  - `DuplicateVerticesAndCalculateTangentsAndUpdateMeshEntries`

### Вспомогательные структуры

- `Vec2`, `Vec3`
- `VertexNeighbours`
- `NormalGroup`
- `VerticeEdges`

> [!TIP]
> Добавлена поддержка корректных tangents для normal mapping.

---

## Mesh Structure

- `MakeSureMeshesAreSequential`

Обеспечивает:

- последовательность вершин
- отсутствие overlap
- корректные offsets

---

## WireData Rebuild

- Пересчёт:
  - `VertexOffset`
  - `MaxVertNoPlusOne`
  - `NoVertices`

- Повторная привязка индексов

---

## Model Validation

- Добавлен:

  ```csharp
  VerifyModelData()
  ```

Проверяет:

- диапазоны вершин
- индексы треугольников
- `VMeshRef`
- `VWireData`

> [!WARNING]
> Некорректные модели теперь выявляются до использования.

---

## Hardpoints / THN

- Добавлены:
  - `ImportHardpointsFromTHN`
  - `ExportHardpointsToTHN`
  - `BuildImportedHardpoints`

---

## Scaling

- `RescaleModel`

Поддерживает:

- `VMeshData`
- `VMeshRef`
- `Hardpoints`

---

## Node System

- Улучшен `NodeChanged`:
  - авто-обновление CRC
  - синхронизация hardpoints
  - уведомление observers

---

## Replace System

- `ReplaceAll`

Поддержка:

- имени нод
- содержимого
- partial / exact match

---

## Rendering / Debug

### Normals Visualization

- Добавлен:
  - `DrawNormals()`

- Используется:
  - `DrawUserPrimitives(LineList)`

### UI Toggle

- `displayVerticeNormalsToolStripMenuItem`
- Реакция:

  ```csharp
  CheckedChanged → Render()
  ```

> [!TIP]
> Позволяет визуально анализировать геометрию и освещение моделей.

---

## Render Pipeline

- Добавлен override:

  ```csharp
  device.SetTextureStageState(0, TextureStage.ColorOperation, SelectArg2);
  ```

- Отключение текстур в debug-режиме

---

## Texture Search

### BEFORE

- Ограничение: максимум 3 уровня вверх

### AFTER

- Поиск до корня:

  ```csharp
  while (...)
      Directory.GetParent(...)
  ```

> [!IMPORTANT]
> Теперь текстуры находятся независимо от глубины вложенности.

---

## VMeshData Changes

### TVertex

- Удалено:
  - `TangentW`

---

### FVF Processing

- `switch` → bitmask (`if`)
- Использование:

  ```csharp
  FlexibleVertexFormat & D3DFVF_*
  ```

---

### Supported FVF

- Обновлён список форматов
- Удалены лишние
- Добавлены недостающие

> [!WARNING]
> Возможна несовместимость со старыми бинарными данными.

---

### Vertex Parsing

- Добавлены флаги:

  ```csharp
  hasNormals
  hasDiffuse
  fvfTexCount
  ```

- Разбор через:

  ```csharp
  switch(fvfTexCount)
  ```

---

### Buffer Size Calculation

- Переписан `GetRawData()`
- Зависит от:
  - `hasNormals`
  - `hasDiffuse`
  - `fvfTexCount`

---

## Data Validation

- Добавлены проверки:
  - пустого `parent`
  - выхода за границы
  - `child == ""`
  - `data == null`

> [!CAUTION]
> Поведение при некорректных данных теперь контролируется, а не приводит к падению.

---

### Версия 3.0

- Улучшено редактирование hardpoint
- Различные исправления и улучшения

---

### Версия 2.1

- Чтение материалов модели из родительских директорий
- Автоматическое раскрытие корневого узла
- Отображение моделей `.3db` и `.sph`
- Отображение масштаба модели числом
- Переработка UI:
  - удалены shortcut-клавиши из контекстного меню
  - добавлены accelerator-клавиши
  - добавлена кнопка переименования
- Возможность задания центра модели

---

### Версия 2.0

- Добавлен редактор анимационных каналов
- Автоматическое обновление CRC для `VMeshRef` при изменении имени `VMeshData`
- Добавлен просмотр модели с автообновлением при изменении `Pris / Rev / Fix`
- Добавлен редактор `VMeshRef`
- Добавлен интерпретатор yaw/pitch/roll для Fix (с ограничениями)

---

## Планируемые улучшения

- Полная поддержка всех FVF форматов
- Улучшение преобразований углов Эйлера
- Добавление просмотра `.tga` и `.dds`
- Добавление редактора hardpoint
- Улучшение управления центром модели (мышью)
