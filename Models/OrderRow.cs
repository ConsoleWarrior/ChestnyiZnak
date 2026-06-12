namespace ChestnyiZnak.Models;

/// <summary>
/// Одна строка заказа кодов маркировки: GTIN товара и нужное количество кодов.
/// </summary>
public class OrderRow
{
    public int RowNumber { get; set; }

    public string Gtin { get; set; } = "";

    /// <summary>«Сырое» значение количества из ячейки (для показа как ввёл пользователь).</summary>
    public string QuantityRaw { get; set; } = "";

    /// <summary>Распознанное количество (валидно только если нет ошибок по этой строке).</summary>
    public int Quantity { get; set; }

    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Итог разбора файла заказа кодов.</summary>
public class OrderParseResult
{
    public List<OrderRow> Rows { get; } = new();
    public List<string> FileErrors { get; } = new();

    public int ValidCount => Rows.Count(r => r.IsValid);
    public int InvalidCount => Rows.Count(r => !r.IsValid);
}
