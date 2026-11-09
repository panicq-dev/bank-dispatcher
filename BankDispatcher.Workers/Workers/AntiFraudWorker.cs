using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BankDispatcher.Workers.Workers;

public class AntiFraudWorker : BackgroundService
{
    private readonly ILogger<AntiFraudWorker> _logger;

    public AntiFraudWorker(ILogger<AntiFraudWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        var conexao = await factory.CreateConnectionAsync(stoppingToken);
        // Em vez de: CreateChannelAsync(stoppingToken)
        var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

        await canal.ExchangeDeclareAsync("ex.bank.transactions", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await canal.QueueDeclareAsync("q.antifraude", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        
        // Regra de binding: transaction.*.flagged (Pega apenas final .flagged)
        await canal.QueueBindAsync("q.antifraude", "ex.bank.transactions", "transaction.*.flagged", cancellationToken: stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(canal);

        consumidor.ReceivedAsync += async (model, evento) =>
        {
            var json = Encoding.UTF8.GetString(evento.Body.ToArray());
            
            _logger.LogWarning("[ALERTA ANTIFRAUDE | Key: {Key}] Suspeita Detectada: {Dados}", evento.RoutingKey, json);

            await canal.BasicAckAsync(evento.DeliveryTag, multiple: false);
        };

        await canal.BasicConsumeAsync("q.antifraude", autoAck: false, consumer: consumidor, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}