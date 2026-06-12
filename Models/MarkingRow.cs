namespace ChestnyiZnak.Models;

/// <summary>
/// Одна строка из загруженного Excel «Ввода в оборот»: код идентификации (КИ).
/// ИНН и параметры документа задаются на странице один раз для всего документа,
/// поэтому в таблице их нет (так требует схема introduce_rf — они на уровне документа).
/// </summary>
public class MarkingRow
{
    /// <summary>Номер строки в исходном Excel (для понятных сообщений пользователю).</summary>
    public int RowNumber { get; set; }

    public string MarkingCode { get; set; } = "";

    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Итог разбора файла: строки с кодами и ошибки уровня файла.</summary>
public class ParseResult
{
    public List<MarkingRow> Rows { get; } = new();
    public List<string> FileErrors { get; } = new();

    public int ValidCount => Rows.Count(r => r.IsValid);
    public int InvalidCount => Rows.Count(r => !r.IsValid);
}
