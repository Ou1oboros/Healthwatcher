using System.ComponentModel.DataAnnotations;

namespace HealthwatcherApi.Domain.Entities;

/// <summary>
/// Identity, audit trail and soft-delete flag. Both the audit stamping and the
/// global "not deleted" query filter are applied by AppDbContext — an entity only
/// has to derive from this to get them.
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [MaxLength(Shared.ValidationConstants.UserNameMaxLength)]
    public string? CreatedBy { get; set; }

    [MaxLength(Shared.ValidationConstants.UserNameMaxLength)]
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
