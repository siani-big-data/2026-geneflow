using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents research identifiers (ORCID ID and Website).
/// </summary>
public sealed partial class ResearchIdentifiers : ValueObject
{
    /// <summary>Maximum length of website URL.</summary>
    public const int WebsiteMaxLength = 500;

    private static readonly Regex OrcidRegex = GeneratedOrcidRegex();
    private static readonly Regex UrlRegex = GeneratedUrlRegex();

    /// <summary>Gets the ORCID ID.</summary>
    public string? OrcidId { get; }

    /// <summary>Gets the website URL.</summary>
    public string? Website { get; }

    /// <summary>Gets the full ORCID URL.</summary>
    public string? OrcidUrl => string.IsNullOrWhiteSpace(OrcidId)
        ? null
        : $"https://orcid.org/{OrcidId}";

    private ResearchIdentifiers(string? orcidId, string? website)
    {
        OrcidId = orcidId;
        Website = website;
    }

    /// <summary>
    /// Creates validated ResearchIdentifiers.
    /// </summary>
    public static Result<ResearchIdentifiers> Create(string? orcidId, string? website)
    {
        var trimmedOrcid = string.IsNullOrWhiteSpace(orcidId) ? null : orcidId.Trim();
        var trimmedWebsite = string.IsNullOrWhiteSpace(website) ? null : website.Trim();

        // Validate ORCID ID
        if (trimmedOrcid is not null && !OrcidRegex.IsMatch(trimmedOrcid))
            return Result.Failure<ResearchIdentifiers>(ProfileErrors.OrcidIdInvalidFormat);

        // Validate Website
        if (trimmedWebsite is not null)
        {
            if (trimmedWebsite.Length > WebsiteMaxLength)
                return Result.Failure<ResearchIdentifiers>(ProfileErrors.WebsiteTooLong(WebsiteMaxLength));

            if (!UrlRegex.IsMatch(trimmedWebsite))
                return Result.Failure<ResearchIdentifiers>(ProfileErrors.WebsiteInvalidFormat);
        }

        return new ResearchIdentifiers(trimmedOrcid, trimmedWebsite);
    }

    /// <summary>
    /// Creates empty ResearchIdentifiers.
    /// </summary>
    public static ResearchIdentifiers Empty => new(null, null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OrcidId;
        yield return Website;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (OrcidId is not null && Website is not null)
            return $"ORCID: {OrcidId}, Website: {Website}";
        if (OrcidId is not null)
            return $"ORCID: {OrcidId}";
        if (Website is not null)
            return $"Website: {Website}";
        return string.Empty;
    }

    // ORCID format: 0000-0000-0000-0000 or 0000-0000-0000-000X
    [GeneratedRegex(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$", RegexOptions.Compiled)]
    private static partial Regex GeneratedOrcidRegex();

    // Basic URL validation
    [GeneratedRegex(@"^https?://[\w\-]+(\.[\w\-]+)+(/[\w\-._~:/?#\[\]@!$&'()*+,;=%]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GeneratedUrlRegex();
}
