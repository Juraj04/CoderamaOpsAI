namespace CoderamaOpsAI.Common.Events;

public record OrderCreatedEvent(
    int OrderId,
    int UserId,
    int ProductId,
    decimal Total
);
