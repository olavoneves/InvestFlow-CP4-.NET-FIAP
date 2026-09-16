using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace InvestFlow.Api.Configuration;

public static class CompressaoExtensions
{
    /// <summary>Compressão Brotli (preferida) e Gzip, inclusive em HTTPS, priorizando velocidade.</summary>
    public static IServiceCollection AddApiResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                .Concat(new[] { "application/json", "application/problem+json" })
                .Distinct();
        });

        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        return services;
    }
}
