using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Sportner.API.Authorization;

namespace Sportner.API.Extensions.Authentication;

public static class AuthenticationExtension
{
    /// <summary>
    /// HMAC-SHA256 icin anahtarin en az 256 bit (32 bayt) olmasi gerekir; daha
    /// kisa bir sir imzanin kaba kuvvetle uretilmesini kolaylastirir.
    /// </summary>
    private const int MinimumSecretBytes = 32;

    public static IServiceCollection AddCustomAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection("JwtSettings");
        var secret = section["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
        var issuer = section["Issuer"];
        var audience = section["Audience"];

        // Bos birakilan issuer/audience dogrulamayi sessizce kapatiyor (asagidaki
        // ValidateIssuer/ValidateAudience kosullarina bakin). Gelistirmede bu
        // kullanissli; production'da baska bir servisin urettigi token'in kabul
        // edilmesi anlamina gelir, o yuzden baslangicta durduruyoruz.
        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Issuer production ortaminda zorunludur; bos birakilirsa "
                    + "token dogrulamasi issuer kontrolu yapmaz.");
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Audience production ortaminda zorunludur; bos birakilirsa "
                    + "token dogrulamasi audience kontrolu yapmaz.");
            }

            if (Encoding.UTF8.GetByteCount(secret) < MinimumSecretBytes)
            {
                throw new InvalidOperationException(
                    $"JwtSettings:Secret en az {MinimumSecretBytes} bayt olmalidir.");
            }
        }

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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // SignalR (browser/mobile) cannot always set Authorization header on WebSockets.
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.Configure<AuthorizationAllowListOptions>(
            configuration.GetSection(AuthorizationAllowListOptions.SectionName));

        services.AddScoped<IAuthorizationHandler, ModeratorAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
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

            options.AddPolicy(
                AuthorizationPolicies.Admin,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ActiveUserRequirement());
                    policy.Requirements.Add(new AdminRequirement());
                });
        });

        return services;
    }
}
