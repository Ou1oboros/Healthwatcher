using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthwatcherApi.Infrastructure.Persistence.Converters;

// SQLite has no date type. Storing UTC in this fixed ISO-8601 shape makes string ordering
// match time ordering, so the history and uptime queries can filter and sort in the database.
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
