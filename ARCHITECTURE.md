# Arquitetura — BSourceNotifier

Detalhes técnicos sobre arquitetura, tecnologias, estrutura do projeto, canais de notificação e logging.

---

## Índice

- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Fluxo de uma notificação](#fluxo-de-uma-notificação)
- [Canais de notificação](#canais-de-notificação)
  - [Email](#email)
  - [WebSocket](#websocket)
  - [Extensibilidade](#extensibilidade)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Logging](#logging)
- [Docker](#docker)

---

## Tecnologias

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET | 8.0 | Runtime e SDK |
| ASP.NET Core | 8.0 | API REST e SignalR |
| SignalR | — | Notificações em tempo real via WebSocket |
| RazorLight | 2.3.1 | Renderização de templates HTML para e-mail |
| Serilog | 10.x | Logging estruturado |
| Swagger / Swashbuckle | 6.6.2 | Documentação interativa da API |
| Docker | — | Containerização |

---

## Arquitetura

O projeto segue **Clean Architecture** com separação em cinco camadas:

```
BSourceNotifier.sln
│
├── BSourceNotifier.API              → Host da aplicação
│   Controllers, DI, middleware, configuração.
│   Referencia: Application, Infrastructure, Contracts.
│
├── BSourceNotifier.Application      → Casos de uso (orquestração)
│   Contém os use cases e as interfaces de porta (INotificationChannel).
│   Referencia: Domain, Contracts.
│
├── BSourceNotifier.Contracts        → Contrato público
│   DTOs, commands e enums expostos para consumidores da API.
│   Sem dependências internas.
│
├── BSourceNotifier.Domain           → Núcleo do domínio
│   Entidades, value objects e enums de domínio.
│   Sem dependências externas.
│
└── BSourceNotifier.Infrastructure   → Adaptadores
    Implementações concretas dos canais (SMTP, SignalR), options de configuração.
    Referencia: Application, Domain, Contracts.
```

### Dependências entre camadas

```
API → Application → Domain
 │        │
 │        └──────→ Contracts
 │
 └→ Infrastructure → Application
                   → Domain
                   → Contracts
```

- **Domain** e **Contracts** não dependem de nenhuma outra camada.
- **Application** depende apenas de Domain e Contracts.
- **Infrastructure** implementa as interfaces definidas em Application.
- **API** orquestra tudo via injeção de dependência.

---

## Fluxo de uma notificação

```
Client
  │
  ▼
POST /api/notifications/send
  │
  ▼
NotificationsController
  │  Recebe o SendNotificationCommand
  │
  ▼
SendNotificationUseCase.ExecuteAsync()
  │  1. Mapeia os canais do DTO para enums de domínio
  │  2. Cria a entidade Notification
  │  3. Itera sobre cada canal solicitado
  │
  ├──▶ EmailNotificationChannel.SendAsync()
  │      1. Verifica se o canal está habilitado (options)
  │      2. Normaliza target.data para ExpandoObject (se JSON)
  │      3. Compila message como template Razor com data como model
  │      4. Envia o HTML via SmtpClient
  │
  └──▶ WebSocketNotificationChannel.SendAsync()
         1. Verifica se o canal está habilitado (options)
         2. Determina o grupo SignalR (group ou user-{userId})
         3. Envia evento "notification" via IHubContext<NotificationHub>
```

Cada canal é processado de forma **independente**. Se um canal falhar, os demais continuam sendo processados. Erros são logados via Serilog.

---

## Canais de notificação

### Email

| Aspecto | Detalhe |
|---------|---------|
| **Classe** | `EmailNotificationChannel` |
| **Engine de template** | [RazorLight](https://github.com/toddams/RazorLight) — compila `message` como template Razor |
| **Model** | `target.data` é passado como `@Model`. Se for `JsonElement`, é convertido para `ExpandoObject` |
| **Transporte** | `SmtpClient` do .NET com TLS/SSL configurável |
| **Configuração** | Seção `Notification:Email` do `appsettings.json` |
| **Toggle** | `Notification:Email:Enabled` — se `false`, o canal é ignorado silenciosamente |

### WebSocket

| Aspecto | Detalhe |
|---------|---------|
| **Classe** | `WebSocketNotificationChannel` |
| **Transporte** | ASP.NET Core SignalR via `IHubContext<NotificationHub>` |
| **Hub** | `NotificationHub` — ao conectar, adiciona o cliente ao grupo `user-{userId}` |
| **Grupo de entrega** | Usa `target.endpoints.webSocket.group` se informado; caso contrário, `user-{userId}` |
| **Evento** | `"notification"` com payload: `{ id, title, message, createdAt, userId, data }` |
| **Toggle** | `Notification:WebSocket:Enabled` — se `false`, o canal é ignorado silenciosamente |

### Extensibilidade

Para adicionar um novo canal de notificação:

1. **Implemente a interface** `INotificationChannel` em `BSourceNotifier.Infrastructure/Channels/`:

   ```csharp
   public sealed class SmsNotificationChannel : INotificationChannel
   {
       public NotificationChannelType ChannelType => NotificationChannelType.Sms;

       public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
       {
           // Lógica de envio SMS
       }
   }
   ```

2. **Registre no container de DI** em `Program.cs`:

   ```csharp
   builder.Services.AddScoped<INotificationChannel, SmsNotificationChannel>();
   ```

3. **Adicione options** (se necessário) em `NotificationOptions` e `appsettings.json`.

4. O canal será automaticamente invocado pelo `SendNotificationUseCase` quando incluído em `channels` no payload.

---

## Estrutura do projeto

```
bsource-notifier/
├── BSourceNotifier.sln
├── README.md                          # Documentação de uso
├── ARCHITECTURE.md                    # Este arquivo
├── docker/
│   ├── Dockerfile                     # Multi-stage build (SDK 8.0 → Runtime 8.0)
│   └── docker-compose.yml             # Orquestração com variáveis de ambiente
└── src/
    ├── BSourceNotifier.API/           # Host — controllers, DI, middleware
    │   ├── Controllers/
    │   │   └── NotificationsController.cs
    │   ├── Properties/
    │   │   └── launchSettings.json
    │   ├── Program.cs                 # Configuração de DI, middleware, rotas
    │   ├── appsettings.json           # Configurações (SMTP, canais)
    │   └── serilog.json               # Configuração do Serilog
    │
    ├── BSourceNotifier.Application/   # Casos de uso e interfaces (portas)
    │   ├── Interfaces/
    │   │   └── INotificationChannel.cs    # Interface que cada canal implementa
    │   └── UseCases/
    │       └── SendNotificationUseCase.cs # Orquestra o envio por múltiplos canais
    │
    ├── BSourceNotifier.Contracts/     # Contrato público (DTOs e commands)
    │   ├── Commands/
    │   │   └── SendNotificationCommand.cs # Payload de entrada da API
    │   ├── Enums/
    │   │   └── NotificationChannelType.cs
    │   └── Models/
    │       ├── NotificationTargetDto.cs
    │       ├── NotificationTargetEndpointsDto.cs
    │       ├── NotificationTargetEmailEndpointDto.cs
    │       └── NotificationTargetWebSocketEndpointDto.cs
    │
    ├── BSourceNotifier.Domain/        # Entidades e regras de domínio
    │   ├── Entities/
    │   │   ├── Notification.cs                    # Entidade raiz
    │   │   ├── NotificationDelivery.cs            # Status de entrega por canal
    │   │   ├── NotificationTarget.cs              # Destinatário
    │   │   ├── NotificationTargetEndpoints.cs     # Endpoints agrupados
    │   │   ├── NotificationTargetEmailEndpoint.cs # Endpoint de e-mail
    │   │   └── NotificationTargetWebSocketEndpoint.cs # Endpoint WebSocket
    │   └── Enums/
    │       ├── DeliveryStatus.cs                  # Pending, Sent, Failed
    │       └── NotificationChannelType.cs         # WebSocket, Email, Sms, etc.
    │
    └── BSourceNotifier.Infrastructure/ # Adaptadores (implementações concretas)
        ├── Channels/
        │   ├── EmailNotificationChannel.cs        # SMTP + RazorLight
        │   └── WebSocketNotificationChannel.cs    # SignalR
        ├── Options/
        │   └── NotificationOptions.cs             # Strongly-typed options
        └── SignalR/
            └── NotificationHub.cs                 # Hub com auto-join em grupo
```

---

## Logging

O logging é feito com **Serilog**, configurado via `serilog.json`.

| Aspecto | Valor |
|---------|-------|
| **Sink** | Console (formato estruturado) |
| **Nível padrão** | `Debug` |
| **Overrides** | `Microsoft` → `Warning`, `System` → `Warning` |
| **Enrich** | `FromLogContext` |
| **Property** | `Application = "BSourceNotifier"` |

**Template de saída:**

```
[HH:mm:ss LVL] Mensagem {Propriedades}
```

Exemplo:

```
[14:32:10 INF] Notification abc123 sent via Email {"NotificationId":"abc123","ChannelType":"Email"}
[14:32:10 ERR] Failed to send notification abc123 via Sms. {"NotificationId":"abc123","ChannelType":"Sms"}
```

---

## Docker

### Dockerfile (multi-stage)

| Stage | Base | Descrição |
|-------|------|-----------|
| `build` | `mcr.microsoft.com/dotnet/sdk:8.0` | Restaura, compila e publica em modo Release |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Imagem final leve, expõe porta 5000 |

### Docker Compose

O `docker-compose.yml` define o serviço `api` com todas as variáveis de ambiente configuráveis. Todas possuem valores padrão e podem ser sobrescritas via arquivo `.env` ou flags de ambiente.

```bash
# Subir com build
docker compose -f docker/docker-compose.yml up --build -d

# Parar
docker compose -f docker/docker-compose.yml down
```
