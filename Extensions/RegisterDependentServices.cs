using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ProdmasterProvidersService.Database;
using ProdmasterProvidersService.Repositories;
using ProdmasterProvidersService.Services;
using ProvidersDomain.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ProdmasterProvidersService.Services.Hosted;
using Newtonsoft.Json.Converters;

namespace ProdmasterProvidersService.Extensions
{
    public static class RegisterDependentServices
    {
        public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
        {
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

            return builder;
        }
    }
}
