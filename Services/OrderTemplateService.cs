using ClosedXML.Excel;

namespace ChestnyiZnak.Services;

/// <summary>
/// Генерирует образец Excel для «Заказа кодов маркировки»: GTIN + количество кодов.
/// Способ выпуска/создания и контактное лицо задаются на странице один раз для всего заказа,
/// поэтому в таблице их нет.
/// </summary>
public class OrderTemplateService
{
    public const string ColGtin = "GTIN";
    public const string ColQuantity = "Количество";

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Заказ кодов");

        ws.Cell(1, 1).Value = ColGtin;
        ws.Cell(1, 2).Value = ColQuantity;

        var header = ws.Range(1, 1, 1, 2);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754"); // bootstrap success
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Пример строки
        ws.Cell(2, 1).Value = "04600000000001";
        ws.Cell(2, 2).Value = 1;

        // GTIN — текст (14 цифр, ведущие нули критичны)
        ws.Column(1).Style.NumberFormat.Format = "@";
        // Количество — целое число
        ws.Column(2).Style.NumberFormat.Format = "0";

        ws.Column(1).Width = 22;
        ws.Column(2).Width = 16;

        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
