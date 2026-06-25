namespace Bridge.Models;

public record ScaleReadResult(
    bool Success,
    decimal Weight,
    string? ErrorMessage
);

public record ReceiptPrintRequest(
    string Text,
    string? InvoiceNumber = null,
    string? CustomerName = null,
    int? PrinterWidthMm = null
);

public record ReceiptPrintResult(
    bool Success,
    string? ErrorMessage
);
