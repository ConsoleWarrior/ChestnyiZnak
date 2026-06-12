using ClosedXML.Excel;

namespace ChestnyiZnak.Services;

/// <summary>
/// Генерирует образец Excel для «Ввода в оборот»: одна колонка с кодами идентификации (КИ).
/// ИНН участника/производителя/собственника и дата производства задаются на странице
/// один раз для всего документа (так требует схема introduce_rf).
/// </summary>
public class ExcelTemplateService
{
    public const string ColMarkingCode = "Код маркировки";

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Коды маркировки");

        ws.Cell(1, 1).Value = ColMarkingCode;

        var header = ws.Range(1, 1, 1, 1);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD"); // bootstrap primary
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Пример строки
        ws.Cell(2, 1).Value = "010460742823950521abcdEFgh...";

        // Текстовый формат — защищает ведущие нули и спецсимволы кода.
        ws.Column(1).Style.NumberFormat.Format = "@";
        ws.Column(1).Width = 60;

        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
