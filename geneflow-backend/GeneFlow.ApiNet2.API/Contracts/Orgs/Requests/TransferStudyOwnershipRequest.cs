namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;

/// <summary>
/// Payload for transferring ownership of a study between a User and an Org.
/// <see cref="NewOwnerType"/> is one of "User" or "Org"; <see cref="NewOwnerId"/>
/// must be the corresponding strongly-typed id (UserId / OrgId string form).
/// </summary>
public sealed record TransferStudyOwnershipRequest(string NewOwnerType, string NewOwnerId);
