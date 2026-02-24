namespace Workshop.Tests.TestSupport;

public static class TestHarness
{
    private static readonly List<string> Failures = new();

    public static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            Failures.Add($"FAIL: {name} -> {ex.Message}");
        }
    }

    public static void AssertEqual(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }

    public static int Complete()
    {
        if (Failures.Count > 0)
        {
            Console.Error.WriteLine($"Self-checks failed: {Failures.Count}");
            foreach (var failure in Failures)
                Console.Error.WriteLine(failure);
            return 1;
        }

        Console.WriteLine("All Workshop self-checks passed.");
        return 0;
    }
}
