using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfilePhoto;

/// <summary>
/// Command to update profile photo.
/// </summary>
public sealed record UpdateProfilePhotoCommand(
    string UserId,
    string PhotoUrl,
    string? ThumbnailUrl = null,
    long? SizeBytes = null) : ICommand<Result<ProfileDto>>;
