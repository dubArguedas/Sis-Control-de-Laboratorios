using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SCLAB_Client.Services
{
    public interface IContactoService
    {
                Task<bool> EnviarCorreoContacto(string nombreRemitente, string emailRemitente, string mensaje);
    }

    public class ContactoService : IContactoService
    {
        private readonly IConfiguration _configuration;

        public ContactoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarCorreoContacto(string nombreRemitente, string emailRemitente, string mensaje)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var email = new MimeMessage();

                // De quién viene el correo (tu correo de PachaSoft)
                email.From.Add(new MailboxAddress(
                    emailSettings["SenderName"],
                    emailSettings["SenderEmail"]
                ));

                // A quién va dirigido (tu correo para recibir mensajes)
                email.To.Add(new MailboxAddress(
                    "PachaSoft Contacto",
                    emailSettings["ReceiverEmail"]
                ));

                // Asunto
                email.Subject = $"Nuevo mensaje de contacto de {nombreRemitente}";

                // Cuerpo del correo en HTML
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background: linear-gradient(135deg, #8B4C4C 0%, #662222 100%); padding: 30px; text-align: center;'>
                                <h1 style='color: white; margin: 0;'>PachaSoft</h1>
                                <p style='color: white; margin: 10px 0 0 0;'>Nuevo Mensaje de Contacto</p>
                            </div>
                            
                            <div style='background-color: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                                <div style='background: white; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                                    <h3 style='color: #662222; margin-top: 0;'>Información del Remitente</h3>
                                    <p><strong>Nombre:</strong> {nombreRemitente}</p>
                                    <p><strong>Email:</strong> {emailRemitente}</p>
                                </div>
                                
                                <div style='background: white; padding: 20px; border-radius: 8px;'>
                                    <h3 style='color: #662222; margin-top: 0;'>Mensaje</h3>
                                    <p style='line-height: 1.6; color: #666;'>{mensaje}</p>
                                </div>
                                
                                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center;'>
                                    <p style='color: #999; font-size: 12px;'>
                                        Este correo fue enviado desde el formulario de contacto de PachaSoft<br/>
                                        Universidad Privada del Valle - La Paz, Bolivia
                                    </p>
                                </div>
                            </div>
                        </div>
                    "
                };

                email.Body = bodyBuilder.ToMessageBody();

                // Configurar cliente SMTP
                using var smtp = new SmtpClient();

                // Conectar al servidor SMTP
                await smtp.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"]),
                    SecureSocketOptions.StartTls
                );

                // Autenticar
                await smtp.AuthenticateAsync(
                    emailSettings["Username"],
                    emailSettings["Password"]
                );

                // Enviar correo
                await smtp.SendAsync(email);

                // Desconectar
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Log del error (puedes usar ILogger aquí)
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
                return false;
            }
        }
    }
}