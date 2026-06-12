using ChestnyiZnak.Models;

namespace ChestnyiZnak.Services;

/// <summary>
/// Извлекает коды идентификации (КИ) из CSV-выгрузки кодов маркировки
/// (из ЛК ГИС МТ / СУЗ: «Документы» → «Коды маркировки» → выгрузить CSV).
///
/// Формат выгрузки заранее не фиксирован, поэтому парсер гибкий:
/// авто-определение разделителя, пропуск строки-заголовка, выбор «похожей на код» ячейки.
/// Результат показывается в превью-таблице, так что ошибочный разбор сразу виден.
/// </summary>
public class CsvCodeParserService
{
    private static readonly char[] Delimiters = { ';', ',', '\t' };

    public ParseResult Parse(string text)
    {
        var result = new ParseResult();

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int rowNum = 0;
        foreach (var raw in lines)
        {
            rowNum++;
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var code = PickCodeCell(line);
            if (code is null)
                continue; // строка-заголовок или мусор без кода

            var row = new MarkingRow { RowNumber = rowNum, MarkingCode = code };
            if (code.Length < 5)
                row.Errors.Add("Слишком короткий код маркировки");
            result.Rows.Add(row);
        }

        if (result.Rows.Count == 0)
            result.FileErrors.Add(
                "В CSV не найдено кодов маркировки. Проверьте, что это выгрузка кодов из СУЗ/ЛК.");

        return result;
    }

    /// <summary>
    /// Разбивает строку по разделителю и возвращает самую длинную «похожую на код» ячейку
    /// (или null, если такой нет — тогда строка пропускается).
    /// </summary>
    private static string? PickCodeCell(string line)
    {
        var delimiter = Delimiters.FirstOrDefault(line.Contains);
        var cells = delimiter == default ? new[] { line } : line.Split(delimiter);

        string? best = null;
        foreach (var cell in cells)
        {
            var value = cell.Trim().Trim('"').Trim();
            if (LooksLikeCode(value) && (best is null || value.Length > best.Length))
                best = value;
        }
        return best;
    }

    /// <summary>
    /// Код маркировки — это длинная строка из ASCII-символов без пробелов.
    /// Заголовки («Код маркировки», «GTIN») отсеиваются: в них кириллица/пробелы.
    /// </summary>
    private static bool LooksLikeCode(string s)
        => s.Length >= 10 && !s.Any(char.IsWhiteSpace) && s.All(ch => ch <= 127);
}
