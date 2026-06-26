using System.Text.Json;

namespace Bridge.Services;

public sealed class BridgeSettingsService
{
    private readonly string _settingsPath;

    public BridgeSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "AICompanionBridge");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "bridge-settings.json");
    }

    public string SettingsPath => _settingsPath;

    public BridgeInstallResult EnsureSettings(string? deviceId, string? bridgeToken)
    {
        var persisted = ReadPersistedSettings();
        var hadPersistedSettings = !string.IsNullOrWhiteSpace(persisted.DeviceId)
            && !string.IsNullOrWhiteSpace(persisted.BridgeToken);

        if (!string.IsNullOrWhiteSpace(deviceId) && !string.IsNullOrWhiteSpace(bridgeToken))
        {
            return new BridgeInstallResult
            {
                CreatedNew = false,
                DeviceId = deviceId,
                BridgeToken = bridgeToken,
                SettingsPath = _settingsPath,
            };
        }

        var finalDeviceId = string.IsNullOrWhiteSpace(deviceId)
            ? persisted.DeviceId ?? $"device_{Guid.NewGuid():N}"
            : deviceId;
        var finalBridgeToken = string.IsNullOrWhiteSpace(bridgeToken)
            ? persisted.BridgeToken ?? Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant()
            : bridgeToken;

        var createdNew = !hadPersistedSettings
            && (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(bridgeToken));

        if (createdNew || string.IsNullOrWhiteSpace(persisted.DeviceId) || string.IsNullOrWhiteSpace(persisted.BridgeToken))
        {
            var payload = new Dictionary<string, object?>
            {
                ["Bridge"] = new Dictionary<string, object?>
                {
                    ["DeviceId"] = finalDeviceId,
                    ["BridgeToken"] = finalBridgeToken,
                },
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            File.WriteAllText(_settingsPath, json);
        }

        return new BridgeInstallResult
        {
            CreatedNew = createdNew,
            DeviceId = finalDeviceId,
            BridgeToken = finalBridgeToken,
            SettingsPath = _settingsPath,
        };
    }

    public string? ReadBridgeToken()
    {
        var persisted = ReadPersistedSettings();
        return persisted.BridgeToken;
    }

    private PersistedBridgeSettings ReadPersistedSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new PersistedBridgeSettings();
        }

        try
        {
            using var stream = File.OpenRead(_settingsPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("Bridge", out var bridgeElement))
            {
                return new PersistedBridgeSettings();
            }

            return new PersistedBridgeSettings
            {
                DeviceId = bridgeElement.TryGetProperty("DeviceId", out var deviceId)
                    ? deviceId.GetString()
                    : null,
                BridgeToken = bridgeElement.TryGetProperty("BridgeToken", out var bridgeToken)
                    ? bridgeToken.GetString()
                    : null,
            };
        }
        catch
        {
            return new PersistedBridgeSettings();
        }
    }

    private sealed class PersistedBridgeSettings
    {
        public string? DeviceId { get; init; }

        public string? BridgeToken { get; init; }
    }
}

public sealed class BridgeInstallResult
{
    public bool CreatedNew { get; init; }

    public string DeviceId { get; init; } = string.Empty;

    public string BridgeToken { get; init; } = string.Empty;

    public string SettingsPath { get; init; } = string.Empty;
}
