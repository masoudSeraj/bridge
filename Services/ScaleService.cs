using System.IO.Ports;
using System.Globalization;

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

            _logger.LogInformation($"Reading weight from scale on {portName}");

            using var serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            serial.ReadTimeout = 5000; // 5 second timeout
            serial.Open();

            // Many scales send weight as a line of text (CRLF terminated)
            string line = serial.ReadLine();
            _logger.LogInformation($"Scale raw data: {line}");

            // Parse weight from scale output
            // Format depends on scale model - adjust parsing logic as needed
            if (decimal.TryParse(line.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight))
            {
                _logger.LogInformation($"Weight read successfully: {weight}");
                return new Models.ScaleReadResult(true, weight, null);
            }
            else
            {
                // Try to extract weight from formatted string (e.g., "1.234 kg" or "1234 g")
                var cleaned = line.Trim()
                    .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("g", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" ", "")
                    .Trim();

                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out weight))
                {
                    _logger.LogInformation($"Weight parsed from formatted string: {weight}");
                    return new Models.ScaleReadResult(true, weight, null);
                }

                _logger.LogWarning($"Cannot parse weight from scale data: {line}");
                return new Models.ScaleReadResult(false, 0, "Cannot parse weight from scale output.");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Scale port access denied");
            return new Models.ScaleReadResult(false, 0, "Scale port access denied. Check if another application is using the port.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid scale port configuration");
            return new Models.ScaleReadResult(false, 0, $"Invalid scale port configuration: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Scale read timeout");
            return new Models.ScaleReadResult(false, 0, "Scale read timeout. Check device connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from scale");
            return new Models.ScaleReadResult(false, 0, $"Error reading from scale: {ex.Message}");
        }
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
