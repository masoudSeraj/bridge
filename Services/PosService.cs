using System.Net.Sockets;
using System.Text;
using Bridge.Models;

namespace Bridge.Services;

public class PosService
{
    private readonly ILogger<PosService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PosService(
        ILogger<PosService> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<Models.PosSaleResult> ProcessSale(decimal amount, string invoiceId)
    {
        try
        {
            _logger.LogInformation("POS payment request received.");

            var mode = (_configuration["Pos:Mode"] ?? Bridge.Models.BridgeModes.Unsupported).Trim().ToLowerInvariant();
            if (!string.Equals(mode, Bridge.Models.BridgeModes.Mock, StringComparison.OrdinalIgnoreCase))
            {
                return Unsupported();
            }

            if (!_environment.IsDevelopment())
            {
                return new Models.PosSaleResult(
                    Success: false,
                    Rrn: null,
                    ErrorMessage: "POS mock mode is only available in Development.",
                    Mode: Bridge.Models.BridgeModes.Disabled,
                    Ready: false,
                    Code: Bridge.Models.BridgeCodes.MockDisabled,
                    ErrorMessageFa: "حالت شبیه‌سازی کارتخوان فقط در محیط توسعه فعال است."
                );
            }

            await Task.Delay(2000);

            var rrn = GenerateRrn();
            _logger.LogInformation("Mock POS payment completed.");

            return new Models.PosSaleResult(
                Success: true,
                Rrn: rrn,
                ErrorMessage: null,
                Mode: Bridge.Models.BridgeModes.Mock,
                Ready: true,
                Code: "pos_mock_success"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing POS payment");
            return new Models.PosSaleResult(
                Success: false,
                Rrn: null,
                ErrorMessage: $"POS communication error: {ex.Message}",
                Mode: Bridge.Models.BridgeModes.Misconfigured,
                Ready: false,
                Code: "pos_error",
                ErrorMessageFa: "خطا در ارتباط با کارتخوان."
            );
        }
    }

    public DeviceReadiness GetReadiness()
    {
        var mode = (_configuration["Pos:Mode"] ?? Bridge.Models.BridgeModes.Unsupported).Trim().ToLowerInvariant();
        if (string.Equals(mode, Bridge.Models.BridgeModes.Mock, StringComparison.OrdinalIgnoreCase))
        {
            var mockReady = _environment.IsDevelopment();
            return new DeviceReadiness
            {
                Capability = "pos",
                Ready = mockReady,
                Mode = mockReady ? Bridge.Models.BridgeModes.Mock : Bridge.Models.BridgeModes.Disabled,
                Code = mockReady ? "pos_mock_ready" : Bridge.Models.BridgeCodes.MockDisabled,
                Message = mockReady
                    ? "POS is running in explicit development mock mode."
                    : "POS mock mode is disabled outside Development.",
                MessageFa = mockReady
                    ? "کارتخوان در حالت شبیه‌سازی توسعه فعال است."
                    : "شبیه‌سازی کارتخوان خارج از محیط توسعه غیرفعال است.",
            };
        }

        return new DeviceReadiness
        {
            Capability = "pos",
            Ready = false,
            Mode = Bridge.Models.BridgeModes.Unsupported,
            Code = Bridge.Models.BridgeCodes.Unsupported,
            Message = "Real POS integration is not implemented yet.",
            MessageFa = "اتصال واقعی کارتخوان هنوز پیاده‌سازی نشده است.",
        };
    }

    private static Models.PosSaleResult Unsupported()
    {
        return new Models.PosSaleResult(
            Success: false,
            Rrn: null,
            ErrorMessage: "Real POS integration is not implemented yet.",
            Mode: Bridge.Models.BridgeModes.Unsupported,
            Ready: false,
            Code: Bridge.Models.BridgeCodes.Unsupported,
            ErrorMessageFa: "اتصال واقعی کارتخوان هنوز پیاده‌سازی نشده است."
        );
    }

    private static string GenerateRrn()
    {
        return Random.Shared
            .NextInt64(100_000_000_000L, 1_000_000_000_000L)
            .ToString();
    }

    // Example TCP/IP implementation (commented out - implement based on your POS device)
    /*
    private async Task<Models.PosSaleResult> ProcessSaleViaTcp(decimal amount, string invoiceId)
    {
        var posIp = _configuration["Pos:IpAddress"] ?? "192.168.1.100";
        var posPort = int.Parse(_configuration["Pos:Port"] ?? "8080");

        using var client = new TcpClient();
        await client.ConnectAsync(posIp, posPort);

        using var stream = client.GetStream();
        var request = Encoding.UTF8.GetBytes($"SALE|{amount}|{invoiceId}\n");
        await stream.WriteAsync(request, 0, request.Length);

        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Parse response and return result
        // Implementation depends on POS device protocol
        return new Models.PosSaleResult(true, response, null);
    }
    */
}
