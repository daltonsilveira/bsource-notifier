# BSourceNotifier

Serviço de notificações em .NET 8 com Clean Architecture, mensageria via RabbitMQ (MassTransit) e canais extensíveis.

## Como executar

- Configure as credenciais de RabbitMQ e SMTP em appsettings.json.
- Execute a API.

## Endpoints

- POST /api/notifications/send
- GET /health
- SignalR Hub: /hubs/notifications
