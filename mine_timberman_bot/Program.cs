using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Rider/VS often run the compiled .exe without ASPNETCORE_ENVIRONMENT=Development,
// so auto user-secrets loading is skipped. Load them explicitly for local runs.
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services
    .AddOptions<TelegramBotOptions>()
    .Bind(builder.Configuration.GetSection(TelegramBotOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Token),
        "Telegram bot token is missing. Configure TelegramBot:Token.")
    .Validate(
        options => !options.UseWebhook || !string.IsNullOrWhiteSpace(options.SecretToken),
        "TelegramBot:SecretToken is required when TelegramBot:WebhookUrl is set.")
    .Validate(
        options => !options.UseWebhook || IsHttpsUrl(options.WebhookUrl),
        "TelegramBot:WebhookUrl must be a valid HTTPS URL.")
    .Validate(
        options => !options.UseWebhook || IsValidSecretToken(options.SecretToken),
        "TelegramBot:SecretToken must be 1-256 characters: A-Z, a-z, 0-9, _, -.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Configure ConnectionStrings:Default.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddHttpClient("telegram_bot")
    .RemoveAllLoggers()
    .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
        return new TelegramBotClient(options.Token, httpClient);
    });

builder.Services.AddSingleton<BotIdentity>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BotIdentity>());
builder.Services.AddSingleton<IUpdateHandler, TelegramUpdateHandler>();
builder.Services.AddScoped<IUserSessionStore, EfUserSessionStore>();
builder.Services.AddScoped<IDuelStore, EfDuelStore>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<DuelResolver>();
builder.Services.AddHostedService<TelegramWebhookService>();
builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddHostedService<DuelTimeoutWorker>();

builder.Services.AddScoped<UpdateDispatcher>();
builder.Services.AddScoped<CommandDispatcher>();
builder.Services.AddScoped<CallbackDispatcher>();

builder.Services.AddScoped<IBotCommand, StartCommand>();
builder.Services.AddScoped<IBotCommand, HelpCommand>();
builder.Services.AddScoped<IBotCommand, CreateCharacterCommand>();
builder.Services.AddScoped<IBotCommand, DoWorkCommand>();
builder.Services.AddScoped<IBotCommand, RestCommand>();
builder.Services.AddScoped<IBotCommand, StatsCommand>();
builder.Services.AddScoped<IBotCommand, FightCommand>();
builder.Services.AddScoped<IBotCommand, RenameCommand>();

builder.Services.AddScoped<ICallbackHandler, FightCallbackHandler>();

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

var botOptions = app.Services.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
if (botOptions.UseWebhook)
{
    var webhookPath = new Uri(botOptions.WebhookUrl).AbsolutePath;
    if (string.IsNullOrWhiteSpace(webhookPath) || webhookPath == "/")
    {
        webhookPath = "/telegram/webhook";
    }

    app.MapPost(webhookPath, HandleTelegramWebhookAsync);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

static async Task<IResult> HandleTelegramWebhookAsync(
    Update update,
    HttpRequest request,
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    IOptions<TelegramBotOptions> options,
    CancellationToken cancellationToken)
{
    var expectedSecret = options.Value.SecretToken;
    if (!string.Equals( request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString(), expectedSecret, StringComparison.Ordinal))
    {
        return Results.Forbid();
    }

    try
    {
        await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
    }
    catch (Exception exception)
    {
        await updateHandler.HandleErrorAsync(
            botClient,
            exception,
            HandleErrorSource.HandleUpdateError,
            cancellationToken);
    }

    return Results.Ok();
}

static bool IsHttpsUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

static bool IsValidSecretToken(string secretToken)
{
    if (secretToken.Length is < 1 or > 256)
    {
        return false;
    }

    return secretToken.All(static ch => char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-');
}

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}