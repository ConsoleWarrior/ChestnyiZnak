using System.Text;
using ChestnyiZnak.Models;

namespace ChestnyiZnak.Services;

/// <summary>
/// Извлекает коды идентификации (КИ) из CSV-выгрузки кодов маркировки
/// (из ЛК ГИС МТ / СУЗ: «Документы» → «Коды маркировки» → выгрузить CSV).
///
/// Парсер настоящий CSV (а не split по разделителю): КИ может содержать спецсимволы
/// (включая разделитель внутри кавычек) — по «Рекомендуемому алгоритму экранирования» ГИС МТ
/// строка КИ в CSV обёрнута в двойные кавычки, внутренние кавычки удвоены.
/// Поддерживает оба формата выгрузки: «только КИ» (один столбец) и «полный отчёт».
/// </summary>
public class CsvCodeParserService
{
    public ParseResult Parse(string text)
    {
        var result = new ParseResult();

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        char delimiter = DetectDelimiter(lines);

        int rowNum = 0;
        foreach (var raw in lines)
        {
            rowNum++;
            if (raw.Trim().Length == 0)
                continue;

            var fields = SplitCsvLine(raw, delimiter);
            var code = PickCodeCell(fields);
            if (code is null)
                continue; // строка-заголовок или без кода

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

    /// <summary>Разделитель определяем по первой непустой строке (заголовок — без кавычек).</summary>
    private static char DetectDelimiter(string[] lines)
    {
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.Length == 0)
                continue;

            int semi = t.Count(c => c == ';');
            int comma = t.Count(c => c == ',');
            int tab = t.Count(c => c == '\t');
            if (semi == 0 && comma == 0 && tab == 0)
                return '\0'; // один столбец
            if (semi >= comma && semi >= tab) return ';';
            if (comma >= tab) return ',';
            return '\t';
        }
        return '\0';
    }

    /// <summary>Разбивает строку на поля с учётом кавычек и удвоенных кавычек (CSV).</summary>
    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        if (delimiter == '\0')
            return new List<string> { Unquote(line) };

        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } // удвоенная кавычка
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == delimiter) { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            s = s[1..^1].Replace("\"\"", "\"");
        return s;
    }

    /// <summary>Возвращает самую длинную «похожую на код» ячейку (или null).</summary>
    private static string? PickCodeCell(IEnumerable<string> cells)
    {
        string? best = null;
        foreach (var cell in cells)
        {
            var value = cell.Trim();
            if (LooksLikeCode(value) && (best is null || value.Length > best.Length))
                best = value;
        }
        return best;
    }

    /// <summary>
    /// Код маркировки — длинная строка из печатных ASCII-символов без пробелов.
    /// Заголовки («Код маркировки», «GTIN») отсеиваются: в них кириллица/пробелы.
    /// </summary>
    private static bool LooksLikeCode(string s)
        => s.Length >= 10 && s.All(ch => ch > 32 && ch <= 126);
}
