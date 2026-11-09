namespace BankDispatcher.Api.DTOs;

public record CreateTransactionRequest(
    string AccountNumber,
    decimal Amount,
    string Channel,
    string Status
);