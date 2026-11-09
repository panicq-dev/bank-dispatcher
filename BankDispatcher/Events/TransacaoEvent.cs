namespace BankDispatcher.Events;

public record TransacaoEvent(
    string Conta,
    decimal Valor,
    string Canal,  // "pix", "ted", "cartao"
    string Status  // "success", "flagged"
);