using System.Net;
using System.Text;

namespace Bridge.Services;

public static class BridgeSetupPageBuilder
{
    public static string Build(bool createdNew, string deviceId, string bridgeToken, string settingsPath)
    {
        var token = WebUtility.HtmlEncode(bridgeToken);
        var device = WebUtility.HtmlEncode(deviceId);
        var encodedSettingsPath = WebUtility.HtmlEncode(settingsPath);
        var installBanner = createdNew
            ? "<div class=\"banner success\">نصب Bridge با موفقیت انجام شد.</div>"
            : "<div class=\"banner success\">Bridge در حال اجرا است.</div>";

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fa\" dir=\"rtl\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine("  <title>AI Companion Bridge</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Tahoma, sans-serif; background: #f8fafc; color: #0f172a; margin: 0; padding: 24px; }");
        html.AppendLine("    .card { max-width: 720px; margin: 0 auto; background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; }");
        html.AppendLine("    h1 { margin-top: 0; font-size: 24px; }");
        html.AppendLine("    .banner { padding: 12px 16px; border-radius: 8px; margin-bottom: 16px; }");
        html.AppendLine("    .success { background: #ecfdf5; color: #047857; border: 1px solid #a7f3d0; }");
        html.AppendLine("    .field { margin: 16px 0; }");
        html.AppendLine("    label { display: block; font-size: 13px; color: #475569; margin-bottom: 6px; }");
        html.AppendLine("    .value { display: flex; gap: 8px; align-items: center; }");
        html.AppendLine("    input { flex: 1; font-family: Consolas, monospace; font-size: 13px; padding: 10px; border-radius: 8px; border: 1px solid #cbd5e1; background: #f8fafc; }");
        html.AppendLine("    button { padding: 10px 14px; border: 0; border-radius: 8px; background: #2563eb; color: #fff; cursor: pointer; }");
        html.AppendLine("    p { line-height: 1.8; color: #475569; }");
        html.AppendLine("    .muted { font-size: 12px; color: #64748b; word-break: break-all; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <div class=\"card\">");
        html.AppendLine($"    {installBanner}");
        html.AppendLine("    <h1>راه‌اندازی Bridge</h1>");
        html.AppendLine("    <p>Bridge روی این سیستم فعال است. توکن زیر برای اتصال امن مرورگر به دستگاه‌های همین کامپیوتر استفاده می‌شود.</p>");
        html.AppendLine("    <div class=\"field\">");
        html.AppendLine("      <label>شناسه دستگاه</label>");
        html.AppendLine($"      <div class=\"value\"><input id=\"deviceId\" readonly value=\"{device}\" /></div>");
        html.AppendLine("    </div>");
        html.AppendLine("    <div class=\"field\">");
        html.AppendLine("      <label>توکن Bridge</label>");
        html.AppendLine("      <div class=\"value\">");
        html.AppendLine($"        <input id=\"bridgeToken\" type=\"password\" readonly value=\"{token}\" />");
        html.AppendLine("        <button type=\"button\" id=\"toggleTokenButton\">نمایش</button>");
        html.AppendLine("        <button type=\"button\" id=\"copyTokenButton\">کپی</button>");
        html.AppendLine("      </div>");
        html.AppendLine("    </div>");
        html.AppendLine($"    <p class=\"muted\">مسیر تنظیمات: {encodedSettingsPath}</p>");
        html.AppendLine("    <p>بعد از کپی توکن، در تنظیمات دستگاه‌های صندوق «بررسی و اتصال» را بزنید. همین اتصال برای چاپگر، ترازو و سایر قابلیت‌های مجاز استفاده می‌شود.</p>");
        html.AppendLine("  </div>");
        html.AppendLine("  <script>");
        html.AppendLine("    document.getElementById('toggleTokenButton').addEventListener('click', function () {");
        html.AppendLine("      var input = document.getElementById('bridgeToken');");
        html.AppendLine("      var button = document.getElementById('toggleTokenButton');");
        html.AppendLine("      var revealing = input.type === 'password';");
        html.AppendLine("      input.type = revealing ? 'text' : 'password';");
        html.AppendLine("      button.textContent = revealing ? 'پنهان' : 'نمایش';");
        html.AppendLine("    });");
        html.AppendLine("    document.getElementById('copyTokenButton').addEventListener('click', function () {");
        html.AppendLine("      var input = document.getElementById('bridgeToken');");
        html.AppendLine("      input.select();");
        html.AppendLine("      input.setSelectionRange(0, 99999);");
        html.AppendLine("      navigator.clipboard.writeText(input.value);");
        html.AppendLine("      alert('توکن کپی شد');");
        html.AppendLine("    });");
        html.AppendLine("  </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }
}
