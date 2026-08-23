using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthwatcherApi.Infrastructure.Persistence.Converters;

/// <summary>
/// SQLite has no date type, and EF can't translate a comparison against a DateTimeOffset
/// stored in the default format. Storing UTC in this fixed ISO-8601 shape makes string
/// ordering equal time ordering, so "WHERE checked_at >= ?" and "ORDER BY checked_at"
/// both work directly in the database - every history/uptime query relies on this.
/// </summary>
public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, string>
{
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
