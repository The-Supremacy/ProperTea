namespace ProperTea.Core.Auth;

public interface ICurrentUser
{
    string? Id { get; }
    bool IsAuthenticated { get; }
}
