using System.IO.Ports;
using System.Globalization;
using System.Text.RegularExpressions;
using Bridge.Models;

namespace Bridge.Services;

public class ScaleService
{
    private readonly ILogger<ScaleService> _logger;
    private readonly IConfiguration _configuration;

    public ScaleService(ILogger<ScaleService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Models.ScaleReadResult ReadWeight()
    {
        try
        {
            // Get scale configuration from appsettings.json
            var portName = _configuration["Scale:PortName"] ?? "COM3";
            var baudRate = int.Parse(_configuration["Scale:BaudRate"] ?? "9600");
            var parity = ParseParity(_configuration["Scale:Parity"] ?? "None");
            var dataBits = int.Parse(_configuration["Scale:DataBits"] ?? "8");
            var stopBits = ParseStopBits(_configuration["Scale:StopBits"] ?? "One");

            _logger.LogInformation("Reading weight from configured scale serial port.");

            using var serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            serial.ReadTimeout = 5000; // 5 second timeout
            serial.Open();

            // Many scales send weight as a line of text (CRLF terminated)
            string line = serial.ReadLine();
            _logger.LogDebug("Scale returned a line of data.");

            var configuredOutputUnit = _configuration["Scale:OutputUnit"];
            if (TryNormalizeKilograms(
                line,
                configuredOutputUnit,
                out var weightKg,
                out var errorCode,
                out var errorMessage,
                out var errorMessageFa))
            {
                _logger.LogInformation("Weight read and normalized to kilograms.");
                return new Models.ScaleReadResult(
                    true,
                    decimal.Round(weightKg, 3, MidpointRounding.AwayFromZero),
                    null,
                    BridgeModes.Real,
                    true,
                    BridgeCodes.Ready,
                    null,
                    "kg");
            }

            _logger.LogWarning("Cannot normalize scale output. Code: {Code}", errorCode);
            return new Models.ScaleReadResult(
                false,
                0,
                errorMessage,
                BridgeModes.Misconfigured,
                false,
                errorCode,
                errorMessageFa
            );
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Configured scale serial port was not found.");
            return new Models.ScaleReadResult(
                false,
                0,
                "Configured scale serial port was not found.",
                BridgeModes.Misconfigured,
                false,
                "scale_port_not_found",
                "پورت سریال تنظیم‌شده برای ترازو پیدا نشد."
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Scale port access denied.");
            return new Models.ScaleReadResult(
                false,
                0,
                "Scale port access denied. Check if another application is using the port.",
                BridgeModes.Misconfigured,
                false,
                "scale_port_access_denied",
                "دسترسی به پورت ترازو رد شد. ممکن است برنامه دیگری از آن استفاده کند."
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid scale port configuration.");
            return new Models.ScaleReadResult(
                false,
                0,
                $"Invalid scale port configuration: {ex.Message}",
                BridgeModes.Misconfigured,
                false,
                "scale_invalid_config",
                "تنظیمات پورت ترازو معتبر نیست."
            );
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Scale read timeout.");
            return new Models.ScaleReadResult(
                false,
                0,
                "Scale read timeout. Check device connection.",
                BridgeModes.Misconfigured,
                false,
                "scale_timeout",
                "خواندن از ترازو زمان‌بر شد. اتصال دستگاه را بررسی کنید."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from scale");
            return new Models.ScaleReadResult(
                false,
                0,
                $"Error reading from scale: {ex.Message}",
                BridgeModes.Misconfigured,
                false,
                "scale_error",
                "خطا در خواندن از ترازو."
            );
        }
    }

    private static bool TryNormalizeKilograms(
        string raw,
        string? configuredBareUnit,
        out decimal weightKg,
        out string errorCode,
        out string errorMessage,
        out string errorMessageFa)
    {
        weightKg = 0;
        errorCode = "scale_parse_error";
        errorMessage = "Cannot parse weight from scale output.";
        errorMessageFa = "خروجی ترازو قابل خواندن نیست. تنظیمات مدل ترازو را بررسی کنید.";

        var match = Regex.Match(
            raw.Trim(),
            @"^\s*[-+]?\d+(?:[\.,]\d+)?\s*(?<unit>[A-Za-z\u0600-\u06FF]+)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return false;
        }

        var numericText = match.Value;
        var capturedUnit = match.Groups["unit"].Value;
        if (!string.IsNullOrWhiteSpace(capturedUnit))
        {
            numericText = numericText[..numericText.LastIndexOf(capturedUnit, StringComparison.OrdinalIgnoreCase)];
        }

        numericText = numericText.Trim().Replace(',', '.');
        if (!decimal.TryParse(numericText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var weight))
        {
            return false;
        }

        if (weight <= 0)
        {
            errorCode = "scale_non_positive_weight";
            errorMessage = "Scale returned a zero or negative weight.";
            errorMessageFa = "وزن خوانده‌شده باید بیشتر از صفر باشد.";
            return false;
        }

        var unit = string.IsNullOrWhiteSpace(capturedUnit)
            ? configuredBareUnit?.Trim().ToLowerInvariant()
            : capturedUnit.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(unit))
        {
            errorCode = "scale_output_unit_missing";
            errorMessage = "Scale output has no unit and Scale:OutputUnit is not configured.";
            errorMessageFa = "واحد خروجی ترازو مشخص نیست. واحد پیش‌فرض ترازو را در تنظیمات Bridge تعیین کنید.";
            return false;
        }

        if (unit is "kg" or "kilogram" or "kilograms" or "کیلو" or "کیلوگرم")
        {
            weightKg = weight;
            return true;
        }

        if (unit is "g" or "gram" or "grams" or "گرم")
        {
            weightKg = weight / 1000m;
            return true;
        }

        errorCode = "scale_unit_unsupported";
        errorMessage = $"Unsupported scale output unit: {unit}";
        errorMessageFa = "واحد خروجی ترازو پشتیبانی نمی‌شود؛ فقط گرم یا کیلوگرم مجاز است.";
        return false;
    }

    public DeviceReadiness GetReadiness()
    {
        var portName = _configuration["Scale:PortName"];
        if (string.IsNullOrWhiteSpace(portName))
        {
            return new DeviceReadiness
            {
                Capability = "scale",
                Ready = false,
                Mode = BridgeModes.Misconfigured,
                Code = "scale_port_missing",
                Message = "Scale serial port is not configured.",
                MessageFa = "پورت سریال ترازو تنظیم نشده است.",
            };
        }

        var exists = SerialPort.GetPortNames()
            .Any(port => string.Equals(port, portName, StringComparison.OrdinalIgnoreCase));

        return new DeviceReadiness
        {
            Capability = "scale",
            Ready = exists,
            Mode = exists ? BridgeModes.Real : BridgeModes.Misconfigured,
            Code = exists ? BridgeCodes.Ready : "scale_port_not_found",
            Message = exists
                ? "Configured scale serial port is available."
                : "Configured scale serial port was not found.",
            MessageFa = exists
                ? "پورت تنظیم‌شده ترازو در دسترس است."
                : "پورت سریال تنظیم‌شده برای ترازو پیدا نشد.",
            Metadata = new Dictionary<string, object?>
            {
                ["portName"] = portName,
            },
        };
    }

    private Parity ParseParity(string parity)
    {
        return parity.ToLower() switch
        {
            "none" => Parity.None,
            "odd" => Parity.Odd,
            "even" => Parity.Even,
            "mark" => Parity.Mark,
            "space" => Parity.Space,
            _ => Parity.None
        };
    }

    private StopBits ParseStopBits(string stopBits)
    {
        return stopBits.ToLower() switch
        {
            "none" => StopBits.None,
            "one" => StopBits.One,
            "two" => StopBits.Two,
            "onepointfive" => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }
}
