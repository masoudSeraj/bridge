namespace Bridge.Models;

/// <summary>
/// Fingerprint device configuration
/// </summary>
public record FingerprintDeviceConfig(
    string IpAddress,
    int Port,
    string? DeviceType = "ZKTeco", // Metadata only until a real vendor protocol is implemented.
    int TimeoutSeconds = 30
);

/// <summary>
/// Request to connect/test a fingerprint device
/// </summary>
public record DeviceConnectRequest(
    string IpAddress,
    int Port,
    string? DeviceType = null
);

/// <summary>
/// Response from device connection test
/// </summary>
public record DeviceConnectResult(
    bool Success,
    string? DeviceInfo = null,
    string? SerialNumber = null,
    int? UserCount = null,
    int? LogCount = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null,
    bool? TcpReachable = null
);

/// <summary>
/// Request to sync attendance logs from device
/// </summary>
public record AttendanceSyncRequest(
    string IpAddress,
    int Port,
    DateTime? LastSyncTime = null,
    string? DeviceType = null
);

/// <summary>
/// Single attendance log from device
/// </summary>
public record AttendanceLogEntry(
    string UserId,         // Employee ID registered in device
    DateTime LogTime,      // Timestamp of attendance
    int LogType,           // 0=In, 1=Out, 2=Break, etc.
    int? VerifyMode = null // 1=Fingerprint, 2=Card, 3=Password
);

/// <summary>
/// Response from attendance sync
/// </summary>
public record AttendanceSyncResult(
    bool Success,
    int TotalLogs,
    List<AttendanceLogEntry>? Logs = null,
    DateTime? SyncedAt = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null
);

/// <summary>
/// Request to get users/employees from device
/// </summary>
public record DeviceUsersRequest(
    string IpAddress,
    int Port,
    string? DeviceType = null
);

/// <summary>
/// User/Employee info from device
/// </summary>
public record DeviceUser(
    string UserId,         // Employee ID in device
    string? Name = null,   // Employee name if stored
    int? Privilege = null, // User privilege level
    bool HasFingerprint = false,
    bool HasCard = false,
    bool HasPassword = false
);

/// <summary>
/// Response from get users
/// </summary>
public record DeviceUsersResult(
    bool Success,
    int TotalUsers,
    List<DeviceUser>? Users = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null
);

/// <summary>
/// Request to add/enroll a user to device
/// </summary>
public record EnrollUserRequest(
    string IpAddress,
    int Port,
    string UserId,         // Employee ID to enroll
    string? UserName = null,
    int? Privilege = null, // 0=User, 2=Admin, 14=SuperAdmin
    string? DeviceType = null
);

/// <summary>
/// Response from user enrollment
/// </summary>
public record EnrollUserResult(
    bool Success,
    string? Message = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null
);

/// <summary>
/// Request to delete a user from device
/// </summary>
public record DeleteUserRequest(
    string IpAddress,
    int Port,
    string UserId,
    string? DeviceType = null
);

/// <summary>
/// Response from user deletion
/// </summary>
public record DeleteUserResult(
    bool Success,
    string? Message = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null
);

/// <summary>
/// Request to clear all attendance logs from device
/// </summary>
public record ClearLogsRequest(
    string IpAddress,
    int Port,
    string? DeviceType = null
);

/// <summary>
/// Response from clearing logs
/// </summary>
public record ClearLogsResult(
    bool Success,
    string? Message = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null
);

/// <summary>
/// Device status/health info
/// </summary>
public record DeviceStatusResult(
    bool Success,
    bool IsConnected,
    string? DeviceName = null,
    string? SerialNumber = null,
    string? FirmwareVersion = null,
    int? UserCount = null,
    int? LogCount = null,
    int? AvailableUserSlots = null,
    int? AvailableLogSlots = null,
    DateTime? DeviceTime = null,
    string? ErrorMessage = null,
    string Mode = BridgeModes.Unsupported,
    bool Ready = false,
    string Code = BridgeCodes.Unsupported,
    string? ErrorMessageFa = null,
    bool? TcpReachable = null
);
