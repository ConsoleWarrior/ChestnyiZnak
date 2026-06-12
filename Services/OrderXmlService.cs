using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChestnyiZnak.Models;

namespace ChestnyiZnak.Services;

/// <summary>
/// Формирует XML «Заказ кодов маркировки» по официальной схеме urn:oms.order
/// (структура подтверждена образцом схемы заказа КМ для легпрома).
/// Один заказ = один способ выпуска/создания + список товаров (GTIN + количество).
/// </summary>
public class OrderXmlService
{
    // Параметры всего заказа (задаются на странице один раз). label → код для XML.
    public static readonly Dictionary<string, string> ReleaseMethods = new()
    {
        ["Маркировка остатков"] = "REMAINS",
        ["Импорт"] = "IMPORT",
        ["Перемаркировка"] = "REMARK",
    };

    public static readonly Dictionary<string, string> CreateMethods = new()
    {
        ["Самостоятельно"] = "SELF_MADE",
        ["Оператором"] = "OPERATOR",
    };

    public static readonly Dictionary<string, string> SerialNumberTypes = new()
    {
        ["Оператором (генерирует ЦРПТ)"] = "OPERATOR",
        ["Самостоятельно"] = "SELF_MADE",
    };

    // Для легпрома эти значения фиксированы (из образца схемы).
    private const string ProductGroup = "lp";
    private const string CisType = "UNIT";  // единица товара
    private const int TemplateId = 10;      // шаблон легпрома

    /// <summary>Параметры заказа уровня документа.</summary>
    public record OrderParams(
        string ContactPerson,
        string ReleaseMethodCode,
        string CreateMethodCode,
        string SerialNumberTypeCode);

    public byte[] Build(IEnumerable<OrderRow> validRows, OrderParams p)
    {
        XNamespace ns = "urn:oms.order";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        var order = new XElement(ns + "order",
            new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
            new XAttribute(xsi + "schemaLocation", "urn:oms.order schema.xsd"),
            new XElement(ns + "lp",
                new XElement(ns + "productGroup", ProductGroup),
                new XElement(ns + "contactPerson", p.ContactPerson),
                new XElement(ns + "releaseMethodType", p.ReleaseMethodCode),
                new XElement(ns + "createMethodType", p.CreateMethodCode),
                new XElement(ns + "products",
                    validRows.Select(r => new XElement(ns + "product",
                        new XElement(ns + "gtin", r.Gtin),
                        new XElement(ns + "quantity", r.Quantity),
                        new XElement(ns + "serialNumberType", p.SerialNumberTypeCode),
                        new XElement(ns + "cisType", CisType),
                        new XElement(ns + "templateId", TemplateId)
                    ))
                )
            )
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), order);
        return ToUtf8Bytes(doc);
    }

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
