namespace TaskTracker.Infrastructure.Email;
public class RabbitMqSettings
{
    public string? Host { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? VHost { get; set; }
}