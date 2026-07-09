using System.Diagnostics;
using System.Text.RegularExpressions;
using Bridge.Models;

namespace Bridge.Services;

public interface IVoipProvider
{
    Task<VoipCallResponse> CallAsync(VoipCallRequest request, CancellationToken cancellationToken = default);
    Task<VoipHealthResponse> HealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Tel URI provider: launches default softphone/handler using a tel:/callto:/sip: URI.
/// </summary>
public class TelUriVoipProvider : IVoipProvider
{
    private readonly string _providerName = "tel_uri";
    private readonly IConfiguration _configuration;

    public TelUriVoipProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<VoipCallResponse> CallAsync(VoipCallRequest request, CancellationToken cancellationToken = default)
    {
        var phone = request.NormalizedPhone ?? request.Phone;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Task.FromResult(new VoipCallResponse
            {
                Success = false,
                Provider = _providerName,
                Status = "invalid",
                ErrorMessage = "Phone is required.",
                ErrorMessageFa = "شماره تلفن الزامی است.",
                Mode = BridgeModes.Misconfigured,
                Ready = false,
                Code = "voip_phone_required",
            });
        }

        phone = NormalizeDigits(phone.Trim());
        if (!Regex.IsMatch(phone, @"^\+?[0-9]{7,15}$"))
        {
            return Task.FromResult(new VoipCallResponse
            {
                Success = false,
                Provider = _providerName,
                Status = "invalid",
                ErrorMessage = "Phone format is invalid.",
                ErrorMessageFa = "فرمت شماره تلفن معتبر نیست.",
                Mode = BridgeModes.Misconfigured,
                Ready = false,
                Code = "voip_phone_invalid",
            });
        }

        var uri = $"tel:{phone}";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true,
            };

            Process.Start(startInfo);

            return Task.FromResult(new VoipCallResponse
            {
                Success = true,
                Provider = _providerName,
                Status = "requested",
                CallSessionId = Guid.NewGuid().ToString("N"),
                ErrorMessage = null,
                Mode = BridgeModes.Real,
                Ready = true,
                Code = "voip_call_requested",
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new VoipCallResponse
            {
                Success = false,
                Provider = _providerName,
                Status = "failed",
                ErrorMessage = ex.Message,
                ErrorMessageFa = "سیستم عامل نتوانست برنامه تماس را اجرا کند.",
                Mode = BridgeModes.Misconfigured,
                Ready = false,
                Code = "voip_launch_failed",
            });
        }
    }

    public Task<VoipHealthResponse> HealthAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var bridgeSection = _configuration.GetSection("Bridge");
        var voipSection = _configuration.GetSection("Voip");
        var provider = voipSection.GetValue<string>("Provider") ?? string.Empty;
        var token = bridgeSection.GetValue<string>("BridgeToken") ?? string.Empty;

        if (!string.Equals(provider, _providerName, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("VOIP provider is not configured as tel_uri.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            issues.Add("Bridge token is not configured.");
        }

        return Task.FromResult(new VoipHealthResponse
        {
            Success = true,
            Enabled = string.Equals(provider, _providerName, StringComparison.OrdinalIgnoreCase),
            Ready = issues.Count == 0,
            Provider = _providerName,
            Issues = issues.ToArray(),
            Mode = issues.Count == 0 ? BridgeModes.Real : BridgeModes.Misconfigured,
            Code = issues.Count == 0 ? BridgeCodes.Ready : "voip_not_ready",
            ErrorMessage = issues.Count == 0 ? null : string.Join(" ", issues),
            ErrorMessageFa = issues.Count == 0 ? null : "تنظیمات VOIP کامل نیست.",
        });
    }

    private static string NormalizeDigits(string value)
    {
        return value
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9')
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9');
    }
}

public class VoipService
{
    private readonly IVoipProvider _provider;

    public VoipService(IVoipProvider provider)
    {
        _provider = provider;
    }

    public Task<VoipHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return _provider.HealthAsync(cancellationToken);
    }

    public Task<VoipCallResponse> CallAsync(VoipCallRequest request, CancellationToken cancellationToken = default)
    {
        return _provider.CallAsync(request, cancellationToken);
    }

    public async Task<DeviceReadiness> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        var health = await _provider.HealthAsync(cancellationToken);
        return new DeviceReadiness
        {
            Capability = "voip",
            Ready = health.Ready,
            Mode = health.Mode,
            Code = health.Code,
            Message = health.ErrorMessage ?? (health.Ready
                ? "VOIP bridge configuration is ready. Softphone availability is verified only when a call is requested."
                : "VOIP bridge configuration is not ready."),
            MessageFa = health.ErrorMessageFa ?? (health.Ready
                ? "تنظیمات VOIP آماده است. وجود نرم‌افزار تماس فقط هنگام درخواست تماس مشخص می‌شود."
                : "تنظیمات VOIP آماده نیست."),
            Metadata = new Dictionary<string, object?>
            {
                ["provider"] = health.Provider,
                ["issues"] = health.Issues,
            },
        };
    }
}

