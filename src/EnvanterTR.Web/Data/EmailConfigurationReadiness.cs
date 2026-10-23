using System.Net.Mail;

namespace EnvanterTR.Web.Data;

internal sealed record EmailConfigurationReadiness(bool HostValid, bool PortValid, bool SenderValid,
    bool PublicBaseUrlValid, bool AuthenticationConsistent, bool TlsSettingValid)
{
    public bool ConfigurationReady => HostValid && PortValid && SenderValid && PublicBaseUrlValid &&
        AuthenticationConsistent && TlsSettingValid;

    public static EmailConfigurationReadiness Evaluate(IConfiguration configuration, bool requireHttps = true)
    {
        var host = configuration["Email:Smtp:Host"];
        var hostValid = !string.IsNullOrWhiteSpace(host) && host == host.Trim() &&
            Uri.CheckHostName(host) != UriHostNameType.Unknown;
        var portValid = int.TryParse(configuration["Email:Smtp:Port"], out var port) && port is >= 1 and <= 65535;
        var senderValid = MailAddress.TryCreate(configuration["Email:FromAddress"], out _);
        var baseUrlValid = Uri.TryCreate(configuration["Email:PublicBaseUrl"], UriKind.Absolute, out var site) &&
            (site.Scheme == Uri.UriSchemeHttps || !requireHttps && site.Scheme == Uri.UriSchemeHttp) &&
            site.UserInfo.Length == 0 && site.Query.Length == 0 && site.Fragment.Length == 0;
        var authenticationConsistent = string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]) ==
            string.IsNullOrWhiteSpace(configuration["Email:Smtp:Password"]);
        var tlsSettingValid = configuration["Email:Smtp:EnableSsl"] is not { } tls || bool.TryParse(tls, out _);
        return new(hostValid, portValid, senderValid, baseUrlValid, authenticationConsistent, tlsSettingValid);
    }
}
