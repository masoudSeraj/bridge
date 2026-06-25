namespace Bridge.Models;

public record PosSaleResult(
    bool Success,
    string? Rrn,
    string? ErrorMessage
);
