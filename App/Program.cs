using App.Clients;
using App.Infrastructure;
using App.Middleware;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<LogRequestOnActionFilterAttribute>();
builder.Services.AddScoped<LogResponseOnResultFilterAttribute>();
builder.Services.AddServiceCollection(builder.Configuration);

// ---- ตัวเรียกระบบปลายทาง ----
// เดิมสร้าง new HttpClient() ใหม่ทุกครั้งที่กดปุ่ม (15 จุด) ซึ่งทำให้ socket ถูกใช้จนหมดเมื่อมีการใช้งานถี่
// และไม่มีการตั้ง timeout เลย ถ้าปลายทางค้างก็ค้างยาว — เปลี่ยนมาใช้ IHttpClientFactory จัดการให้
var cs = builder.Configuration.GetSection("ConnectionStrings");

void AddDownstream(string name, string? baseUrl, Action<HttpClient>? extra = null)
{
    builder.Services.AddHttpClient(name, c =>
    {
        c.BaseAddress = new Uri((baseUrl ?? "http://localhost").TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(60);
        extra?.Invoke(c);
    });
}

var callerHeader = cs["ApiUserHeader"] ?? builder.Environment.EnvironmentName;

AddDownstream("c100", cs["C100"], c =>
{
    if (!string.IsNullOrWhiteSpace(cs["C100Apikey"])) c.DefaultRequestHeaders.Add("Apikey", cs["C100Apikey"]);
});

AddDownstream("esig", cs["SGAPIESIG"], c =>
{
    if (!string.IsNullOrWhiteSpace(cs["Apikey"])) c.DefaultRequestHeaders.Add("apikey", cs["Apikey"]);
    c.DefaultRequestHeaders.Add("user", callerHeader);
});

// URL นี้เดิมฝังอยู่ในโค้ด 4 จุด (SGF_ReCreateESig / ByPassCustomer / ByPassIMEI / Mail)
AddDownstream("posservice", cs["PosService"] ?? "https://sg-posservice.singerthai.co.th:10082");

AddDownstream("sgb", cs["SGBCancelApi"], c =>
{
    if (!string.IsNullOrWhiteSpace(cs["SGBCancelApikey"])) c.DefaultRequestHeaders.Add("apikey", cs["SGBCancelApikey"]);
});

builder.Services.AddSingleton<IDownstreamApi, DownstreamApi>();

// เก็บผลค้นหาของเคสที่ใช้บ่อย (วันนี้ หน้าแรก) ไว้ในหน่วยความจำสั้น ๆ
builder.Services.AddMemoryCache();

// อายุของผลค้นหาที่เก็บไว้ ตั้งจาก config ได้ (0 = ปิด cache ทั้งหมด)
App.Controllers.HomeController.ConfigureCache(builder.Configuration.GetValue("Search:CacheSeconds", 10));

// อ่านข้อมูลของวันนี้เป็นระยะ เพื่อไม่ให้ผู้ใช้คนแรกของช่วงต้องรอ query ที่ข้อมูลหลุดจากหน่วยความจำ
builder.Services.AddHostedService<SearchPrewarmService>();

// เปิดการเขียนข้อมูลเฉพาะเมื่อระบุไว้ชัดเจนเท่านั้น — ไม่ระบุ = อ่านอย่างเดียว
App.Filters.InvalidateSearchCacheAttribute.ConfigureWrites(
    builder.Configuration.GetValue("App:AllowWrites", false));
builder.Logging.ClearProviders().AddConsole();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(86400);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "Handled {RequestPath}";
    // Emit debug-level events instead of the defaults
    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    };
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();



app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
