namespace BlobBackup;

public static class Helper
{
    public static void WriteLine()
    {
        Console.WriteLine();
    }

    public static void WriteLine(string value)
    {
        Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss.fff} {value}");
    }
}
