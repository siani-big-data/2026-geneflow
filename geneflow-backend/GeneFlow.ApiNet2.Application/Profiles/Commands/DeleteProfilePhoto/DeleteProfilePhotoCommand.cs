using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.DeleteProfilePhoto;

/// <summary>
/// Command to delete profile photo.
/// </summary>
public sealed record DeleteProfilePhotoCommand(string UserId) : ICommand<Result>;
