using Stockhub.Common.Domain;

namespace Stockhub.Modules.Users.Domain;

public sealed class User : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FullName { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    private User() { }

    public User(string email, string fullName, decimal currentBalance)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        FullName = fullName;
        CurrentBalance = currentBalance;
    }

    public void UpdateFullName(string fullName)
    {
        FullName = fullName;
    }

    public void UpdateEmail(string email)
    {
        Email = email;
    }

    public void UpdateBalance(decimal newBalance)
    {
        CurrentBalance = newBalance;
    }
}
