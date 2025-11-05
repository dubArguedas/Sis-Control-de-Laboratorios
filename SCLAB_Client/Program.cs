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


//url api
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:5081/") 
});

//servicios
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddBlazoredLocalStorage();

//servcioAutenticacion
builder.Services.AddScoped<IAuthService, AuthService>();

//agregando el servicoi del Contacto
builder.Services.AddScoped<IContactoService, ContactoService>();

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
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.UseCors();

app.Run();