using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace SampleCode;

/**
 * @brief Discord.Net Bot のPrefix ルーティングを行うコマンドサービス
 * @details ! / $ / % / ％ の4種類のPrefixを処理し、それぞれに対応したコマンドを実行します。
 *          ExecuteAsync内でMessageReceivedイベントを登録して処理を行います。
 */
public class SampleCommandService
    (DiscordSocketClient client, ILogger<SampleCommandService> logger)
    : DiscordClientService(client, logger)
{
    /**
     * @brief 開発者ID（暫定的なハードコード）
     * @details 実際の開発者のDiscord IDに置き換える必要があります。
     *          将来的には設定ファイルや環境変数から読み込むことを推奨します。
     */
    private const ulong DeveloperId = 123456789012345678; // 実際の開発者IDに置き換えてください

    /**
     * @brief サービスの実行を開始し、MessageReceivedイベントを登録します
     * @param stoppingToken キャンセルトークン
     * @return 完了したTask
     */
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.MessageReceived += HandleMessageAsync;
        Logger.LogInformation("SampleCommandService started. Prefix routing enabled for !, $, %, ％");
        return Task.CompletedTask;
    }

    /**
     * @brief メッセージ受信時のハンドラ
     * @param message 受信したDiscordメッセージ
     * @details Bot自身のメッセージと空メッセージを無視し、Prefix（先頭1文字）に応じて
     *          適切なコマンドハンドラに処理を振り分けます。
     *          例外が発生してもプロセスが停止しないようにログだけを残します。
     */
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

    /**
     * @brief 一般コマンド（! prefix）を処理します
     * @param message 受信したDiscordメッセージ
     * @param body Prefix以降の本文
     * @details 現在は "ping" コマンドのみ対応しています。
     *          入力: !ping → 出力: pong
     */
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

    /**
     * @brief エコーコマンド（$ prefix）を処理します
     * @param message 受信したDiscordメッセージ
     * @param body Prefix以降の本文
     * @details $ 以降の文字列をトリムせずにそのまま返信します。
     *          空文字の場合は返信しません。
     *          入力: $hello test → 出力: hello test
     */
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

    /**
     * @brief 開発者専用コマンド（% または ％ prefix）を処理します
     * @param message 受信したDiscordメッセージ
     * @param body Prefix以降の本文
     * @details 開発者ID（DeveloperId）と一致する場合のみコマンドを実行します。
     *          不一致の場合は「開発者専用コマンドです。」と返信します。
     *          現在は "ping" コマンドのみ対応しています。
     *          入力: %ping または ％ping → 出力: dev-pong（開発者のみ）
     */
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
