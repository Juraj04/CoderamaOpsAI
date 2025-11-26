namespace CoderamaOpsAI.Common.Events;

public record OrderCompletedEvent(
    int OrderId,
    int UserId,
    decimal Total
);
