
using ProvidersDomain.Services;

namespace ProdmasterProvidersService.Services.Hosted
{
    public class UpdateStandartsHostedService : IHostedService
    {
        private readonly ILogger<UpdateStandartsHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        public UpdateStandartsHostedService(ILogger<UpdateStandartsHostedService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Не блокируем поток выполнения: StartAsync должен запустить выполнение фоновой задачи и завершить работу
            UpdateStocks(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task UpdateStocks(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var updateService = scope.ServiceProvider.GetRequiredService<IUpdateStandartsService>();
                    await updateService.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to update stocks\nError: {ex}");
                }

                await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Если нужно дождаться завершения очистки, но контролировать время, то стоит предусмотреть в контракте использование CancellationToken
            //await someService.DoSomeCleanupAsync(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
