namespace ProperTea.Core.Auth;

public interface ICurrentUser
{
    public string? Id { get; }
    public bool IsAuthenticated { get; }
}
