# BSourceNotifier

Serviço de notificações multicanal construído com **.NET 8** e **Clean Architecture**.  
Expõe uma API REST para disparo de notificações e um hub **SignalR** para entrega em tempo real via WebSocket.

---

## Índice

- [Visão geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Pré-requisitos](#pré-requisitos)
- [Como executar](#como-executar)
  - [Local](#local)
  - [Docker](#docker)
- [Configuração](#configuração)
  - [SMTP / E-mail](#smtp--e-mail)
  - [Canais](#canais)
- [API](#api)
  - [Endpoints](#endpoints)
  - [Payload de envio](#payload-de-envio)
  - [Exemplo completo](#exemplo-completo)
- [SignalR](#signalr)
  - [Conexão](#conexão)
  - [Eventos](#eventos)
- [Canais de notificação](#canais-de-notificação)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Logging](#logging)

---

## Visão geral

O **BSourceNotifier** é um microserviço responsável pelo disparo centralizado de notificações. Ele recebe um comando via API REST, processa o conteúdo (com suporte a templates Razor para e-mail) e distribui a notificação pelos canais solicitados.

**Canais implementados:**

| Canal | Status | Descrição |
|-------|:------:|-----------|
| **Email** | ✅ Ativo | Envio via SMTP com templates HTML/Razor |
| **WebSocket** | ✅ Ativo | Entrega em tempo real via SignalR |
| SMS | 🔜 Planejado | — |
| Telegram | 🔜 Planejado | — |
| WhatsApp | 🔜 Planejado | — |

---

## Arquitetura

O projeto segue **Clean Architecture** com separação em cinco camadas:

```
BSourceNotifier.sln
├── BSourceNotifier.API              → Host da aplicação (controllers, configuração, DI)
├── BSourceNotifier.Application      → Casos de uso e interfaces de porta (orquestração)
├── BSourceNotifier.Contracts        → DTOs, commands e enums compartilhados (contrato público)
├── BSourceNotifier.Domain           → Entidades, enums e regras de domínio
└── BSourceNotifier.Infrastructure   → Implementações de canais (SMTP, SignalR), options
```

**Fluxo de uma notificação:**

```
Client → POST /api/notifications/send
           ↓
     NotificationsController
           ↓
     SendNotificationUseCase
           ↓ (para cada canal)
     INotificationChannel
       ├── EmailNotificationChannel   → Razor + SMTP
       └── WebSocketNotificationChannel → SignalR Hub
```

---

## Tecnologias

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET | 8.0 | Runtime e SDK |
| ASP.NET Core | 8.0 | API REST e SignalR |
| SignalR | — | Notificações em tempo real |
| RazorLight | 2.3.1 | Renderização de templates HTML de e-mail |
| Serilog | 10.x | Logging estruturado |
| Swagger / Swashbuckle | 6.6.2 | Documentação da API |
| Docker | — | Containerização |

---

## Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Opcional) [Docker](https://docs.docker.com/get-docker/) e Docker Compose

---

## Como executar

### Local

1. Clone o repositório:

   ```bash
   git clone https://github.com/seu-org/bsource-notifier.git
   cd bsource-notifier
   ```

2. Configure as credenciais de SMTP em `src/BSourceNotifier.API/appsettings.json` (veja [Configuração](#configuração)).

3. Restaure, compile e execute:

   ```bash
   dotnet build BSourceNotifier.sln
   dotnet run --project src/BSourceNotifier.API
   ```

4. Acesse:
   - Swagger UI: `http://localhost:5000/swagger`
   - Health check: `http://localhost:5000/health`

### Docker

```bash
cd docker
docker compose up --build -d
```

A API ficará disponível em `http://localhost:5000`.

As variáveis de ambiente podem ser configuradas no `docker-compose.yml` ou via arquivo `.env` na pasta `docker/`. Veja os detalhes na seção de [Configuração](#configuração).

---

## Configuração

Toda a configuração fica na seção `Notification` do `appsettings.json` ou via variáveis de ambiente.

### SMTP / E-mail

| Configuração | Variável de ambiente | Padrão | Descrição |
|-------------|----------------------|--------|-----------|
| `Notification:Email:Enabled` | `EMAIL_ENABLED` | `true` | Habilita/desabilita o canal de e-mail. |
| `Notification:Email:From` | `EMAIL_FROM` | — | Endereço de e-mail remetente. |
| `Notification:Email:Smtp:Host` | `SMTP_HOST` | `smtp.gmail.com` | Servidor SMTP. |
| `Notification:Email:Smtp:Port` | `SMTP_PORT` | `587` | Porta SMTP. |
| `Notification:Email:Smtp:Username` | `SMTP_USERNAME` | — | Usuário para autenticação SMTP. |
| `Notification:Email:Smtp:Password` | `SMTP_PASSWORD` | — | Senha ou app password SMTP. |
| `Notification:Email:Smtp:EnableSsl` | `SMTP_ENABLE_SSL` | `true` | Usar TLS/SSL na conexão SMTP. |

### Canais

| Configuração | Variável de ambiente | Padrão | Descrição |
|-------------|----------------------|--------|-----------|
| `Notification:WebSocket:Enabled` | `WEBSOCKET_ENABLED` | `true` | Habilita/desabilita o canal WebSocket. |
| `Notification:Sms:Enabled` | `SMS_ENABLED` | `false` | Reservado para implementação futura. |
| `Notification:Telegram:Enabled` | `TELEGRAM_ENABLED` | `false` | Reservado para implementação futura. |
| `Notification:WhatsApp:Enabled` | `WHATSAPP_ENABLED` | `false` | Reservado para implementação futura. |

---

## API

### Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/notifications/send` | Envia uma notificação pelos canais especificados. |
| `GET` | `/health` | Health check da aplicação. |
| — | `/hubs/notifications` | Hub SignalR para conexões WebSocket. |
| `GET` | `/swagger` | Documentação interativa (somente em Development). |

### Payload de envio

`POST /api/notifications/send`

#### Corpo da requisição (`SendNotificationCommand`)

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `title` | `string` | Sim | Título da notificação. Exibido ao destinatário e enviado no evento SignalR. |
| `message` | `string` | Sim | Corpo da notificação. Para o canal de e-mail, aceita HTML com sintaxe **Razor** (ex.: `@Model.Name`). Para WebSocket, é enviado como texto plano no evento. |
| `channels` | `string[]` | Sim | Canais pelos quais a notificação será enviada. Valores aceitos: `Email`, `WebSocket`, `Sms`, `Telegram`, `WhatsApp`. Cada canal listado será acionado de forma independente. |
| `target` | `object` | Sim | Dados do destinatário e configurações de entrega por canal. Veja detalhes abaixo. |

#### `target`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `userId` | `string` | Sim | Identificador único do usuário destinatário. Usado pelo SignalR como fallback para o grupo de entrega (`user-{userId}`). |
| `endpoints` | `object` | Sim | Contém os endpoints de entrega específicos de cada canal. Apenas os endpoints dos canais listados em `channels` precisam ser preenchidos. |
| `data` | `object` | Não | Objeto dinâmico de dados contextuais. **No e-mail:** utilizado como model Razor — as propriedades ficam acessíveis via `@Model.Prop` no template HTML. **No WebSocket:** enviado integralmente no evento SignalR para que o cliente front-end trate lógicas e regras de negócio no lado do cliente (ex.: exibir detalhes, navegar para uma tela, atualizar estado local). |

#### `target.endpoints`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `email` | `object` | Condicional | Endpoint de e-mail. **Obrigatório** quando `Email` estiver em `channels`. |
| `email.to` | `string` | Sim | Endereço de e-mail do destinatário. |
| `webSocket` | `object` | Condicional | Endpoint WebSocket/SignalR. **Obrigatório** quando `WebSocket` estiver em `channels`. |
| `webSocket.group` | `string` | Não | Nome do grupo SignalR para entrega direcionada. Se omitido, o sistema usa o grupo padrão `user-{userId}`. |

#### Resposta

| Status | Descrição |
|--------|-----------|
| `202 Accepted` | A notificação foi aceita e será processada. |

### Exemplo completo

```json
{
  "title": "Pedido aprovado",
  "message": "<h1>Olá @Model.Name</h1><p>Seu pedido @Model.OrderId foi aprovado.</p>",
  "channels": ["Email", "WebSocket"],
  "target": {
    "userId": "user-123",
    "endpoints": {
      "email": {
        "to": "cliente@empresa.com"
      },
      "webSocket": {
        "group": "user-user-123"
      }
    },
    "data": {
      "name": "João",
      "orderId": "A-1020"
    }
  }
}
```

#### O que acontece em cada canal

- **Email:** o campo `message` é compilado como template Razor com `target.data` como model. O HTML resultante é enviado via SMTP para o endereço em `target.endpoints.email.to`.
- **WebSocket:** a notificação é enviada como evento SignalR `"notification"` para o grupo especificado (ou `user-{userId}`). O payload do evento inclui `id`, `title`, `message`, `createdAt`, `userId` e `data`.

---

## SignalR

### Conexão

O hub SignalR está disponível em `/hubs/notifications`.

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/notifications?userId=user-123")
  .build();

await connection.start();
```

Ao conectar, o servidor adiciona o cliente automaticamente ao grupo `user-{userId}` com base no query parameter `userId` ou na identidade autenticada.

### Eventos

| Evento | Payload | Descrição |
|--------|---------|-----------|
| `notification` | `{ id, title, message, createdAt, userId, data }` | Notificação recebida. O campo `data` contém os mesmos dados enviados em `target.data`, permitindo que o front-end execute lógicas específicas (roteamento, atualização de estado, exibição contextual etc.). |

---

## Canais de notificação

### Email

- **Engine de template:** [RazorLight](https://github.com/toddams/RazorLight) compila o campo `message` como template Razor.
- **Model:** o objeto `target.data` é passado como `@Model`. Se for um JSON, é automaticamente convertido para `ExpandoObject` para compatibilidade com Razor.
- **Transporte:** SMTP via `SmtpClient` do .NET.
- **Configuração:** seção `Notification:Email` do `appsettings.json`.

### WebSocket

- **Transporte:** ASP.NET Core SignalR.
- **Grupos:** usa `target.endpoints.webSocket.group` se informado; caso contrário, `user-{userId}`.
- **Payload:** envia o evento `"notification"` com os campos `id`, `title`, `message`, `createdAt`, `userId` e `data`.
- **Configuração:** seção `Notification:WebSocket` do `appsettings.json`.

### Extensibilidade

Para adicionar um novo canal:

1. Crie uma classe que implemente `INotificationChannel` em `BSourceNotifier.Infrastructure/Channels/`.
2. Registre-a no container de DI em `Program.cs`:
   ```csharp
   builder.Services.AddScoped<INotificationChannel, SeuNovoChannel>();
   ```
3. O canal já será automaticamente invocado quando incluído em `channels` no payload.

---

## Estrutura do projeto

```
bsource-notifier/
├── BSourceNotifier.sln
├── README.md
├── docker/
│   ├── Dockerfile                     # Multi-stage build (SDK → Runtime)
│   └── docker-compose.yml             # Orquestração com variáveis de ambiente
└── src/
    ├── BSourceNotifier.API/           # Host — controllers, DI, middleware
    │   ├── Controllers/
    │   │   └── NotificationsController.cs
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── serilog.json
    ├── BSourceNotifier.Application/   # Casos de uso e interfaces (portas)
    │   ├── Interfaces/
    │   │   └── INotificationChannel.cs
    │   └── UseCases/
    │       └── SendNotificationUseCase.cs
    ├── BSourceNotifier.Contracts/     # DTOs e commands (contrato público)
    │   ├── Commands/
    │   │   └── SendNotificationCommand.cs
    │   ├── Enums/
    │   │   └── NotificationChannelType.cs
    │   └── Models/
    │       ├── NotificationTargetDto.cs
    │       ├── NotificationTargetEndpointsDto.cs
    │       ├── NotificationTargetEmailEndpointDto.cs
    │       └── NotificationTargetWebSocketEndpointDto.cs
    ├── BSourceNotifier.Domain/        # Entidades e regras de domínio
    │   ├── Entities/
    │   │   ├── Notification.cs
    │   │   ├── NotificationDelivery.cs
    │   │   ├── NotificationTarget.cs
    │   │   ├── NotificationTargetEndpoints.cs
    │   │   ├── NotificationTargetEmailEndpoint.cs
    │   │   └── NotificationTargetWebSocketEndpoint.cs
    │   └── Enums/
    │       ├── DeliveryStatus.cs
    │       └── NotificationChannelType.cs
    └── BSourceNotifier.Infrastructure/ # Adaptadores (canais, SignalR, options)
        ├── Channels/
        │   ├── EmailNotificationChannel.cs
        │   └── WebSocketNotificationChannel.cs
        ├── Options/
        │   └── NotificationOptions.cs
        └── SignalR/
            └── NotificationHub.cs
```

---

## Logging

O logging é feito com **Serilog** e configurado via `serilog.json`.

- **Sink:** Console (formato estruturado)
- **Nível padrão:** `Debug`
- **Overrides:** `Microsoft` e `System` em `Warning`
- **Template de saída:**
  ```
  [HH:mm:ss LVL] Mensagem {Propriedades}
  ```

---

## Licença

Uso interno — BSource.
