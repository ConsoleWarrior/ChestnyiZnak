using System.Xml;
using System.Xml.Schema;

namespace ChestnyiZnak.Services;

/// <summary>
/// Проверяет готовый XML на соответствие официальной XSD-схеме прямо в браузере.
/// Схемы (основную + базовые типы) пользователь загружает сам со страницы шаблонов ГИС МТ —
/// так мы всегда работаем с актуальной версией, а не с устаревшей копией.
/// </summary>
public class XsdValidationService
{
    public record Result(bool IsValid, List<string> Messages);

    /// <param name="xmlBytes">Проверяемый XML.</param>
    /// <param name="mainXsd">Основная схема (например, Производство_РФ.xsd).</param>
    /// <param name="baseTypesXsd">Базовые типы (LP_base_types.xsd) — на них ссылается xs:include.</param>
    public Result Validate(byte[] xmlBytes, byte[] mainXsd, byte[]? baseTypesXsd)
    {
        var messages = new List<string>();
        try
        {
            var schemas = new XmlSchemaSet();

            // Основная схема через xs:include подтягивает LP_base_types.xsd по относительному пути,
            // которого в браузере нет. Подставляем загруженные base types через свой резолвер.
            schemas.XmlResolver = baseTypesXsd is not null
                ? new BaseTypesResolver(baseTypesXsd)
                : null;

            using (var xsdStream = new MemoryStream(mainXsd))
            using (var xsdReader = XmlReader.Create(xsdStream))
                schemas.Add(null, xsdReader); // схемы без targetNamespace → null

            schemas.Compile();

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };
            settings.ValidationEventHandler += (_, e) =>
            {
                var kind = e.Severity == XmlSeverityType.Error ? "Ошибка" : "Предупреждение";
                var line = e.Exception?.LineNumber ?? 0;
                messages.Add(line > 0 ? $"{kind} (строка {line}): {e.Message}" : $"{kind}: {e.Message}");
            };

            using var xmlStream = new MemoryStream(xmlBytes);
            using var reader = XmlReader.Create(xmlStream, settings);
            while (reader.Read()) { }

            var hasErrors = messages.Any(m => m.StartsWith("Ошибка"));
            return new Result(!hasErrors, messages);
        }
        catch (XmlSchemaException ex)
        {
            return new Result(false, new List<string>
            {
                $"Не удалось загрузить XSD: {ex.Message}. Убедитесь, что приложены ОБА файла — основная схема и LP_base_types.xsd."
            });
        }
        catch (Exception ex)
        {
            return new Result(false, new List<string> { $"Ошибка проверки: {ex.Message}" });
        }
    }

    /// <summary>Возвращает загруженные base types на любой запрос xs:include, игнорируя путь.</summary>
    private sealed class BaseTypesResolver : XmlResolver
    {
        private readonly byte[] _baseTypes;
        public BaseTypesResolver(byte[] baseTypes) => _baseTypes = baseTypes;

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
            => new Uri("inmemory:///" + (relativeUri ?? "base.xsd"));

        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            => new MemoryStream(_baseTypes);
    }
}
