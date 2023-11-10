using ProdmasterProvidersService.Extensions;

WebApplication app = WebApplication.CreateBuilder(args)
    .RegisterServices()
    .Build();

app.SetupMiddleware()
    .Run();