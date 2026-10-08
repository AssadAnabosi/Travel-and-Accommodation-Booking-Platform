using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Fonts;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // The DbContext is the unit of work; hand out the same scoped instance for IUnitOfWork.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAmenityRepository, AmenityRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IHotelVisitRepository, HotelVisitRepository>();

        // --- Services ---
        var jwtSettings = new JwtSettings
        {
            Secret = configuration["JwtSettings:Secret"]!,
            Issuer = configuration["JwtSettings:Issuer"]!,
            Audience = configuration["JwtSettings:Audience"]!,
            AccessTokenMinutes = int.TryParse(configuration["JwtSettings:AccessTokenMinutes"], out var m) ? m : 60,
            RefreshTokenDays = int.TryParse(configuration["JwtSettings:RefreshTokenDays"], out var d) ? d : 7
        };
        services.AddSingleton(jwtSettings);

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        // Real SMTP (MailKit) when Smtp:Host is set — compose points it at MailHog; otherwise log only.
        var smtpSettings = new SmtpSettings
        {
            Host = configuration["Smtp:Host"] ?? "",
            Port = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 25,
            Security = Enum.TryParse<SecureSocketOptions>(configuration["Smtp:Security"], true, out var security)
                ? security
                : SecureSocketOptions.Auto,
            Username = configuration["Smtp:Username"],
            Password = configuration["Smtp:Password"],
            FromAddress = configuration["Smtp:FromAddress"] is { Length: > 0 } from ? from : "no-reply@tabp.dev",
            FromName = configuration["Smtp:FromName"] is { Length: > 0 } name ? name : "TABP Hotels",
            TimeoutMs = int.TryParse(configuration["Smtp:TimeoutMs"], out var timeout) ? timeout : 10_000
        };
        if (string.IsNullOrWhiteSpace(smtpSettings.Host))
        {
            services.AddScoped<IEmailService, LoggingEmailService>();
        }
        else
        {
            services.AddSingleton(smtpSettings);
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        // Set the PDF font resolver once, before any PDF is generated (the slim container has no
        // OS fonts, which PdfSharpCore's default resolver requires). See FileFontResolver.
        GlobalFontSettings.FontResolver = new FileFontResolver("AppSans");
        services.AddScoped<IPdfGenerator, PdfGenerator>();

        return services;
    }
}