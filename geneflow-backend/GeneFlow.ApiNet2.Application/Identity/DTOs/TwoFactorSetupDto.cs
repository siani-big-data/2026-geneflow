namespace GeneFlow.ApiNet2.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for two-factor authentication setup.
/// </summary>
/// <param name="Secret">The Base32-encoded secret key.</param>
/// <param name="QrCodeUri">The otpauth:// URI for QR code generation.</param>
public sealed record TwoFactorSetupDto(string Secret, string QrCodeUri);
