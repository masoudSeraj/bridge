using Bridge.Services;
using Bridge.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure to listen only on localhost for security
builder.WebHost.UseUrls("http://localhost:5005");

var bridgeSettingsService = new BridgeSettingsService();
builder.Configuration.AddJsonFile(bridgeSettingsService.SettingsPath, optional: true, reloadOnChange: true);

// Add services
builder.Services.AddLogging();
builder.Services.AddSingleton<PosService>();
builder.Services.AddSingleton<ScaleService>();
builder.Services.AddSingleton<PrinterService>();
builder.Services.AddSingleton<FingerprintService>();
builder.Services.AddSingleton(bridgeSettingsService);

// VOIP provider wiring (only tel_uri for VOIP-A)
builder.Services.AddSingleton<IVoipProvider, TelUriVoipProvider>();
builder.Services.AddSingleton<VoipService>();

// Load bridge and VOIP security config
var bridgeSection = builder.Configuration.GetSection("Bridge");
var voipSection = builder.Configuration.GetSection("Voip");
var configuredAllowedOrigins = bridgeSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var configuredDeviceId = bridgeSection.GetValue<string>("DeviceId");
var configuredBridgeToken = bridgeSection.GetValue<string>("BridgeToken");

var installResult = bridgeSettingsService.EnsureSettings(configuredDeviceId, configuredBridgeToken);
builder.Configuration.AddJsonFile(bridgeSettingsService.SettingsPath, optional: true, reloadOnChange: true);

bridgeSection = builder.Configuration.GetSection("Bridge");
voipSection = builder.Configuration.GetSection("Voip");
var allowedOrigins = bridgeSection.GetSection("AllowedOrigins").Get<string[]>() ?? configuredAllowedOrigins;
var allowAnyWebOrigin = bridgeSection.GetValue<bool?>("AllowAnyWebOrigin") ?? false;
var allowTenantSubdomains = bridgeSection.GetValue<bool?>("AllowTenantSubdomains") ?? false;
var trustedDomainSuffix = bridgeSection.GetValue<string>("TrustedDomainSuffix") ?? string.Empty;
var bridgeToken = bridgeSection.GetValue<string>("BridgeToken") ?? string.Empty;
var deviceId = bridgeSection.GetValue<string>("DeviceId") ?? string.Empty;
var bridgeVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
var configuredMaxCallsPerMinute = voipSection.GetValue<int?>("MaxCallsPerMinute") ?? 30;
var maxCallsPerMinute = configuredMaxCallsPerMinute <= 0 || configuredMaxCallsPerMinute > 300
    ? 30
    : configuredMaxCallsPerMinute;
var rateLimitWindow = TimeSpan.FromMinutes(1);
var callRateStore = new ConcurrentDictionary<string, List<DateTime>>();

var listenUrl = "http://localhost:5005";
var setupUrl = $"{listenUrl}/setup";

bool IsLoopbackRequest(HttpContext context)
{
    var remoteIp = context.Connection.RemoteIpAddress;
    if (remoteIp is null)
    {
        return true;
    }

    if (System.Net.IPAddress.IsLoopback(remoteIp))
    {
        return true;
    }

    return false;
}

BridgeHealthResponse CreateBridgeHealthResponse(DeviceReadiness[] services, bool paired)
{
    var tokenConfigured = !string.IsNullOrWhiteSpace(bridgeToken);
    var readyCapabilities = services
        .Where(service => service.Ready)
        .Select(service => service.Capability)
        .ToArray();

    return new BridgeHealthResponse
    {
        Success = true,
        Installed = true,
        Status = "running",
        Message = tokenConfigured
            ? "Bridge is installed and running successfully."
            : "Bridge is installed, but the token is not configured yet.",
        MessageFa = tokenConfigured
            ? "Bridge با موفقیت نصب شده و در حال اجرا است."
            : "Bridge نصب شده است، اما توکن هنوز پیکربندی نشده است.",
        DeviceId = deviceId,
        BridgeVersion = bridgeVersion,
        Capabilities = readyCapabilities,
        RequiresPairing = true,
        TokenConfigured = tokenConfigured,
        Paired = paired,
        ListenUrl = listenUrl,
        SettingsPath = bridgeSettingsService.SettingsPath,
        SetupUrl = setupUrl,
        Services = services,
    };
}

DeviceReadiness CreateBarcodeReadiness()
{
    return new DeviceReadiness
    {
        Capability = "barcode",
        Ready = true,
        Mode = BridgeModes.Real,
        Code = "barcode_keyboard_wedge",
        Message = "Barcode scanners are handled as keyboard input; the bridge does not detect barcode hardware.",
        MessageFa = "بارکدخوان به صورت ورودی کیبورد کار می‌کند و Bridge سخت‌افزار بارکدخوان را تشخیص نمی‌دهد.",
    };
}

bool IsOriginAllowed(string? origin)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
    {
        return false;
    }

    if (originUri.Scheme is not ("http" or "https"))
    {
        return false;
    }

    // Hardware endpoints require the workstation token. Allowing arbitrary
    // web origins here supports custom tenant domains without granting device
    // access to sites that do not possess that token.
    if (allowAnyWebOrigin)
    {
        return true;
    }

    if (allowedOrigins.Any(candidate => string.Equals(candidate, origin, StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    if (!allowTenantSubdomains || string.IsNullOrWhiteSpace(trustedDomainSuffix))
    {
        return false;
    }

    var host = originUri.Host;
    var suffix = trustedDomainSuffix.Trim();
    if (!suffix.StartsWith('.'))
    {
        suffix = $".{suffix}";
    }

    return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}

// Enable CORS for configured app origins (browser security)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin))
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Access-Control-Request-Private-Network", out var requested) &&
        string.Equals(requested.ToString(), "true", StringComparison.OrdinalIgnoreCase) &&
        IsOriginAllowed(context.Request.Headers.Origin.ToString()))
    {
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
    }

    if (HttpMethods.IsOptions(context.Request.Method))
    {
        await next();
        return;
    }

    await next();
});

// Use CORS
app.UseCors();

bool IsAuthorizedBridgeRequest(HttpRequest request)
{
    if (string.IsNullOrWhiteSpace(bridgeToken))
    {
        return false;
    }

    if (!request.Headers.TryGetValue("X-Bridge-Token", out var header)
        || string.IsNullOrWhiteSpace(header))
    {
        return false;
    }

    var expected = Encoding.UTF8.GetBytes(bridgeToken);
    var supplied = Encoding.UTF8.GetBytes(header.ToString());
    return expected.Length == supplied.Length
        && CryptographicOperations.FixedTimeEquals(expected, supplied);
}

bool RequiresHardwareAuthorization(PathString path)
{
    return path.StartsWithSegments("/api/pos")
        || path.StartsWithSegments("/api/print")
        || path.StartsWithSegments("/api/printers")
        || path.StartsWithSegments("/api/scale")
        || path.StartsWithSegments("/api/fingerprint")
        || path.StartsWithSegments("/api/pairing/verify");
}

app.Use(async (context, next) =>
{
    if (HttpMethods.IsOptions(context.Request.Method)
        || !RequiresHardwareAuthorization(context.Request.Path))
    {
        await next();
        return;
    }

    if (!IsAuthorizedBridgeRequest(context.Request))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            ready = false,
            mode = BridgeModes.Misconfigured,
            code = BridgeCodes.PairingRequired,
            errorMessage = "Bridge pairing token is missing or invalid.",
            errorMessageFa = "توکن اتصال Bridge وارد نشده یا نامعتبر است.",
        });
        return;
    }

    await next();
});

app.MapGet("/api/pairing/verify", () => Results.Ok(new
{
    success = true,
    paired = true,
    deviceId,
    bridgeVersion,
}));

// POS payment endpoint
app.MapPost("/api/pos/sale", async (PosSaleRequest req, PosService posService) =>
{
    var result = await posService.ProcessSale(req.Amount, req.InvoiceId);
    return Results.Ok(result);
});

// Thermal receipt printing
app.MapPost("/api/print/receipt", (ReceiptPrintRequest req, PrinterService printerService) =>
{
    var result = printerService.PrintReceipt(
        req.Text,
        req.InvoiceNumber,
        req.CustomerName,
        req.PrinterWidthMm,
        req.PrinterName);
    return Results.Ok(result);
});

app.MapGet("/api/printers", (PrinterService printerService) =>
{
    return Results.Ok(printerService.GetInstalledPrinters());
});

// Read scale weight from serial port
app.MapGet("/api/scale/read", (ScaleService scaleService) =>
{
    var result = scaleService.ReadWeight();
    return Results.Ok(result);
});

app.MapGet("/api/scale/configuration", (
    BridgeSettingsService settingsService,
    IConfiguration configuration) =>
{
    return Results.Ok(settingsService.GetScaleConfiguration(configuration));
});

app.MapPut("/api/scale/configuration", (
    ScaleConfigurationRequest request,
    BridgeSettingsService settingsService) =>
{
    var result = settingsService.UpdateScaleConfiguration(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// ==================== Fingerprint Device Endpoints ====================

// Test connection to fingerprint device
app.MapPost("/api/fingerprint/connect", async (DeviceConnectRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.ConnectAsync(req);
    return Results.Ok(result);
});

// Get device status and information
app.MapPost("/api/fingerprint/status", async (DeviceConnectRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.GetStatusAsync(req);
    return Results.Ok(result);
});

// Sync attendance logs from device
app.MapPost("/api/fingerprint/sync", async (AttendanceSyncRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.SyncAttendanceAsync(req);
    return Results.Ok(result);
});

// Get users registered on device
app.MapPost("/api/fingerprint/users", async (DeviceUsersRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.GetUsersAsync(req);
    return Results.Ok(result);
});

// Enroll new user on device
app.MapPost("/api/fingerprint/enroll", async (EnrollUserRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.EnrollUserAsync(req);
    return Results.Ok(result);
});

// Delete user from device
app.MapPost("/api/fingerprint/delete-user", async (DeleteUserRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.DeleteUserAsync(req);
    return Results.Ok(result);
});

// Clear all attendance logs from device
app.MapPost("/api/fingerprint/clear-logs", async (ClearLogsRequest req, FingerprintService fingerprintService) =>
{
    var result = await fingerprintService.ClearLogsAsync(req);
    return Results.Ok(result);
});

// Health check endpoint
app.MapGet("/api/health", () =>
{
    return Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow });
});

app.MapGet(
    "/api/bridge/health",
    async (
        HttpRequest request,
        PosService posService,
        ScaleService scaleService,
        PrinterService printerService,
        FingerprintService fingerprintService,
        VoipService voipService) =>
    {
        var services = new[]
        {
            posService.GetReadiness(),
            scaleService.GetReadiness(),
            printerService.GetReadiness(),
            fingerprintService.GetReadiness(),
            await voipService.GetReadinessAsync(),
            CreateBarcodeReadiness(),
        };

        return Results.Ok(CreateBridgeHealthResponse(
            services,
            IsAuthorizedBridgeRequest(request)));
    });

app.MapGet("/setup", (HttpContext context) =>
{
    if (!IsLoopbackRequest(context))
    {
        return Results.NotFound();
    }

    return Results.Content(
        BridgeSetupPageBuilder.Build(
            installResult.CreatedNew,
            deviceId,
            bridgeToken,
            bridgeSettingsService.SettingsPath),
        "text/html; charset=utf-8");
});

bool IsRateLimited(HttpRequest request)
{
    var origin = request.Headers.Origin.ToString();
    var token = request.Headers["X-Bridge-Token"].ToString();
    var key = $"{origin}:{token}";
    var now = DateTime.UtcNow;

    var timestamps = callRateStore.GetOrAdd(key, _ => new List<DateTime>());
    lock (timestamps)
    {
        timestamps.RemoveAll(ts => now - ts > rateLimitWindow);
        if (timestamps.Count >= maxCallsPerMinute)
        {
            return true;
        }

        timestamps.Add(now);
        return false;
    }
}

// VOIP-A endpoints
app.MapGet("/api/voip/health", async (VoipService voipService) =>
{
    var result = await voipService.GetHealthAsync();
    return Results.Ok(result);
});

app.MapPost("/api/voip/call", async (HttpRequest httpRequest, VoipCallRequest req, VoipService voipService) =>
{
    if (!IsAuthorizedBridgeRequest(httpRequest))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (IsRateLimited(httpRequest))
    {
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    var result = await voipService.CallAsync(req);
    return Results.Ok(result);
});

var installMessage = installResult.CreatedNew
    ? "Bridge installed successfully."
    : "Bridge is running.";
Console.WriteLine("============================================================");
Console.WriteLine(installMessage);
Console.WriteLine("Bridge health: http://localhost:5005/api/bridge/health");
Console.WriteLine($"Bridge setup:  {setupUrl}");
Console.WriteLine($"Device ID:     {deviceId}");
Console.WriteLine($"Settings file: {bridgeSettingsService.SettingsPath}");
Console.WriteLine("============================================================");

app.Run();
