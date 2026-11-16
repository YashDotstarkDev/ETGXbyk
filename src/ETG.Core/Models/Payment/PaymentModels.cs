using System.ComponentModel.DataAnnotations;

namespace ETG.Core.Models.Payment;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Invoice booking reference is required.")]
    public string InvoiceReference { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public double Amount { get; set; }

    public string RecaptchaToken { get; set; } = string.Empty;

    public bool AcceptTerms { get; set; }
}

public class CreatePaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public TravelPayClientParameters? TravelPay { get; set; }
}

public class TravelPayClientParameters
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public double PaymentAmount { get; set; }
    public int OrderId { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public int Mode { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
}

public class TravelPayCallbackResult
{
    public int PaymentStatus { get; set; }
    public string PaymentStatusString { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public double BaseAmount { get; set; }
    public double CustomerFee { get; set; }
    public double FundsToMerchant { get; set; }
    public double ProcessedAmount { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public string FailureCode { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
}

public class TravelPayVerificationResult
{
    public bool IsSuccess { get; set; }
    public bool IsApproved { get; set; }
    public bool IsInvalidRequest { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string MerchantUniquePaymentId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Currency { get; set; } = "AUD";
    public int PaymentStatus { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string? CardMasked { get; set; }
    public string? CardType { get; set; }
    public string? RawPayload { get; set; }
}
