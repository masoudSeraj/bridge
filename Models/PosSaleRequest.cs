namespace Bridge.Models;

public record PosSaleRequest(
    decimal Amount,
    string InvoiceId
);
