using System.Collections.Generic;
using System.Threading.Tasks;
using ETG.Core.Models.Payment;
using ETG.Module.Booking.Classes.Info;

namespace ETG.Web.Services.Payment;

public interface ITravelPayService
{
    Task<TravelPayClientParameters> BuildClientParameters(PaymentInfo payment, string? redirectUrl = null);
    string GenerateFingerprint(int paymentId, double amount, string timestamp);
    bool IsPaymentSuccessful(int paymentStatus);
    Task<TravelPayVerificationResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string> parameters, PaymentInfo expectedPayment);
}
