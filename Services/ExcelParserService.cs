using ChestnyiZnak.Models;
using ClosedXML.Excel;

namespace ChestnyiZnak.Services;

/// <summary>
/// Читает Excel «Ввода в оборот» (одна колонка — код маркировки) и валидирует строки.
/// </summary>
public class ExcelParserService
{
    public ParseResult Parse(Stream xlsxStream)
    {
        var result = new ParseResult();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(xlsxStream);
        }
        catch (Exception)
        {
            result.FileErrors.Add("Не удалось прочитать файл. Убедитесь, что это .xlsx (Excel).");
            return result;
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                result.FileErrors.Add("В файле нет ни одного листа.");
                return result;
            }

            var h1 = Clean(ws.Cell(1, 1).GetString());
            if (!h1.Equals(ExcelTemplateService.ColMarkingCode, StringComparison.OrdinalIgnoreCase))
            {
                result.FileErrors.Add(
                    $"Неверный заголовок первой колонки. Ожидается «{ExcelTemplateService.ColMarkingCode}». " +
                    "Скачайте свежий шаблон.");
                return result;
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= lastRow; r++)
            {
                var code = Clean(ws.Cell(r, 1).GetString());
                if (code.Length == 0)
                    continue; // пустые строки пропускаем

                var row = new MarkingRow { RowNumber = r, MarkingCode = code };
                if (code.Length < 5)
                    row.Errors.Add("Слишком короткий код маркировки");

                result.Rows.Add(row);
            }

            if (result.Rows.Count == 0)
                result.FileErrors.Add("В файле нет кодов. Заполните шаблон и загрузите снова.");
        }

        return result;
    }

    /// <summary>Убирает обычные и неразрывные пробелы по краям; ведущие нули не трогает.</summary>
    private static string Clean(string? value)
        => (value ?? "").Replace(' ', ' ').Trim();
}
