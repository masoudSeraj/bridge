using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using Bridge.Models;

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
        int? printerWidthMm = null,
        string? requestedPrinterName = null)
    {
        try
        {
            var printerName = string.IsNullOrWhiteSpace(requestedPrinterName)
                ? _configuration["Printer:Name"]
                : requestedPrinterName.Trim();
            var widthMm = printerWidthMm == 58 ? 58 : 80;
            var paperWidthPixels = widthMm == 58 ? 219 : 283; // ~58mm / ~80mm at 96 DPI

            _logger.LogInformation(
                "Printing receipt. WidthMm: {WidthMm}; Printer: {PrinterName}",
                widthMm,
                string.IsNullOrWhiteSpace(printerName) ? "(default)" : printerName);

            using PrintDocument pd = new PrintDocument();

            if (!string.IsNullOrEmpty(printerName))
            {
                pd.PrinterSettings.PrinterName = printerName;
                if (!pd.PrinterSettings.IsValid)
                {
                    return new Models.ReceiptPrintResult(
                        false,
                        $"Printer not found: {printerName}",
                        BridgeModes.Misconfigured,
                        false,
                        "printer_not_found",
                        "چاپگر انتخاب‌شده روی این کامپیوتر پیدا نشد."
                    );
                }
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

                if (args.Graphics is null)
                {
                    throw new InvalidOperationException("Printer graphics context is unavailable.");
                }

                args.Graphics.DrawString(receiptText, font, brush, rect, format);
            };

            pd.Print();

            _logger.LogInformation("Receipt printed successfully ({WidthMm}mm)", widthMm);
            return new Models.ReceiptPrintResult(true, null, BridgeModes.Real, true, BridgeCodes.Ready);
        }
        catch (InvalidPrinterException ex)
        {
            _logger.LogError(ex, "Invalid printer configuration");
            return new Models.ReceiptPrintResult(
                false,
                $"Printer not found: {ex.Message}",
                BridgeModes.Misconfigured,
                false,
                "printer_not_found",
                "چاپگر تنظیم‌شده پیدا نشد."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing receipt");
            return new Models.ReceiptPrintResult(
                false,
                $"Print error: {ex.Message}",
                BridgeModes.Misconfigured,
                false,
                "printer_error",
                "خطا در چاپ رسید."
            );
        }
    }

    public Models.PrinterListResult GetInstalledPrinters()
    {
        try
        {
            var printers = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var defaultPrinter = new PrinterSettings().PrinterName;

            return new Models.PrinterListResult(
                true,
                printers,
                string.IsNullOrWhiteSpace(defaultPrinter) ? null : defaultPrinter,
                null,
                BridgeModes.Real,
                printers.Length > 0,
                printers.Length > 0 ? BridgeCodes.Ready : "printer_not_found",
                printers.Length > 0 ? null : "هیچ چاپگری روی این کامپیوتر نصب نشده است."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate installed printers");
            return new Models.PrinterListResult(
                false,
                Array.Empty<string>(),
                null,
                $"Printer enumeration failed: {ex.Message}",
                BridgeModes.Misconfigured,
                false,
                "printer_list_error",
                "دریافت فهرست چاپگرهای نصب‌شده ناموفق بود."
            );
        }
    }

    public DeviceReadiness GetReadiness()
    {
        var printerName = _configuration["Printer:Name"];
        var settings = new PrinterSettings();

        if (!string.IsNullOrWhiteSpace(printerName))
        {
            settings.PrinterName = printerName;
        }

        var isValid = settings.IsValid;
        var installedCount = PrinterSettings.InstalledPrinters.Count;

        return new DeviceReadiness
        {
            Capability = "print",
            Ready = isValid && installedCount > 0,
            Mode = isValid && installedCount > 0 ? BridgeModes.Real : BridgeModes.Misconfigured,
            Code = isValid && installedCount > 0 ? BridgeCodes.Ready : "printer_not_found",
            Message = isValid && installedCount > 0
                ? "Configured printer is available."
                : "No usable printer is configured.",
            MessageFa = isValid && installedCount > 0
                ? "چاپگر تنظیم‌شده در دسترس است."
                : "چاپگر قابل استفاده‌ای تنظیم نشده است.",
            Metadata = new Dictionary<string, object?>
            {
                ["configuredPrinter"] = string.IsNullOrWhiteSpace(printerName) ? null : printerName,
                ["installedPrinterCount"] = installedCount,
            },
        };
    }

    private static string FormatReceiptText(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        return normalized.TrimEnd() + Environment.NewLine + Environment.NewLine;
    }
}
