using Bridge.Services;
using Bridge.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen only on localhost for security
builder.WebHost.UseUrls("http://localhost:5005");

// Add services
builder.Services.AddLogging();
builder.Services.AddSingleton<PosService>();
builder.Services.AddSingleton<ScaleService>();
builder.Services.AddSingleton<PrinterService>();
builder.Services.AddSingleton<FingerprintService>();

// Enable CORS for localhost (browser security)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use CORS
app.UseCors();

// POS payment endpoint
app.MapPost("/api/pos/sale", async (PosSaleRequest req, PosService posService) =>
{
    var result = await posService.ProcessSale(req.Amount, req.InvoiceId);
    return Results.Ok(result);
});

// Thermal receipt printing
app.MapPost("/api/print/receipt", (ReceiptPrintRequest req, PrinterService printerService) =>
{
    var result = printerService.PrintReceipt(req.Text, req.InvoiceNumber, req.CustomerName, req.PrinterWidthMm);
    return Results.Ok(result);
});

// Read scale weight from serial port
app.MapGet("/api/scale/read", (ScaleService scaleService) =>
{
    var result = scaleService.ReadWeight();
    return Results.Ok(result);
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

app.Run();
