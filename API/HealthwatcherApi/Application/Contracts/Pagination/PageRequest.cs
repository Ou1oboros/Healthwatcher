namespace HealthwatcherApi.Application.Contracts;

/// <summary>
/// Bound from the query string with [FromQuery]. Normalises itself on assignment so
/// a caller cannot ask for page 0 or for fifty thousand rows.
/// </summary>
public class PageRequest
{
    public const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _pageIndex = 1;
    private int _pageSize = DefaultPageSize;

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? DefaultPageSize : value;
    }
}
