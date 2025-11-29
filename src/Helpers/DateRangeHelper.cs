namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for date range operations.
/// </summary>
public static class DateRangeHelper
{
    /// <summary>
    /// Generates a list of dates between start and end date (inclusive).
    /// </summary>
    /// <param name="startDate">Start date of the range.</param>
    /// <param name="endDate">End date of the range.</param>
    /// <returns>List of dates in the range.</returns>
    public static List<DateOnly> GenerateDateRange(DateOnly startDate, DateOnly endDate)
    {
        var dates = new List<DateOnly>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            dates.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }

        return dates;
    }
}
