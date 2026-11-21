namespace SmartEMobilityHub.Tests;

public sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}

public static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new AssertionException(message);
    }

    public static void False(bool condition, string message) => True(!condition, message);

    public static void Equal<T>(T expected, T actual, string message = "")
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException($"{message} (mong đợi: {expected}, thực tế: {actual})");
    }

    public static void InRange(double actual, double min, double max, string message)
    {
        if (actual < min || actual > max)
            throw new AssertionException($"{message} (thực tế: {actual:0.###}, khoảng: {min}–{max})");
    }

    public static void Throws<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"{message} — ném {ex.GetType().Name} thay vì {typeof(TException).Name}");
        }

        throw new AssertionException($"{message} — không ném {typeof(TException).Name}");
    }
}
