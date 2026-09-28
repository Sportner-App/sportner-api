using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sportner.Infrastructure;

namespace Sportner.API.IntegrationTests;

/// <summary>
/// <c>AddInfrastructure</c> is shared by the API and all three workers, and
/// their environments are not the same. Anything it requires at startup must
/// be something every host actually has.
///
/// This is a regression guard: binding the Apple and Google options with
/// ValidateOnStart inside AddInfrastructure crashed all three workers on boot
/// ("The WebClientId field is required"), because only the API carries those
/// keys. Host-specific requirements belong behind an explicit opt-in that the
/// host calls for itself — see ValidateSocialAuthOptionsOnStart.
///
/// The guard is not specific to those two options: the test boots a host with
/// nothing but a connection string, so it goes red for any startup requirement
/// added anywhere under AddInfrastructure — storage, email and Places included.
/// Verified by adding one to each in turn.
/// </summary>
public class SharedCompositionTests
{
    /// <summary>The one thing every host genuinely needs.</summary>
    private const string ConnectionString =
        "Host=localhost;Database=sportner_test;Username=test;Password=test";

    [Fact]
    public async Task AddInfrastructure_StartsWithoutHostSpecificConfiguration()
    {
        using var host = BuildHost(services => services.AddInfrastructure(Configuration()));

        // Starting the host is what runs startup-time options validation, so
        // this reproduces the worker boot path rather than approximating it.
        var start = async () => await host.StartAsync();

        await start.Should().NotThrowAsync(
            "AddInfrastructure is shared by the API and the workers; a startup "
            + "requirement added here takes down every host that does not "
            + "happen to carry that configuration");

        await host.StopAsync();
    }

    [Fact]
    public async Task ValidateSocialAuthOptionsOnStart_FailsFastWhenKeysAreMissing()
    {
        using var host = BuildHost(services =>
        {
            services.AddInfrastructure(Configuration());
            services.ValidateSocialAuthOptionsOnStart();
        });

        var start = async () => await host.StartAsync();

        // The other half of the contract: opting in must still fail loudly,
        // otherwise a misconfigured API would silently reject every social
        // sign-in instead of refusing to start.
        await start.Should().ThrowAsync<Exception>(
            "the API opts into this check precisely so missing Apple/Google "
            + "configuration stops the host instead of breaking sign-in at runtime");
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupabaseConnection"] = ConnectionString,
            })
            .Build();

    private static IHost BuildHost(Action<IServiceCollection> configure)
    {
        var builder = Host.CreateApplicationBuilder();
        configure(builder.Services);
        return builder.Build();
    }
}
