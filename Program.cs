using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using ProdmasterProvidersService.Database;
using ProdmasterProvidersService.Repositories;
using ProdmasterProvidersService.Services;
using ProdmasterProvidersService.Services.Hosted;
using ProvidersDomain.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);



//Services
{
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddScoped<ISpecificationService, SpecificationService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ISpecificationApiService, SpecificationApiService>();
    builder.Services.AddScoped<IUpdateProvidersService, UpdateProvidersService>();
    builder.Services.AddScoped<IHomeService, HomeService>();
    builder.Services.AddHostedService<UpdateStandartsHostedService>();
    builder.Services.AddHttpClient<IUpdateStandartsService, UpdateStandartsService>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));
}

//Repository
{
    builder.Services.AddScoped<UserRepository>();
    builder.Services.AddScoped<StandartRepository>();
    builder.Services.AddScoped<ProductRepository>();
    builder.Services.AddScoped<CountryRepository>();
    builder.Services.AddScoped<ManufacturerRepository>();
    builder.Services.AddScoped<SpecificationRepository>();
    builder.Services.AddScoped<OrderRepository>(); 
}

//Database
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContext<UserContext>(ConfigureUserContextConnection);
}

//Authentication
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => //CookieAuthenticationOptions
        {
            options.LoginPath = new PathString("/account/login");
        });
}

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.Formatting = Formatting.Indented;
        options.SerializerSettings.Converters.Add(new StringEnumConverter
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        });
    }
);
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Prodmaster providers service API - V1",
            Version = "v1"
        }
     );
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
    xmlFiles.ForEach(xmlFile => c.IncludeXmlComments(xmlFile));
});

void ConfigureUserContextConnection(DbContextOptionsBuilder options)
{
    String connectionStringTag = "UserContext";
    #if DEBUG
        connectionStringTag = "UserContextDev";
    #endif
    options.UseLazyLoadingProxies()
        .UseNpgsql(builder.Configuration.GetConnectionString(connectionStringTag)).ConfigureWarnings(w => w.Ignore(CoreEventId.LazyLoadOnDisposedContextWarning))
        .EnableSensitiveDataLogging();
}

var app = builder.Build();

#if DEBUG
app.UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
#endif

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Handle Lets Encrypt Route (before MVC processing!)
app.UseRouter(r =>
{
    r.MapGet(".well-known/acme-challenge/{id}", async (request, response, routeData) =>
    {
        var id = routeData.Values["id"] as string;
        var file = Path.Combine(app.Environment.WebRootPath, ".well-known", "acme-challenge", id);
        await response.SendFileAsync(file);
    });
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();
