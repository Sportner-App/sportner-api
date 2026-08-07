using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Sportner.API.Authorization;

namespace Sportner.API.Extensions.Authentication;

public static class AuthenticationExtension
{
    public static IServiceCollection AddCustomAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("JwtSettings");
        var secret = section["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
        var issuer = section["Issuer"];
        var audience = section["Audience"];

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = validationParameters;
                options.MapInboundClaims = false;
            });

        services.Configure<ModeratorAuthorizationOptions>(
            configuration.GetSection(ModeratorAuthorizationOptions.SectionName));

        services.AddScoped<IAuthorizationHandler, ModeratorAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CanCreateContentAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement())
                .Build();

            options.AddPolicy(
                AuthorizationPolicies.Authenticated,
                policy => policy.RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.ActiveUser,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ActiveUserRequirement());
                });

            options.AddPolicy(
                AuthorizationPolicies.CanCreateContent,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ActiveUserRequirement());
                    policy.Requirements.Add(new CanCreateContentRequirement());
                });

            options.AddPolicy(
                AuthorizationPolicies.Moderator,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ActiveUserRequirement());
                    policy.Requirements.Add(new ModeratorRequirement());
                });
        });

        return services;
    }
}
