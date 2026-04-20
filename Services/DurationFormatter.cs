namespace SmartELibrary.Services;

public static class DurationFormatter
{
    public static string ToHms(double totalSeconds)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        var seconds = (long)Math.Floor(totalSeconds);
        var hours = seconds / 3600;
        var minutes = (seconds % 3600) / 60;
        var secs = seconds % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }
}
