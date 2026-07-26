namespace DailyMart.Application.Common.Options;

/// <summary>Ops-managed SMTP credentials - same "env vars, not admin UI" placement as JwtSettings, since
/// these are secrets rather than shop-facing settings. Enabled defaults to false so a fresh deploy with no
/// SMTP configured fails fast with a clear business-rule message instead of a raw SMTP connection error.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "DailyMart";
}
