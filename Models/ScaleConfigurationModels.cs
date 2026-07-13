namespace Bridge.Models;

public sealed class ScaleConfigurationRequest
{
    public string? PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public string Parity { get; init; } = "None";
    public int DataBits { get; init; } = 8;
    public string StopBits { get; init; } = "One";
    public string OutputUnit { get; init; } = "kg";
}

public sealed class ScaleConfigurationResponse
{
    public bool Success { get; init; }
    public string[] Ports { get; init; } = Array.Empty<string>();
    public string? PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public string Parity { get; init; } = "None";
    public int DataBits { get; init; } = 8;
    public string StopBits { get; init; } = "One";
    public string OutputUnit { get; init; } = "kg";
    public string? ErrorMessage { get; init; }
    public string? ErrorMessageFa { get; init; }
    public string Mode { get; init; } = BridgeModes.Real;
    public bool Ready { get; init; }
    public string Code { get; init; } = BridgeCodes.Ready;
}
