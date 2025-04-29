public class EmailAppConfig
{
    public required string FromName {get ; set;}
    public required string FromAddress {get; set;}
    public required string Server {get; set;}
    public required ushort Port {get; set;}
    public string? AuthUsername {get; set;}
    public string? AuthPassword {get; set;}
}