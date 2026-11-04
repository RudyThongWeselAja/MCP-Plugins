namespace WeselAja.Integration.Configuration;

public static class EnvLoader
{
    public static void Load(
        string fileName = ".env")
    {
        var path =
            FindFile(fileName);

        if (path is null)
        {
            return;
        }

        foreach (
            var rawLine
            in File.ReadAllLines(path))
        {
            var line =
                rawLine
                    .Trim()
                    .TrimStart('\uFEFF');

            if (
                string.IsNullOrWhiteSpace(line)
                ||
                line.StartsWith("#"))
            {
                continue;
            }

            var separator =
                line.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            var key =
                line[..separator]
                    .Trim();

            var value =
                line[(separator + 1)..]
                    .Trim();

            value =
                RemoveQuotes(value);

            if (
                string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable(
                        key)))
            {
                Environment.SetEnvironmentVariable(
                    key,
                    value);
            }
        }
    }

    private static string? FindFile(
        string fileName)
    {
        var current =
            new DirectoryInfo(
                Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var candidate =
                Path.Combine(
                    current.FullName,
                    fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current =
                current.Parent;
        }

        return null;
    }

    private static string RemoveQuotes(
        string value)
    {
        if (
            value.Length >= 2
            &&
            (
                (
                    value.StartsWith("\"")
                    &&
                    value.EndsWith("\"")
                )
                ||
                (
                    value.StartsWith("'")
                    &&
                    value.EndsWith("'")
                )
            ))
        {
            return value[1..^1];
        }

        return value;
    }
}