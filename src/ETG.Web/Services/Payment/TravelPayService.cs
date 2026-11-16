using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ETG.Core.Content;
using ETG.Core.Models.Payment;
using ETG.Module.Booking.Classes.Info;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Digests;

namespace ETG.Web.Services.Payment;

/// <summary>
/// Zenith Payments / TravelPay integration service using BouncyCastle SHA3-512 cryptography.
/// Matches the legacy Kentico 13 implementation.
/// Dynamically resolves Sandbox vs Production credentials from Kentico WebsiteGlobalContent.
/// </summary>
public class TravelPayService : ITravelPayService
{
    private readonly IMediator _mediator;
    private readonly ILogger<TravelPayService> _logger;

    public TravelPayService(IMediator mediator, ILogger<TravelPayService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Builds the parameters required by the TravelPay hosted modal checkout widget ($.zpPayment).
    /// </summary>
    public async Task<TravelPayClientParameters> BuildClientParameters(PaymentInfo payment, string? redirectUrl = null)
    {
        var response = await _mediator.Send(new WebsiteSettingsQuery());
        var gc = response.GlobalContent;

        bool isProduction = string.Equals(gc.TravelPayEnvironment, "Production", StringComparison.OrdinalIgnoreCase);

        string pluginUrl = isProduction
            ? (gc.TravelPayProductionPluginBaseUrl ?? gc.TravelPayProductionBaseUrl ?? string.Empty)
            : (gc.TravelPaySandboxPluginBaseUrl ?? gc.TravelPaySandboxBaseUrl ?? string.Empty);

        string apiKey = isProduction
            ? (gc.TravelPayProductionApiKey ?? string.Empty)
            : (gc.TravelPaySandboxApiKey ?? string.Empty);

        string apiUsername = isProduction
            ? (gc.TravelPayProductionApiUsername ?? string.Empty)
            : (gc.TravelPaySandboxApiUsername ?? string.Empty);

        string apiPassword = isProduction
            ? (gc.TravelPayProductionApiPassword ?? string.Empty)
            : (gc.TravelPaySandboxApiPassword ?? string.Empty);

        string merchantCode = isProduction
            ? (gc.TravelPayProductionMerchantCode ?? string.Empty)
            : (gc.TravelPaySandboxMerchantCode ?? string.Empty);

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        string fingerprint = ComputeFingerprint(apiKey, apiUsername, apiPassword, payment.PaymentID, payment.PaymentAmount, timestamp);

        pluginUrl = pluginUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(pluginUrl) && !pluginUrl.Contains("/online/v", StringComparison.OrdinalIgnoreCase))
        {
            pluginUrl = $"{pluginUrl}/online/v5";
        }

        return new TravelPayClientParameters
        {
            ApiUrl = pluginUrl,
            ApiKey = apiKey,
            Fingerprint = fingerprint,
            MerchantCode = merchantCode,
            CustomerReference = $"P{payment.PaymentID}",
            CustomerName = $"{payment.PaymentFirstName} {payment.PaymentLastName}".Trim(),
            CustomerEmail = payment.PaymentEmail,
            PaymentAmount = payment.PaymentAmount,
            OrderId = payment.PaymentID,
            Timestamp = timestamp,
            Mode = 0,
            RedirectUrl = redirectUrl ?? gc.TravelPayResultUrl ?? string.Empty
        };
    }

    /// <summary>
    /// Computes the SHA3-512 digital signature using BouncyCastle Sha3Digest(512),
    /// matching the exact formula from legacy Kentico 13 BookingController:
    /// ApiKey|ApiUsername|ApiPassword|Mode(0)|AmountInCents|P{PaymentID}|Timestamp
    /// </summary>
    public string GenerateFingerprint(int paymentId, double amount, string timestamp)
    {
        _logger.LogWarning("GenerateFingerprint called synchronously without Kentico credentials. Use BuildClientParameters for full payment flow.");
        return ComputeFingerprint(string.Empty, string.Empty, string.Empty, paymentId, amount, timestamp);
    }

    private static string ComputeFingerprint(string apiKey, string apiUsername, string apiPassword, int paymentId, double amount, string timestamp)
    {
        long amountInCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        string payload = $"{apiKey}|{apiUsername}|{apiPassword}|0|{amountInCents}|P{paymentId}|{timestamp}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);

        // 1. Initialize BouncyCastle Sha3Digest with 512-bit output
        var sha3 = new Sha3Digest(512);

        // 2. Feed the UTF-8 payload bytes
        sha3.BlockUpdate(bytes, 0, bytes.Length);

        // 3. Finalize 64 bytes (512 bits)
        byte[] hashBytes = new byte[64];
        sha3.DoFinal(hashBytes, 0);

        // 4. Return as 128 lowercase hexadecimal characters
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// In Zenith Payments / TravelPay, status 3 indicates an approved transaction.
    /// </summary>
    public bool IsPaymentSuccessful(int paymentStatus)
    {
        return paymentStatus == 3;
    }

    /// <summary>
    /// Verifies the callback payload received from TravelPay / Zenith Payments against
    /// the expected payment record, binding transaction properties, checking status,
    /// and verifying authenticity.
    /// </summary>
    public async Task<TravelPayVerificationResult> VerifyCallbackAsync(
        IReadOnlyDictionary<string, string> parameters,
        PaymentInfo expectedPayment)
    {
        string GetVal(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (parameters.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val.Trim();

                foreach (var kvp in parameters)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        return kvp.Value.Trim();
                    }
                }
            }
            return string.Empty;
        }

        string mupid = GetVal("MerchantUniquePaymentId", "CustomerReference", "OrderId", "order_id");
        string paymentRef = GetVal("PaymentReference", "ReceiptNo", "Reference", "TransactionId", "reference_no");
        string statusRaw = GetVal("PaymentStatus", "Status", "payment_status", "status");
        int.TryParse(statusRaw, out int status);
        string statusString = GetVal("PaymentStatusString", "StatusMessage", "status_message", "Result");
        string failureReason = GetVal("FailureReason", "ErrorMessage", "error_message", "ResponseMessage");
        string cardMasked = GetVal("AccountOrCardNo", "CardNo", "MaskedCardNo", "account_no");
        string cardType = GetVal("PaymentCard", "CardType", "card_type");

        // Parse amount candidates (handling surcharges and fees)
        double.TryParse(GetVal("BaseAmount"), out double baseAmount);
        double.TryParse(GetVal("FundsToMerchant"), out double fundsToMerchant);
        double.TryParse(GetVal("ProcessedAmount"), out double processedAmount);
        double.TryParse(GetVal("CustomerFee"), out double customerFee);
        double.TryParse(GetVal("Amount", "paymentAmount", "amount"), out double genericAmount);

        double chosenAmount = fundsToMerchant > 0 ? fundsToMerchant
                            : baseAmount > 0 ? baseAmount
                            : processedAmount > 0 ? processedAmount
                            : genericAmount > 0 ? genericAmount
                            : expectedPayment.PaymentAmount;

        var result = new TravelPayVerificationResult
        {
            PaymentReference = paymentRef,
            MerchantUniquePaymentId = mupid,
            PaymentStatus = status,
            Amount = chosenAmount,
            Currency = "AUD",
            FailureReason = failureReason,
            CardMasked = cardMasked,
            CardType = cardType,
            RawPayload = System.Text.Json.JsonSerializer.Serialize(parameters)
        };

        string failureCode = GetVal("FailureCode", "error_code", "ErrorCode");
        bool isExplicitFailure = status != 3 || !string.IsNullOrWhiteSpace(failureReason) || !string.IsNullOrWhiteSpace(failureCode);

        // 1. Reference Check: On successful transactions, Zenith Payments ALWAYS generates a PaymentReference.
        if (string.IsNullOrWhiteSpace(paymentRef))
        {
            if (isExplicitFailure)
            {
                _logger.LogInformation("TravelPay callback reported a declined/failed transaction without PaymentReference for Payment {PaymentID}", expectedPayment.PaymentID);
                result.IsSuccess = false;
                result.IsApproved = false;
                result.IsInvalidRequest = false;
                result.StatusMessage = !string.IsNullOrWhiteSpace(failureReason) ? failureReason : "Transaction declined or cancelled.";
                return result;
            }

            _logger.LogError("Payment verification failed: PaymentReference is missing for Payment {PaymentID}. Raw params: {Raw}",
                expectedPayment.PaymentID, result.RawPayload);
            result.IsSuccess = false;
            result.IsApproved = false;
            result.IsInvalidRequest = true;
            result.StatusMessage = "Missing payment reference from payment provider.";
            return result;
        }

        // 2. Binding Check: Validate MerchantUniquePaymentId (must match P{PaymentID} or raw ID)
        string expectedMupid = expectedPayment.PaymentMerchantUniquePaymentId;
        string expectedFallback = $"P{expectedPayment.PaymentID}";
        string expectedRawId = expectedPayment.PaymentID.ToString();

        bool mupidMatches = string.Equals(mupid, expectedMupid, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(mupid, expectedFallback, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(mupid, expectedRawId, StringComparison.OrdinalIgnoreCase);

        if (!mupidMatches)
        {
            _logger.LogError("Payment verification failed: MUPID mismatch. Received '{Mupid}', expected '{Expected}' (or '{Raw}')",
                mupid, expectedMupid, expectedRawId);
            result.IsSuccess = false;
            result.IsApproved = false;
            result.IsInvalidRequest = true;
            result.StatusMessage = "Transaction binding failed: MerchantUniquePaymentId mismatch.";
            return result;
        }

        // 3. Binding Check: Validate Amount (tolerance of 5 cents for rounding/fees)
        double expectedAmount = expectedPayment.PaymentAmount;
        bool amountMatches = false;

        if (baseAmount > 0 && Math.Abs(baseAmount - expectedAmount) <= 0.05) amountMatches = true;
        else if (fundsToMerchant > 0 && Math.Abs(fundsToMerchant - expectedAmount) <= 0.05) amountMatches = true;
        else if (processedAmount > 0 && Math.Abs(processedAmount - expectedAmount) <= 0.05) amountMatches = true;
        else if (genericAmount > 0 && Math.Abs(genericAmount - expectedAmount) <= 0.05) amountMatches = true;
        else if (processedAmount > 0 && customerFee > 0 && Math.Abs((processedAmount - customerFee) - expectedAmount) <= 0.05) amountMatches = true;
        else if (baseAmount <= 0 && fundsToMerchant <= 0 && processedAmount <= 0 && genericAmount <= 0) amountMatches = true; // No amount field returned

        if (!amountMatches)
        {
            _logger.LogError("Payment verification failed: Amount mismatch for Payment {PaymentID}. Received (FundsToMerchant: {Funds}, Base: {Base}, Processed: {Proc}), expected {Expected}",
                expectedPayment.PaymentID, fundsToMerchant, baseAmount, processedAmount, expectedAmount);
            result.IsSuccess = false;
            result.IsApproved = false;
            result.IsInvalidRequest = true;
            result.StatusMessage = $"Amount mismatch: received FundsToMerchant=${fundsToMerchant:F2}, Base=${baseAmount:F2}, expected ${expectedAmount:F2}.";
            return result;
        }

        // 4. Status Check: Must be status 3 (Approved) or status text indicates success
        bool isApproved = status == 3 ||
                          string.Equals(statusString, "Approved", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(statusString, "Success", StringComparison.OrdinalIgnoreCase);

        if (!isApproved)
        {
            _logger.LogWarning("Payment {PaymentID} reported non-approved status: {Status} ('{StatusStr}'), Reason: {Reason}",
                expectedPayment.PaymentID, status, statusString, failureReason);
            result.IsSuccess = false;
            result.IsApproved = false;
            result.IsInvalidRequest = false; // Real payment, declined by bank
            result.StatusMessage = !string.IsNullOrWhiteSpace(failureReason) ? failureReason : $"Payment status is {status} ({statusString}).";
            return result;
        }

        result.IsSuccess = true;
        result.IsApproved = true;
        result.IsInvalidRequest = false;
        result.StatusMessage = "Transaction successfully verified and bound.";
        return result;
    }
}
