using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace SampleCode;

public class SampleCommandService
    (DiscordSocketClient client, ILogger<SampleCommandService> logger)
    : DiscordClientService(client, logger)
{
    // 開発者ID（暫定的なハードコード）
    private const ulong DeveloperId = 123456789012345678; // 実際の開発者IDに置き換えてください

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.MessageReceived += HandleMessageAsync;
        Logger.LogInformation("SampleCommandService started. Prefix routing enabled for !, $, %, ％");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(SocketMessage message)
    {
        try
        {
            // Bot自身のメッセージは無視
            if (message.Author.IsBot)
            {
                return;
            }

            // 空メッセージは無視
            if (string.IsNullOrEmpty(message.Content))
            {
                return;
            }

            var content = message.Content;
            var prefix = content[0];
            var body = content.Length > 1 ? content[1..] : string.Empty;

            Logger.LogInformation("Received message with prefix '{Prefix}' from user {UserId}", prefix, message.Author.Id);

            // Prefix判定と処理
            switch (prefix)
            {
                case '!':
                    await HandleGeneralCommandAsync(message, body);
                    break;

                case '$':
                    await HandleEchoCommandAsync(message, body);
                    break;

                case '%':
                case '％': // 全角パーセント（U+FF05）
                    await HandleDeveloperCommandAsync(message, body);
                    break;

                default:
                    // その他のPrefixは無視
                    return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling message from user {UserId}", message.Author.Id);
        }
    }

    private async Task HandleGeneralCommandAsync(SocketMessage message, string body)
    {
        var command = body.Trim();

        if (command == "ping")
        {
            await message.Channel.SendMessageAsync("pong");
            Logger.LogInformation("Responded to !ping command");
        }
        // 他のコマンドは未対応のため無視
    }

    private async Task HandleEchoCommandAsync(SocketMessage message, string body)
    {
        // 空文字は返信しない
        if (string.IsNullOrEmpty(body))
        {
            return;
        }

        // トリムせずそのまま返信
        await message.Channel.SendMessageAsync(body);
        Logger.LogInformation("Echoed message: {Body}", body);
    }

    private async Task HandleDeveloperCommandAsync(SocketMessage message, string body)
    {
        // 開発者ID判定
        if (message.Author.Id != DeveloperId)
        {
            await message.Channel.SendMessageAsync("開発者専用コマンドです。");
            Logger.LogWarning("Non-developer user {UserId} attempted to use developer command", message.Author.Id);
            return;
        }

        var command = body.Trim();

        if (command == "ping")
        {
            await message.Channel.SendMessageAsync("dev-pong");
            Logger.LogInformation("Responded to developer ping command");
        }
        // 他のコマンドは未対応のため無視
    }
}
