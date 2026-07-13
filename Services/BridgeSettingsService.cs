using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Bridge.Models;

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
            var root = ReadSettingsRoot();
            var bridge = root["Bridge"] as JsonObject ?? new JsonObject();
            bridge["DeviceId"] = finalDeviceId;
            bridge["BridgeToken"] = finalBridgeToken;
            root["Bridge"] = bridge;
            WriteSettingsRoot(root);
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

    public ScaleConfigurationResponse GetScaleConfiguration(IConfiguration configuration)
    {
        var ports = SerialPort.GetPortNames()
            .OrderBy(port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var portName = configuration["Scale:PortName"]?.Trim();
        var hasPort = !string.IsNullOrWhiteSpace(portName);
        var ready = hasPort
            && ports.Contains(portName, StringComparer.OrdinalIgnoreCase);

        return new ScaleConfigurationResponse
        {
            Success = true,
            Ports = ports,
            PortName = string.IsNullOrWhiteSpace(portName) ? null : portName,
            BaudRate = configuration.GetValue<int?>("Scale:BaudRate") ?? 9600,
            Parity = configuration["Scale:Parity"] ?? "None",
            DataBits = configuration.GetValue<int?>("Scale:DataBits") ?? 8,
            StopBits = configuration["Scale:StopBits"] ?? "One",
            OutputUnit = configuration["Scale:OutputUnit"] ?? "kg",
            Ready = ready,
            Mode = ready ? BridgeModes.Real : BridgeModes.Misconfigured,
            Code = ready ? BridgeCodes.Ready : hasPort ? "scale_port_not_found" : "scale_port_missing",
            ErrorMessage = ready
                ? null
                : hasPort
                    ? "Configured scale serial port was not found."
                    : "Scale serial port is not configured.",
            ErrorMessageFa = ready
                ? null
                : hasPort
                    ? "پورت سریال تنظیم‌شده برای ترازو پیدا نشد."
                    : "پورت سریال ترازو تنظیم نشده است.",
        };
    }

    public ScaleConfigurationResponse UpdateScaleConfiguration(ScaleConfigurationRequest request)
    {
        var ports = SerialPort.GetPortNames()
            .OrderBy(port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var portName = request.PortName?.Trim();
        var validBaudRates = new HashSet<int>
        {
            1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200,
        };
        var validParity = Enum.TryParse<Parity>(request.Parity, true, out var parity);
        var validStopBits = Enum.TryParse<StopBits>(request.StopBits, true, out var stopBits)
            && stopBits != StopBits.None;
        var outputUnit = (request.OutputUnit ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(portName))
        {
            return ScaleConfigurationError(ports, "Scale serial port is required.", "انتخاب پورت سریال ترازو الزامی است.");
        }

        if (!Regex.IsMatch(portName, "^COM[0-9]+$", RegexOptions.IgnoreCase))
        {
            return ScaleConfigurationError(ports, "Scale serial port must use a Windows COM name.", "نام پورت ترازو باید مانند COM3 باشد.");
        }

        if (!validBaudRates.Contains(request.BaudRate))
        {
            return ScaleConfigurationError(ports, "Scale baud rate is not supported.", "سرعت ارتباط انتخاب‌شده برای ترازو پشتیبانی نمی‌شود.");
        }

        if (!validParity || request.DataBits is < 5 or > 8 || !validStopBits)
        {
            return ScaleConfigurationError(ports, "Scale serial framing is invalid.", "تنظیمات ارتباط سریال ترازو نامعتبر است.");
        }

        if (outputUnit is not ("kg" or "g"))
        {
            return ScaleConfigurationError(ports, "Scale output unit must be kg or g.", "واحد خروجی ترازو باید کیلوگرم یا گرم باشد.");
        }

        try
        {
            var root = ReadSettingsRoot();
            root["Scale"] = new JsonObject
            {
                ["PortName"] = portName,
                ["BaudRate"] = request.BaudRate,
                ["Parity"] = parity.ToString(),
                ["DataBits"] = request.DataBits,
                ["StopBits"] = stopBits.ToString(),
                ["OutputUnit"] = outputUnit,
            };
            WriteSettingsRoot(root);

            var ready = ports.Contains(portName, StringComparer.OrdinalIgnoreCase);
            return new ScaleConfigurationResponse
            {
                Success = true,
                Ports = ports,
                PortName = portName,
                BaudRate = request.BaudRate,
                Parity = parity.ToString(),
                DataBits = request.DataBits,
                StopBits = stopBits.ToString(),
                OutputUnit = outputUnit,
                Ready = ready,
                Mode = ready ? BridgeModes.Real : BridgeModes.Misconfigured,
                Code = ready ? BridgeCodes.Ready : "scale_port_not_found",
                ErrorMessage = ready ? null : "Settings were saved, but the configured scale port is not currently available.",
                ErrorMessageFa = ready ? null : "تنظیمات ذخیره شد، اما پورت انتخاب‌شده در حال حاضر در دسترس نیست.",
            };
        }
        catch (Exception exception)
        {
            return ScaleConfigurationError(
                ports,
                $"Unable to save scale settings: {exception.Message}",
                "ذخیره تنظیمات ترازو ناموفق بود.");
        }
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

    private JsonObject ReadSettingsRoot()
    {
        if (!File.Exists(_settingsPath))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(File.ReadAllText(_settingsPath)) as JsonObject
            ?? new JsonObject();
    }

    private void WriteSettingsRoot(JsonObject root)
    {
        var temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static ScaleConfigurationResponse ScaleConfigurationError(
        string[] ports,
        string message,
        string messageFa)
    {
        return new ScaleConfigurationResponse
        {
            Success = false,
            Ports = ports,
            Ready = false,
            Mode = BridgeModes.Misconfigured,
            Code = BridgeCodes.Misconfigured,
            ErrorMessage = message,
            ErrorMessageFa = messageFa,
        };
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
