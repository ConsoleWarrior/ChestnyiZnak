using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChestnyiZnak.Models;

namespace ChestnyiZnak.Services;

/// <summary>Тип документа «Ввод в оборот» (каждому соответствует своя XSD).</summary>
public enum IntroductionType
{
    /// <summary>Производство РФ — introduce_rf (version 9).</summary>
    ProductionRf,
    /// <summary>Контрактное производство — introduce_contract (version 7).</summary>
    Contract,
    /// <summary>Маркировка остатков — vvod_ostatky (version 3).</summary>
    Remains,
    /// <summary>Импорт с ФТС — introduce_import_fts (version 3).</summary>
    ImportFts,
    /// <summary>Производство вне ЕАЭС — introduce_import (version 5).</summary>
    ProductionOutsideEaeu
}

/// <summary>
/// Формирует XML документов «Ввод в оборот» строго по официальным XSD-схемам ГИС МТ.
/// Все схемы без namespace; в товаре заполняем только обязательный минимум — код (ki).
/// </summary>
public class IntroductionXmlService
{
    /// <summary>Параметры документа (нужные поля зависят от типа).</summary>
    public record IntroduceParams(
        IntroductionType Type,
        string? TradeParticipantInn,
        string? ProducerInn,
        string? OwnerInn,
        DateOnly? ProductionDate,
        string? DeclarationNumber = null,
        DateOnly? DeclarationDate = null,
        string? CustomsCode = null,
        string? DecisionCode = null);

    public byte[] Build(IEnumerable<MarkingRow> validRows, IntroduceParams p)
    {
        var rows = validRows.ToList();
        var root = p.Type switch
        {
            IntroductionType.ProductionRf => BuildProductionRf(rows, p),
            IntroductionType.Contract => BuildContract(rows, p),
            IntroductionType.Remains => BuildRemains(rows, p),
            IntroductionType.ImportFts => BuildImportFts(rows, p),
            IntroductionType.ProductionOutsideEaeu => BuildProductionOutsideEaeu(rows, p),
            _ => throw new ArgumentOutOfRangeException(nameof(p))
        };

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return ToUtf8Bytes(doc);
    }

    /// <summary>Имя файла по типу документа.</summary>
    public string FileName(IntroductionType type) => type switch
    {
        IntroductionType.ProductionRf => "vvod_proizvodstvo_rf.xml",
        IntroductionType.Contract => "vvod_kontraktnoe.xml",
        IntroductionType.Remains => "vvod_ostatki.xml",
        IntroductionType.ImportFts => "vvod_import_fts.xml",
        IntroductionType.ProductionOutsideEaeu => "vvod_vne_eaes.xml",
        _ => "vvod_v_oborot.xml"
    };

    // introduce_rf (v9): участник + производитель + собственник + production_order
    private static XElement BuildProductionRf(List<MarkingRow> rows, IntroduceParams p)
    {
        var root = new XElement("introduce_rf",
            new XAttribute("version", "9"),
            new XElement("trade_participant_inn", p.TradeParticipantInn),
            new XElement("producer_inn", p.ProducerInn),
            new XElement("owner_inn", p.OwnerInn));
        AddProductionDate(root, p.ProductionDate);
        root.Add(new XElement("production_order", "OWN_PRODUCTION"));
        root.Add(ProductsList(rows));
        return root;
    }

    // introduce_contract (v7): производитель + собственник + production_order
    private static XElement BuildContract(List<MarkingRow> rows, IntroduceParams p)
    {
        var root = new XElement("introduce_contract",
            new XAttribute("version", "7"),
            new XElement("producer_inn", p.ProducerInn),
            new XElement("owner_inn", p.OwnerInn));
        AddProductionDate(root, p.ProductionDate);
        root.Add(new XElement("production_order", "CONTRACT_PRODUCTION"));
        root.Add(ProductsList(rows));
        return root;
    }

    // vvod_ostatky (v3): только участник оборота
    private static XElement BuildRemains(List<MarkingRow> rows, IntroduceParams p)
        => new XElement("vvod_ostatky",
            new XAttribute("version", "3"),
            new XElement("trade_participant_inn", p.TradeParticipantInn),
            ProductsList(rows));

    // introduce_import_fts (v3): участник + номер ДТ + дата ДТ
    // Порядок элементов по схеме: trade_participant_inn, declaration_number, declaration_date
    private static XElement BuildImportFts(List<MarkingRow> rows, IntroduceParams p)
        => new XElement("introduce_import_fts",
            new XAttribute("version", "3"),
            new XElement("trade_participant_inn", p.TradeParticipantInn),
            new XElement("declaration_number", p.DeclarationNumber),
            new XElement("declaration_date", FormatDate(p.DeclarationDate)),
            ProductsList(rows));

    // introduce_import (v5, «Производство вне ЕАЭС»)
    // Порядок: trade_participant_inn, declaration_date, declaration_number, customs_code, decision_code
    private static XElement BuildProductionOutsideEaeu(List<MarkingRow> rows, IntroduceParams p)
        => new XElement("introduce_import",
            new XAttribute("version", "5"),
            new XElement("trade_participant_inn", p.TradeParticipantInn),
            new XElement("declaration_date", FormatDate(p.DeclarationDate)),
            new XElement("declaration_number", p.DeclarationNumber),
            new XElement("customs_code", p.CustomsCode),
            new XElement("decision_code", p.DecisionCode),
            ProductsList(rows));

    private static XElement ProductsList(List<MarkingRow> rows)
        => new XElement("products_list",
            // КИ может содержать спецсимволы → оборачиваем в CDATA
            // (по «Рекомендуемому алгоритму экранирования» ГИС МТ).
            rows.Select(r => new XElement("product", new XElement("ki", new XCData(r.MarkingCode)))));

    private static void AddProductionDate(XElement root, DateOnly? date)
    {
        if (date is { } d)
            root.Add(new XElement("production_date", FormatDate(d)));
    }

    private static string? FormatDate(DateOnly? d)
        => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static byte[] ToUtf8Bytes(XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) // без BOM
        };
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            doc.Save(writer);
        }
        return ms.ToArray();
    }
}
