using System.Reflection;

namespace NSPdfMerge.App.Services;

public static class AppInfo
{
    public static string AppName => Assembly.GetEntryAssembly()?.GetName().Name ?? "NSPdfMerge";

    public static string CurrentVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "0.0.0.0" : version.ToString();
        }
    }

    public static string Changelog { get; } =
        "Что нового в версии 1.2.0:\r\n" +
        "• Меню единого стиля: равная высота и ширина выпадающих списков\r\n" +
        "• Пресеты разделены на поиск и сохранение, размещены в меню Файл\r\n" +
        "• Пресеты хранятся в отдельном presets.json и редактируются через Файл → Редактировать пресеты\r\n" +
        "• Имя пресета по умолчанию берётся из полного пути\r\n" +
        "• Текст таблицы в тёмной теме вынесен в #F0F0F0, бейдж Дубль — тёмный на жёлтом\r\n" +
        "• Компактный верхний блок и меню\r\n" +
        "• Подсветка строк NotFound (#F7C7AC) и дубликатов (#FFF7DC)\r\n" +
        "• Excel-подобное выделение текста и копирование ячеек\r\n" +
        "• Сохранение ручного выбора файла при сборке PDF\r\n" +
        "• Отмена последнего действия (Ctrl+Z)\r\n" +
        "• Множественное перетаскивание строк\r\n" +
        "• Строка состояния с путём к собранному PDF\r\n" +
        "• Кнопка «Открыть» в верхней панели\r\n" +
        "• Удаление дублей с конца списка\r\n" +
        "• Порядковый номер строки (№) в таблице\r\n" +
        "• Локализация интерфейса (RU / EN / UK)\r\n";
}
