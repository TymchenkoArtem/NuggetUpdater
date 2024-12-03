namespace NuggetUpdater.Models;

public sealed record UserCredentials
{
    public string Email { get; set; }
    public string Token { get; set; }
}
