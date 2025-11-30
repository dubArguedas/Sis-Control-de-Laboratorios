using Microsoft.AspNetCore.SignalR;
using SCLAB_Entities;

namespace SCLAB_API.Services;

public class AlertasHub : Hub<IAlertasClient> 
{
    override public async Task OnConnectedAsync()
    {
        await Clients.Client(Context.ConnectionId).RecibirAlerta($"Alerta {Context.User?.Identity?.Name}");
        await base.OnConnectedAsync();
    }
}

public interface IAlertasClient{
    Task RecibirAlerta(string mensaje);
    Task RecibirNuevaAlerta(AlertaPayloadDto alerta); 
    Task RecibirCambioEstado(int laboratorioId, int maquinaId, string nuevoEstado);
}
public class AlertaPayloadDto
{
    public int AlertaId { get; set; }
    public string MaquinaCodigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}


