using System.ComponentModel.DataAnnotations;

namespace HealthwatcherApi.Domain.Entities;

// Deriving from this is all an entity needs for the audit stamps and soft-delete filter
// that AppDbContext applies.
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    [MaxLength(Shared.ValidationConstants.UserNameMaxLength)]
    public string? CreatedBy { get; set; }

    [MaxLength(Shared.ValidationConstants.UserNameMaxLength)]
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
