namespace CoderamaOpsAI.Common.Events;

public record OrderExpiredEvent(
    int OrderId,
    int UserId
);
