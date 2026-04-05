using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Tests.API;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// </summary>
public class GeneFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();
    public IUserUnitOfWork MockUserUnitOfWork { get; } = Substitute.For<IUserUnitOfWork>();
    public IPasswordHasher MockPasswordHasher { get; } = Substitute.For<IPasswordHasher>();
    public IJwtTokenGenerator MockJwtTokenGenerator { get; } = Substitute.For<IJwtTokenGenerator>();
    public IEmailService MockEmailService { get; } = Substitute.For<IEmailService>();
    public ISequenceGenerator MockSequenceGenerator { get; } = Substitute.For<ISequenceGenerator>();
    public IUserAuthenticationValidator MockAuthValidator { get; } = Substitute.For<IUserAuthenticationValidator>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real DbContext and add in-memory
            services.RemoveAll<DbContextOptions<UserContext>>();
            services.RemoveAll<UserContext>();
            services.AddDbContext<UserContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // Remove real Redis
            services.RemoveAll<IConnectionMultiplexer>();
            var mockMultiplexer = Substitute.For<IConnectionMultiplexer>();
            services.AddSingleton(mockMultiplexer);

            // Replace services with mocks
            services.RemoveAll<IUserRepository>();
            services.AddScoped(_ => MockUserRepository);

            services.RemoveAll<IUserUnitOfWork>();
            services.AddScoped(_ => MockUserUnitOfWork);

            services.RemoveAll<IPasswordHasher>();
            services.AddScoped(_ => MockPasswordHasher);

            services.RemoveAll<IJwtTokenGenerator>();
            services.AddScoped(_ => MockJwtTokenGenerator);

            services.RemoveAll<IEmailService>();
            services.AddScoped(_ => MockEmailService);

            services.RemoveAll<ISequenceGenerator>();
            services.AddSingleton(_ => MockSequenceGenerator);

            services.RemoveAll<IUserAuthenticationValidator>();
            services.AddScoped(_ => MockAuthValidator);
        });
    }

    /// <summary>
    /// Resets all mocks to their default state.
    /// </summary>
    public void ResetMocks()
    {
        MockUserRepository.ClearReceivedCalls();
        MockUserUnitOfWork.ClearReceivedCalls();
        MockPasswordHasher.ClearReceivedCalls();
        MockJwtTokenGenerator.ClearReceivedCalls();
        MockEmailService.ClearReceivedCalls();
        MockSequenceGenerator.ClearReceivedCalls();
        MockAuthValidator.ClearReceivedCalls();
    }
}
