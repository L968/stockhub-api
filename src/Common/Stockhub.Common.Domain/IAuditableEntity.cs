namespace Stockhub.Common.Domain;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}
