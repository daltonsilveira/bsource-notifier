using BSourceNotifier.Application.UseCases;
using BSourceNotifier.Contracts.Commands;
using MassTransit;

namespace BSourceNotifier.Application.Consumers;

public sealed class SendNotificationConsumer : IConsumer<SendNotificationCommand>
{
    private readonly SendNotificationUseCase _useCase;

    public SendNotificationConsumer(SendNotificationUseCase useCase)
    {
        _useCase = useCase;
    }

    public async Task Consume(ConsumeContext<SendNotificationCommand> context)
    {
        await _useCase.ExecuteAsync(context.Message, context.CancellationToken);
    }
}
