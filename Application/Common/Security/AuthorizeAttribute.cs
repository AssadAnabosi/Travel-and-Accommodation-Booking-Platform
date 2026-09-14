namespace Application.Common.Security;

// Attribute on a Command/Query class itself — not ASP.NET's attribute, since Application
// can't reference ASP.NET Core. Roles is a comma-separated list; omit it to just require "logged in".
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AuthorizeAttribute : Attribute
{
    public string? Roles { get; set; }
}