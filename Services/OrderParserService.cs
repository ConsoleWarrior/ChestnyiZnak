using System.Text.RegularExpressions;
using ChestnyiZnak.Models;
using ClosedXML.Excel;

namespace ChestnyiZnak.Services;

/// <summary>
/// Читает Excel заказа кодов маркировки и валидирует строки:
/// GTIN — ровно 14 цифр; количество — целое число больше нуля.
/// </summary>
public class OrderParserService
{
    private static readonly Regex GtinRegex = new(@"^\d{14}$", RegexOptions.Compiled);

    public OrderParseResult Parse(Stream xlsxStream)
    {
        var result = new OrderParseResult();

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
            var h2 = Clean(ws.Cell(1, 2).GetString());

            if (!h1.Equals(OrderTemplateService.ColGtin, StringComparison.OrdinalIgnoreCase) ||
                !h2.Equals(OrderTemplateService.ColQuantity, StringComparison.OrdinalIgnoreCase))
            {
                result.FileErrors.Add(
                    $"Неверные заголовки колонок. Ожидаются: «{OrderTemplateService.ColGtin}» и " +
                    $"«{OrderTemplateService.ColQuantity}». Скачайте свежий шаблон.");
                return result;
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= lastRow; r++)
            {
                var gtin = Clean(ws.Cell(r, 1).GetString());
                var qtyRaw = Clean(ws.Cell(r, 2).GetString());

                if (gtin.Length == 0 && qtyRaw.Length == 0)
                    continue;

                var row = new OrderRow { RowNumber = r, Gtin = gtin, QuantityRaw = qtyRaw };
                Validate(row);
                result.Rows.Add(row);
            }

            if (result.Rows.Count == 0)
                result.FileErrors.Add("В файле нет строк с данными. Заполните шаблон и загрузите снова.");
        }

        return result;
    }

    private static void Validate(OrderRow row)
    {
        // GTIN
        if (row.Gtin.Length == 0)
            row.Errors.Add("Не указан GTIN");
        else if (!GtinRegex.IsMatch(row.Gtin))
            row.Errors.Add($"Некорректный GTIN «{row.Gtin}» — должно быть ровно 14 цифр");

        // Количество
        var qty = Regex.Replace(row.QuantityRaw, @"\s", "");
        if (qty.Length == 0)
            row.Errors.Add("Не указано количество");
        else if (!int.TryParse(qty, out var n) || n <= 0)
            row.Errors.Add($"Некорректное количество «{row.QuantityRaw}» — нужно целое число больше нуля");
        else
            row.Quantity = n;
    }

    private static string Clean(string? value)
        => (value ?? "").Replace(' ', ' ').Trim();
}
