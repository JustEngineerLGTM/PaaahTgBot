using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PaaahBot;

public class BotWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private TelegramBotClient? _botClient;
    private readonly Regex _regex = new(@"^П[аАa]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public BotWorker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botToken = _configuration["BotConfiguration:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentNullException(nameof(botToken), "Bot token is not configured.");
        }

        _botClient = new TelegramBotClient(botToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMe(stoppingToken);
        Console.WriteLine($"Start listening for @{me.Username}");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        if (_regex.IsMatch(messageText))
        {
            Console.WriteLine($"Received a '{messageText}' message in chat {message.Chat.Id}.");

            const string picturesDirectory = "PaaahPictures";
            if (!Directory.Exists(picturesDirectory))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Папка с картинками 'PaaahPictures' не найдена!",
                    cancellationToken: cancellationToken);
                return;
            }

            var imageFiles = Directory.GetFiles(picturesDirectory);
            if (imageFiles.Length == 0)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "В папке 'PaaahPictures' нет картинок!",
                    cancellationToken: cancellationToken);
                return;
            }

            var randomImage = imageFiles[Random.Shared.Next(imageFiles.Length)];
            
            await using var stream = File.OpenRead(randomImage);
            await botClient.SendPhoto(
                chatId: message.Chat.Id,
                photo: new InputFileStream(stream, Path.GetFileName(randomImage)),
                cancellationToken: cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram API Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
