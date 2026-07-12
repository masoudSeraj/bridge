namespace Bridge.Models;

public record ScaleReadResult(
    bool Success,
    decimal Weight,
    string? ErrorMessage,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null,
    string? Unit = null
);

public record ReceiptPrintRequest(
    string Text,
    string? InvoiceNumber = null,
    string? CustomerName = null,
    int? PrinterWidthMm = null,
    string? PrinterName = null
);

public record ReceiptPrintResult(
    bool Success,
    string? ErrorMessage,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null
);

public record PrinterListResult(
    bool Success,
    string[] Printers,
    string? DefaultPrinter,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Real,
    bool Ready = true,
    string Code = BridgeCodes.Ready,
    string? ErrorMessageFa = null
);
