using Renci.SshNet;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SshClientTelegramBot;

public class TelegramBot
{
    private Dictionary<string, SshClient> sshClients = new();
    private Dictionary<string, CancellationTokenSource> cancellationTokenSources = new();
    private HashSet<string> allowedUsers = new();
    public async Task RunAsync(string apiKey, string address, string login, string password, string admins, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(apiKey))
            return;

        var offset = 0;
        var telegramBot = new TelegramBotClient(apiKey);
        allowedUsers = admins.Split(',').ToHashSet();

        foreach (var admin in allowedUsers)
        {
            sshClients.Add(admin, new(address, login, password));
            cancellationTokenSources.Add(admin, new());
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            var updates = Array.Empty<Update>();
            try
            {
                updates = await telegramBot.GetUpdates(offset, timeout: (int)pollPeriod.TotalSeconds, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var update in updates)
                {
                    offset = update.Id + 1;

                    try
                    {
                        if (update is null || 
                            update.Message is null || 
                            string.IsNullOrEmpty(update.Message.Text) ||
                            !allowedUsers.Contains(update.Message.Chat.Username!))
                            continue;

                        var cancellationTokenSource = cancellationTokenSources[update.Message.Chat.Username!];
                        var sshClient = sshClients[update.Message.Chat.Username!];

                        if (!sshClient.IsConnected)
                            await sshClient.ConnectAsync(cancellationToken);

                        switch (update.Message.Text)
                        {
                            case "exit":
                                ExecuteCommandAsync(telegramBot, sshClient, update, cancellationTokenSource.Token);
                                sshClient.Disconnect();
                                break;
                            case "stop":
                                cancellationTokenSource.Cancel();
                                cancellationTokenSource.Dispose();
                                cancellationTokenSource = new();
                                cancellationTokenSources.Remove(update.Message.Chat.Username!);
                                cancellationTokenSources.Add(update.Message.Chat.Username!, cancellationTokenSource);
                                telegramBot.SendMessage(update.Message.Chat, "Stopped", replyParameters: new ReplyParameters { MessageId = update.Message.Id }, cancellationToken: cancellationTokenSource.Token);
                                break;
                            case "reboot now":
                                telegramBot.GetUpdates(++offset, timeout: (int)pollPeriod.TotalSeconds, cancellationToken: cancellationTokenSource.Token);
                                ExecuteCommandAsync(telegramBot, sshClient, update, cancellationTokenSource.Token);
                                sshClient.Disconnect();
                                break;
                            default:
                                ExecuteCommandAsync(telegramBot, sshClient, update, cancellationTokenSource.Token);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
    private async Task ExecuteCommandAsync(TelegramBotClient telegramBot, SshClient sshClient, Update update, CancellationToken cancellationToken)
    {
        await telegramBot.SendChatAction(update.Message!.Chat, ChatAction.Typing, cancellationToken:cancellationToken);
        var commandResult = string.Empty;
        await Task.Run(() => commandResult = sshClient.CreateCommand(update.Message.Text!).Execute(), cancellationToken);
        await telegramBot.SendMessage(update.Message.Chat, string.IsNullOrEmpty(commandResult) ? "No content" : "```\n" + commandResult + "\n```", ParseMode.Markdown, replyParameters: new ReplyParameters { MessageId = update.Message.Id }, cancellationToken: cancellationToken);
    }
}
