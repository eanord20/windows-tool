# NS PDF Merge — Обзор проекта

## Назначение

Десктопное Windows-приложение на WPF (.NET 8) для объединения нескольких PDF-файлов в один документ. Оптимизировано под работу со списками деталей/чертежей: пользователь вставляет из Excel столбцы «Номер» и «Название», программа ищет соответствующие PDF в заданной папке и собирает итоговый PDF.

## Структура решения

```
D:\My_project\NS_PDF_Merge
├── src\NSPdfMerge.App\              # Основной WPF-проект
│   ├── MainWindow.xaml              # Главное окно и разметка DataGrid
│   ├── MainWindow.xaml.cs           # Code-behind: drag-and-drop строк
│   ├── ViewModels\MainViewModel.cs  # Бизнес-логика, команды, состояние
│   ├── ViewModels\RelayCommand.cs   # Реализация ICommand
│   ├── Models\FileRow.cs            # Модель строки таблицы
│   ├── Models\ResolveStatus.cs       # Статусы поиска файла
│   └── Services\                    # Сервисы
│       ├── ClipboardImportService.cs  # Импорт из буфера обмена
│       ├── ExcelImportService.cs     # Импорт .xlsx / .xls
│       ├── FileResolveService.cs     # Поиск PDF по номеру/названию
│       ├── PdfMergeService.cs        # Сборка итогового PDF
│       ├── NameNormalize.cs          # Нормализация имён для сравнения
│       ├── AppSettingsService.cs     # Сохранение/загрузка настроек
│       ├── PresetService.cs          # Загрузка/сохранение пресетов путей в presets.json
│       ├── LocalizationService.cs    # Локализация RU/EN/UK
│       └── ThemeService.cs           # Применение тёмной/светлой темы
├── src\NSPdfMerge.Package\          # MSIX-упаковка (не затрагивается)
├── pdf-generation-interface\       # Next.js-прототип UI (не используется)
└── installer.iss                    # Inno Setup скрипт
```

## Основной пользовательский поток

1. Вставить список из буфера (TSV из Excel) или импортировать `.xlsx`/`.xls`.
2. Задать «Путь поиска» — корневую папку с PDF.
3. Задать «Путь сохранения PDF» — куда сохранить результат.
4. Использовать **Файл → Пресеты** для быстрого сохранения/применения путей.
5. Нажать **Найти файлы**.
6. При необходимости выбрать файл вручную или разрешить неоднозначности.
7. Нажать **Собрать PDF**.

## Ключевые сущности

### `FileRow`

| Свойство | Назначение |
|----------|------------|
| `Include` | Включать ли строку в итоговый PDF |
| `Number` | Номер детали/чертежа |
| `Title` | Название |
| `RowPath` | Путь поиска для конкретной строки (пока не используется) |
| `ResolvedPath` | Найденный путь к PDF |
| `Status` | `Pending`, `Found`, `NotFound`, `Ambiguous`, `InvalidPath` |
| `Candidates` | Список кандидатов при `Ambiguous` |
| `IsDuplicate` | Пометка дубля по `Number` + `Title` |

### `MainViewModel`

Хранит `ObservableCollection<FileRow> Rows`, пути, счётчики по статусам, команды, стек undo. Содержит основную логику UI, включая команды пресетов и локализации.

### `FileResolveService`

Ищет файлы рекурсивно по `CommonSearchPath`, сопоставляя нормализованное имя файла с `Number + " " + Title`. При отсутствии точного совпадения делает fallback по префиксу номера.

### `PdfMergeService`

Использует `PdfSharp` 6.1.1. Для строк `Found` импортирует страницы, для остальных добавляет placeholder-страницу А4 с текстом ошибки.

## Текущая версия

`1.2.0` — см. `About` / `installer.iss`.

## Выпуск релизов и автообновление (запланировано)

Распространение обновлений будет происходить через **GitHub Releases** репозитория `eanord20/windows-tool`.

### Формат релиза

- Тег релиза: `v{Major}.{Minor}.{Patch}`, например `v1.2.1`.
- Имя ассета (прикреплённого файла): `NSPdfMerge_Setup.exe`.
- Текущая версия приложения задаётся в `src/NSPdfMerge.App/NSPdfMerge.App.csproj` и дублируется в `installer.iss`.

### Подготовка установщика

1. Собрать приложение в конфигурации Release:
   ```bash
   dotnet build -c Release src/NSPdfMerge.App/NSPdfMerge.App.csproj
   ```
2. Собрать установщик Inno Setup:
   ```bash
   iscc installer.iss
   ```
   Результат: `installer_output/NSPdfMerge_Setup.exe`.

### Публикация релиза

**Вручную (через веб-интерфейс GitHub):**
1. Открыть `github.com/eanord20/windows-tool/releases`.
2. Нажать **Draft a new release**.
3. В поле **Choose a tag** ввести `v1.2.1` и создать тег.
4. Заголовок: `v1.2.1`.
5. Прикрепить `NSPdfMerge_Setup.exe` в разделе Attach binaries.
6. Опубликовать релиз.

**Через терминал (`gh` CLI):**
```bash
gh release create v1.2.1 --title "v1.2.1" --generate-notes installer_output/NSPdfMerge_Setup.exe
```
Требуется авторизация: `gh auth login`.

**Автоматизация через GitHub Actions (опционально):**
Можно добавить workflow, который при пуше тега `v*` собирает Release, запускает `iscc` и публикует ассет. Это исключает ручные ошибки при сборке.

### Принцип будущего автообновления

- Приложение запрашивает `https://api.github.com/repos/eanord20/windows-tool/releases/latest`.
- Сравнивает тег релиза с текущей `AssemblyVersion`.
- Если тег новее, скачивает `NSPdfMerge_Setup.exe` во временную папку и запускает его с флагом `/SILENT`.
- Установщик Inno Setup заменяет файлы приложения; текущий процесс завершаётся перед запуском установщика.

> Реализация UI и сервиса автообновления отложена; согласован формат релиза и имя ассета — `NSPdfMerge_Setup.exe`.

## Известные технические ограничения

- `MainWindow.xaml` содержит большую часть стилей и шаблонов ячеек — при дальнейшем росте UI рекомендуется выносить ресурсы в отдельные файлы.
- Пресеты редактируются через внешний JSON-редактор: изменения применяются после перезапуска приложения.
- Локализация требует перезапуска при смене языка.
- Drag-and-drop работает только внутри таблицы; перетаскивание извне не поддерживается.
