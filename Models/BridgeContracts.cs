namespace Bridge.Models;

public static class BridgeModes
{
    public const string Real = "real";
    public const string Mock = "mock";
    public const string Disabled = "disabled";
    public const string Unsupported = "unsupported";
    public const string Misconfigured = "misconfigured";
}

public static class BridgeCodes
{
    public const string Ready = "ready";
    public const string Unsupported = "unsupported";
    public const string Disabled = "disabled";
    public const string Misconfigured = "misconfigured";
    public const string MockDisabled = "mock_disabled";
    public const string BridgeUnavailable = "bridge_unavailable";
    public const string PairingRequired = "bridge_pairing_required";
}

public sealed class DeviceReadiness
{
    public string Capability { get; init; } = string.Empty;
    public bool Ready { get; init; }
    public string Mode { get; init; } = BridgeModes.Unsupported;
    public string Code { get; init; } = BridgeCodes.Unsupported;
    public string? Message { get; init; }
    public string? MessageFa { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
