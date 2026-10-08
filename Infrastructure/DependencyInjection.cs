using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();

        return services;
    }
}