using Radzen;
using SCLAB_Client.Components;
using SCLAB_Client.Components.Service;
using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using SCLAB_Client.Services;
using SCLAB_Client.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

// Configuración de HttpClient para la API
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/"); // Usando el puerto correcto de tu API
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Servicio de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// Otros servicios
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
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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