using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UploadProfilePhoto;

/// <summary>
/// Command to upload a profile photo.
/// The photo binary data is sent to the datalake for storage.
/// </summary>
public sealed record UploadProfilePhotoCommand(
    string UserId,
    string PhotoDataBase64,
    string FileName,
    string ContentType,
    long SizeBytes) : ICommand<Result<ProfileDto>>;
