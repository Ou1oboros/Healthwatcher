using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthwatcherApi.Infrastructure.Persistence.Converters;

/// <summary>
/// SQLite has no date type, and EF cannot translate a comparison on a DateTimeOffset it
/// stored in the default format — which every history and uptime query depends on.
/// Storing UTC in this fixed ISO-8601 shape makes string ordering equal time ordering,
/// so "WHERE checked_at >= ?" and "ORDER BY checked_at" both work in the database.
/// </summary>
public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, string>
{
    /// <summary>Fixed width and always UTC — otherwise lexical order would not match chronological order.</summary>
    private const string SortableUtcFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public UtcDateTimeOffsetConverter()
        : base(
            value => value.ToUniversalTime().ToString(SortableUtcFormat, CultureInfo.InvariantCulture),
            stored => DateTimeOffset.ParseExact(
                stored,
                SortableUtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal))
    {
    }
}
