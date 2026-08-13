using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using NSPdfMerge.App.Models;
using NSPdfMerge.App.Services;

namespace NSPdfMerge.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string AuthorName = "Nikolay Sukhomlin";
    private const string ContactEmail = "nik.sukhomlin20@gmail.com";

    private readonly ClipboardImportService _clipboardImportService = new();
    private readonly ExcelImportService _excelImportService = new();
    private readonly FileResolveService _fileResolveService = new();
    private readonly PdfMergeService _pdfMergeService = new();
    private readonly AppSettingsService _settingsService = new();
    private readonly PresetService _presetService = new();
    private readonly ThemeService _themeService = new();

    private string _commonSearchPath = string.Empty;
    private string _outputPdfPath = string.Empty;
    private string _outputFileName = string.Empty;
    private string _outputFilePrefix = string.Empty;
    private string _logText = string.Empty;
    private string _lastOutputPath = string.Empty;
    private string _language = "ru";
    private FileRow? _selectedRow;
    private bool _isDarkTheme;
    private readonly LinkedList<List<FileRow>> _undoStack = new();
    private const int MaxUndoDepth = 50;

    public ObservableCollection<FileRow> Rows { get; } = new();
    public ObservableCollection<PathPreset> SearchPathPresets { get; } = new();
    public ObservableCollection<PathPreset> OutputPathPresets { get; } = new();

    public FileRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (ReferenceEquals(value, _selectedRow)) return;
            _selectedRow = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    private bool CanSelectAmbiguousFile(object? parameter)
    {
        var can = parameter is FileRow row && row.Status == ResolveStatus.Ambiguous;
        AppLog.Info($"CanSelectAmbiguousFile: type={parameter?.GetType().Name}, can={can}");
        return can;
    }

    private void SelectAmbiguousFile(object? parameter)
    {
        AppLog.Info($"SelectAmbiguousFile started. Parameter type={parameter?.GetType().Name}");
        if (parameter is not FileRow row)
        {
            AppLog.Info("SelectAmbiguousFile: parameter is not FileRow");
            return;
        }

        var candidates = row.Candidates;
        if (candidates.Count == 0)
        {
            candidates = _fileResolveService.GetCandidates(row, CommonSearchPath);
        }

        candidates = candidates
            .Where(c => string.IsNullOrWhiteSpace(c) == false)
            .Distinct()
            .ToList();

        row.Candidates = candidates;
        AppLog.Info($"SelectAmbiguousFile for row {row.RowNumber}: candidates={candidates.Count}");
        if (candidates.Count == 0)
        {
            AppendLog($"[{row.Number}] Ambiguous: список кандидатов пуст.\r\n");
            System.Windows.MessageBox.Show("Совпадения не найдены.", "Выбор файла", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        NSPdfMerge.App.AmbiguousSelectWindow? wnd = null;
        bool? ok = null;
        try
        {
            wnd = new NSPdfMerge.App.AmbiguousSelectWindow(candidates)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            AppLog.Info($"SelectAmbiguousFile: showing AmbiguousSelectWindow with {candidates.Count} candidates");
            ok = wnd.ShowDialog();
            AppLog.Info($"SelectAmbiguousFile: dialog result={ok}");
        }
        catch (Exception ex)
        {
            AppLog.Error("SelectAmbiguousFile: failed to show dialog", ex);
            throw;
        }
        if (ok != true) return;

        var selected = wnd?.SelectedPath;
        if (string.IsNullOrWhiteSpace(selected))
        {
            System.Windows.MessageBox.Show("Файл не выбран.", "Выбор файла", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        row.ResolvedPath = selected;
        row.Status = ResolveStatus.Found;
        row.Candidates = [];
        RaiseCountersChanged();
        RaiseCommandStates();
    }

    public int TotalCount => Rows.Count;
    public int IncludedCount => Rows.Count(r => r.Include);
    public int FoundCount => Rows.Count(r => r.Status == ResolveStatus.Found);
    public int NotFoundCount => Rows.Count(r => r.Status == ResolveStatus.NotFound);
    public int AmbiguousCount => Rows.Count(r => r.Status == ResolveStatus.Ambiguous);
    public int InvalidPathCount => Rows.Count(r => r.Status == ResolveStatus.InvalidPath);
    public int PendingCount => Rows.Count(r => r.Status == ResolveStatus.Pending);

    public string CommonSearchPath
    {
        get => _commonSearchPath;
        set
        {
            if (value == _commonSearchPath) return;
            _commonSearchPath = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public string OutputPdfPath
    {
        get => _outputPdfPath;
        set
        {
            if (value == _outputPdfPath) return;
            _outputPdfPath = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public string OutputFileName
    {
        get => _outputFileName;
        set
        {
            if (value == _outputFileName) return;
            _outputFileName = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public string OutputFilePrefix
    {
        get => _outputFilePrefix;
        set
        {
            if (value == _outputFilePrefix) return;
            _outputFilePrefix = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (value == _isDarkTheme) return;
            _isDarkTheme = value;
            OnPropertyChanged();
            _themeService.ApplyTheme(_isDarkTheme);
            SaveSettings();
        }
    }

    public string LogText
    {
        get => _logText;
        private set
        {
            if (value == _logText) return;
            _logText = value;
            OnPropertyChanged();
        }
    }

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

    public string LastOutputFileName => System.IO.Path.GetFileName(_lastOutputPath);

    public bool CanUndo => _undoStack.Count > 0;

    public ICommand PasteFromClipboardCommand { get; }
    public ICommand ImportFromExcelFileCommand { get; }
    public ICommand ResolveFilesCommand { get; }
    public ICommand MergePdfCommand { get; }
    public ICommand SaveListCommand { get; }
    public ICommand OpenListCommand { get; }
    public ICommand BrowseCommonSearchPathCommand { get; }
    public ICommand BrowseOutputPdfPathCommand { get; }
    public ICommand AddRowCommand { get; }
    public ICommand DeleteSelectedRowsCommand { get; }
    public ICommand ClearTableCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand OpenPdfCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand InstructionCommand { get; }
    public ICommand SelectAmbiguousFileCommand { get; }
    public ICommand SelectFileManuallyCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand CopyToClipboardCommand { get; }
    public ICommand DeleteDuplicatesCommand { get; }
    public ICommand SaveSearchPresetCommand { get; }
    public ICommand SaveOutputPresetCommand { get; }
    public ICommand ApplySearchPresetCommand { get; }
    public ICommand ApplyOutputPresetCommand { get; }
    public ICommand DeleteSearchPresetCommand { get; }
    public ICommand DeleteOutputPresetCommand { get; }
    public ICommand EditPresetsCommand { get; }
    public ICommand SetLanguageCommand { get; }

    public string AppName => Assembly.GetEntryAssembly()?.GetName().Name ?? "NSPdfMerge";

    public string AppVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "" : version.ToString();
        }
    }

    public MainViewModel()
    {
        PasteFromClipboardCommand = new RelayCommand(_ => PasteFromClipboard());
        ImportFromExcelFileCommand = new RelayCommand(_ => ImportFromExcelFile());
        ResolveFilesCommand = new RelayCommand(_ => ResolveFiles());
        MergePdfCommand = new RelayCommand(_ => MergePdf());
        SaveListCommand = new RelayCommand(_ => SaveListToFile(), _ => Rows.Count > 0);
        OpenListCommand = new RelayCommand(_ => OpenListFromFile());
        BrowseCommonSearchPathCommand = new RelayCommand(_ => BrowseCommonSearchPath());
        BrowseOutputPdfPathCommand = new RelayCommand(_ => BrowseOutputPdfPath());

        AddRowCommand = new RelayCommand(_ => AddRow());
        DeleteSelectedRowsCommand = new RelayCommand(DeleteSelectedRows, _ => Rows.Count > 0);
        ClearTableCommand = new RelayCommand(_ => ClearTable(), _ => Rows.Count > 0);
        MoveUpCommand = new RelayCommand(p => MoveSelected(p, -1), p => CanMoveSelected(p, -1));
        MoveDownCommand = new RelayCommand(p => MoveSelected(p, 1), p => CanMoveSelected(p, 1));
        OpenPdfCommand = new RelayCommand(OpenPdf, CanOpenPdf);
        AboutCommand = new RelayCommand(_ => ShowAbout());
        InstructionCommand = new RelayCommand(_ => ShowInstruction());
        SelectAmbiguousFileCommand = new RelayCommand(SelectAmbiguousFile, CanSelectAmbiguousFile);
        SelectFileManuallyCommand = new RelayCommand(SelectFileManually);
        UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
        CopyToClipboardCommand = new RelayCommand(CopySelectedRowsToClipboard, _ => Rows.Count > 0);
        DeleteDuplicatesCommand = new RelayCommand(_ => DeleteDuplicates(), _ => Rows.Count > 0);
        SaveSearchPresetCommand = new RelayCommand(_ => SaveSearchPreset());
        SaveOutputPresetCommand = new RelayCommand(_ => SaveOutputPreset());
        ApplySearchPresetCommand = new RelayCommand(ApplySearchPreset);
        ApplyOutputPresetCommand = new RelayCommand(ApplyOutputPreset);
        DeleteSearchPresetCommand = new RelayCommand(DeleteSearchPreset);
        DeleteOutputPresetCommand = new RelayCommand(DeleteOutputPreset);
        EditPresetsCommand = new RelayCommand(_ => EditPresets());
        SetLanguageCommand = new RelayCommand(SetLanguage);

        Rows.CollectionChanged += Rows_CollectionChanged;

        LoadSettings();

        AppendLog("Готово. Вставь список из Excel (2 колонки: номер, название).\r\n");
    }

    private void ShowAbout()
    {
        var year = DateTime.Now.Year;
        var text = $"{AppName} v{AppVersion}\r\n\r\n" +
                   $"Author: {AuthorName}\r\n" +
                   $"Contact:\r\n{ContactEmail}\r\n\r\n" +
                   $"{AppName} is a lightweight tool designed to merge multiple PDF files into a single document.\r\n" +
                   $"It is optimized for handling documentation, and large-format PDFs while preserving original quality and structure.\r\n\r\n" +
                   $"© {year} {AuthorName}. All rights reserved. The program is free.";

        var wnd = new NSPdfMerge.App.AboutWindow(text)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        wnd.ShowDialog();
    }

    private void ShowInstruction()
    {
        var text =
            "1. Скопируйте из Эксель Номер и название документа, можно только номер или название.\r\n" +
            "2. Для вставки из буфера нажмите кнопку - Вставить из буфера\r\n" +
            "3. При вставке Эксель файла строки столбца Номер заполняются вторым столбцом из файла, Название - третим.\r\n" +
            "4. Выберите путь поиска файлов\r\n" +
            "5. Выберите путь сохранения файлов\r\n" +
            "6. Нажмите Найти файлы\r\n" +
            "7. Нажмите Собрать PDF";

        var wnd = new NSPdfMerge.App.InstructionWindow(text)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        wnd.ShowDialog();
    }

    private void Rows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<FileRow>())
            {
                item.PropertyChanged -= Row_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<FileRow>())
            {
                item.PropertyChanged += Row_PropertyChanged;
            }
        }

        UpdateDuplicates();
        RecalcRowNumbers(GetRecalcStartIndex(e));
        RaiseCountersChanged();
        RaiseCommandStates();
    }

    private int GetRecalcStartIndex(NotifyCollectionChangedEventArgs e)
    {
        return e.Action switch
        {
            NotifyCollectionChangedAction.Add => e.NewStartingIndex,
            NotifyCollectionChangedAction.Remove => e.OldStartingIndex,
            NotifyCollectionChangedAction.Move => Math.Min(e.OldStartingIndex, e.NewStartingIndex),
            NotifyCollectionChangedAction.Replace => Math.Min(e.OldStartingIndex, e.NewStartingIndex),
            _ => 0
        };
    }

    private void RecalcRowNumbers(int startIndex)
    {
        if (startIndex < 0) startIndex = 0;
        for (int i = startIndex; i < Rows.Count; i++)
        {
            var expected = i + 1;
            if (Rows[i].RowNumber != expected)
                Rows[i].RowNumber = expected;
        }
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Важные поля, влияющие на счётчики и UX.
        if (e.PropertyName is nameof(FileRow.Status) or nameof(FileRow.Include))
        {
            RaiseCountersChanged();
            RaiseCommandStates();
        }

        if (e.PropertyName is nameof(FileRow.Number) or nameof(FileRow.Title))
        {
            UpdateDuplicates();
        }
    }

    private void UpdateDuplicates()
    {
        var groups = Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Number) || !string.IsNullOrWhiteSpace(r.Title))
            .GroupBy(r => (Number: (r.Number ?? string.Empty).Trim(), Title: (r.Title ?? string.Empty).Trim()))
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var row in Rows)
        {
            var key = ((row.Number ?? string.Empty).Trim(), (row.Title ?? string.Empty).Trim());
            row.IsDuplicate = groups.TryGetValue(key, out var count) && count > 1;
        }
    }

    internal void PushUndoSnapshot()
    {
        while (_undoStack.Count >= MaxUndoDepth)
            _undoStack.RemoveFirst();

        _undoStack.AddLast(Rows.Select(r => r.Clone()).ToList());
        OnPropertyChanged(nameof(CanUndo));
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var snapshot = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        foreach (var row in Rows.ToList())
            row.PropertyChanged -= Row_PropertyChanged;

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
        OnPropertyChanged(nameof(CanUndo));
        AppendLog("Отменено последнее действие.\r\n");
    }

    private void CopySelectedRowsToClipboard(object? parameter)
    {
        var selected = parameter is System.Collections.IList items
            ? items.OfType<FileRow>().ToList()
            : Rows.Where(r => r.Include).ToList();

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

    private void SaveListToFile()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel-compatible TSV (*.tsv)|*.tsv|CSV (*.csv)|*.csv",
                FileName = "list.tsv"
            };

            var ok = dialog.ShowDialog();
            if (ok != true) return;

            var isCsv = dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            var sep = isCsv ? ',' : '\t';

            var sb = new StringBuilder();
            sb.AppendLine($"Номер{sep}Название");
            foreach (var r in Rows)
            {
                var n = (r.Number ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                var t = (r.Title ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                if (isCsv)
                {
                    n = EscapeCsv(n);
                    t = EscapeCsv(t);
                }
                sb.Append(n);
                sb.Append(sep);
                sb.AppendLine(t);
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            AppendLog($"Список сохранён: {dialog.FileName}\r\n");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Ошибка сохранения", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenListFromFile()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "List (*.tsv;*.csv)|*.tsv;*.csv|All files (*.*)|*.*"
            };

            var ok = dialog.ShowDialog();
            if (ok != true) return;

            PushUndoSnapshot();
            var text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            var lines = text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var imported = new List<FileRow>();
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                // allow both CSV and TSV
                var parts = line.Contains('\t') ? line.Split('\t') : line.Split(',');
                if (parts.Length < 1) continue;

                var number = parts[0].Trim().Trim('"');
                var title = parts.Length >= 2 ? parts[1].Trim().Trim('"') : string.Empty;

                if (imported.Count == 0 && IsHeaderLike(number, title))
                {
                    continue;
                }

                imported.Add(new FileRow
                {
                    Include = true,
                    Number = number,
                    Title = title,
                    RowPath = string.Empty,
                    ResolvedPath = string.Empty,
                    Status = ResolveStatus.Pending,
                    Candidates = []
                });
            }

            Rows.Clear();
            foreach (var r in imported)
            {
                Rows.Add(r);
            }

            SelectedRow = Rows.LastOrDefault();
            AppendLog($"Список загружен: {dialog.FileName}. Строк: {Rows.Count}\r\n");
            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Ошибка открытия", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static bool IsHeaderLike(string number, string title)
    {
        var n = (number ?? string.Empty).Trim().ToLowerInvariant();
        var t = (title ?? string.Empty).Trim().ToLowerInvariant();
        return n.Contains("номер") && t.Contains("назв");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"')) value = value.Replace("\"", "\"\"");
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value}\"";
        }

        return value;
    }

    private void RaiseCountersChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(IncludedCount));
        OnPropertyChanged(nameof(FoundCount));
        OnPropertyChanged(nameof(NotFoundCount));
        OnPropertyChanged(nameof(AmbiguousCount));
        OnPropertyChanged(nameof(InvalidPathCount));
        OnPropertyChanged(nameof(PendingCount));
    }

    private void RaiseCommandStates()
    {
        static void Raise(ICommand command)
        {
            if (command is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        Raise(PasteFromClipboardCommand);
        Raise(ImportFromExcelFileCommand);
        Raise(ResolveFilesCommand);
        Raise(MergePdfCommand);
        Raise(SaveListCommand);
        Raise(OpenListCommand);
        Raise(BrowseCommonSearchPathCommand);
        Raise(BrowseOutputPdfPathCommand);
        Raise(AddRowCommand);
        Raise(DeleteSelectedRowsCommand);
        Raise(ClearTableCommand);
        Raise(MoveUpCommand);
        Raise(MoveDownCommand);
        Raise(OpenPdfCommand);
        Raise(AboutCommand);
        Raise(InstructionCommand);
        Raise(SelectAmbiguousFileCommand);
        Raise(UndoCommand);
        Raise(CopyToClipboardCommand);
        Raise(DeleteDuplicatesCommand);
    }

    private void PasteFromClipboard()
    {
        try
        {
            PushUndoSnapshot();
            var imported = _clipboardImportService.ImportRowsFromClipboardTsv();
            var added = 0;
            foreach (var row in imported)
            {
                Rows.Add(row);
                added++;
            }

            if (added > 0)
            {
                SelectedRow = Rows.LastOrDefault();
            }

            AppendLog($"Добавлено строк из буфера: {added}. Всего: {Rows.Count}\r\n");
            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Ошибка импорта", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ImportFromExcelFile()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*"
            };

            var ok = dialog.ShowDialog();
            if (ok != true)
            {
                return;
            }

            PushUndoSnapshot();
            var imported = _excelImportService.ImportRowsFromExcelFile(dialog.FileName);
            var added = 0;
            foreach (var row in imported)
            {
                Rows.Add(row);
                added++;
            }

            if (added > 0)
            {
                SelectedRow = Rows.LastOrDefault();
            }

            AppendLog($"Добавлено строк из Excel файла: {added}. Всего: {Rows.Count}\r\n");
            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Ошибка импорта", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void AddRow()
    {
        PushUndoSnapshot();
        var row = new FileRow
        {
            Include = true,
            Number = string.Empty,
            Title = string.Empty,
            RowPath = string.Empty,
            ResolvedPath = string.Empty,
            Status = ResolveStatus.Pending
        };

        Rows.Add(row);
        SelectedRow = row;
        RaiseCommandStates();
    }

    private void DeleteSelectedRows(object? parameter)
    {
        // Prefer removing selected items from DataGrid (SelectedItems passed as CommandParameter).
        if (parameter is IList selectedItems && selectedItems.Count > 0)
        {
            PushUndoSnapshot();
            var toRemove = selectedItems.OfType<FileRow>().ToList();
            foreach (var row in toRemove)
            {
                Rows.Remove(row);
            }

            SelectedRow = Rows.LastOrDefault();
            RaiseCommandStates();
            return;
        }

        // Fallback: remove SelectedRow.
        if (SelectedRow is null) return;

        PushUndoSnapshot();
        var index = Rows.IndexOf(SelectedRow);
        if (index < 0) return;

        Rows.RemoveAt(index);
        SelectedRow = index < Rows.Count ? Rows[index] : Rows.LastOrDefault();
        RaiseCommandStates();
    }

    private void ClearTable()
    {
        PushUndoSnapshot();
        Rows.Clear();
        SelectedRow = null;
        AppendLog("Таблица очищена.\r\n");
        RaiseCommandStates();
    }

    private void DeleteDuplicates()
    {
        PushUndoSnapshot();

        var groups = Rows
            .Select((row, index) => (Row: row, Index: index))
            .Where(x => !string.IsNullOrWhiteSpace(x.Row.Number) || !string.IsNullOrWhiteSpace(x.Row.Title))
            .GroupBy(x => ((x.Row.Number ?? string.Empty).Trim(), (x.Row.Title ?? string.Empty).Trim()))
            .Where(g => g.Count() > 1)
            .ToList();

        var toRemove = new HashSet<FileRow>();
        foreach (var group in groups)
        {
            // Keep the first (topmost) occurrence, remove the rest.
            foreach (var item in group.OrderBy(x => x.Index).Skip(1))
            {
                toRemove.Add(item.Row);
            }
        }

        if (toRemove.Count == 0)
        {
            AppendLog("Дубли не найдены.\r\n");
            return;
        }

        foreach (var row in toRemove)
        {
            Rows.Remove(row);
        }

        SelectedRow = Rows.LastOrDefault();
        AppendLog($"Удалено дублей: {toRemove.Count}.\r\n");
        RaiseCommandStates();
    }

    private bool CanMoveSelected(object? parameter, int delta)
    {
        var selected = GetSelectedRows(parameter);
        if (selected.Count == 0) return false;

        if (delta < 0)
            return selected.Min() > 0;

        return selected.Max() < Rows.Count - 1;
    }

    private void MoveSelected(object? parameter, int delta)
    {
        if (!CanMoveSelected(parameter, delta)) return;

        PushUndoSnapshot();
        var selected = GetSelectedRows(parameter);

        if (delta < 0)
        {
            // Move up: process from top to bottom.
            foreach (var index in selected.OrderBy(i => i))
            {
                Rows.Move(index, index - 1);
            }
        }
        else
        {
            // Move down: process from bottom to top.
            foreach (var index in selected.OrderByDescending(i => i))
            {
                Rows.Move(index, index + 1);
            }
        }

        RaiseCommandStates();
    }

    private List<int> GetSelectedRows(object? parameter)
    {
        var result = new List<int>();
        if (parameter is IEnumerable items)
        {
            foreach (var item in items.OfType<FileRow>())
            {
                var index = Rows.IndexOf(item);
                if (index >= 0 && !result.Contains(index))
                    result.Add(index);
            }
        }
        else if (SelectedRow is not null)
        {
            var index = Rows.IndexOf(SelectedRow);
            if (index >= 0)
                result.Add(index);
        }
        return result;
    }

    private bool CanOpenPdf(object? parameter)
    {
        if (parameter is FileRow row)
        {
            return row.Status == ResolveStatus.Found
                   && !string.IsNullOrWhiteSpace(row.ResolvedPath)
                   && File.Exists(row.ResolvedPath);
        }

        if (parameter is string path)
        {
            return path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(path);
        }

        return SelectedRow is not null
               && SelectedRow.Status == ResolveStatus.Found
               && !string.IsNullOrWhiteSpace(SelectedRow.ResolvedPath)
               && File.Exists(SelectedRow.ResolvedPath);
    }

    private void OpenPdf(object? parameter)
    {
        var path = parameter switch
        {
            FileRow row => row.ResolvedPath,
            string s => s,
            _ => SelectedRow?.ResolvedPath ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        try
        {
            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to open PDF", ex);
            System.Windows.MessageBox.Show(ex.Message, "Не удалось открыть файл", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void SelectFileManually(object? parameter)
    {
        if (parameter is not FileRow row) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
            Title = "Выберите PDF файл"
        };

        var result = dialog.ShowDialog();
        if (result != true) return;

        var selectedPath = dialog.FileName;
        if (!File.Exists(selectedPath))
        {
            System.Windows.MessageBox.Show("Файл не найден.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        PushUndoSnapshot();
        row.ResolvedPath = selectedPath;
        row.Status = ResolveStatus.Found;
        row.Candidates = [];
        row.IsManuallyResolved = true;
        RaiseCountersChanged();
        RaiseCommandStates();
        AppendLog($"[Файл выбран вручную] {row.Number ?? row.Title}: {selectedPath}\r\n");
    }

    private void ResolveFiles()
    {
        try
        {
            AppLog.Info($"ResolveFiles started. SearchPath='{CommonSearchPath}', Rows={Rows.Count}");
            var report = _fileResolveService.ResolveAll(Rows, CommonSearchPath);
            AppLog.Info("ResolveFiles completed");
            AppendLog(report);
            RaiseCountersChanged();
        }
        catch (Exception ex)
        {
            AppLog.Error("ResolveFiles failed", ex);
            System.Windows.MessageBox.Show(ex.Message, "Ошибка поиска", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void MergePdf()
    {
        var finalOutputPath = string.Empty;
        try
        {
            if (Rows.Count == 0)
            {
                System.Windows.MessageBox.Show("Нет строк для сборки.", "Сборка PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // Build final output path with filename and prefix
            finalOutputPath = GetFinalOutputPath();
            if (string.IsNullOrWhiteSpace(finalOutputPath))
            {
                System.Windows.MessageBox.Show("Выбери путь для сохранения итогового PDF.", "Сборка PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var outputDir = Path.GetDirectoryName(finalOutputPath);
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                System.Windows.MessageBox.Show("Некорректный путь для сохранения PDF.", "Сборка PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Check if file exists and ask for confirmation
            if (File.Exists(finalOutputPath))
            {
                var dialogResult = System.Windows.MessageBox.Show(
                    $"Файл уже существует:\n{finalOutputPath}\n\nЗаменить существующий файл?",
                    "Подтверждение замены файла",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (dialogResult != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }
            }

            AppLog.Info($"Merge requested. Rows={Rows.Count}, OutputPdfPath={finalOutputPath}");

            // Если ещё не искали файлы — выполним резолв автоматически только для Pending строк.
            if (Rows.Any(r => r.Include && r.Status == ResolveStatus.Pending))
            {
                var report = _fileResolveService.ResolveAll(Rows, CommonSearchPath);
                AppendLog(report);
            }

            var result = _pdfMergeService.MergeToFile(Rows, finalOutputPath);
            LastOutputPath = result.OutputPath;

            AppendLog($"Сборка завершена. Файлов добавлено: {result.AddedFiles}, страниц: {result.AddedPages}, заглушек: {result.Placeholders}, ошибок: {result.Errors}, пропущено (выкл): {result.SkippedNotIncluded}.\r\n");
            AppLog.Info($"Merge done. Output={result.OutputPath}, AddedFiles={result.AddedFiles}, AddedPages={result.AddedPages}, Placeholders={result.Placeholders}, Errors={result.Errors}");

            System.Windows.MessageBox.Show($"Готово: {result.OutputPath}", "Сборка PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            AppLog.Error("Merge failed: file locked", ex);
            var message = $"Не удалось сохранить PDF, потому что файл используется другой программой:\n{finalOutputPath}\n\nЗакройте файл и попробуйте снова.";
            System.Windows.MessageBox.Show(message, "Ошибка сборки PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            AppLog.Error("Merge failed", ex);
            System.Windows.MessageBox.Show($"Ошибка сборки PDF:\n{ex.Message}", "Ошибка сборки PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private string GetFinalOutputPath()
    {
        var basePath = OutputPdfPath;
        if (string.IsNullOrWhiteSpace(basePath))
            return string.Empty;

        var dir = Path.GetDirectoryName(basePath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);

        // Ensure .pdf extension
        if (!string.IsNullOrWhiteSpace(ext) && ext.ToLowerInvariant() != ".pdf")
        {
            ext = ".pdf";
        }
        else if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".pdf";
        }

        return Path.Combine(dir, $"{baseName}{ext}");
    }

    private void BrowseCommonSearchPath()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "Выберите общий путь поиска PDF";
        var result = dialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            CommonSearchPath = dialog.SelectedPath;
        }
    }

    private void BrowseOutputPdfPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = string.IsNullOrWhiteSpace(OutputPdfPath) ? "result.pdf" : System.IO.Path.GetFileName(OutputPdfPath),
            InitialDirectory = string.IsNullOrWhiteSpace(OutputPdfPath) ? null : System.IO.Path.GetDirectoryName(OutputPdfPath)
        };

        var ok = dialog.ShowDialog();
        if (ok == true)
        {
            OutputPdfPath = dialog.FileName;
        }
    }

    internal void AppendLog(string text)
    {
        var sb = new StringBuilder(LogText);
        sb.Append(text);
        LogText = sb.ToString();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        _isDarkTheme = settings.IsDarkTheme;
        OnPropertyChanged(nameof(IsDarkTheme));
        _themeService.ApplyTheme(_isDarkTheme);

        _commonSearchPath = settings.CommonSearchPath ?? string.Empty;
        OnPropertyChanged(nameof(CommonSearchPath));

        _outputPdfPath = settings.OutputPdfPath ?? string.Empty;
        OnPropertyChanged(nameof(OutputPdfPath));

        _outputFileName = settings.OutputFileName ?? string.Empty;
        OnPropertyChanged(nameof(OutputFileName));

        _outputFilePrefix = settings.OutputFilePrefix ?? string.Empty;
        OnPropertyChanged(nameof(OutputFilePrefix));

        _language = settings.Language ?? "en";

        SearchPathPresets.Clear();
        OutputPathPresets.Clear();
        var presets = _presetService.Load();
        foreach (var preset in presets.SearchPathPresets)
            SearchPathPresets.Add(preset);
        foreach (var preset in presets.OutputPathPresets)
            OutputPathPresets.Add(preset);
    }

    private void SaveSettings()
    {
        var settings = _settingsService.Load();
        settings.CommonSearchPath = CommonSearchPath;
        settings.OutputPdfPath = OutputPdfPath;
        settings.OutputFileName = OutputFileName;
        settings.OutputFilePrefix = OutputFilePrefix;
        settings.IsDarkTheme = IsDarkTheme;
        settings.Language = _language;
        _settingsService.Save(settings);
    }

    public void SavePathPresets()
    {
        _presetService.Save(new PresetsData
        {
            SearchPathPresets = SearchPathPresets.ToList(),
            OutputPathPresets = OutputPathPresets.ToList()
        });
    }

    private void SaveSearchPreset()
    {
        var defaultName = string.IsNullOrWhiteSpace(CommonSearchPath)
            ? "Пресет поиска"
            : CommonSearchPath.Trim();

        var wnd = new NSPdfMerge.App.InputBoxWindow("Название пресета поиска", defaultName);
        var owner = System.Windows.Application.Current.MainWindow;
        wnd.Owner = owner;
        if (wnd.ShowDialog() != true) return;

        var name = wnd.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var existing = SearchPathPresets.FirstOrDefault(p => p.Name == name);
        if (existing is not null)
        {
            existing.SearchPath = CommonSearchPath;
        }
        else
        {
            SearchPathPresets.Add(new PathPreset { Name = name, SearchPath = CommonSearchPath });
        }

        SavePathPresets();
        AppendLog($"Пресет поиска сохранён: {name}\r\n");
    }

    private void SaveOutputPreset()
    {
        var defaultName = string.IsNullOrWhiteSpace(OutputPdfPath)
            ? "Пресет сохранения"
            : OutputPdfPath.Trim();

        var wnd = new NSPdfMerge.App.InputBoxWindow("Название пресета сохранения", defaultName);
        var owner = System.Windows.Application.Current.MainWindow;
        wnd.Owner = owner;
        if (wnd.ShowDialog() != true) return;

        var name = wnd.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var existing = OutputPathPresets.FirstOrDefault(p => p.Name == name);
        if (existing is not null)
        {
            existing.OutputPath = OutputPdfPath;
        }
        else
        {
            OutputPathPresets.Add(new PathPreset { Name = name, OutputPath = OutputPdfPath });
        }

        SavePathPresets();
        AppendLog($"Пресет сохранения сохранён: {name}\r\n");
    }

    private void ApplySearchPreset(object? parameter)
    {
        if (parameter is not PathPreset preset) return;

        CommonSearchPath = preset.SearchPath ?? string.Empty;
        AppendLog($"Применён пресет поиска: {preset.Name}\r\n");
    }

    private void ApplyOutputPreset(object? parameter)
    {
        if (parameter is not PathPreset preset) return;

        OutputPdfPath = preset.OutputPath ?? string.Empty;
        AppendLog($"Применён пресет сохранения: {preset.Name}\r\n");
    }

    private void SetLanguage(object? parameter)
    {
        if (parameter is not string lang || lang == _language) return;

        _language = lang;
        LocalizationService.ApplyLanguage(lang);
        SaveSettings();

        var result = System.Windows.MessageBox.Show(
            "Для смены языка нужно перезапустить приложение. Перезапустить сейчас?",
            "Смена языка",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            RestartApplication();
        }
    }

    private static void RestartApplication()
    {
        var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(processPath)
            {
                UseShellExecute = true
            });
        }
        System.Windows.Application.Current.Shutdown();
    }

    private void DeleteSearchPreset(object? parameter)
    {
        if (parameter is not PathPreset preset) return;

        SearchPathPresets.Remove(preset);
        SavePathPresets();
        AppendLog($"Пресет поиска удалён: {preset.Name}\r\n");
    }

    private void DeleteOutputPreset(object? parameter)
    {
        if (parameter is not PathPreset preset) return;

        OutputPathPresets.Remove(preset);
        SavePathPresets();
        AppendLog($"Пресет сохранения удалён: {preset.Name}\r\n");
    }

    private void EditPresets()
    {
        try
        {
            var path = _presetService.PresetsFilePath;
            if (!File.Exists(path))
            {
                // Ensure file exists with empty presets so user has something to edit.
                _presetService.Save(new PresetsData());
            }

            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to open presets file", ex);
            System.Windows.MessageBox.Show(ex.Message, "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
