using Radzen;
using SCLAB_Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using SCLAB_Client.Services;
using SCLAB_Client.Models;

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

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7241/") // Ajusta el puerto según tu API
});

// Servicio de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// UsuarioService con IHttpClientFactory
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddBlazoredLocalStorage();

// Servicio del Contacto
builder.Services.AddScoped<IContactoService, ContactoService>();

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