using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace PaaahBot;

public partial class BotWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private TelegramBotClient? _botClient;

    [GeneratedRegex("^П[аАa]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex Regex { get; }

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
            AllowedUpdates = new[]
            {
                UpdateType.Message, UpdateType.InlineQuery
            }
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

    private async Task OnInlineQueryReceived(InlineQuery inlineQuery, ITelegramBotClient botClient)
    {
        var fileIds = new[]
        {
            "AgACAgIAAxkBAAEBeS9ozG3KwLppvha4wHvmArMNFePAaAACuvkxG7V6YErWSDPl1QYiXgEAAwIAA3gAAzYE",
            "AgACAgIAAxkBAAEBeStozG3EAAEnwfqgU4qPSrTrwkDuoAwAAqP5MRu1emBKAspCmbOD-ZoBAAMCAAN5AAM2BA",
            "AgACAgIAAxkBAAEBeSJozG2Pl68NVN-bQma93nGKMR26wQACufkxG7V6YErUx0eWsjoUjwEAAwIAA3kAAzYE"
        };

        if (fileIds.Length == 0)
            return;

        var results = new List<InlineQueryResultCachedPhoto>();
        var counter = 0;
        foreach (var fileId in fileIds)
        {
            results.Add(new InlineQueryResultCachedPhoto( $"{counter}", fileId));
            counter++;
        }

        await botClient.AnswerInlineQuery(
            inlineQuery.Id,
            results,
            isPersonal: true,
            cacheTime: 0
        );
    }


    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.InlineQuery && update.InlineQuery != null)
        {
            await OnInlineQueryReceived(update.InlineQuery, botClient);
        }
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        

        if (Regex.IsMatch(messageText))
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

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram API Error: {exception.Message}");
        return Task.CompletedTask;
    }
}