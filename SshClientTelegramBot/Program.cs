using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Renci.SshNet;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace SshClientTelegramBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tgBotPollPeriodInSeconds = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("TG_BOT_POLL_PERIOD_SECONDS"), out var _tgBotPollPeriodInSeconds) ? _tgBotPollPeriodInSeconds : 5d);
        var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;
        var address = Environment.GetEnvironmentVariable("ADDRESS")!;
        var login = Environment.GetEnvironmentVariable("LOGIN")!;
        var password = Environment.GetEnvironmentVariable("PASSWORD")!;
        var admins = Environment.GetEnvironmentVariable("ADMINS")!;

        Console.WriteLine("bot run!");

        await Task.WhenAll([
                new TelegramBot().RunAsync(
                        apiKey,
                        address,
                        login,
                        password,
                        admins,
                        tgBotPollPeriodInSeconds,
                        CancellationToken.None)
            ]);

    }
}
