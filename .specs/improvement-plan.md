# NS PDF Merge — План доработок UI/UX

> **Для агентной реализации:** рекомендуется использовать `superpowers:subagent-driven-development` или `superpowers:executing-plans` по задачам. Шаги оформлены чекбоксами `- [ ]`.

**Версия:** 1.2.0 (текущая). См. `About` и `installer.iss`.

**Выполненные доработки (1.2.0):**
- [x] Унифицированный стиль меню: одинаковая высота и ширина выпадающих списков.
- [x] Пресеты разделены на поиск и сохранение, вынесены в отдельный `presets.json`, размещены в меню Файл.
- [x] Имя пресета по умолчанию = полный путь.
- [x] Текст таблицы в тёмной теме — `#F0F0F0`, бейдж дубликатов — тёмный на жёлтом.
- [x] Локализация интерфейса RU/EN/UK.

**Цель:** улучшить удобство работы в главном окне: компактный верх, Excel-подобное копирование, исправление потери ручного выбора файла, подсветка статусов, Undo, множественное перетаскивание строк, строка состояния с результатом и перенос кнопки «Открыть» в верхнюю панель.

**Архитектура:** изменения сосредоточены в `MainWindow.xaml` (вёрстка, стили, команды привязки) и `MainViewModel` (состояние, команды, Undo-стек). Code-behind `MainWindow.xaml.cs` отвечает только за drag-and-drop. Сервисы `FileResolveService` и `PdfMergeService` трогаются минимально — только для сохранения ручного выбора и передачи пути результата.

**Стек:** WPF, .NET 8, MaterialDesignThemes 4.9.0, PdfSharp 6.1.1, ExcelDataReader 3.7.0.

---

## Глобальные ограничения

- Не добавлять новые внешние зависимости без необходимости.
- Все «магические» цвета и отступы выносить в ресурсы или константы, не оставлять в логике/UI.
- Не ломать существующий пользовательский поток: вставка → поиск → выбор → сборка PDF.
- При изменении `MainViewModel` обновлять счётчики и состояния команд через `RaiseCountersChanged()` / `RaiseCommandStates()`.
- Для undo сохранять глубокую копию строк (`FileRow.Clone()`), не копировать ссылки.
- После каждой задачи — сборка проекта (`dotnet build`) и проверка в UI, если возможно.

---

## Task 1: Компактный верхний блок и выравнивание разметки

**Файлы:**
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`
- Modify: `src/NSPdfMerge.App/MainWindow.xaml` resources (цвета/отступы)

**Что меняем:**
- Уменьшить внешние отступы (`Grid.Margin`), padding в `Border`, высоту `Menu`, padding внутри `TextBox`/`Button`.
- Вынести цвета подсветок (`#F7C7AC`, `#FFF7DC`) и акцентные отступы в `Window.Resources` как `SolidColorBrush`.
- Сделать блок путей и блок кнопок одной высотой, убрав лишние внутренние разделители.

**Шаги:**

- [ ] **Step 1: Добавить ресурсы цветов**

  В `Window.Resources` добавить:

  ```xml
  <SolidColorBrush x:Key="NotFoundRowBrush" Color="#F7C7AC" />
  <SolidColorBrush x:Key="DuplicateRowBrush" Color="#FFF7DC" />
  <SolidColorBrush x:Key="DuplicateBadgeBackground" Color="#FFF7DC" />
  <SolidColorBrush x:Key="DuplicateBadgeBorder" Color="#E0C080" />
  ```

- [ ] **Step 2: Уменьшить отступы и шрифты верхнего блока**

  - `Window`/`Grid` `Margin="8"` (было `12`).
  - `Menu` `MinHeight="22"`, `Padding="0"`, `MenuItem` `Padding="8,1"`, `MinHeight="22"`.
  - `TextBox` стиль по умолчанию: `Padding="4,2"`, `FontSize="13"`, `MinHeight="24"`.
  - `Button` стиль по умолчанию: `Padding="10,3"`, `FontSize="13"`.
  - `PrimaryRaisedButton`: `Padding="10,4"`.
  - `SmallOutlinedButton`: `Padding="8,2"`, `FontSize="12"`.
  - `Border` путей и кнопок: `Padding="8"` (было `10`).
  - `TextBlock` меток: `FontSize="13"`.

- [ ] **Step 3: Пересчитать RowDefinitions**

  Внешняя сетка остаётся 7 строк, но `Height="8"` заменить на `Height="6"`.

- [x] **Step 4: Сборка и проверка**

  Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
  Expected: success, верхнее меню и блоки стали компактнее.

---

## Task 2: Excel-подобное выделение и копирование

**Файлы:**
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs`
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`
- Modify: `src/NSPdfMerge.App/MainWindow.xaml.cs`

**Что меняем:**
- Заменить `DataGridTextColumn` на `DataGridTemplateColumn` с `TextBox` `IsReadOnly="True"`, чтобы текст внутри ячейки можно было выделить мышью.
- Добавить команду `CopyToClipboardCommand`, привязанную к `Ctrl+C` и контекстному меню.
- Копирование формирует TSV (табуляция) по выделенным строкам: Вкл, Номер, Название, Найдено, Статус.

**Шаги:**

- [ ] **Step 1: Добавить команду и реализацию копирования**

  В `MainViewModel`:

  ```csharp
  public ICommand CopyToClipboardCommand { get; }
  ```

  В конструкторе:

  ```csharp
  CopyToClipboardCommand = new RelayCommand(_ => CopySelectedRowsToClipboard(), _ => Rows.Count > 0);
  ```

  Метод:

  ```csharp
  private void CopySelectedRowsToClipboard()
  {
      var selected = Rows.Where(r => r.Include).ToList();
      if (selected.Count == 0) return;

      var sb = new StringBuilder();
      foreach (var row in selected)
      {
          sb.Append(row.Number).Append('\t')
            .Append(row.Title).Append('\t')
            .Append(row.ResolvedPath).Append('\t')
            .Append(row.Status).AppendLine();
      }

      System.Windows.Clipboard.SetText(sb.ToString());
      AppendLog($"Скопировано строк: {selected.Count}\r\n");
  }
  ```

  Примечание: позже привяжем к реальному `SelectedItems` DataGrid, чтобы копировать именно выделенные строки, а не все `Include=true`.

- [ ] **Step 2: Подменить текстовые колонки на selectable TextBox**

  Для `Номер`, `Название`, `Найдено` использовать `DataGridTemplateColumn`:

  ```xml
  <DataGridTemplateColumn Header="Номер" Width="300">
      <DataGridTemplateColumn.CellTemplate>
          <DataTemplate>
              <TextBox Text="{Binding Number, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                       IsReadOnly="False"
                       BorderThickness="0"
                       Background="Transparent"
                       Padding="2,0"
                       FontSize="14" />
          </DataTemplate>
      </DataGridTemplateColumn.CellTemplate>
  </DataGridTemplateColumn>
  ```

  Аналогично для `Название`. Для `Найдено` `IsReadOnly="True"`.

- [ ] **Step 3: Привязать Ctrl+C**

  В `MainWindow.xaml` внутри `<Window>`:

  ```xml
  <Window.InputBindings>
      <KeyBinding Key="C" Modifiers="Ctrl" Command="{Binding CopyToClipboardCommand}" />
      <KeyBinding Key="Z" Modifiers="Ctrl" Command="{Binding UndoCommand}" />
  </Window.InputBindings>
  ```

  (Undo-реализация в Task 5.)

- [x] **Step 4: Сборка и проверка**

  Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj`
  Expected: success, текст в ячейках выделяется, `Ctrl+C` кладёт TSV в буфер.

---

## Task 3: Сохранение ручного выбора файла при сборке PDF

**Файлы:**
- Modify: `src/NSPdfMerge.App/Models/FileRow.cs`
- Modify: `src/NSPdfMerge.App/Services/FileResolveService.cs`
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs`

**Проблема:** при нажатии «Собрать PDF» вызывается автоматический `ResolveAll`, который сбрасывает `ResolvedPath` и статус у всех строк, включая вручную выбранные. В итоге вместо выбранного PDF добавляется заглушка.

**Шаги:**

- [ ] **Step 1: Добавить флаг ручного выбора**

  В `FileRow.cs`:

  ```csharp
  private bool _isManuallyResolved;
  public bool IsManuallyResolved
  {
      get => _isManuallyResolved;
      set
      {
          if (value == _isManuallyResolved) return;
          _isManuallyResolved = value;
          OnPropertyChanged();
      }
  }
  ```

  Добавить метод глубокого копирования для Undo:

  ```csharp
  public FileRow Clone()
  {
      return new FileRow
      {
          Include = Include,
          Number = Number,
          Title = Title,
          RowPath = RowPath,
          ResolvedPath = ResolvedPath,
          Status = Status,
          Candidates = new List<string>(Candidates),
          IsDuplicate = IsDuplicate,
          IsManuallyResolved = IsManuallyResolved
      };
  }
  ```

- [ ] **Step 2: Установить флаг при ручном выборе**

  В `MainViewModel.SelectFileManually` после установки `ResolvedPath`:

  ```csharp
  row.ResolvedPath = selectedPath;
  row.Status = ResolveStatus.Found;
  row.Candidates = [];
  row.IsManuallyResolved = true;
  ```

- [ ] **Step 3: Не перезаписывать ручной выбор в FileResolveService**

  В `FileResolveService.ResolveOne` в начале заменить безусловный сброс на условный:

  ```csharp
  if (!row.IsManuallyResolved)
  {
      row.ResolvedPath = string.Empty;
      row.Status = ResolveStatus.Pending;
      row.Candidates = [];
  }
  else
  {
      log.AppendLine($"[{row.Number}] Пропущен: файл выбран вручную.");
      return;
  }
  ```

- [ ] **Step 4: Ограничить авто-резолв в MergePdf**

  В `MainViewModel.MergePdf` заменить условие авто-резолва:

  ```csharp
  if (Rows.Any(r => r.Include && r.Status == ResolveStatus.Pending))
  ```

  (Убрать `|| string.IsNullOrWhiteSpace(r.ResolvedPath)` — строки с пустым путём, но статусом Found, не должны триггерить перезапись.)

- [ ] **Step 5: Сборка и проверка**

  Run: `dotnet build`
  Expected: после ручного выбора файла и нажатия «Собрать PDF» файл не теряется.

---

## Task 4: Подсветка NotFound и дубликатов

**Файлы:**
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`

**Шаги:**

- [ ] **Step 1: Обновить триггер дубликатов**

  В `DataGridRow` стиле:

  ```xml
  <DataTrigger Binding="{Binding IsDuplicate}" Value="True">
      <Setter Property="Background" Value="{StaticResource DuplicateRowBrush}" />
  </DataTrigger>
  ```

- [ ] **Step 2: Добавить триглер NotFound**

  После триггера дубликата (порядок важен — последний выигрывает):

  ```xml
  <DataTrigger Binding="{Binding Status}" Value="NotFound">
      <Setter Property="Background" Value="{StaticResource NotFoundRowBrush}" />
  </DataTrigger>
  ```

- [ ] **Step 3: Обновить бейдж дубля**

  В `DupBadge`:

  ```xml
  <Border ... Background="{StaticResource DuplicateBadgeBackground}" BorderBrush="{StaticResource DuplicateBadgeBorder}" ...>
  ```

- [x] **Step 4: Сборка и проверка**

  Run: `dotnet build`
  Expected: дубли светлые (`#FFF7DC`), NotFound оранжевые (`#F7C7AC`), NotFound перекрывает дубль.

---

## Task 5: Undo (Ctrl+Z + кнопка)

**Файлы:**
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs`
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`

**Шаги:**

- [ ] **Step 1: Добавить стек снимков**

  В `MainViewModel`:

  ```csharp
  private readonly Stack<List<FileRow>> _undoStack = new();
  private const int MaxUndoDepth = 50;
  ```

  Свойство:

  ```csharp
  public ICommand UndoCommand { get; }
  public bool CanUndo => _undoStack.Count > 0;
  ```

  В конструкторе:

  ```csharp
  UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
  ```

- [ ] **Step 2: Создать метод PushUndoSnapshot**

  ```csharp
  private void PushUndoSnapshot()
  {
      if (_undoStack.Count >= MaxUndoDepth)
      {
          // remove oldest
          var old = _undoStack.ToList();
          old.RemoveAt(old.Count - 1);
          _undoStack.Clear();
          foreach (var item in old)
              _undoStack.Push(item);
      }
      _undoStack.Push(Rows.Select(r => r.Clone()).ToList());
  }
  ```

  (Лучше реализовать через `LinkedList` или просто `List` с индексом, чтобы избежать перестроения стека; оставить комментарий.)

- [ ] **Step 3: Сохранять снимки перед изменениями**

  Вставить вызов `PushUndoSnapshot()` в начале:
  - `PasteFromClipboard`
  - `ImportFromExcelFile`
  - `AddRow`
  - `DeleteSelectedRows`
  - `ClearTable`
  - `MoveSelected`
  - `SelectFileManually`
  - `OpenListFromFile`

- [ ] **Step 4: Реализовать Undo**

  ```csharp
  private void Undo()
  {
      if (_undoStack.Count == 0) return;

      var snapshot = _undoStack.Pop();
      foreach (var row in Rows.ToList())
      {
          row.PropertyChanged -= Row_PropertyChanged;
      }
      Rows.Clear();
      foreach (var row in snapshot)
      {
          row.PropertyChanged += Row_PropertyChanged;
          Rows.Add(row);
      }

      SelectedRow = Rows.LastOrDefault();
      UpdateDuplicates();
      RaiseCountersChanged();
      RaiseCommandStates();
      if (UndoCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
      AppendLog("Отменено последнее действие.\r\n");
  }
  ```

  Также уведомлять UI о `CanUndo` через `INotifyPropertyChanged`, если нужно. Добавить `OnPropertyChanged(nameof(CanUndo))` в `Undo` и после каждого `PushUndoSnapshot`.

- [ ] **Step 5: Добавить кнопку Undo в верхнюю панель**

  В панель над таблицей добавить:

  ```xml
  <Button Margin="0,0,8,0" Content="Отменить" Command="{Binding UndoCommand}" Style="{StaticResource SmallOutlinedButton}" />
  ```

- [ ] **Step 6: Сборка и проверка**

  Run: `dotnet build`
  Expected: после вставки/удаления/перемещения `Ctrl+Z` и кнопка «Отменить» восстанавливают предыдущее состояние.

---

## Task 6: Множественное перетаскивание строк

**Файлы:**
- Modify: `src/NSPdfMerge.App/MainWindow.xaml.cs`

**Шаги:**

- [ ] **Step 1: Запоминать все выделенные строки при старте перетаскивания**

  В `PreviewMouseMove` заменить передачу одного `FileRow` на список выделенных:

  ```csharp
  var draggedRows = dataGrid.SelectedItems.Cast<FileRow>().ToList();
  if (draggedRows.Count == 0) return;
  DragDrop.DoDragDrop(dataGrid, draggedRows, DragDropEffects.Move);
  ```

- [ ] **Step 2: Обработать Drop для нескольких строк**

  В `RowsGrid_Drop`:

  ```csharp
  if (!e.Data.GetDataPresent(typeof(List<FileRow>))) return;
  var droppedRows = e.Data.GetData(typeof(List<FileRow>)) as List<FileRow>;
  if (droppedRows is null || droppedRows.Count == 0) return;

  if (DataContext is not MainViewModel vm) return;

  var targetRow = FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource);
  var targetData = targetRow?.Item as FileRow;

  // snapshot handled in MoveSelected/MoveRows if called through VM, but drop is code-behind -> push snapshot manually
  vm.PushUndoSnapshot();

  var targetIndex = targetData is null ? vm.Rows.Count : vm.Rows.IndexOf(targetData);
  if (targetIndex < 0) targetIndex = vm.Rows.Count;

  // remove in reverse order
  var orderedToMove = droppedRows
      .Select(r => (Row: r, Index: vm.Rows.IndexOf(r)))
      .Where(x => x.Index >= 0)
      .OrderByDescending(x => x.Index)
      .ToList();

  foreach (var item in orderedToMove)
  {
      vm.Rows.RemoveAt(item.Index);
  }

  // compute insertion index after removal
  var insertIndex = targetIndex;
  foreach (var item in orderedToMove)
  {
      if (item.Index < targetIndex) insertIndex--;
  }

  // insert in original ascending order
  var ascending = orderedToMove.OrderBy(x => x.Index).Select(x => x.Row).ToList();
  foreach (var row in ascending)
  {
      vm.Rows.Insert(insertIndex++, row);
  }

  vm.SelectedRow = ascending.FirstOrDefault();
  ```

  В `DragOver` обновить проверку:

  ```csharp
  if (!e.Data.GetDataPresent(typeof(List<FileRow>))) return;
  ```

- [ ] **Step 3: Сделать PushUndoSnapshot доступным из code-behind**

  `MainViewModel.PushUndoSnapshot()` должен быть `internal`.

- [x] **Step 4: Сборка и проверка**

  Run: `dotnet build`
  Expected: выделенные строки перетаскиваются группой, порядок сохраняется.

---

## Task 7: Строка состояния с путём к результату

**Файлы:**
- Modify: `src/NSPdfMerge.App/ViewModels/MainViewModel.cs`
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`

**Шаги:**

- [ ] **Step 1: Добавить свойства**

  В `MainViewModel`:

  ```csharp
  private string _lastOutputPath = string.Empty;
  public string LastOutputPath
  {
      get => _lastOutputPath;
      private set
      {
          if (value == _lastOutputPath) return;
          _lastOutputPath = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(LastOutputFileName));
          RaiseCommandStates();
      }
  }

  public string LastOutputFileName => Path.GetFileName(_lastOutputPath);
  ```

- [ ] **Step 2: Установить после успешной сборки**

  В `MergePdf`, после `_pdfMergeService.MergeToFile`:

  ```csharp
  LastOutputPath = result.OutputPath;
  ```

  При `ClearTable` или новом импорте можно оставить старый путь, либо сбрасывать — оставить на усмотрение.

- [ ] **Step 3: Добавить статус-бар под таблицей**

  В `MainWindow.xaml` добавить строку `Grid.Row="7"` (или внутрь того же Border под DataGrid):

  ```xml
  <Border Grid.Row="7" ... Padding="8,4" Margin="0,6,0,0">
      <DockPanel LastChildFill="True">
          <TextBlock DockPanel.Dock="Left" Text="Результат:" VerticalAlignment="Center" Margin="0,0,8,0" />
          <Button DockPanel.Dock="Right" Content="Открыть" Command="{Binding OpenPdfCommand}" CommandParameter="{Binding LastOutputPath}" Style="{StaticResource SmallOutlinedButton}" />
          <TextBlock Text="{Binding LastOutputPath}" VerticalAlignment="Center" TextTrimming="CharacterEllipsis" ToolTip="{Binding LastOutputPath}" />
      </DockPanel>
  </Border>
  ```

  Для кнопки «Открыть» использовать существующий `OpenPdfCommand` с параметром `LastOutputPath`.

- [x] **Step 4: Сборка и проверка**

  Run: `dotnet build`
  Expected: после сборки в строке состояния виден путь и кнопка «Открыть» открывает PDF.

---

## Task 8: Перенос кнопки «Открыть» из таблицы в верхнюю панель

**Файлы:**
- Modify: `src/NSPdfMerge.App/MainWindow.xaml`

**Шаги:**

- [ ] **Step 1: Убрать кнопку из колонки Статус**

  В шаблоне колонки «Статус» удалить второй `Button` с `Content="Открыть"`.

- [ ] **Step 2: Добавить кнопку в верхнюю панель**

  В `DockPanel` над таблицей, рядом с «Удалить»/«Очистить»:

  ```xml
  <Button Margin="0,0,8,0" Content="Открыть" Command="{Binding OpenPdfCommand}" CommandParameter="{Binding SelectedRow}" Style="{StaticResource SmallOutlinedButton}" />
  ```

  `OpenPdfCommand` уже умеет работать с `FileRow`, `string` и fallback на `SelectedRow`.

- [ ] **Step 3: Сборка и проверка**

  Run: `dotnet build`
  Expected: в строках таблицы нет кнопки «Открыть», она есть в верхней панели и работает для выделенной строки.

---

## Task 9: `.gitignore` для C#-проекта

**Файлы:**
- Create: `src/.gitignore` (или `D:\My_project\NS_PDF_Merge\.gitignore`)

**Шаги:**

- [ ] **Step 1: Создать файл `.gitignore`**

  Содержимое:

  ```gitignore
  # Visual Studio
  .vs/
  *.user
  *.suo

  # Build outputs
  bin/
  obj/
  out/
  publish/

  # NuGet
  packages/

  # Windows
  Thumbs.db
  Desktop.ini
  ```

- [ ] **Step 2: Проверить**

  Run: `git status` (если репозиторий инициализирован) или просто убедиться, что `bin`/`obj` игнорируются.

---

## Task 10: Итоговая сборка и ручная проверка

**Шаги:**

- [ ] **Step 1: Собрать решение**

  Run: `dotnet build src/NSPdfMerge.App/NSPdfMerge.App.csproj -c Release`
  Expected: 0 ошибок, 0 предупреждений.

- [ ] **Step 2: Проверить сценарии**

  1. Вставить список → строки компактны, top-блок невысок.
  2. Выделить текст в ячейке → копируется полностью, а не до точки.
  3. Выбрать файл вручную → «Собрать PDF» → файл на месте, заглушка не добавляется.
  4. Сделать несколько дублей → подсветка `#FFF7DC`.
  5. Нажать «Найти файлы» → NotFound строки подсвечены `#F7C7AC`.
  6. `Ctrl+Z` / кнопка «Отменить» → откатывает вставку/удаление/перемещение.
  7. Выделить несколько строк → перетащить вверх/вниз.
  8. Собрать PDF → внизу появляется путь и кнопка «Открыть».
  9. Кнопка «Открыть» в верхней панели открывает выделенный найденный PDF.

---

## Проверка покрытия требований

| Требование | Задача |
|------------|--------|
| Компактный верх, разметка | Task 1 |
| Выделение и копирование как в Excel | Task 2 |
| Ручной файл не слетает при сборке | Task 3 |
| NotFound → `#F7C7AC` | Task 4 |
| Дубли → `#FFF7DC` | Task 4 |
| Undo `Ctrl+Z` + кнопка | Task 5 |
| Множественный drag-and-drop | Task 6 |
| Строка состояния с путём и Open | Task 7 |
| Перенос кнопки Open вверх | Task 8 |
| `.gitignore` | Task 9 |
