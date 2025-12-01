using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Radzen;
using System;

namespace SCLAB_Client.Components.Service.SignalR
{
    public class AlertaPayloadDto
    {
        public int AlertaId { get; set; }
        public string MaquinaCodigo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
    public class SignalRAlertasService : IAsyncDisposable
    {
        private HubConnection? hubConnection;
        private readonly NavigationManager Navigation;
        private readonly NotificationService NotificationService;

        public event Action<AlertaPayloadDto>? OnAlertaRecibida;
        public SignalRAlertasService(NavigationManager navigation, NotificationService notificationService)
        {
            Navigation = navigation;
            NotificationService = notificationService;
        }

        public async Task StartConnection()
        {
            var hubUrl = "https://localhost:7241/alertas";

            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<AlertaPayloadDto>("RecibirNuevaAlerta", (alerta) =>
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = $"⚠️ Nueva Alerta: {alerta.MaquinaCodigo}",
                    Duration = 8000,
                    Style = "width: 300px;" 
                });
                OnAlertaRecibida?.Invoke(alerta);
            });

            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error conectando SignalR: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }
    }
}

