using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MineTimbermanBot.Application;
using MineTimbermanBot.Application.Callbacks;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Sessions;
using MineTimbermanBot.Configuration;
using MineTimbermanBot.Features.Callbacks;
using MineTimbermanBot.Features.Commands;
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

builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<TelegramBotOptions>>()
        .Value;

    return new TelegramBotClient(options.Token);
});

builder.Services.AddSingleton<IUpdateHandler, TelegramUpdateHandler>();
builder.Services.AddSingleton<IUserSessionStore, InMemoryUserSessionStore>();
builder.Services.AddHostedService<TelegramBotWorker>();

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

builder.Services.AddScoped<ICallbackHandler, SideCallbackHandler>();

await builder.Build().RunAsync();