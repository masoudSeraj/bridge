namespace Bridge.Models;

public record PosSaleResult(
    bool Success,
    string? Rrn,
    string? ErrorMessage,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null
);
