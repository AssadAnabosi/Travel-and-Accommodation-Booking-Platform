using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.RejectHotel;
using Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Application.UnitTests.Common.Behaviors;

/// <summary>
/// Regression: the pipeline behaviors were constrained to <c>IRequest&lt;TResponse&gt;</c>, which void
/// commands (<c>IRequest</c>) don't satisfy, so DI silently skipped validation and authorization for
/// every void command. These tests go through the real <c>AddApplication()</c> registration.
/// </summary>
public class PipelineBehaviorRegistrationTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IHotelRepository> _hotels = new();

    private ISender BuildSender()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton(_currentUser.Object);
        services.AddSingleton(_hotels.Object);
        services.AddSingleton(Mock.Of<IUserRepository>());
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        services.AddSingleton(Mock.Of<IEmailService>());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private void SignedInAs(string role)
    {
        _currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(r => r == role);
    }

    [Fact]
    public async Task VoidCommand_WithInvalidInput_IsRejectedByTheValidator()
    {
        SignedInAs("Admin");

        var act = () => BuildSender().Send(new RejectHotelCommand(1, ""));

        await act.Should().ThrowAsync<ValidationException>();
        _hotels.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoidCommand_WithWrongRole_IsRejectedByTheAuthorizationBehavior()
    {
        SignedInAs("Customer");

        var act = () => BuildSender().Send(new RejectHotelCommand(1, "reason"));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task VoidCommand_Anonymous_IsRejectedByTheAuthorizationBehavior()
    {
        var act = () => BuildSender().Send(new RejectHotelCommand(1, "reason"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VoidCommand_ValidAndAuthorized_ReachesTheHandler()
    {
        SignedInAs("Admin");
        _hotels.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid()));

        await BuildSender().Send(new RejectHotelCommand(1, "reason"));

        _hotels.Verify(r => r.Update(It.IsAny<Hotel>()), Times.Once);
    }
}
