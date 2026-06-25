using System.Net.Sockets;
using System.Text;
using Bridge.Models;

namespace Bridge.Services;

/// <summary>
/// Service for communicating with fingerprint/attendance devices
/// Supports ZKTeco, Suprema, Anviz and similar devices via TCP/IP
/// </summary>
public class FingerprintService
{
    private readonly ILogger<FingerprintService> _logger;
    private readonly IConfiguration _configuration;

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
            _logger.LogInformation($"Testing connection to device at {request.IpAddress}:{request.Port}");

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            // Wait with timeout
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                return new DeviceConnectResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد. لطفاً IP و Port را بررسی کنید."
                );
            }

            await connectTask; // Await to check for exceptions

            if (client.Connected)
            {
                // Try to get device info based on device type
                var deviceInfo = await GetDeviceInfoAsync(client, deviceType);
                
                return new DeviceConnectResult(
                    Success: true,
                    DeviceInfo: deviceInfo.DeviceName,
                    SerialNumber: deviceInfo.SerialNumber,
                    UserCount: deviceInfo.UserCount,
                    LogCount: deviceInfo.LogCount
                );
            }

            return new DeviceConnectResult(
                Success: false,
                ErrorMessage: "اتصال به دستگاه برقرار نشد."
            );
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, $"Socket error connecting to device at {request.IpAddress}:{request.Port}");
            return new DeviceConnectResult(
                Success: false,
                ErrorMessage: $"خطای شبکه: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to fingerprint device");
            return new DeviceConnectResult(
                Success: false,
                ErrorMessage: $"خطا در اتصال: {ex.Message}"
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
            _logger.LogInformation($"Getting status from device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                return new DeviceStatusResult(
                    Success: false,
                    IsConnected: false,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new DeviceStatusResult(
                    Success: false,
                    IsConnected: false,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var info = await GetDeviceInfoAsync(client, deviceType);

            return new DeviceStatusResult(
                Success: true,
                IsConnected: true,
                DeviceName: info.DeviceName,
                SerialNumber: info.SerialNumber,
                FirmwareVersion: info.FirmwareVersion,
                UserCount: info.UserCount,
                LogCount: info.LogCount,
                AvailableUserSlots: info.AvailableUserSlots,
                AvailableLogSlots: info.AvailableLogSlots,
                DeviceTime: info.DeviceTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device status");
            return new DeviceStatusResult(
                Success: false,
                IsConnected: false,
                ErrorMessage: $"خطا: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Sync attendance logs from device
    /// </summary>
    public async Task<AttendanceSyncResult> SyncAttendanceAsync(AttendanceSyncRequest request)
    {
        try
        {
            _logger.LogInformation($"Syncing attendance from device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(10000)) != connectTask)
            {
                return new AttendanceSyncResult(
                    Success: false,
                    TotalLogs: 0,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new AttendanceSyncResult(
                    Success: false,
                    TotalLogs: 0,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var logs = await GetAttendanceLogsAsync(client, deviceType, request.LastSyncTime);

            return new AttendanceSyncResult(
                Success: true,
                TotalLogs: logs.Count,
                Logs: logs,
                SyncedAt: DateTime.Now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing attendance");
            return new AttendanceSyncResult(
                Success: false,
                TotalLogs: 0,
                ErrorMessage: $"خطا در همگام‌سازی: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Get list of users/employees registered on device
    /// </summary>
    public async Task<DeviceUsersResult> GetUsersAsync(DeviceUsersRequest request)
    {
        try
        {
            _logger.LogInformation($"Getting users from device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(10000)) != connectTask)
            {
                return new DeviceUsersResult(
                    Success: false,
                    TotalUsers: 0,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new DeviceUsersResult(
                    Success: false,
                    TotalUsers: 0,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var users = await GetDeviceUsersAsync(client, deviceType);

            return new DeviceUsersResult(
                Success: true,
                TotalUsers: users.Count,
                Users: users
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device users");
            return new DeviceUsersResult(
                Success: false,
                TotalUsers: 0,
                ErrorMessage: $"خطا: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Enroll a new user on the device (enables fingerprint enrollment mode)
    /// </summary>
    public async Task<EnrollUserResult> EnrollUserAsync(EnrollUserRequest request)
    {
        try
        {
            _logger.LogInformation($"Enrolling user {request.UserId} on device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                return new EnrollUserResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new EnrollUserResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var result = await EnrollUserOnDeviceAsync(client, deviceType, request);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling user");
            return new EnrollUserResult(
                Success: false,
                ErrorMessage: $"خطا: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Delete a user from the device
    /// </summary>
    public async Task<DeleteUserResult> DeleteUserAsync(DeleteUserRequest request)
    {
        try
        {
            _logger.LogInformation($"Deleting user {request.UserId} from device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                return new DeleteUserResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new DeleteUserResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var result = await DeleteUserFromDeviceAsync(client, deviceType, request.UserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return new DeleteUserResult(
                Success: false,
                ErrorMessage: $"خطا: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Clear all attendance logs from device
    /// </summary>
    public async Task<ClearLogsResult> ClearLogsAsync(ClearLogsRequest request)
    {
        try
        {
            _logger.LogInformation($"Clearing logs from device at {request.IpAddress}:{request.Port}");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(request.IpAddress, request.Port);
            
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                return new ClearLogsResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه با timeout مواجه شد."
                );
            }

            await connectTask;

            if (!client.Connected)
            {
                return new ClearLogsResult(
                    Success: false,
                    ErrorMessage: "اتصال به دستگاه برقرار نشد."
                );
            }

            var deviceType = request.DeviceType ?? _configuration["Fingerprint:DeviceType"] ?? "ZKTeco";
            var result = await ClearDeviceLogsAsync(client, deviceType);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logs");
            return new ClearLogsResult(
                Success: false,
                ErrorMessage: $"خطا: {ex.Message}"
            );
        }
    }

    #region Private Implementation Methods

    /// <summary>
    /// Get device information - implement based on your device protocol
    /// </summary>
    private async Task<DeviceStatusResult> GetDeviceInfoAsync(TcpClient client, string deviceType)
    {
        // TODO: Implement actual device communication based on device type
        // This is a placeholder that simulates device response
        // 
        // For ZKTeco devices, you would use the ZK Protocol:
        // - Send command to get device info
        // - Parse response for serial number, firmware, user count, etc.
        //
        // For other devices, implement according to their SDK/protocol

        _logger.LogInformation($"Getting device info (Device Type: {deviceType})");

        // Simulate getting device info
        await Task.Delay(100);

        return new DeviceStatusResult(
            Success: true,
            IsConnected: true,
            DeviceName: $"{deviceType} Attendance Device",
            SerialNumber: "DEVICE_SERIAL_001",
            FirmwareVersion: "Ver 6.60",
            UserCount: 150,
            LogCount: 5000,
            AvailableUserSlots: 850,
            AvailableLogSlots: 195000,
            DeviceTime: DateTime.Now
        );
    }

    /// <summary>
    /// Get attendance logs from device
    /// </summary>
    private async Task<List<AttendanceLogEntry>> GetAttendanceLogsAsync(
        TcpClient client, 
        string deviceType, 
        DateTime? lastSyncTime)
    {
        // TODO: Implement actual attendance log retrieval
        // 
        // For ZKTeco devices:
        // 1. Send CMD_ATTLOG_RRQ command to request attendance logs
        // 2. Receive and parse binary data
        // 3. Each record contains: user ID, timestamp, status, verify mode
        //
        // Example ZKTeco packet structure:
        // - User ID: 9 bytes
        // - Timestamp: 4 bytes (seconds since 2000-01-01)
        // - Status: 1 byte (0=In, 1=Out)
        // - Verify: 1 byte (1=FP, 2=Card, 3=Password)

        _logger.LogInformation($"Fetching attendance logs (since: {lastSyncTime})");

        // Simulate log retrieval
        await Task.Delay(500);

        var logs = new List<AttendanceLogEntry>();
        
        // Simulate some attendance logs
        var random = new Random();
        var now = DateTime.Now;
        
        for (int i = 0; i < 10; i++)
        {
            logs.Add(new AttendanceLogEntry(
                UserId: $"EMP{random.Next(100, 999)}",
                LogTime: now.AddHours(-random.Next(1, 24)),
                LogType: random.Next(0, 2), // 0=In, 1=Out
                VerifyMode: 1 // Fingerprint
            ));
        }

        return logs;
    }

    /// <summary>
    /// Get users from device
    /// </summary>
    private async Task<List<DeviceUser>> GetDeviceUsersAsync(TcpClient client, string deviceType)
    {
        // TODO: Implement actual user retrieval from device

        _logger.LogInformation("Fetching users from device");

        await Task.Delay(300);

        var users = new List<DeviceUser>();
        
        // Simulate users
        var random = new Random();
        for (int i = 1; i <= 5; i++)
        {
            users.Add(new DeviceUser(
                UserId: $"EMP{i:D3}",
                Name: $"Employee {i}",
                Privilege: 0,
                HasFingerprint: random.Next(0, 2) == 1,
                HasCard: random.Next(0, 2) == 1,
                HasPassword: false
            ));
        }

        return users;
    }

    /// <summary>
    /// Enroll user on device
    /// </summary>
    private async Task<EnrollUserResult> EnrollUserOnDeviceAsync(
        TcpClient client, 
        string deviceType, 
        EnrollUserRequest request)
    {
        // TODO: Implement actual user enrollment
        // 
        // For ZKTeco:
        // 1. Send CMD_USER_WRQ with user info
        // 2. Optionally enable enroll mode for fingerprint capture
        // 3. Receive confirmation

        _logger.LogInformation($"Enrolling user {request.UserId}");

        await Task.Delay(200);

        return new EnrollUserResult(
            Success: true,
            Message: $"کاربر {request.UserId} با موفقیت در دستگاه ثبت شد. اکنون اثر انگشت خود را روی دستگاه ثبت کنید."
        );
    }

    /// <summary>
    /// Delete user from device
    /// </summary>
    private async Task<DeleteUserResult> DeleteUserFromDeviceAsync(
        TcpClient client, 
        string deviceType, 
        string userId)
    {
        // TODO: Implement actual user deletion

        _logger.LogInformation($"Deleting user {userId}");

        await Task.Delay(100);

        return new DeleteUserResult(
            Success: true,
            Message: $"کاربر {userId} با موفقیت از دستگاه حذف شد."
        );
    }

    /// <summary>
    /// Clear logs from device
    /// </summary>
    private async Task<ClearLogsResult> ClearDeviceLogsAsync(TcpClient client, string deviceType)
    {
        // TODO: Implement actual log clearing

        _logger.LogInformation("Clearing device logs");

        await Task.Delay(100);

        return new ClearLogsResult(
            Success: true,
            Message: "لاگ‌های حضور و غیاب با موفقیت از دستگاه پاک شد."
        );
    }

    #endregion
}
