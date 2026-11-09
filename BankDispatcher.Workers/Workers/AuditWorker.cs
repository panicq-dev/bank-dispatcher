using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BankDispatcher.Workers.Workers;

public class AuditWorker : BackgroundService
{
    private readonly ILogger<AuditWorker> _logger;

    public AuditWorker(ILogger<AuditWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Conexão
        var factory = new ConnectionFactory { HostName = "localhost" };
        var conexao = await factory.CreateConnectionAsync(stoppingToken);
        var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);
        // Exchange.
        await canal.ExchangeDeclareAsync(
            "ex.bank.transactions",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        // Criando a fila
        await canal.QueueDeclareAsync("q.auditoria",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Conexão da Fila e Exchange (Bind)
        await canal.QueueBindAsync("q.auditoria",
            "ex.bank.transactions",
            "transaction.#",
            cancellationToken: stoppingToken);

        // Consumidor
        var consumidor = new AsyncEventingBasicConsumer(canal);

        // O que fazer quando a mensagem chegar?
        consumidor.ReceivedAsync += async (model, evento) =>
        {
            // Byte recebido para JSON
            var json = Encoding.UTF8.GetString(evento.Body.ToArray());
            
            _logger.LogInformation("[AUDITORIA | Key: {Key}] Registrado: {Dados}", evento.RoutingKey, json);

            // Confirma para o RabbitMQ que a mensagem foi processada (remove da fila)
            await canal.BasicAckAsync(evento.DeliveryTag, multiple: false);
        };

        // Deixa o Consumidor aberto para pegar da fila q.auditoria
        await canal.BasicConsumeAsync("q.auditoria", autoAck: false, consumer: consumidor, cancellationToken: stoppingToken);

        // Mantém o Worker vivo enquanto a aplicação estiver rodando
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}