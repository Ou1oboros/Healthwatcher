using System.ComponentModel.DataAnnotations;

namespace HealthwatcherApi.Domain.Entities;

// Audit stamping and the global "not deleted" query filter both live in AppDbContext -
// deriving from this is all an entity needs to do to get them.
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
