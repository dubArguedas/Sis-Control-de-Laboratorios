using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Radzen;
using System;

namespace SCLAB_Client.Components.Service.SignalR
{
    public class SignalRAlertasService : IAsyncDisposable
    {
        private HubConnection? hubConnection;
        private readonly NavigationManager Navigation;
        private readonly NotificationService NotificationService;

        public event Action? OnAlertaRecibida;

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

            hubConnection.On<string>("RecibirAlerta", (mensaje) =>
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Nueva Alerta",
                    Detail = mensaje,
                    Duration = 10000
                });

                OnAlertaRecibida?.Invoke();
            });


            await hubConnection.StartAsync();
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

