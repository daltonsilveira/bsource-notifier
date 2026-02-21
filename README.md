# BSourceNotifier

Serviço de notificações multicanal que centraliza o disparo de notificações para diferentes canais (e-mail, WebSocket, SMS, Telegram, WhatsApp) através de uma única API REST.

> Para detalhes sobre arquitetura, tecnologias, estrutura do projeto e logging, consulte o [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Índice

- [O que é](#o-que-é)
- [Canais disponíveis](#canais-disponíveis)
- [Como executar](#como-executar)
- [Configuração](#configuração)
- [Documentação da API](#documentação-da-api)
  - [Endpoints](#endpoints)
  - [Payload — SendNotificationCommand](#payload--sendnotificationcommand)
  - [Resposta](#resposta)
- [SignalR — Notificações em tempo real](#signalr--notificações-em-tempo-real)
- [Exemplos de uso](#exemplos-de-uso)

---

## O que é

O **BSourceNotifier** recebe um comando de notificação via `POST /api/notifications/send`, processa o conteúdo e distribui a mensagem pelos canais solicitados. Cada canal opera de forma independente: se um falhar, os demais continuam sendo processados.

Principais capacidades:

- **Templates Razor para e-mail** — o corpo da notificação aceita HTML com sintaxe Razor (`@Model.Prop`), renderizado dinamicamente com dados do destinatário.
- **Entrega em tempo real via SignalR** — notificações são enviadas como eventos WebSocket para que o front-end trate em tempo real.
- **Extensível** — novos canais podem ser adicionados implementando uma única interface.

---

## Canais disponíveis

| Canal | Status | Descrição |
|-------|:------:|-----------|
| **Email** | ✅ Ativo | Envio de HTML via SMTP com suporte a templates Razor |
| **WebSocket** | ✅ Ativo | Entrega em tempo real via SignalR |
| SMS | 🔜 Planejado | — |
| Telegram | 🔜 Planejado | — |
| WhatsApp | 🔜 Planejado | — |

---

## Como executar

### Local

```bash
# 1. Clone o repositório
git clone https://github.com/seu-org/bsource-notifier.git
cd bsource-notifier

# 2. Configure SMTP em src/BSourceNotifier.API/appsettings.json (veja Configuração)

# 3. Compile e execute
dotnet build BSourceNotifier.sln
dotnet run --project src/BSourceNotifier.API
```

Acesse: `http://localhost:5000/swagger`

### Docker

```bash
cd docker
docker compose up --build -d
```

A API ficará disponível em `http://localhost:5000`. Variáveis de ambiente podem ser configuradas no `docker-compose.yml` ou via arquivo `.env` na pasta `docker/`.

---

## Configuração

Toda a configuração fica na seção `Notification` do `appsettings.json` ou via variáveis de ambiente (ideal para Docker).

### E-mail / SMTP

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
| `Notification:WebSocket:Enabled` | `WEBSOCKET_ENABLED` | `true` | Habilita/desabilita o canal WebSocket/SignalR. |
| `Notification:Sms:Enabled` | `SMS_ENABLED` | `false` | Reservado para implementação futura. |
| `Notification:Telegram:Enabled` | `TELEGRAM_ENABLED` | `false` | Reservado para implementação futura. |
| `Notification:WhatsApp:Enabled` | `WHATSAPP_ENABLED` | `false` | Reservado para implementação futura. |

---

## Documentação da API

### Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/notifications/send` | Envia uma notificação pelos canais especificados. |
| `GET` | `/health` | Health check da aplicação. |
| — | `/hubs/notifications` | Hub SignalR para conexões WebSocket. |
| `GET` | `/swagger` | Documentação interativa (somente em Development). |

### Payload — `SendNotificationCommand`

`POST /api/notifications/send` — `Content-Type: application/json`

#### Campos do corpo

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `title` | `string` | Sim | Título da notificação. Exibido ao destinatário e enviado no evento SignalR. |
| `message` | `string` | Sim | Corpo da notificação. Para e-mail, aceita HTML com sintaxe **Razor** (ex.: `@Model.Name`). Para WebSocket, é enviado como texto no evento. |
| `channels` | `string[]` | Sim | Canais de envio. Valores: `Email`, `WebSocket`, `Sms`, `Telegram`, `WhatsApp`. Cada canal é acionado de forma independente. |
| `target` | `object` | Sim | Dados do destinatário e configurações de entrega. Detalhes abaixo. |

#### `target`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `userId` | `string` | Sim | Identificador único do usuário destinatário. Usado pelo SignalR como grupo padrão de entrega (`user-{userId}`). |
| `endpoints` | `object` | Sim | Endpoints de entrega por canal. Preencha apenas os endpoints dos canais informados em `channels`. |
| `data` | `object` | Não | Dados dinâmicos contextuais. **E-mail:** usado como model Razor — propriedades acessíveis via `@Model.Prop` no template HTML. **WebSocket:** enviado integralmente no evento SignalR para que o front-end trate lógicas e regras de negócio (ex.: exibir detalhes, navegar para uma tela, atualizar estado local). |

#### `target.endpoints`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|:-----------:|-----------|
| `email` | `object` | Condicional | Endpoint de e-mail. **Obrigatório** quando `Email` estiver em `channels`. |
| `email.to` | `string` | Sim | Endereço de e-mail do destinatário. |
| `webSocket` | `object` | Condicional | Endpoint SignalR. **Obrigatório** quando `WebSocket` estiver em `channels`. |
| `webSocket.group` | `string` | Não | Grupo SignalR para entrega direcionada. Se omitido, usa o grupo padrão `user-{userId}`. |

### Resposta

| Status | Descrição |
|--------|-----------|
| `202 Accepted` | Notificação aceita para processamento. |

---

## SignalR — Notificações em tempo real

### Conexão

Hub disponível em `/hubs/notifications`. O servidor adiciona o cliente ao grupo `user-{userId}` automaticamente.

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/notifications?userId=user-123")
  .build();

connection.on("notification", (payload) => {
  console.log("Notificação recebida:", payload);
  // payload = { id, title, message, createdAt, userId, data }
});

await connection.start();
```

### Evento `notification`

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `id` | `string` | ID único da notificação. |
| `title` | `string` | Título da notificação. |
| `message` | `string` | Corpo da notificação. |
| `createdAt` | `string` | Data/hora de criação (ISO 8601). |
| `userId` | `string` | ID do usuário destinatário. |
| `data` | `object` | Dados contextuais enviados em `target.data` — use para lógicas de UI, roteamento ou atualização de estado. |

---

## Exemplos de uso

### 1. Notificação apenas por e-mail

```json
{
  "title": "Bem-vindo!",
  "message": "<h1>Olá @Model.Name</h1><p>Sua conta foi criada com sucesso.</p>",
  "channels": ["Email"],
  "target": {
    "userId": "user-456",
    "endpoints": {
      "email": {
        "to": "novo-usuario@empresa.com"
      }
    },
    "data": {
      "name": "Maria"
    }
  }
}
```

O HTML é compilado com Razor: `@Model.Name` será substituído por `"Maria"`.

### 2. Notificação apenas por WebSocket

```json
{
  "title": "Nova mensagem",
  "message": "Você recebeu uma nova mensagem no chat.",
  "channels": ["WebSocket"],
  "target": {
    "userId": "user-789",
    "endpoints": {
      "webSocket": {
        "group": "user-user-789"
      }
    },
    "data": {
      "chatId": "chat-100",
      "senderId": "user-001",
      "preview": "Oi, tudo bem?"
    }
  }
}
```

O front-end recebe o evento `notification` com o campo `data` contendo `chatId`, `senderId` e `preview`, podendo abrir a tela de chat automaticamente.

### 3. Notificação multicanal (e-mail + WebSocket)

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

- **E-mail:** renderiza o template Razor e envia o HTML para `cliente@empresa.com`.
- **WebSocket:** envia o evento SignalR com `data` para o grupo `user-user-123`.

### 4. WebSocket sem grupo específico (usa userId como fallback)

```json
{
  "title": "Atualização de status",
  "message": "Seu documento foi processado.",
  "channels": ["WebSocket"],
  "target": {
    "userId": "user-555",
    "endpoints": {
      "webSocket": {}
    },
    "data": {
      "documentId": "doc-200",
      "status": "processed"
    }
  }
}
```

Como `group` não foi informado, a notificação é enviada para o grupo `user-user-555` (padrão baseado no `userId`).

---

## Licença

Uso interno — BSource.
