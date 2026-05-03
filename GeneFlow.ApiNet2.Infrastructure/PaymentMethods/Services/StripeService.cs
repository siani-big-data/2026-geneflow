using GeneFlow.ApiNet2.Application.PaymentMethods.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Services;

/// <summary>
/// Stripe integration service.
/// </summary>
public sealed class StripeService : IStripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly string? _secretKey;
    private readonly bool _isConfigured;
    private readonly CustomerService _customerService;
    private readonly SetupIntentService _setupIntentService;
    private readonly PaymentMethodService _paymentMethodService;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _logger = logger;
        _secretKey = configuration["Stripe:SecretKey"];
        _isConfigured = !string.IsNullOrWhiteSpace(_secretKey) &&
                        !_secretKey.Contains("YOUR_SECRET_KEY");

        if (_isConfigured)
        {
            StripeConfiguration.ApiKey = _secretKey;
            _customerService = new CustomerService();
            _setupIntentService = new SetupIntentService();
            _paymentMethodService = new PaymentMethodService();
            _logger.LogInformation("Stripe configured successfully");
        }
        else
        {
            _logger.LogWarning("Stripe is not configured. Payment methods will use stub implementation.");
            _customerService = null!;
            _setupIntentService = null!;
            _paymentMethodService = null!;
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<string> GetOrCreateCustomerAsync(string userId, string email, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            // Return a fake customer ID for development
            return $"cus_dev_{userId.Replace("-", "")[..8]}";
        }

        try
        {
            // Search for existing customer by metadata
            var searchOptions = new CustomerSearchOptions
            {
                Query = $"metadata['user_id']:'{userId}'"
            };
            var searchResult = await _customerService.SearchAsync(searchOptions, cancellationToken: cancellationToken);

            if (searchResult.Data.Count > 0)
            {
                return searchResult.Data[0].Id;
            }

            // Create new customer
            var createOptions = new CustomerCreateOptions
            {
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId }
                }
            };

            var customer = await _customerService.CreateAsync(createOptions, cancellationToken: cancellationToken);
            _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get or create Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreateSetupIntentAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            // Return a fake client secret for development
            return $"seti_dev_{Guid.NewGuid():N}_secret_dev";
        }

        try
        {
            var options = new SetupIntentCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                Usage = "off_session" // Allow charging later without customer present
            };

            var setupIntent = await _setupIntentService.CreateAsync(options, cancellationToken: cancellationToken);
            _logger.LogInformation("Created SetupIntent {SetupIntentId} for customer {CustomerId}",
                setupIntent.Id, customerId);

            return setupIntent.ClientSecret;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create SetupIntent for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task AttachPaymentMethodAsync(string paymentMethodId, string customerId, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("Stub: Attaching payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
            return;
        }

        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            await _paymentMethodService.AttachAsync(paymentMethodId, options, cancellationToken: cancellationToken);
            _logger.LogInformation("Attached payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
            throw;
        }
    }

    public async Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("Stub: Detaching payment method {PaymentMethodId}", paymentMethodId);
            return;
        }

        try
        {
            await _paymentMethodService.DetachAsync(paymentMethodId, cancellationToken: cancellationToken);
            _logger.LogInformation("Detached payment method {PaymentMethodId}", paymentMethodId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to detach payment method {PaymentMethodId}", paymentMethodId);
            throw;
        }
    }

    public async Task SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("Stub: Setting default payment method {PaymentMethodId} for customer {CustomerId}",
                paymentMethodId, customerId);
            return;
        }

        try
        {
            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            await _customerService.UpdateAsync(customerId, options, cancellationToken: cancellationToken);
            _logger.LogInformation("Set default payment method {PaymentMethodId} for customer {CustomerId}",
                paymentMethodId, customerId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to set default payment method {PaymentMethodId} for customer {CustomerId}",
                paymentMethodId, customerId);
            throw;
        }
    }

    public async Task<StripePaymentMethodDetails?> GetPaymentMethodDetailsAsync(string paymentMethodId, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            // Return fake payment method details for development
            var random = new Random();
            var brands = new[] { "visa", "mastercard", "amex", "discover" };
            var brand = brands[random.Next(brands.Length)];

            return new StripePaymentMethodDetails(
                paymentMethodId,
                brand,
                random.Next(1000, 9999).ToString(),
                random.Next(1, 12),
                DateTime.UtcNow.Year + random.Next(1, 5)
            );
        }

        try
        {
            var paymentMethod = await _paymentMethodService.GetAsync(paymentMethodId, cancellationToken: cancellationToken);

            if (paymentMethod.Card == null)
            {
                _logger.LogWarning("Payment method {PaymentMethodId} has no card details", paymentMethodId);
                return null;
            }

            return new StripePaymentMethodDetails(
                paymentMethod.Id,
                paymentMethod.Card.Brand,
                paymentMethod.Card.Last4,
                (int)paymentMethod.Card.ExpMonth,
                (int)paymentMethod.Card.ExpYear
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get payment method details for {PaymentMethodId}", paymentMethodId);
            return null;
        }
    }
}
