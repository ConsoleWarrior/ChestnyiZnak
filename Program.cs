using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChestnyiZnak;
using ChestnyiZnak.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Наши сервисы
// Ввод в оборот
builder.Services.AddScoped<ExcelTemplateService>();
builder.Services.AddScoped<ExcelParserService>();
builder.Services.AddScoped<CsvCodeParserService>();
builder.Services.AddScoped<IntroductionXmlService>();
builder.Services.AddScoped<XsdValidationService>();

// Заказ кодов маркировки
builder.Services.AddScoped<OrderTemplateService>();
builder.Services.AddScoped<OrderParserService>();
builder.Services.AddScoped<OrderXmlService>();

await builder.Build().RunAsync();
