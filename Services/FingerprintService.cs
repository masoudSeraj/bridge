using System.Net.Sockets;
using System.Text;
using Bridge.Models;

namespace Bridge.Services;

/// <summary>
/// Service for attendance-device readiness checks.
/// Real vendor protocols are intentionally unsupported until an SDK/protocol is implemented.
/// </summary>
public class FingerprintService
{
    private readonly ILogger<FingerprintService> _logger;
    private readonly IConfiguration _configuration;
    private const string ProtocolUnsupportedMessage = "Real fingerprint/attendance device protocol is not implemented yet.";
    private const string ProtocolUnsupportedMessageFa = "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است.";

    public FingerprintService(ILogger<FingerprintService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Test connection to a fingerprint device
    /// </summary>
    public async Task<DeviceConnectResult> ConnectAsync(DeviceConnectRequest request)
    {
        try
        {
            _logger.LogInformation("Testing TCP reachability for attendance device.");
            var reachable = await IsTcpReachableAsync(request.IpAddress, request.Port, TimeSpan.FromSeconds(5));

            return reachable
                ? UnsupportedConnectResult(true)
                : new DeviceConnectResult(
                    Success: false,
                    ErrorMessage: "Device TCP endpoint is not reachable.",
                    Mode: BridgeModes.Misconfigured,
                    Ready: false,
                    Code: "fingerprint_tcp_unreachable",
                    ErrorMessageFa: "اتصال TCP به دستگاه برقرار نشد. IP و پورت را بررسی کنید.",
                    TcpReachable: false
                );
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Socket error while testing attendance device reachability.");
            return new DeviceConnectResult(
                Success: false,
                ErrorMessage: $"Network error: {ex.Message}",
                Mode: BridgeModes.Misconfigured,
                Ready: false,
                Code: "fingerprint_network_error",
                ErrorMessageFa: "خطای شبکه هنگام اتصال به دستگاه.",
                TcpReachable: false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing attendance device reachability.");
            return new DeviceConnectResult(
                Success: false,
                ErrorMessage: $"Connection error: {ex.Message}",
                Mode: BridgeModes.Misconfigured,
                Ready: false,
                Code: "fingerprint_connection_error",
                ErrorMessageFa: "خطا در تست اتصال دستگاه.",
                TcpReachable: false
            );
        }
    }

    /// <summary>
    /// Get device status and information
    /// </summary>
    public async Task<DeviceStatusResult> GetStatusAsync(DeviceConnectRequest request)
    {
        try
        {
            _logger.LogInformation("Testing TCP reachability for attendance device status.");
            var reachable = await IsTcpReachableAsync(request.IpAddress, request.Port, TimeSpan.FromSeconds(5));

            return reachable
                ? UnsupportedStatusResult(true)
                : new DeviceStatusResult(
                    Success: false,
                    IsConnected: false,
                    ErrorMessage: "Device TCP endpoint is not reachable.",
                    Mode: BridgeModes.Misconfigured,
                    Ready: false,
                    Code: "fingerprint_tcp_unreachable",
                    ErrorMessageFa: "اتصال TCP به دستگاه برقرار نشد. IP و پورت را بررسی کنید.",
                    TcpReachable: false
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing attendance device status.");
            return new DeviceStatusResult(
                Success: false,
                IsConnected: false,
                ErrorMessage: $"Status error: {ex.Message}",
                Mode: BridgeModes.Misconfigured,
                Ready: false,
                Code: "fingerprint_status_error",
                ErrorMessageFa: "خطا در بررسی وضعیت دستگاه.",
                TcpReachable: false
            );
        }
    }

    /// <summary>
    /// Sync attendance logs from device
    /// </summary>
    public async Task<AttendanceSyncResult> SyncAttendanceAsync(AttendanceSyncRequest request)
    {
        _logger.LogInformation("Attendance sync requested, but real device protocol is not implemented.");
        await Task.CompletedTask;
        return UnsupportedSyncResult();
    }

    /// <summary>
    /// Get list of users/employees registered on device
    /// </summary>
    public async Task<DeviceUsersResult> GetUsersAsync(DeviceUsersRequest request)
    {
        _logger.LogInformation("Attendance device users requested, but real device protocol is not implemented.");
        await Task.CompletedTask;
        return new DeviceUsersResult(
            Success: false,
            TotalUsers: 0,
            Users: Array.Empty<DeviceUser>().ToList(),
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: BridgeCodes.Unsupported,
            ErrorMessageFa: ProtocolUnsupportedMessageFa
        );
    }

    /// <summary>
    /// Enroll a new user on the device (enables fingerprint enrollment mode)
    /// </summary>
    public async Task<EnrollUserResult> EnrollUserAsync(EnrollUserRequest request)
    {
        _logger.LogInformation("Attendance device enrollment requested, but real device protocol is not implemented.");
        await Task.CompletedTask;
        return new EnrollUserResult(
            Success: false,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: BridgeCodes.Unsupported,
            ErrorMessageFa: ProtocolUnsupportedMessageFa
        );
    }

    /// <summary>
    /// Delete a user from the device
    /// </summary>
    public async Task<DeleteUserResult> DeleteUserAsync(DeleteUserRequest request)
    {
        _logger.LogInformation("Attendance device user deletion requested, but real device protocol is not implemented.");
        await Task.CompletedTask;
        return new DeleteUserResult(
            Success: false,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: BridgeCodes.Unsupported,
            ErrorMessageFa: ProtocolUnsupportedMessageFa
        );
    }

    /// <summary>
    /// Clear all attendance logs from device
    /// </summary>
    public async Task<ClearLogsResult> ClearLogsAsync(ClearLogsRequest request)
    {
        _logger.LogInformation("Attendance device log clearing requested, but real device protocol is not implemented.");
        await Task.CompletedTask;
        return new ClearLogsResult(
            Success: false,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: BridgeCodes.Unsupported,
            ErrorMessageFa: ProtocolUnsupportedMessageFa
        );
    }

    public DeviceReadiness GetReadiness()
    {
        return new DeviceReadiness
        {
            Capability = "fingerprint",
            Ready = false,
            Mode = BridgeModes.Unsupported,
            Code = BridgeCodes.Unsupported,
            Message = ProtocolUnsupportedMessage,
            MessageFa = ProtocolUnsupportedMessageFa,
        };
    }

    private static DeviceConnectResult UnsupportedConnectResult(bool tcpReachable)
    {
        return new DeviceConnectResult(
            Success: false,
            DeviceInfo: null,
            SerialNumber: null,
            UserCount: null,
            LogCount: null,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: "fingerprint_protocol_unsupported",
            ErrorMessageFa: ProtocolUnsupportedMessageFa,
            TcpReachable: tcpReachable
        );
    }

    private static DeviceStatusResult UnsupportedStatusResult(bool tcpReachable)
    {
        return new DeviceStatusResult(
            Success: false,
            IsConnected: false,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: "fingerprint_protocol_unsupported",
            ErrorMessageFa: ProtocolUnsupportedMessageFa,
            TcpReachable: tcpReachable
        );
    }

    private static AttendanceSyncResult UnsupportedSyncResult()
    {
        return new AttendanceSyncResult(
            Success: false,
            TotalLogs: 0,
            Logs: Array.Empty<AttendanceLogEntry>().ToList(),
            SyncedAt: null,
            ErrorMessage: ProtocolUnsupportedMessage,
            Mode: BridgeModes.Unsupported,
            Ready: false,
            Code: BridgeCodes.Unsupported,
            ErrorMessageFa: ProtocolUnsupportedMessageFa
        );
    }

    private static async Task<bool> IsTcpReachableAsync(string ipAddress, int port, TimeSpan timeout)
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(ipAddress, port);
        if (await Task.WhenAny(connectTask, Task.Delay(timeout)) != connectTask)
        {
            return false;
        }

        await connectTask;
        return client.Connected;
    }

}
