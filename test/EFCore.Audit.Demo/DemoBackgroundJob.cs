namespace EFCore.Audit.Demo;

public class DemoBackgroundJob : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;

    public DemoBackgroundJob(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("BACKGROUND SERVICE STARTED");

        await Task.Delay(5000, stoppingToken);

        using var scope = scopeFactory.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<Service>();

        Console.WriteLine("JOB STARTED");

        await service.CreatePostAsync(stoppingToken);

        Console.WriteLine("JOB FINISHED");
    }
}
