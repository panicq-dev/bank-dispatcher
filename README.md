# BankDispatcher

O **BankDispatcher** é um projeto de estudos desenvolvido em C# e RabbitMQ para simular o processamento de transações em um sistema bancário.

A ideia é trabalhar com processamento assíncrono: a API recebe uma solicitação de transação, envia um evento para o RabbitMQ e retorna `HTTP 202 Accepted`, sem precisar esperar que todos os processos sejam concluídos.

A partir daí, outros serviços ficam responsáveis por consumir esses eventos e executar tarefas como auditoria e análise de possíveis fraudes.

Para isso, o projeto utiliza um **Topic Exchange**, permitindo que uma mesma mensagem seja direcionada para diferentes filas, de acordo com sua Routing Key.

---

## Estrutura do projeto

A solução está dividida em três projetos, cada um com uma responsabilidade específica:

```text
BankDispatcher/
├── BankDispatcher/               # Contratos e modelos compartilhados
├── BankDispatcher.Api/           # API responsável por receber e publicar eventos
└── BankDispatcher.Workers/       # Serviços responsáveis por consumir os eventos
```

### Como cada projeto funciona

- **BankDispatcher:** contém os modelos e contratos compartilhados entre a API e os Workers. É onde fica a definição do `TransactionEvent`, por exemplo.
- **BankDispatcher.Api:** recebe as requisições HTTP através do endpoint `POST /api/transactions`. Após validar os dados, publica o evento no RabbitMQ por meio do serviço `IRabbitMqPublisher`.
- **BankDispatcher.Workers:** executa os consumidores em segundo plano, utilizando `BackgroundService`.

Dentro dos Workers, temos duas responsabilidades:

- **AuditWorker:** recebe todas as transações e registra os eventos para fins de auditoria e histórico.
- **AntiFraudWorker:** recebe apenas as transações identificadas como suspeitas (`flagged`) e registra os alertas de possível fraude.

## Como funciona a mensageria

O RabbitMQ é responsável por receber os eventos publicados pela API e distribuí-los entre as filas, seguindo as regras configuradas no Exchange.

Neste projeto, utilizamos um **Topic Exchange**, que permite filtrar as mensagens de acordo com padrões definidos nas Routing Keys.

### Componentes utilizados

| Componente | Nome | Configuração | Responsabilidade |
|---|---|---|---|
| Exchange | `ex.bank.transactions` | Topic / Durable | Recebe os eventos e direciona as mensagens. |
| Fila | `q.auditoria` | Durable | Recebe todas as transações. |
| Fila | `q.antifraude` | Durable | Recebe somente transações suspeitas. |

### Routing Keys e Bindings

A API publica os eventos utilizando o seguinte padrão:

```text
transaction.<canal>.<status>
```

Alguns exemplos:

```text
transaction.pix.success
transaction.ted.success
transaction.cartao.flagged
```

Cada fila possui um Binding diferente, determinando quais mensagens ela deve receber.

**Fila de auditoria — `q.auditoria`**

```text
transaction.#
```

Utiliza o `#` para aceitar qualquer sequência de palavras após `transaction`. Dessa forma, todas as transações são direcionadas para essa fila.

**Fila de antifraude — `q.antifraude`**

```text
transaction.*.flagged
```

Utiliza o `*` para representar exatamente uma palavra. Assim, somente eventos que terminam com `flagged` são direcionados para essa fila.

Com essa configuração, uma transação suspeita pode ser recebida pelas duas filas ao mesmo tempo: uma para auditoria e outra para análise de risco.

## Tecnologias utilizadas

- C# / .NET 10
- ASP.NET Core Web API
- Worker Services
- RabbitMQ
- RabbitMQ.Client 7.x
- Docker

## Executando o projeto

### Pré-requisitos

- .NET 10 SDK instalado.
- Docker Desktop instalado e em execução.

### 1. Iniciar o RabbitMQ

Execute o comando abaixo para criar e iniciar um container com o RabbitMQ e sua interface de gerenciamento:

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

A interface de gerenciamento ficará disponível em:

`http://localhost:15672`

Credenciais padrão:

- Usuário: `guest`
- Senha: `guest`

### 2. Executar os Workers

Na raiz da solução, abra um terminal e execute:

```bash
dotnet run --project BankDispatcher.Workers
```

Os consumidores ficarão em execução, aguardando novas mensagens nas filas.

### 3. Executar a API

Em outro terminal, inicie a Web API:

```bash
dotnet run --project BankDispatcher.Api
```

Com os dois projetos em execução e o RabbitMQ conectado, já será possível enviar transações para a API.

## Testando a aplicação

Podemos testar o funcionamento utilizando requisições HTTP.

### Cenário 1 — Transação convencional

Envie uma requisição simulando uma transação PIX:

```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "accountNumber": "12345-6",
    "amount": 250.00,
    "channel": "pix",
    "status": "success"
  }'
```

**O que deve acontecer?**

1. A API recebe e valida a requisição.
2. O evento é publicado no Topic Exchange.
3. O RabbitMQ identifica que a mensagem corresponde ao Binding `transaction.#`.
4. A mensagem é encaminhada para `q.auditoria`.
5. O `AuditWorker` consome e registra a transação.

A API retorna `202 Accepted`, enquanto o processamento acontece em segundo plano.

### Cenário 2 — Transação suspeita

Agora, vamos simular uma transação identificada como suspeita:

```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "accountNumber": "98765-4",
    "amount": 15000.00,
    "channel": "cartao",
    "status": "flagged"
  }'
```

**O que deve acontecer?**

1. A API recebe e publica o evento.
2. O RabbitMQ verifica os Bindings configurados.
3. A mensagem corresponde aos dois padrões:
   - `transaction.#`
   - `transaction.*.flagged`
4. O evento é encaminhado para as duas filas.
5. O `AuditWorker` registra a transação.
6. O `AntiFraudWorker` registra um alerta de possível fraude.

Novamente, a API retorna `202 Accepted`, sem precisar aguardar o processamento dos consumidores.

---

## Objetivo do projeto

O principal objetivo do BankDispatcher é praticar conceitos de mensageria com RabbitMQ em um cenário próximo de aplicações reais.

Durante o desenvolvimento, serão trabalhados conceitos como:

- Publicação e consumo de mensagens.
- Exchanges e suas regras de roteamento.
- Routing Keys e Bindings.
- Comunicação assíncrona entre serviços.
- Separação de responsabilidades entre Producer e Consumers.
- Integração do RabbitMQ com aplicações .NET.

A proposta é construir uma aplicação simples, mas que permita entender como o RabbitMQ pode ser utilizado para desacoplar processos e distribuir responsabilidades dentro de um sistema.
