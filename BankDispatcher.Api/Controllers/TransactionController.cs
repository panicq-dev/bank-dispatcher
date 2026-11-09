using BankDispatcher.Api.DTOs;
using BankDispatcher.Api.Services;
using BankDispatcher.Events;
using Microsoft.AspNetCore.Mvc;

namespace BankDispatcher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IRabbitMqPublisher _publisher;
    
    public TransactionsController(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> CriarTransacao([FromBody] CreateTransactionRequest requisicao)
    {
        if (requisicao.Amount <= 0)
        {
            return BadRequest(new { mensagem = "O valor da transação deve ser maior que zero." });
        }

        // Transformando em Evento para jogar no RabbitMQ.
        var evento = new TransactionEvent(
            Id: Guid.NewGuid(),
            AccountNumber: requisicao.AccountNumber,
            Amount: requisicao.Amount,
            Channel: requisicao.Channel,
            Status: requisicao.Status,
            CreatedAt: DateTime.UtcNow
        );

        // Envia para o RabbitMQ, pelo nosso serviço.
        await _publisher.EnviarTransacaoAsync(evento);

        // Se deu certo, retorna 202 (Sucesso).
        return Accepted(new 
        { 
            mensagem = "Transação enviada para a fila com sucesso!",
            id = evento.Id
        });
    }
}