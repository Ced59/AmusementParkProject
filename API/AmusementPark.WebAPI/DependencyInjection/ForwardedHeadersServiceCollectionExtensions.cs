using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using AmusementPark.WebAPI.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemNetIPNetwork = System.Net.IPNetwork;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Configures forwarded headers for the trusted production proxy chain only.
/// </summary>
public static class ForwardedHeadersServiceCollectionExtensions
{
    private static readonly char[] ForwardedHeaderSeparators = [';', ','];

    public static IServiceCollection AddApiForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ForwardedHeadersSettings settings = configuration
            .GetSection(ForwardedHeadersSettings.SectionName)
            .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = Math.Max(1, settings.ForwardLimit);
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.AllowedHosts.Clear();

            IReadOnlyCollection<IPAddress> knownProxies = ParseKnownProxies(settings.KnownProxies);
            foreach (IPAddress knownProxy in knownProxies)
            {
                options.KnownProxies.Add(knownProxy);
            }

            IReadOnlyCollection<SystemNetIPNetwork> knownNetworks = ParseKnownNetworks(settings.KnownNetworks);
            foreach (SystemNetIPNetwork knownNetwork in knownNetworks)
            {
                options.KnownIPNetworks.Add(knownNetwork);
            }

            IReadOnlyCollection<string> allowedHosts = SplitConfiguredValues(settings.AllowedHosts);
            foreach (string allowedHost in allowedHosts)
            {
                options.AllowedHosts.Add(allowedHost);
            }
        });

        return services;
    }

    private static IReadOnlyCollection<IPAddress> ParseKnownProxies(IEnumerable<string> configuredValues)
    {
        List<IPAddress> knownProxies = [];

        foreach (string configuredValue in SplitConfiguredValues(configuredValues))
        {
            if (!IPAddress.TryParse(configuredValue, out IPAddress? address))
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers known proxy '{configuredValue}'. Expected an IPv4 or IPv6 address.");
            }

            knownProxies.Add(address);
        }

        return knownProxies;
    }

    private static IReadOnlyCollection<SystemNetIPNetwork> ParseKnownNetworks(IEnumerable<string> configuredValues)
    {
        List<SystemNetIPNetwork> knownNetworks = [];

        foreach (string configuredValue in SplitConfiguredValues(configuredValues))
        {
            int separatorIndex = configuredValue.IndexOf('/', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == configuredValue.Length - 1)
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers known network '{configuredValue}'. Expected CIDR notation, for example '172.30.31.0/24'.");
            }

            string prefix = configuredValue[..separatorIndex];
            string prefixLengthValue = configuredValue[(separatorIndex + 1)..];

            if (!IPAddress.TryParse(prefix, out IPAddress? address))
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers known network prefix '{prefix}' in '{configuredValue}'.");
            }

            if (!int.TryParse(prefixLengthValue, NumberStyles.None, CultureInfo.InvariantCulture, out int prefixLength))
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers known network prefix length '{prefixLengthValue}' in '{configuredValue}'.");
            }

            try
            {
                knownNetworks.Add(new SystemNetIPNetwork(address, prefixLength));
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers known network '{configuredValue}'. Expected a canonical CIDR network base address, for example '172.30.31.0/24'.",
                    exception);
            }
        }

        return knownNetworks;
    }

    private static IReadOnlyCollection<string> SplitConfiguredValues(IEnumerable<string> configuredValues)
    {
        return configuredValues
            .Where(static configuredValue => !string.IsNullOrWhiteSpace(configuredValue))
            .SelectMany(static configuredValue => configuredValue.Split(ForwardedHeaderSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(static configuredValue => !string.IsNullOrWhiteSpace(configuredValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
