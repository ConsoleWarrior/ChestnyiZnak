using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChestnyiZnak.Models;

namespace ChestnyiZnak.Services;

/// <summary>
/// Формирует XML документа «Вывод из оборота» (корень withdrawal, version=8)
/// по официальной схеме Вывод_из_оборота.xsd. Код идентификации — в элементе &lt;cis&gt; (CDATA).
/// </summary>
public class WithdrawalXmlService
{
    /// <summary>Причины вывода из оборота (label → код). Из справочника withdrawal_type.</summary>
    public static readonly Dictionary<string, string> Reasons = new()
    {
        ["Розничная продажа"] = "RETAIL",
        ["Продажа по образцам"] = "BY_SAMPLES",
        ["Дистанционная продажа"] = "DISTANCE",
        ["Продажа через вендинговый аппарат"] = "VENDING",
        ["Продажа по образцам / дистанционно"] = "REMOTE_SALE",
        ["Безвозмездная передача"] = "DONATION",
        ["Использование для собственных нужд"] = "OWN_USE",
        ["Собственные нужды предприятия"] = "ENTERPRISE_USE",
        ["Использование для производственных целей"] = "PRODUCTION_USE",
        ["Продажа по гос. (муниципальному) контракту"] = "STATE_CONTRACT",
        ["Продажа по сделке с гостайной"] = "STATE_SECRET",
        ["Экспорт за пределы ЕАЭС"] = "BEYOND_EEC_EXPORT",
        ["Экспорт в страны ЕАЭС"] = "EEC_EXPORT",
        ["Трансграничная продажа в ЕАЭС"] = "EAS_TRADE",
        ["Возврат физическому лицу"] = "RETURN",
        ["Утрата"] = "LOSS",
        ["Утрата или повреждение"] = "DAMAGE_LOSS",
        ["Уничтожение"] = "DESTRUCTION",
        ["Утилизация"] = "UTILIZATION",
        ["Конфискация"] = "CONFISCATION",
        ["Отзыв с рынка"] = "RECALL",
        ["Фасовка"] = "PACKING",
        ["Истечение срока годности"] = "EXPIRATION",
        ["Ликвидация предприятия"] = "LIQUIDATION",
        ["Другое"] = "OTHER",
    };

    /// <summary>Типы первичного документа (label → код). Из справочника withdrawal_primary_document_type.</summary>
    public static readonly Dictionary<string, string> PrimaryDocTypes = new()
    {
        ["Кассовый чек"] = "RECEIPT",
        ["Товарный чек"] = "SALES_RECEIPT",
        ["Таможенная декларация"] = "CUSTOMS_DECLARATION",
        ["Товарная накладная"] = "CONSIGNMENT_NOTE",
        ["Универсальный передаточный документ (УПД)"] = "UTD",
        ["Акт уничтожения / утраты / утилизации"] = "DESTRUCTION_ACT",
        ["Прочее"] = "OTHER",
    };

    public const string OtherReasonCode = "OTHER";

    public record WithdrawalParams(
        string TradeParticipantInn,
        string WithdrawalTypeCode,
        DateOnly WithdrawalDate,
        string? WithdrawalTypeOther = null,
        string? BuyerInn = null,
        string? PrimaryDocTypeCode = null,
        string? PrimaryDocNumber = null,
        DateOnly? PrimaryDocDate = null);

    public byte[] Build(IEnumerable<MarkingRow> validRows, WithdrawalParams p)
    {
        // Порядок элементов строго по xs:sequence схемы.
        var root = new XElement("withdrawal", new XAttribute("version", "8"));
        root.Add(new XElement("trade_participant_inn", p.TradeParticipantInn));

        if (!string.IsNullOrWhiteSpace(p.BuyerInn))
            root.Add(new XElement("buyer_inn", p.BuyerInn));

        root.Add(new XElement("withdrawal_type", p.WithdrawalTypeCode));

        if (p.WithdrawalTypeCode == OtherReasonCode && !string.IsNullOrWhiteSpace(p.WithdrawalTypeOther))
            root.Add(new XElement("withdrawal_type_other", p.WithdrawalTypeOther));

        root.Add(new XElement("withdrawal_date", Date(p.WithdrawalDate)));

        if (!string.IsNullOrWhiteSpace(p.PrimaryDocTypeCode))
            root.Add(new XElement("primary_document_type", p.PrimaryDocTypeCode));
        if (!string.IsNullOrWhiteSpace(p.PrimaryDocNumber))
            root.Add(new XElement("primary_document_number", p.PrimaryDocNumber));
        if (p.PrimaryDocDate is { } pdd)
            root.Add(new XElement("primary_document_date", Date(pdd)));

        root.Add(new XElement("products_list",
            validRows.Select(r => new XElement("product",
                new XElement("cis", new XCData(r.MarkingCode))))));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return ToUtf8Bytes(doc);
    }

    private static string Date(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static byte[] ToUtf8Bytes(XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            doc.Save(writer);
        }
        return ms.ToArray();
    }
}
