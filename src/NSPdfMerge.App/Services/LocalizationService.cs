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
            ["WindowTitle"] = "NS PDF Merge",
            ["MenuUpdate"] = "Обновление",
            ["UpdateWindowTitle"] = "Обновление",
            ["UpdateCurrentVersion"] = "Текущая версия:",
            ["UpdateLatestVersion"] = "Последняя версия:",
            ["UpdateChangelogHeader"] = "Что нового",
            ["ButtonCheckUpdate"] = "Проверить обновление",
            ["ButtonUpdate"] = "Обновить",
            ["UpdateUpToDate"] = "У вас установлена актуальная версия.",
            ["UpdateAvailable"] = "Доступна новая версия:",
            ["UpdateErrorPrefix"] = "Ошибка:",
            ["UpdateNoRelease"] = "Нет опубликованных релизов.",
            ["InstructionText"] = "Инструкция по работе с NS PDF Merge\r\n\r\n" +
                "1. Подготовь список деталей в Excel: минимум один столбец — номер детали/чертежа, желательно второй — название.\r\n" +
                "2. Скопируй нужные строки в буфер обмена и нажми Вставить из буфера. Альтернатива: меню Файл → Вставить файл Excel для импорта .xlsx или .xls.\r\n" +
                "3. Задай Путь поиска — корневую папку, в которой программа будет искать PDF.\r\n" +
                "4. Задай Путь сохранения PDF — куда сохранить итоговый файл.\r\n" +
                "5. Используй меню Файл → Пресеты, чтобы быстро сохранять и применять частые пути.\r\n" +
                "6. Нажми Найти файлы. Программа сопоставит номера/названия со списком PDF.\r\n" +
                "7. Если строка отмечена как Ambiguous, кликни по ней или нажми Выбрать файл вручную, чтобы указать правильный PDF.\r\n" +
                "8. Исключи лишние строки с помощью столбца Вкл.\r\n" +
                "9. Нажми Собрать PDF. Готовый файл появится по указанному пути.\r\n" +
                "10. Открой результат через строку состояния внизу окна или кнопку Открыть на панели инструментов.\r\n\r\n" +
                "Дополнительные возможности\r\n" +
                "• Меняй порядок строк: выдели строки и перетащи мышью.\r\n" +
                "• Ctrl+Z — отмена последнего действия.\r\n" +
                "• Удалить дубли — убирает повторяющиеся строки с конца списка.\r\n" +
                "• Тема — переключение между светлой и тёмной темой.\r\n" +
                "• Справка → Язык — смена языка интерфейса (RU / EN / UK).\r\n" +
                "• Справка → Обновление — проверка и установка обновлений.\r\n" +
                "• Справка → About — информация о программе.\r\n"
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
            ["WindowTitle"] = "NS PDF Merge",
            ["MenuUpdate"] = "Update",
            ["UpdateWindowTitle"] = "Update",
            ["UpdateCurrentVersion"] = "Current version:",
            ["UpdateLatestVersion"] = "Latest version:",
            ["UpdateChangelogHeader"] = "What's new",
            ["ButtonCheckUpdate"] = "Check for updates",
            ["ButtonUpdate"] = "Update",
            ["UpdateUpToDate"] = "You have the latest version.",
            ["UpdateAvailable"] = "New version available:",
            ["UpdateErrorPrefix"] = "Error:",
            ["UpdateNoRelease"] = "No published releases found.",
            ["InstructionText"] = "How to use NS PDF Merge\r\n\r\n" +
                "1. Prepare a parts list in Excel: at least one column with the part/drawing number, preferably a second column with the title.\r\n" +
                "2. Copy the rows to the clipboard and click Paste from clipboard. Alternatively, use File → Paste Excel file to import .xlsx or .xls.\r\n" +
                "3. Set the Search path — the root folder where the program will look for PDFs.\r\n" +
                "4. Set the Output PDF path — where to save the resulting file.\r\n" +
                "5. Use File → Presets to quickly save and apply frequently used paths.\r\n" +
                "6. Click Find files. The program matches numbers/titles against the PDF list.\r\n" +
                "7. If a row is marked Ambiguous, click it or use Select file manually to choose the correct PDF.\r\n" +
                "8. Exclude unwanted rows using the Incl column.\r\n" +
                "9. Click Build PDF. The finished file will be saved to the selected path.\r\n" +
                "10. Open the result from the status bar at the bottom or the Open button on the toolbar.\r\n\r\n" +
                "Additional features\r\n" +
                "• Reorder rows by selecting and dragging them.\r\n" +
                "• Ctrl+Z — undo the last action.\r\n" +
                "• Delete duplicates — removes duplicate rows from the end of the list.\r\n" +
                "• Theme — switch between light and dark themes.\r\n" +
                "• Help → Language — change interface language (RU / EN / UK).\r\n" +
                "• Help → Update — check for and install updates.\r\n" +
                "• Help → About — program information.\r\n"
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
            ["WindowTitle"] = "NS PDF Merge",
            ["MenuUpdate"] = "Оновлення",
            ["UpdateWindowTitle"] = "Оновлення",
            ["UpdateCurrentVersion"] = "Поточна версія:",
            ["UpdateLatestVersion"] = "Остання версія:",
            ["UpdateChangelogHeader"] = "Що нового",
            ["ButtonCheckUpdate"] = "Перевірити оновлення",
            ["ButtonUpdate"] = "Оновити",
            ["UpdateUpToDate"] = "У вас встановлена актуальна версія.",
            ["UpdateAvailable"] = "Доступна нова версія:",
            ["UpdateErrorPrefix"] = "Помилка:",
            ["UpdateNoRelease"] = "Немає опублікованих релізів.",
            ["InstructionText"] = "Інструкція з роботи в NS PDF Merge\r\n\r\n" +
                "1. Підготуй список деталей у Excel: мінімум один стовпець — номер деталі/креслення, бажано другий — назва.\r\n" +
                "2. Скопіюй потрібні рядки в буфер обміну та натисни Вставити з буфера. Альтернатива: меню Файл → Вставити файл Excel для імпорту .xlsx або .xls.\r\n" +
                "3. Задай Шлях пошуку — кореневу папку, в якій програма шукатиме PDF.\r\n" +
                "4. Задай Шлях збереження PDF — куди зберегти підсумковий файл.\r\n" +
                "5. Використовуй меню Файл → Пресети, щоб швидко зберігати та застосовувати часті шляхи.\r\n" +
                "6. Натисни Знайти файли. Програма зіставить номери/назви зі списком PDF.\r\n" +
                "7. Якщо рядок позначено як Ambiguous, клікни по ньому або натисни Вибрати файл вручну, щоб вказати правильний PDF.\r\n" +
                "8. Виключи зайві рядки за допомогою стовпця Вкл.\r\n" +
                "9. Натисни Зібрати PDF. Готовий файл з’явиться за вказаним шляхом.\r\n" +
                "10. Відкрий результат через рядок стану внизу вікна або кнопку Відкрити на панелі інструментів.\r\n\r\n" +
                "Додаткові можливості\r\n" +
                "• Змінюй порядок рядків: виділи рядки та перетягни мишею.\r\n" +
                "• Ctrl+Z — скасування останньої дії.\r\n" +
                "• Видалити дублі — прибирає повторювані рядки з кінця списку.\r\n" +
                "• Тема — перемикання між світлою та темною темою.\r\n" +
                "• Довідка → Мова — зміна мови інтерфейсу (RU / EN / UK).\r\n" +
                "• Довідка → Оновлення — перевірка та встановлення оновлень.\r\n" +
                "• Довідка → About — інформація про програму.\r\n"
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
    public string MenuUpdate => Get(nameof(MenuUpdate));
    public string UpdateWindowTitle => Get(nameof(UpdateWindowTitle));
    public string UpdateCurrentVersion => Get(nameof(UpdateCurrentVersion));
    public string UpdateLatestVersion => Get(nameof(UpdateLatestVersion));
    public string UpdateChangelogHeader => Get(nameof(UpdateChangelogHeader));
    public string ButtonCheckUpdate => Get(nameof(ButtonCheckUpdate));
    public string ButtonUpdate => Get(nameof(ButtonUpdate));
    public string UpdateUpToDate => Get(nameof(UpdateUpToDate));
    public string UpdateAvailable => Get(nameof(UpdateAvailable));
    public string UpdateErrorPrefix => Get(nameof(UpdateErrorPrefix));
    public string UpdateNoRelease => Get(nameof(UpdateNoRelease));
    public string InstructionText => Get(nameof(InstructionText));

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
