# BSourceNotifier

Serviço de notificações em .NET 8 com Clean Architecture, mensageria via RabbitMQ (MassTransit) e canais extensíveis.

## Como executar

- Configure as credenciais de RabbitMQ e SMTP em appsettings.json.
- Execute a API.

## Endpoints

- POST /api/notifications/send
- GET /health
- SignalR Hub: /hubs/notifications

## Exemplo de payload

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
  },
  "metadata": {
    "tenant": "acme"
  }
}
```

No canal de Email, o HTML é renderizado com Razor usando `target.data` como model.
