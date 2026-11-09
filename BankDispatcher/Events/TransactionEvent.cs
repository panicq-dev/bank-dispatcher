namespace BankDispatcher.Events;

public record TransactionEvent(
    Guid Id,
    string AccountNumber,
    decimal Amount,
    string Channel,  // "pix", "ted", "cartao"
    string Status,  // "success", "flagged"
    DateTime CreatedAt
);