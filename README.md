# Device Bridge Application

A local C#/.NET bridge for hardware-adjacent workflows that browsers cannot perform directly. The MVP is intentionally conservative: bridge process liveness is separate from per-device readiness, and unsupported hardware integrations return structured errors instead of fake success.

## Architecture

The bridge runs locally on the user's PC and exposes HTTP endpoints that the Next.js frontend can call from the browser. This allows the web application to interact with hardware devices that browsers cannot access directly.

## MVP Device Matrix

- **Fingerprint/Attendance Devices**: TCP reachability can be tested, but ZKTeco/Suprema/Anviz protocol and SDK support is not implemented yet.
- **POS Terminals**: real POS integration is not implemented yet. The endpoint returns `unsupported` unless explicit development-only mock mode is enabled.
- **Scales**: serial-port reads are real and return structured JSON for missing COM ports, invalid config, access denial, parse failures, and timeouts.
- **Thermal Printers**: readiness checks validate installed/default printer availability without printing. Receipt printing is attempted only by `/api/print/receipt`.
- **Barcode Scanners**: handled as keyboard-wedge input in the browser. The bridge does not detect barcode scanner hardware.
- **VOIP / Softphone**: validates bridge token/provider/config and launches `tel:` URLs. It cannot guarantee that the OS softphone is registered or available.

## Response Contract

Device-facing responses include stable status metadata where applicable:

- `success`
- `mode`: `real`, `mock`, `disabled`, `unsupported`, or `misconfigured`
- `ready`
- `code`
- `errorMessage`
- `errorMessageFa`

`/api/health` is process-only liveness. `/api/bridge/health` returns bridge status plus per-service readiness.

## Prerequisites

- .NET 8 SDK
- Windows OS (for SerialPort and printer access)
- Hardware devices connected and configured only for the integrations you are actually using

## Installation

1. Navigate to the bridge directory:
   ```bash
   cd bridge
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the application:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

The bridge will start on `http://localhost:5005`.

## Configuration

Edit `appsettings.json` to configure your devices:

### Fingerprint Device Configuration
```json
{
  "Fingerprint": {
    "DeviceType": "ZKTeco",
    "DefaultPort": 4370,
    "TimeoutSeconds": 30
  }
}
```

`DeviceType` is accepted as request/config metadata only. Real vendor protocol support must be implemented with the matching SDK or protocol before these devices can be marked ready.

### POS Configuration
```json
{
  "Pos": {
    "Mode": "unsupported",
    "IpAddress": "",
    "Port": "",
    "ConnectionType": ""
  }
}
```

Set `Mode` to `mock` only for local development tests. Outside `Development`, mock mode returns `disabled`.

### Scale Configuration
```json
{
  "Scale": {
    "PortName": "COM3",
    "BaudRate": "9600",
    "Parity": "None",
    "DataBits": "8",
    "StopBits": "One"
  }
}
```

### Printer Configuration
```json
{
  "Printer": {
    "Name": ""  // Leave empty to use default printer, or specify printer name
  }
}
```

### VOIP Configuration
```json
{
  "Bridge": {
    "DeviceId": "",
    "BridgeToken": "",
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://127.0.0.1:3000"
    ],
    "AllowTenantSubdomains": false,
    "TrustedDomainSuffix": ""
  },
  "Voip": {
    "MaxCallsPerMinute": 30,
    "Provider": "tel_uri"
  }
}
```

VOIP notes:
- `bridge/appsettings.json` is the installer/default config
- generated values are persisted in `%AppData%/AICompanionBridge/bridge-settings.json`
- VOIP endpoints require `X-Bridge-Token`
- If `BridgeToken` is empty, `POST /api/voip/call` returns `403`
- The bridge only listens on localhost
- `AllowedOrigins` must include the frontend origin
- Production deployments must provide explicit allowed origins and trusted tenant suffixes through config. There is no wildcard CORS fallback.

## API Endpoints

### Fingerprint Device Endpoints

#### POST /api/fingerprint/connect
Test TCP reachability for a fingerprint/attendance endpoint. A reachable TCP socket is not treated as verified attendance-device support.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "deviceType": "ZKTeco"
}
```

**Response:**
```json
{
  "success": false,
  "deviceInfo": null,
  "serialNumber": null,
  "userCount": null,
  "logCount": null,
  "mode": "unsupported",
  "ready": false,
  "code": "fingerprint_protocol_unsupported",
  "tcpReachable": true,
  "errorMessage": "Real fingerprint/attendance device protocol is not implemented yet.",
  "errorMessageFa": "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است."
}
```

#### POST /api/fingerprint/status
Test TCP reachability for status. Real status metadata is not returned until a vendor protocol/SDK is implemented.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "deviceType": "ZKTeco"
}
```

**Response:**
```json
{
  "success": false,
  "isConnected": false,
  "tcpReachable": true,
  "mode": "unsupported",
  "ready": false,
  "code": "fingerprint_protocol_unsupported",
  "errorMessage": "Real fingerprint/attendance device protocol is not implemented yet.",
  "errorMessageFa": "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است."
}
```

#### POST /api/fingerprint/sync
Sync attendance logs from device. This returns `unsupported` until real device protocol support is implemented.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "lastSyncTime": "2025-01-14T00:00:00Z",
  "deviceType": "ZKTeco"
}
```

**Response:**
```json
{
  "success": false,
  "totalLogs": 0,
  "logs": [],
  "syncedAt": null,
  "mode": "unsupported",
  "ready": false,
  "code": "unsupported",
  "errorMessage": "Real fingerprint/attendance device protocol is not implemented yet.",
  "errorMessageFa": "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است."
}
```

#### POST /api/fingerprint/users
Get users registered on device.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "deviceType": "ZKTeco"
}
```

**Response:**
```json
{
  "success": false,
  "totalUsers": 0,
  "users": [],
  "mode": "unsupported",
  "ready": false,
  "code": "unsupported",
  "errorMessage": "Real fingerprint/attendance device protocol is not implemented yet.",
  "errorMessageFa": "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است."
}
```

#### POST /api/fingerprint/enroll
Register a user on the device for fingerprint enrollment.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "userId": "EMP001",
  "userName": "علی محمدی",
  "privilege": 0,
  "deviceType": "ZKTeco"
}
```

**Response:**
```json
{
  "success": false,
  "mode": "unsupported",
  "ready": false,
  "code": "unsupported",
  "errorMessage": "Real fingerprint/attendance device protocol is not implemented yet.",
  "errorMessageFa": "اتصال واقعی دستگاه حضور و غیاب هنوز پیاده‌سازی نشده است."
}
```

#### POST /api/fingerprint/delete-user
Delete a user from device.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "userId": "EMP001",
  "deviceType": "ZKTeco"
}
```

#### POST /api/fingerprint/clear-logs
Clear all attendance logs from device.

**Request:**
```json
{
  "ipAddress": "192.168.1.201",
  "port": 4370,
  "deviceType": "ZKTeco"
}
```

---

### POS Endpoints

#### POST /api/pos/sale
Process a POS payment. Real POS integration returns `unsupported` until a certified/vendor integration is implemented.

**Request:**
```json
{
  "amount": 100000,
  "invoiceId": "INV-123456"
}
```

**Response:**
```json
{
  "success": false,
  "rrn": null,
  "mode": "unsupported",
  "ready": false,
  "code": "unsupported",
  "errorMessage": "Real POS integration is not implemented yet.",
  "errorMessageFa": "اتصال واقعی کارتخوان هنوز پیاده‌سازی نشده است."
}
```

### Scale Endpoints

#### GET /api/scale/read
Read weight from the scale.

**Response:**
```json
{
  "success": true,
  "weight": 1.234,
  "mode": "real",
  "ready": true,
  "code": "ready",
  "errorMessage": null
}
```

### Printer Endpoints

#### POST /api/print/receipt
Print a receipt on the thermal printer.

**Request:**
```json
{
  "text": "Receipt content...",
  "invoiceNumber": "INV-123456",
  "customerName": "John Doe"
}
```

**Response:**
```json
{
  "success": true,
  "errorMessage": null
}
```

### Health Check

#### GET /api/health
Health check endpoint.

**Response:**
```json
{
  "status": "ok",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### VOIP Endpoints

#### GET /api/bridge/health
Returns generic bridge presence, pairing metadata, and per-service readiness.

**Response example:**
```json
{
  "success": true,
  "deviceId": "device_abc123",
  "bridgeVersion": "1.0.0",
  "capabilities": ["barcode"],
  "requiresPairing": true,
  "services": [
    {
      "capability": "pos",
      "ready": false,
      "mode": "unsupported",
      "code": "unsupported",
      "message": "Real POS integration is not implemented yet.",
      "messageFa": "اتصال واقعی کارتخوان هنوز پیاده‌سازی نشده است."
    },
    {
      "capability": "barcode",
      "ready": true,
      "mode": "real",
      "code": "barcode_keyboard_wedge",
      "message": "Barcode scanners are handled as keyboard input; the bridge does not detect barcode hardware."
    }
  ]
}
```

This endpoint never returns `BridgeToken`.

#### GET /api/voip/health
Returns local VOIP bridge readiness for the frontend.

**Response example when token is missing:**
```json
{
  "success": true,
  "enabled": true,
  "ready": false,
  "provider": "tel_uri",
  "issues": ["Bridge token is not configured."],
  "mode": "misconfigured",
  "code": "voip_token_missing"
}
```

#### POST /api/voip/call
Launches the local OS softphone handler for a phone number.

**Headers:**
```text
Content-Type: application/json
X-Bridge-Token: YOUR_TOKEN
```

**Request:**
```json
{
  "phone": "09123456789",
  "normalizedPhone": "09123456789",
  "leadId": 123,
  "displayName": "مشتری نمونه"
}
```

**Response:**
```json
{
  "success": true,
  "callSessionId": "4b1e4b32f89349ebb71e1f5d6d149b34",
  "provider": "tel_uri",
  "status": "requested",
  "errorMessage": null
}
```

## Device Integration

### Fingerprint/Attendance Devices
The MVP tests TCP reachability only. The `FingerprintService.cs` deliberately returns `unsupported` for device metadata, users, logs, enrollment, delete, and clear-log operations until a real vendor SDK/protocol is implemented.

**ZKTeco Devices** (Most Common):
- Default port: 4370
- Protocol: ZK Protocol (TCP/IP)
- The current service does not implement or simulate the ZK protocol
- To implement actual device communication, add verified ZK SDK/protocol calls

**Implementation Notes**:
```csharp
// In FingerprintService.cs, implement actual device communication:
// 1. Use vendor SDK (ZKT eco SDK, libzkfp, etc.)
// 2. Or implement raw TCP protocol based on device documentation
// 3. Parse binary responses according to device specification
```

### POS Terminal
Real POS integration is not implemented. `Pos:Mode=mock` is development-only and returns `mode: "mock"` so it cannot be mistaken for a real terminal approval. Add a certified/vendor implementation before returning `mode: "real"` or a real RRN.

### Scale
Configured via SerialPort. Adjust the parsing logic in `ScaleService.cs` based on your scale's output format.

### Thermal Printer
Uses Windows printing API. For ESC/POS commands, uncomment and implement the `GetEscPosCommands` method in `PrinterService.cs`.

### Barcode Scanner
Most USB barcode scanners work as keyboard wedge (they type into focused input). No special integration needed - just focus an input field and the scanner will "type" the barcode.

## Troubleshooting

### Bridge not starting
- Check if port 5005 is already in use
- Verify .NET 8 SDK is installed
- Check firewall settings
- Try running as Administrator

### Fingerprint device not connecting
- Verify device IP address is correct (check device's network settings)
- Default port for ZKTeco is 4370 - verify port matches device configuration
- Ensure device and PC are on same network/subnet
- Check firewall allows outbound TCP connections to device
- Ping device IP from command prompt: `ping 192.168.1.201`
- Verify device is not in restricted mode or locked
- Try power cycling the device

### Sync returns no logs
- Check if device has attendance logs stored
- Verify lastSyncTime parameter is not in the future
- Some devices clear logs after download - check device settings
- Verify enrolled users have matching IDs in the system

### Enrollment failing
- Ensure user ID follows device format (some devices require numeric IDs)
- Check if device has available user slots
- Verify device is not at capacity

### Scale not reading
- Verify COM port is correct in `appsettings.json`
- Check if another application is using the port
- Verify scale is powered on and connected
- Adjust baud rate and other serial settings if needed

### Printer not printing
- Verify printer is installed and set as default
- Check printer name in `appsettings.json` if using specific printer
- Ensure printer has paper and is online

### POS not connecting
- Verify IP address and port in `appsettings.json`
- Check network connectivity to POS device
- Review POS device documentation for connection requirements

## Development

Add device-specific code only when vendor/model/protocol/SDK and deployment details are known. Keep unsupported services returning structured errors until real verification exists.

## Security

- The bridge only listens on localhost (not exposed to network)
- All input is validated before processing
- VOIP endpoints require `X-Bridge-Token`
- `BridgeToken` is stored in local writable settings, not returned by health endpoints
- If `BridgeToken` is empty, `POST /api/voip/call` returns `403`
- `AllowedOrigins` must include the frontend origin for browser access
- Do not infer device readiness from bridge liveness

## License

Part of the AI Companion project.
