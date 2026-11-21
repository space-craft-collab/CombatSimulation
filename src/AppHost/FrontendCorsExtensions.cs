using Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// CORS for the SvelteKit frontend (ADR-0004): the browser calls
/// the HTTP API and the SignalR hub from another origin.
/// </summary>
internal static class FrontendCorsExtensions
{
    /// <summary>
    /// The configuration section listing the origins the frontend
    /// is served from. Empty or missing allows no other origin.
    /// </summary>
    public const string AllowedOriginsSection = "Cors:AllowedOrigins";

    /// <summary>
    /// Registers a default CORS policy that admits the configured
    /// frontend origins and nothing else.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCors();

        // Read when the policy is first needed, not at registration,
        // so settings a test host adds late still count. No
        // credentials: nothing is authenticated by a cookie, and the
        // frontend's SignalR client connects without them.
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(configuration.GetSection(AllowedOriginsSection).Get<string[]>() ?? [])
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }
}
