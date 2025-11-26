using CoderamaOpsAI.Common.Interfaces;

namespace CoderamaOpsAI.Common.Services;

public class PaymentSimulator : IPaymentSimulator
{
    private readonly Random _random = new();

    public bool ShouldCompletePayment()
    {
        return _random.Next(0, 2) == 1; // 50% chance
    }
}
