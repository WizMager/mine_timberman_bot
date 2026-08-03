using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MineTimbermanBot.Application;
using MineTimbermanBot.Application.Callbacks;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Duels;
using MineTimbermanBot.Application.Sessions;
using MineTimbermanBot.Configuration;
using MineTimbermanBot.Features.Callbacks;
using MineTimbermanBot.Features.Commands;
using MineTimbermanBot.Infrastructure.Persistence;
using MineTimbermanBot.Telegram;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Rider/VS often run the compiled .exe without DOTNET_ENVIRONMENT=Development,
// so auto user-secrets loading is skipped. Load them explicitly for local runs.
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services
    .AddOptions<TelegramBotOptions>()
    .Bind(builder.Configuration.GetSection(TelegramBotOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Token),
        "Telegram bot token is missing. Configure TelegramBot:Token.")
    .ValidateOnStart();

var configuredConnection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=data/bot.db";
var connectionString = ResolveSqliteConnectionString(configuredConnection, builder.Environment.ContentRootPath);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services.AddSingleton<IUpdateHandler, TelegramUpdateHandler>();
builder.Services.AddScoped<IUserSessionStore, EfUserSessionStore>();
builder.Services.AddScoped<IDuelStore, EfDuelStore>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<DuelResolver>();
builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddHostedService<DuelTimeoutWorker>();

builder.Services.AddScoped<UpdateDispatcher>();
builder.Services.AddScoped<CommandDispatcher>();
builder.Services.AddScoped<CallbackDispatcher>();

builder.Services.AddScoped<IBotCommand, StartCommand>();
builder.Services.AddScoped<IBotCommand, HelpCommand>();
builder.Services.AddScoped<IBotCommand, PlayCommand>();
builder.Services.AddScoped<IBotCommand, CreateCharacterCommand>();
builder.Services.AddScoped<IBotCommand, DoWorkCommand>();
builder.Services.AddScoped<IBotCommand, RestCommand>();
builder.Services.AddScoped<IBotCommand, StatsCommand>();
builder.Services.AddScoped<IBotCommand, FightCommand>();
builder.Services.AddScoped<IBotCommand, RenameCommand>();

builder.Services.AddScoped<ICallbackHandler, FightCallbackHandler>();

var host = builder.Build();

await InitializeDatabaseAsync(host.Services);

await host.RunAsync();

static string ResolveSqliteConnectionString(string connectionString, string contentRootPath)
{
    var sqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    if (!Path.IsPathRooted(sqlite.DataSource))
    {
        sqlite.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, sqlite.DataSource));
    }

    var directory = Path.GetDirectoryName(sqlite.DataSource);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    return sqlite.ToString();
}

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}
