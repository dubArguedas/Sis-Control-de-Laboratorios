using Radzen;
using SCLAB_Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using SCLAB_Client.Services;
using SCLAB_Client.Models;
using SCLAB_Client.Components.Service.GestionLaboratorio;
using SCLAB_Client.Components.Service.ServiciosApi;
using SCLAB_Client.Components.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

// ⚠️ CRÍTICO: TokenStateService debe ser SINGLETON, no Scoped
builder.Services.AddSingleton<ITokenStateService, TokenStateService>();

// Configurar HttpClient con el AuthHandler
builder.Services.AddScoped<AuthHttpClientHandler>();

// HttpClient para API general (sin autenticación)
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// HttpClient para API con autenticación (CON AuthHandler)
builder.Services.AddHttpClient("AuthApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
})
.AddHttpMessageHandler<AuthHttpClientHandler>();

// HttpClient genérico (opcional, mantener si lo usas en otros lugares)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7241/")
});
builder.Services.AddHttpClient();

// Registrar servicios
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.AddScoped<AsistenciaService, AsistenciaService>();


// Registrar UsuarioService con la configuración específica
builder.Services.AddScoped<IUsuarioService, UsuarioService>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AuthApiClient");
    var tokenState = serviceProvider.GetRequiredService<ITokenStateService>();

    return new UsuarioService(httpClient, tokenState);
});

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<IContactoService, ContactoService>();
builder.Services.AddScoped<LaboratorioService>();
builder.Services.AddScoped<CronogramaService>();
builder.Services.AddScoped<MaquinaService>();
builder.Services.AddScoped<DocenteService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<ReportesService>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DialogService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7241")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseCors();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();