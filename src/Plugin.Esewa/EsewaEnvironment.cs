namespace Plugin.Esewa;

/// <summary>
/// Target eSewa backend environment.
/// </summary>
public enum EsewaEnvironment
{
    /// <summary>Test / sandbox environment used for integration testing.</summary>
    Test,

    /// <summary>Live production environment used for real transactions.</summary>
    Production,
}
