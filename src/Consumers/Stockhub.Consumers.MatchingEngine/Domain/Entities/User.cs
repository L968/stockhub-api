using Stockhub.Common.Domain;

namespace Stockhub.Consumers.MatchingEngine.Domain.Entities;

internal sealed class User : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public void Debit(decimal amount)
    {
        if (amount > CurrentBalance)
        {
            throw new InvalidOperationException("Insufficient balance");
        }

        CurrentBalance -= amount;
    }

    public void Credit(decimal amount)
    {
        CurrentBalance += amount;
    }
}
