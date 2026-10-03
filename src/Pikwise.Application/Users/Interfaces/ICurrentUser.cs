namespace Pikwise.Application.Users.Interfaces;

// The API supplies identity from validated claims; user-owned operations take no client user Id.
public interface ICurrentUser
{
    string? Subject { get; }
    string? Email { get; }
}
