using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;

namespace Bridge.Services;

public class PrinterService
{
    private readonly ILogger<PrinterService> _logger;
    private readonly IConfiguration _configuration;

    public PrinterService(ILogger<PrinterService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Models.ReceiptPrintResult PrintReceipt(
        string text,
        string? invoiceNumber = null,
        string? customerName = null,
        int? printerWidthMm = null)
    {
        try
        {
            var printerName = _configuration["Printer:Name"];
            var widthMm = printerWidthMm == 58 ? 58 : 80;
            var paperWidthPixels = widthMm == 58 ? 219 : 283; // ~58mm / ~80mm at 96 DPI

            _logger.LogInformation(
                "Printing receipt. Invoice: {InvoiceNumber}, WidthMm: {WidthMm}",
                invoiceNumber,
                widthMm);

            PrintDocument pd = new PrintDocument();

            if (!string.IsNullOrEmpty(printerName))
            {
                pd.PrinterSettings.PrinterName = printerName;
            }

            pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", paperWidthPixels, 1000);

            var fontName = _configuration["Printer:ReceiptFontName"] ?? "Tahoma";
            var fontSize = float.TryParse(
                _configuration["Printer:ReceiptFontSize"],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedSize)
                ? parsedSize
                : 9f;

            pd.PrintPage += (sender, args) =>
            {
                using var font = new Font(fontName, fontSize, FontStyle.Regular);
                using var brush = new SolidBrush(Color.Black);

                var receiptText = FormatReceiptText(text);

                var rect = new RectangleF(0, 0, args.PageBounds.Width, args.PageBounds.Height);
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft
                };

                args.Graphics.DrawString(receiptText, font, brush, rect, format);
            };

            pd.Print();

            _logger.LogInformation("Receipt printed successfully ({WidthMm}mm)", widthMm);
            return new Models.ReceiptPrintResult(true, null);
        }
        catch (InvalidPrinterException ex)
        {
            _logger.LogError(ex, "Invalid printer configuration");
            return new Models.ReceiptPrintResult(false, $"Printer not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing receipt");
            return new Models.ReceiptPrintResult(false, $"Print error: {ex.Message}");
        }
    }

    private static string FormatReceiptText(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        return normalized.TrimEnd() + Environment.NewLine + Environment.NewLine;
    }
}
