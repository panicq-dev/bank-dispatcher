using System.Text;
using System.Text.Json;
using BankDispatcher.Events;
using RabbitMQ.Client;

namespace BankDispatcher.Api.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly string _host = "localhost";
    private readonly string _exchangeName = "ex.bank.transactions";

    public async Task EnviarTransacaoAsync(TransactionEvent evento)
    {
        // Configuração da Conexão.
        var factory = new ConnectionFactory { HostName = _host };
        using var conexao = await factory.CreateConnectionAsync();
        using var canal = await conexao.CreateChannelAsync();

        // Criando Exchange, do tipo Topic.
        await canal.ExchangeDeclareAsync(
            exchange: _exchangeName, 
            type: ExchangeType.Topic, 
            durable: true
        );

        // Montando Routing Key, segundo o parâmetro passado no EnviarTransacaoAsync (acima).
        string routingKey = $"transaction.{evento.Channel.ToLower()}.{evento.Status.ToLower()}";

       // Conversões para JSON e Byte.
        string json = JsonSerializer.Serialize(evento);
        byte[] corpoMensagem = Encoding.UTF8.GetBytes(json);

        // Envia a mensagem para a Exchange com a Routing Key definida
        await canal.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            body: corpoMensagem
        );
    }
}