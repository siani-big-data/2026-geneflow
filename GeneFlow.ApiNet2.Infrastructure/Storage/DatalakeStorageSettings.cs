namespace GeneFlow.ApiNet2.Infrastructure.Storage;

/// <summary>
/// Settings for Datalake storage connection (MinIO/S3 compatible).
/// </summary>
public sealed class DatalakeStorageSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DatalakeStorage";

    /// <summary>
    /// Gets or sets the MinIO/S3 endpoint URL.
    /// </summary>
    /// <example>http://localhost:9000</example>
    public string EndpointUrl { get; set; } = "http://localhost:9000";

    /// <summary>
    /// Gets or sets the access key (username).
    /// </summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>
    /// Gets or sets the secret key (password).
    /// </summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>
    /// Gets or sets the bucket name for trace storage.
    /// </summary>
    public string Bucket { get; set; } = "geneflow-traces";

    /// <summary>
    /// Gets or sets whether to use HTTPS.
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Gets or sets the region (optional, for AWS S3).
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
