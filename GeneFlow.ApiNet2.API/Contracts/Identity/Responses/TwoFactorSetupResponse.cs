namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response model for TOTP 2FA setup.
/// </summary>
/// <param name="Secret">The Base32-encoded secret key to be stored by the authenticator app.</param>
/// <param name="QrCodeUri">The otpauth:// URI for QR code generation.</param>
public sealed record TwoFactorSetupResponse(string Secret, string QrCodeUri);
