using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NSPdfMerge.App.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();
    public static LocalizationService Instance => _instance;

    private string _currentLanguage = "ru";

    private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["ru"] = new()
        {
            ["MenuFile"] = "Файл",
            ["MenuSaveList"] = "Сохранить список",
            ["MenuOpenList"] = "Открыть список",
            ["MenuSaveSearchPreset"] = "Сохранить пресет поиска",
            ["MenuSaveOutputPreset"] = "Сохранить пресет сохранения",
            ["MenuSearchPresets"] = "Пресеты поиска",
            ["MenuOutputPresets"] = "Пресеты сохранения",
            ["MenuEditPresets"] = "Редактировать пресеты",
            ["MenuTheme"] = "Тема",
            ["MenuDarkTheme"] = "Тёмная тема",
            ["MenuHelp"] = "Помощь",
            ["MenuInstruction"] = "Инструкция",
            ["MenuAbout"] = "About",
            ["MenuLanguage"] = "Язык",
            ["LangRussian"] = "Русский",
            ["LangEnglish"] = "English",
            ["LangUkrainian"] = "Українська",
            ["LabelSearchPath"] = "Путь поиска",
            ["LabelOutputPath"] = "Путь сохранения PDF",
            ["ButtonBrowse"] = "Выбрать",
            ["ButtonPasteClipboard"] = "Вставить из буфера",
            ["ButtonPasteExcel"] = "Вставить файл Excel",
            ["ButtonFindFiles"] = "Найти файлы",
            ["ButtonMergePdf"] = "Собрать PDF",
            ["ButtonClear"] = "Очистить",
            ["StatTotal"] = "Строк",
            ["StatIncluded"] = "Вкл",
            ["StatFound"] = "Found",
            ["StatNotFound"] = "NotFound",
            ["StatAmbiguous"] = "Ambiguous",
            ["StatPending"] = "Pending",
            ["ToolbarUndo"] = "Отменить",
            ["ToolbarUp"] = "Вверх",
            ["ToolbarDown"] = "Вниз",
            ["ToolbarAdd"] = "Добавить",
            ["ToolbarSelectManually"] = "Выбрать файл вручную",
            ["ToolbarDelete"] = "Удалить",
            ["ToolbarDeleteDuplicates"] = "Удалить дубли",
            ["ToolbarOpen"] = "Открыть",
            ["ColumnRowIndex"] = "№",
            ["ColumnInclude"] = "Вкл",
            ["ColumnPartNumber"] = "Номер",
            ["ColumnTitle"] = "Название",
            ["ColumnResolved"] = "Найдено",
            ["ColumnStatus"] = "Статус",
            ["ContextCopy"] = "Копировать",
            ["ContextSelectManually"] = "Выбрать файл вручную",
            ["ContextDelete"] = "Удалить",
            ["DupBadge"] = "Дубль",
            ["StatusResult"] = "Результат",
            ["StatusOpen"] = "Открыть",
            ["ButtonOk"] = "OK",
            ["WindowTitle"] = "NS PDF Merge"
        },
        ["en"] = new()
        {
            ["MenuFile"] = "File",
            ["MenuSaveList"] = "Save list",
            ["MenuOpenList"] = "Open list",
            ["MenuSaveSearchPreset"] = "Save search preset",
            ["MenuSaveOutputPreset"] = "Save output preset",
            ["MenuSearchPresets"] = "Search presets",
            ["MenuOutputPresets"] = "Output presets",
            ["MenuEditPresets"] = "Edit presets",
            ["MenuTheme"] = "Theme",
            ["MenuDarkTheme"] = "Dark theme",
            ["MenuHelp"] = "Help",
            ["MenuInstruction"] = "Instructions",
            ["MenuAbout"] = "About",
            ["MenuLanguage"] = "Language",
            ["LangRussian"] = "Русский",
            ["LangEnglish"] = "English",
            ["LangUkrainian"] = "Українська",
            ["LabelSearchPath"] = "Search path",
            ["LabelOutputPath"] = "Output PDF path",
            ["ButtonBrowse"] = "Browse",
            ["ButtonPasteClipboard"] = "Paste from clipboard",
            ["ButtonPasteExcel"] = "Paste Excel file",
            ["ButtonFindFiles"] = "Find files",
            ["ButtonMergePdf"] = "Build PDF",
            ["ButtonClear"] = "Clear",
            ["StatTotal"] = "Rows",
            ["StatIncluded"] = "Incl",
            ["StatFound"] = "Found",
            ["StatNotFound"] = "NotFound",
            ["StatAmbiguous"] = "Ambiguous",
            ["StatPending"] = "Pending",
            ["ToolbarUndo"] = "Undo",
            ["ToolbarUp"] = "Up",
            ["ToolbarDown"] = "Down",
            ["ToolbarAdd"] = "Add",
            ["ToolbarSelectManually"] = "Select file manually",
            ["ToolbarDelete"] = "Delete",
            ["ToolbarDeleteDuplicates"] = "Delete duplicates",
            ["ToolbarOpen"] = "Open",
            ["ColumnRowIndex"] = "№",
            ["ColumnInclude"] = "Incl",
            ["ColumnPartNumber"] = "Number",
            ["ColumnTitle"] = "Title",
            ["ColumnResolved"] = "Resolved",
            ["ColumnStatus"] = "Status",
            ["ContextCopy"] = "Copy",
            ["ContextSelectManually"] = "Select file manually",
            ["ContextDelete"] = "Delete",
            ["DupBadge"] = "Dup",
            ["StatusResult"] = "Result",
            ["StatusOpen"] = "Open",
            ["ButtonOk"] = "OK",
            ["WindowTitle"] = "NS PDF Merge"
        },
        ["uk"] = new()
        {
            ["MenuFile"] = "Файл",
            ["MenuSaveList"] = "Зберегти список",
            ["MenuOpenList"] = "Відкрити список",
            ["MenuSaveSearchPreset"] = "Зберегти пресет пошуку",
            ["MenuSaveOutputPreset"] = "Зберегти пресет збереження",
            ["MenuSearchPresets"] = "Пресети пошуку",
            ["MenuOutputPresets"] = "Пресети збереження",
            ["MenuEditPresets"] = "Редагувати пресети",
            ["MenuTheme"] = "Тема",
            ["MenuDarkTheme"] = "Темна тема",
            ["MenuHelp"] = "Довідка",
            ["MenuInstruction"] = "Інструкція",
            ["MenuAbout"] = "About",
            ["MenuLanguage"] = "Мова",
            ["LangRussian"] = "Русский",
            ["LangEnglish"] = "English",
            ["LangUkrainian"] = "Українська",
            ["LabelSearchPath"] = "Шлях пошуку",
            ["LabelOutputPath"] = "Шлях збереження PDF",
            ["ButtonBrowse"] = "Вибрати",
            ["ButtonPasteClipboard"] = "Вставити з буфера",
            ["ButtonPasteExcel"] = "Вставити файл Excel",
            ["ButtonFindFiles"] = "Знайти файли",
            ["ButtonMergePdf"] = "Зібрати PDF",
            ["ButtonClear"] = "Очистити",
            ["StatTotal"] = "Рядків",
            ["StatIncluded"] = "Вкл",
            ["StatFound"] = "Found",
            ["StatNotFound"] = "NotFound",
            ["StatAmbiguous"] = "Ambiguous",
            ["StatPending"] = "Pending",
            ["ToolbarUndo"] = "Скасувати",
            ["ToolbarUp"] = "Вгору",
            ["ToolbarDown"] = "Вниз",
            ["ToolbarAdd"] = "Додати",
            ["ToolbarSelectManually"] = "Вибрати файл вручну",
            ["ToolbarDelete"] = "Видалити",
            ["ToolbarDeleteDuplicates"] = "Видалити дублі",
            ["ToolbarOpen"] = "Відкрити",
            ["ColumnRowIndex"] = "№",
            ["ColumnInclude"] = "Вкл",
            ["ColumnPartNumber"] = "Номер",
            ["ColumnTitle"] = "Назва",
            ["ColumnResolved"] = "Знайдено",
            ["ColumnStatus"] = "Статус",
            ["ContextCopy"] = "Копіювати",
            ["ContextSelectManually"] = "Вибрати файл вручну",
            ["ContextDelete"] = "Видалити",
            ["DupBadge"] = "Дубль",
            ["StatusResult"] = "Результат",
            ["StatusOpen"] = "Відкрити",
            ["ButtonOk"] = "OK",
            ["WindowTitle"] = "NS PDF Merge"
        }
    };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (value == _currentLanguage) return;
            _currentLanguage = value;
            OnPropertyChanged(string.Empty);
        }
    }

    public string Get(string key)
    {
        if (_strings.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        if (_strings["en"].TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }

    public string MenuFile => Get(nameof(MenuFile));
    public string MenuSaveList => Get(nameof(MenuSaveList));
    public string MenuOpenList => Get(nameof(MenuOpenList));
    public string MenuSaveSearchPreset => Get(nameof(MenuSaveSearchPreset));
    public string MenuSaveOutputPreset => Get(nameof(MenuSaveOutputPreset));
    public string MenuSearchPresets => Get(nameof(MenuSearchPresets));
    public string MenuOutputPresets => Get(nameof(MenuOutputPresets));
    public string MenuEditPresets => Get(nameof(MenuEditPresets));
    public string MenuTheme => Get(nameof(MenuTheme));
    public string MenuDarkTheme => Get(nameof(MenuDarkTheme));
    public string MenuHelp => Get(nameof(MenuHelp));
    public string MenuInstruction => Get(nameof(MenuInstruction));
    public string MenuAbout => Get(nameof(MenuAbout));
    public string MenuLanguage => Get(nameof(MenuLanguage));
    public string LangRussian => Get(nameof(LangRussian));
    public string LangEnglish => Get(nameof(LangEnglish));
    public string LangUkrainian => Get(nameof(LangUkrainian));
    public string LabelSearchPath => Get(nameof(LabelSearchPath));
    public string LabelOutputPath => Get(nameof(LabelOutputPath));
    public string ButtonBrowse => Get(nameof(ButtonBrowse));
    public string ButtonPasteClipboard => Get(nameof(ButtonPasteClipboard));
    public string ButtonPasteExcel => Get(nameof(ButtonPasteExcel));
    public string ButtonFindFiles => Get(nameof(ButtonFindFiles));
    public string ButtonMergePdf => Get(nameof(ButtonMergePdf));
    public string ButtonClear => Get(nameof(ButtonClear));
    public string StatTotal => Get(nameof(StatTotal));
    public string StatIncluded => Get(nameof(StatIncluded));
    public string StatFound => Get(nameof(StatFound));
    public string StatNotFound => Get(nameof(StatNotFound));
    public string StatAmbiguous => Get(nameof(StatAmbiguous));
    public string StatPending => Get(nameof(StatPending));
    public string ToolbarUndo => Get(nameof(ToolbarUndo));
    public string ToolbarUp => Get(nameof(ToolbarUp));
    public string ToolbarDown => Get(nameof(ToolbarDown));
    public string ToolbarAdd => Get(nameof(ToolbarAdd));
    public string ToolbarSelectManually => Get(nameof(ToolbarSelectManually));
    public string ToolbarDelete => Get(nameof(ToolbarDelete));
    public string ToolbarDeleteDuplicates => Get(nameof(ToolbarDeleteDuplicates));
    public string ToolbarOpen => Get(nameof(ToolbarOpen));
    public string ColumnRowIndex => Get(nameof(ColumnRowIndex));
    public string ColumnInclude => Get(nameof(ColumnInclude));
    public string ColumnPartNumber => Get(nameof(ColumnPartNumber));
    public string ColumnTitle => Get(nameof(ColumnTitle));
    public string ColumnResolved => Get(nameof(ColumnResolved));
    public string ColumnStatus => Get(nameof(ColumnStatus));
    public string ContextCopy => Get(nameof(ContextCopy));
    public string ContextSelectManually => Get(nameof(ContextSelectManually));
    public string ContextDelete => Get(nameof(ContextDelete));
    public string DupBadge => Get(nameof(DupBadge));
    public string StatusResult => Get(nameof(StatusResult));
    public string StatusOpen => Get(nameof(StatusOpen));
    public string ButtonOk => Get(nameof(ButtonOk));
    public string WindowTitle => Get(nameof(WindowTitle));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public static void ApplyLanguage(string language)
    {
        Instance.CurrentLanguage = language;
        CultureInfo.CurrentUICulture = new CultureInfo(language == "uk" ? "uk-UA" : language == "en" ? "en-US" : "ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture;
    }
}
