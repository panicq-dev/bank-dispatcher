using BankDispatcher.Events;

namespace BankDispatcher.Api.Services;

public interface IRabbitMqPublisher
{
    Task EnviarTransacaoAsync(TransactionEvent evento);
}