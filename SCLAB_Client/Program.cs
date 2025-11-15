using Radzen;
using SCLAB_Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using SCLAB_Client.Services;
using SCLAB_Client.Models;
using SCLAB_Client.Components.Service.GestionLaboratorio;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

// IMPORTANTE: Configuración de HttpClientFactory
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Ignorar errores de certificado SSL en desarrollo
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// NUEVO: HttpClient con autenticación automática
builder.Services.AddHttpClient("AuthApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Ignorar errores de certificado SSL en desarrollo
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
})
.AddHttpMessageHandler<AuthHttpClientHandler>(); // Agregar el handler de autenticación

// HttpClient genérico (mantener por compatibilidad)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7241/") // Ajusta el puerto según tu API
});
builder.Services.AddHttpClient();

// NUEVO: Handler para autenticación automática
builder.Services.AddScoped<AuthHttpClientHandler>();

// Servicio de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// UsuarioService con IHttpClientFactory - MODIFICADO para usar ambos clients
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddBlazoredLocalStorage();

// Servicio del Contacto
builder.Services.AddScoped<IContactoService, ContactoService>();
builder.Services.AddScoped<LaboratorioService>();
builder.Services.AddScoped<CronogramaService>();
builder.Services.AddScoped<MaquinaService>();

// Servicio de notificaciones de Radzen
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DialogService>();

// Configuración
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