namespace Bridge.Models;

public class BridgeHealthResponse
{
    public bool Success { get; set; }
    public bool Installed { get; set; }
    public string Status { get; set; } = "running";
    public string? Message { get; set; }
    public string? MessageFa { get; set; }
    public string? DeviceId { get; set; }
    public string? BridgeVersion { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public bool RequiresPairing { get; set; }
    public bool TokenConfigured { get; set; }
    public string? ListenUrl { get; set; }
    public string? SettingsPath { get; set; }
    public string? SetupUrl { get; set; }
    public DeviceReadiness[] Services { get; set; } = Array.Empty<DeviceReadiness>();
}

public class VoipCallRequest
{
    public string? Phone { get; set; }
    public string? NormalizedPhone { get; set; }
    public int? LeadId { get; set; }
    public string? DisplayName { get; set; }
    public int? SellerUserId { get; set; }
}

public class VoipCallResponse
{
    public bool Success { get; set; }
    public string? CallSessionId { get; set; }
    public string? Provider { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string Mode { get; set; } = BridgeModes.Real;
    public bool Ready { get; set; } = true;
    public string Code { get; set; } = BridgeCodes.Ready;
    public string? ErrorMessageFa { get; set; }
}

public class VoipHealthResponse
{
    public bool Success { get; set; }
    public bool Enabled { get; set; }
    public bool Ready { get; set; }
    public string? Provider { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string Mode { get; set; } = BridgeModes.Real;
    public string Code { get; set; } = BridgeCodes.Ready;
    public string? ErrorMessage { get; set; }
    public string? ErrorMessageFa { get; set; }
}

