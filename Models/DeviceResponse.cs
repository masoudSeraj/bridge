namespace Bridge.Models;

public record ScaleReadResult(
    bool Success,
    decimal Weight,
    string? ErrorMessage,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null
);

public record ReceiptPrintRequest(
    string Text,
    string? InvoiceNumber = null,
    string? CustomerName = null,
    int? PrinterWidthMm = null
);

public record ReceiptPrintResult(
    bool Success,
    string? ErrorMessage,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null
);
