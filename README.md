# Device Bridge Application

A C#/.NET bridge application that connects hardware devices (POS terminal, scale, thermal printer, barcode scanner, **fingerprint readers**, and VOIP softphone click-to-call) to the web application.

## Architecture

The bridge runs locally on the user's PC and exposes HTTP endpoints that the Next.js frontend can call from the browser. This allows the web application to interact with hardware devices that browsers cannot access directly.

## Supported Devices

- **Fingerprint/Attendance Devices**: ZKTeco, Suprema, Anviz and similar TCP/IP devices
- **POS Terminals**: TCP/IP connected payment terminals
- **Scales**: Serial port connected digital scales
- **Thermal Printers**: Windows printers for receipt printing
- **Barcode Scanners**: USB keyboard wedge scanners
- **VOIP / Softphone**: click-to-call through `tel:` URI using the OS default handler

## Prerequisites

- .NET 8 SDK
- Windows OS (for SerialPort and printer access)
- Hardware devices connected and configured on the network

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

Supported device types:
- `ZKTeco` - ZKTeco fingerprint devices (most common)
- `Suprema` - Suprema biometric devices
- `Anviz` - Anviz attendance devices

### POS Configuration
```json
{
  "Pos": {
    "IpAddress": "192.168.1.100",
    "Port": "8080",
    "ConnectionType": "TCP"
  }
}
```

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

## API Endpoints

### Fingerprint Device Endpoints

#### POST /api/fingerprint/connect
Test connection to a fingerprint device.

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
  "success": true,
  "deviceInfo": "ZKTeco Attendance Device",
  "serialNumber": "DEVICE_SERIAL_001",
  "userCount": 150,
  "logCount": 5000,
  "errorMessage": null
}
```

#### POST /api/fingerprint/status
Get device status and detailed information.

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
  "success": true,
  "isConnected": true,
  "deviceName": "ZKTeco Attendance Device",
  "serialNumber": "DEVICE_SERIAL_001",
  "firmwareVersion": "Ver 6.60",
  "userCount": 150,
  "logCount": 5000,
  "availableUserSlots": 850,
  "availableLogSlots": 195000,
  "deviceTime": "2025-01-15T10:30:00Z"
}
```

#### POST /api/fingerprint/sync
Sync attendance logs from device.

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
  "success": true,
  "totalLogs": 25,
  "logs": [
    {
      "userId": "EMP001",
      "logTime": "2025-01-15T08:30:00Z",
      "logType": 0,
      "verifyMode": 1
    }
  ],
  "syncedAt": "2025-01-15T10:30:00Z"
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
  "success": true,
  "totalUsers": 150,
  "users": [
    {
      "userId": "EMP001",
      "name": "علی محمدی",
      "privilege": 0,
      "hasFingerprint": true,
      "hasCard": false,
      "hasPassword": false
    }
  ]
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
  "success": true,
  "message": "کاربر EMP001 با موفقیت در دستگاه ثبت شد. اکنون اثر انگشت خود را روی دستگاه ثبت کنید."
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
Process a POS payment.

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
  "success": true,
  "rrn": "123456789012",
  "errorMessage": null
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
Returns generic bridge presence and pairing metadata.

**Response example:**
```json
{
  "success": true,
  "deviceId": "device_abc123",
  "bridgeVersion": "1.0.0",
  "capabilities": ["voip", "print", "scale", "pos", "fingerprint"],
  "requiresPairing": true
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
  "issues": ["Bridge token is not configured."]
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
Supports TCP/IP connected fingerprint devices. The `FingerprintService.cs` provides a template for device communication.

**ZKTeco Devices** (Most Common):
- Default port: 4370
- Protocol: ZK Protocol (TCP/IP)
- The service includes placeholder methods that simulate device responses
- To implement actual device communication, replace the simulation code with ZK SDK calls

**Implementation Notes**:
```csharp
// In FingerprintService.cs, implement actual device communication:
// 1. Use vendor SDK (ZKT eco SDK, libzkfp, etc.)
// 2. Or implement raw TCP protocol based on device documentation
// 3. Parse binary responses according to device specification
```

### POS Terminal
Currently implemented as a simulation. Replace the `ProcessSale` method in `PosService.cs` with actual device communication based on your POS device's protocol (TCP/IP, Serial, or vendor SDK).

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

The bridge includes TODO comments where device-specific code should be implemented. Replace simulation code with actual device communication based on your hardware specifications.

## Security

- The bridge only listens on localhost (not exposed to network)
- All input is validated before processing
- VOIP endpoints require `X-Bridge-Token`
- `BridgeToken` is stored in local writable settings, not returned by health endpoints
- If `BridgeToken` is empty, `POST /api/voip/call` returns `403`
- `AllowedOrigins` must include the frontend origin for browser access

## License

Part of the AI Companion project.
