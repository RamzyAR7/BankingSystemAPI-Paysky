#region Usings
using BankingSystemAPI.Application.Authorization;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Application.Behaviors;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Application.Mapping;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Identity;
using BankingSystemAPI.Infrastructure.Jobs;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.Seeding;
using BankingSystemAPI.Infrastructure.Services;
using BankingSystemAPI.Infrastructure.Setting;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Presentation.Filters;
using BankingSystemAPI.Presentation.Middlewares;
using BankingSystemAPI.Presentation.Swagger;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.Services;
#endregion


var builder = WebApplication.CreateBuilder(args);

// Add logging filter as a service so DI can inject ILogger
builder.Services.AddScoped<RequestResponseLoggingFilter>();

builder.Services.AddControllers(options =>
{
    // register globally
    options.Filters.AddService<RequestResponseLoggingFilter>();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

#region Rate Limiting
// Policies:
// - "AuthPolicy": limits anonymous or auth attempts (login/refresh) per IP.
// - "MoneyPolicy": limits financial operations per authenticated user (falls back to IP for anonymous).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, ct) =>
    {
        // Add Retry-After header and a JSON body explaining the rejection
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.StatusCode = 429;
        var payload = JsonSerializer.Serialize(new { code = 429, message = ApiResponseMessages.Infrastructure.RateLimitExceeded });
        await context.HttpContext.Response.WriteAsync(payload, ct);
    };

    options.AddPolicy("AuthPolicy", context =>
    {
        // partition by remote IP for auth endpoints
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddPolicy("MoneyPolicy", context =>
    {
        // partition by authenticated user id if present, otherwise by IP
        var userId = context.User?.FindFirst("uid")?.Value;
        var key = !string.IsNullOrEmpty(userId) ? userId : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 20,
            TokensPerPeriod = 20,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

});
#endregion

#region Swagger
var info = new OpenApiInfo()
{
    Title = "Banking System API",
    Version = "v1",
    Description = "This API serves as the backbone of the Bank System platform, enabling seamless account and transaction management. It provides a range of endpoints for creating and managing user accounts, and tracking financial activities. Built with scalability and security in mind, the API supports robust error handling, logging, and integration with modern banking workflows.\r\n\r\nDesigned for developers, it includes detailed request and response structures, offering flexibility for integration into diverse applications. Whether for personal finance tools, corporate banking solutions, or payment platforms, this API facilitates efficient and reliable financial operations.",
    Contact = new OpenApiContact()
    {
        Name = "Ahmed Bassem Ramzy",
        Email = "rameya683@gmail.com",
    }
};

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", info);

    // Add JWT Bearer definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Include XML comments (controller and DTO comments)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Keep all controllers and order by group mapping
    options.DocInclusionPredicate((docName, apiDesc) => true);

    var groupOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Auth"] = 0,
        ["Users"] = 1,
        ["UserRoles"] = 2,
        ["Roles"] = 3,
        ["RoleClaims"] = 4,
        ["Banks"] = 5,
        ["Accounts"] = 6,
        ["CheckingAccounts"] = 7,
        ["SavingsAccounts"] = 8,
        ["AccountTransactions"] = 9,
        ["Transactions"] = 10,
        ["Currency"] = 11,
        ["Default"] = 99
    };

    options.OrderActionsBy(apiDesc =>
    {
        var group = apiDesc.GroupName ?? "Default";
        var orderKey = groupOrder.TryGetValue(group, out var v) ? v : groupOrder["Default"];
        // return string with numeric prefix to sort
        return $"{orderKey:00}_{group}_{apiDesc.RelativePath}";
    });

    // Register our operation filters
    options.OperationFilter<RateLimitResponsesOperationFilter>();
    options.OperationFilter<AuthResponsesOperationFilter>();
    options.OperationFilter<DefaultResponsesOperationFilter>();
});
#endregion

#region DbContext and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(option =>
{
    option.Password.RequiredLength = 9;
    option.Password.RequireDigit = true;
    option.Password.RequireLowercase = true;
    option.Password.RequireUppercase = true;
    option.Password.RequireNonAlphanumeric = false;
    option.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();

#endregion

// Register memory cache for repositories and services that need it
builder.Services.AddMemoryCache();

// Register ICacheService wrapper around IMemoryCache
builder.Services.AddSingleton<ICacheService, BankingSystemAPI.Infrastructure.Cache.MemoryCacheService>();

// Register MediatR (use Application assembly where handlers live)
builder.Services.AddMediatR(typeof(MappingProfile).Assembly);

// Register FluentValidation (validators live in Application assembly)
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// Register specific validators with dependencies
builder.Services.AddScoped<CreateUserCommandValidator>(provider =>
    new CreateUserCommandValidator(provider.GetService<ICurrentUserService>()));

// Register MediatR validation behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

#region Register Repositories and Unit of Work
// Register Optimized Unit of Work
builder.Services.AddScoped<IUnitOfWork, BankingSystemAPI.Infrastructure.UnitOfWork.UnitOfWork>();

// Register High-Performance Repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAccountTransactionRepository, AccountTransactionRepository>();
builder.Services.AddScoped<IInterestLogRepository, InterestLogRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IBankRepository, BankRepository>();
#endregion

#region Register Services
// Identity Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRolesService, UserRolesService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoleClaimsService, RoleClaimsService>();

// Register IHttpContextAccessor for services that need access to the current user
builder.Services.AddHttpContextAccessor();

// Register Authorization Services (they already use ICurrentUserService which extracts JWT claims)
builder.Services.AddScoped<IAccountAuthorizationService, AccountAuthorizationService>();
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();

// Register DB capabilities and TransactionAuthorizationService
builder.Services.Configure<BankingSystemAPI.Application.Interfaces.Infrastructure.DbCapabilitiesOptions>(builder.Configuration.GetSection("DbCapabilities"));
builder.Services.AddSingleton<BankingSystemAPI.Application.Interfaces.Infrastructure.IDbCapabilities, BankingSystemAPI.Infrastructure.Setting.DbCapabilities>();
builder.Services.AddScoped<ITransactionAuthorizationService, TransactionAuthorizationService>();

// Register CurrentUserService helper (extracts JWT claims)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// Register TransactionHelperService
builder.Services.AddScoped<ITransactionHelperService, TransactionHelperService>();
// Register ScopeResolver
builder.Services.AddScoped<IScopeResolver, ScopeResolver>();

// Register error response factory
builder.Services.AddSingleton<IErrorResponseFactory, ErrorResponseFactory>();

// Register success message provider
builder.Services.AddSingleton<ISuccessMessageProvider, SuccessMessageProvider>();
#endregion

#region Register Job Services
builder.Services.AddHostedService<RefreshTokenCleanupJob>();
builder.Services.AddHostedService<AddInterestJob>();
#endregion

#region Register AutoMapper
// Register only the unified mapping profile from Application layer
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});
#endregion

#region JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwt = jwtSection.Get<JwtSettings>();
    if (jwt == null || string.IsNullOrWhiteSpace(jwt.Key) || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
    {
        throw new InvalidOperationException("Missing or invalid JWT configuration. Ensure the 'Jwt' section contains non-empty 'Key', 'Issuer' and 'Audience'.");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
    };

    // Validate security stamp included in token matches current user security stamp
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var uid = ctx.Principal?.FindFirst("uid")?.Value;
            var tokenStamp = ctx.Principal?.FindFirst("sst")?.Value;

            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(tokenStamp))
            {
                ctx.Fail(AuthorizationConstants.ErrorMessages.InvalidToken);
                return;
            }

            var user = await userManager.FindByIdAsync(uid);
            if (user == null)
            {
                ctx.Fail(ApiResponseMessages.Validation.UserNotFound);
                return;
            }

            var currentStamp = await userManager.GetSecurityStampAsync(user);
            if (!string.Equals(tokenStamp, currentStamp, StringComparison.Ordinal))
            {
                ctx.Fail(AuthorizationConstants.ErrorMessages.InvalidToken);
                return;
            }
        }
    };
});
#endregion

var app = builder.Build();

#region Seeding Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        // 1) Ensure role seeding completes
        await IdentitySeeding.SeedingRoleAsync(roleManager);

        // 2) Verify roles were created successfully before proceeding
        var rolesExist = await roleManager.Roles.AnyAsync();
        if (!rolesExist)
        {
            logger.LogWarning(ApiResponseMessages.Logging.SeedingNoRolesFound);
        }
        else
        {
            await IdentitySeeding.SeedingUsersAsync(userManager, roleManager, db);
        }
        // Seed currencies
        await CurrencySeeding.SeedAsync(db);

        // Seed banks
        await BankSeeding.SeedAsync(db);

        Console.ForegroundColor = ConsoleColor.Green;
        logger.LogInformation(ApiResponseMessages.Logging.SeedingCompleted);
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        logger.LogError(ex, ApiResponseMessages.Logging.SeedingFailed, ex.Message);
        Console.ResetColor();
    }
}
#endregion

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Add timing middleware after exception middleware so exceptions are still captured
app.UseMiddleware<RequestTimingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Enable rate limiting middleware globally (policies selected via EnableRateLimiting attribute)
app.UseRateLimiter();

app.MapControllers();

// After building services, expose ServiceProvider for legacy constructors
app.Run();

