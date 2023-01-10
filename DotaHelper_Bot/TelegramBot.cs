using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace DotaHelper_Bot
{
    public class TelegramBot
    {
        private ITelegramBotClient bot = new TelegramBotClient("TOKEN"); 

        private DotabuffServer server = new DotabuffServer();

        private const string FirstButton = "FirstHero";
        private const string SecondButton = "SecondHero";
        private const string ThirdButton = "ThirdHero";
        private const string FourtButton = "FourthHero";
        private const string FifthButton = "FifthHero";

        private List<Hero> current_Counterpeaks = new List<Hero>(); 

        public void Run()
        {
            try
            {
                Console.WriteLine("Bot launched " + bot.GetMeAsync().Result.FirstName);

                // Creating a timer with an hourly countdown
                var timer = new System.Timers.Timer(3600000);
                timer.Elapsed += new ElapsedEventHandler(TimerTick);
                timer.Start();

                // Binding the bot to certain methods and passing options and a token
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }, // receive all update types
                };

                bot.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                );
            } 
            catch ( Exception ex)
            {
                Console.WriteLine($"When starting the bot [{ex.Message}]");
            }

            Console.ReadLine();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            // Update information from the site
            server.UpdateInfo();
        }

        async Task HandleButton(CallbackQuery query)
        {
            if (current_Counterpeaks.Count > 0)
            {
                string hero = string.Empty;
                switch (query.Data) // Which button is pressed
                {
                    case FirstButton:
                        hero = current_Counterpeaks[0].Name;
                        break;
                    case SecondButton:
                        hero = current_Counterpeaks[1].Name;
                        break;
                    case ThirdButton:
                        hero = current_Counterpeaks[2].Name;
                        break;
                    case FourtButton:
                        hero = current_Counterpeaks[3].Name;
                        break;
                    case FifthButton:
                        hero = current_Counterpeaks[4].Name;
                        break;
                    default:
                        break;
                }

                query.Message.Text = $"/find " + hero.Trim();
                await HandleMessage(query.Message);
            }

            // Close the query to end the client-side loading animation
            try
            {
                await bot.AnswerCallbackQueryAsync(query.Id);
            } 
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при закрытии запроса\n" + ex.Message);
            }
        }

        async Task HandleMessage(Message msg)
        {
            var user = msg.From;
            var text = msg.Text ?? string.Empty;

            if (user is null)
                return;

            // Outputting a message to the console
            Console.WriteLine($"{msg.Chat.FirstName} {msg.Chat.LastName} wrote {msg.Text} |{msg.Date.AddHours(2)}| "); // Ukrainian time zone orientation (UTC +02:00)
            const string CommandsINFO = "Command List: " +
                   "\n/start  -  Running the bot" +
                   "\n/find  [character name]  -  Search for character counterpicks " +
                   "\n/commands  -  View the commands list";

            // Command processing
            if (text.Equals("/start"))
            {
                await bot.SendTextMessageAsync(msg.Chat, "👋DotaHelper welcomes you👋\n" + CommandsINFO);
            }
            else if (text.Contains("/find "))
            {
                msg.Text = text.Replace("/find ", "");
                await SendHero(msg);
                await SendHeroDrafts(msg);
            }
            else if (text.Equals("/commands"))
            {
                await bot.SendTextMessageAsync(msg.Chat, CommandsINFO); 
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "This command is unknown");
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                // A message was received
                case UpdateType.Message:
                    await HandleMessage(update.Message);
                    break;

                // A button was pressed
                case UpdateType.CallbackQuery:
                    await HandleButton(update.CallbackQuery);
                    break;
            }
        }

        private async Task SendHero(Message message)
        {
            var hero = server.GetHero(message.Text);

            if (hero.Name != null && message.Text.Length > 0) // Character found
            {
                string screen = hero.Path;
                try
                {
                    var fileStream = new FileStream(screen, FileMode.Open, FileAccess.Read, FileShare.Read); // Виведення персонажа
                    var result = server.GetHeroCounterpeaks(message.Text);

                    // Character output
                    await bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: new InputOnlineFile(fileStream),
                        caption: Align(hero.Name.ToUpper())
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                await bot.SendTextMessageAsync(message.Chat, "The character is not found! \nCheck the indicated name");
            }
        }

        private async Task SendHeroDrafts(Message message)
        {
            current_Counterpeaks?.Clear();
            current_Counterpeaks = server.GetHeroCounterpeaks(message.Text);

            if (current_Counterpeaks != null && message.Text.Length > 0) // Character found
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton($"☛{current_Counterpeaks[0].Name}☚") { CallbackData = FirstButton},
                    },
                    new[]
                    {
                        new InlineKeyboardButton($"☛{current_Counterpeaks[1].Name}☚"){ CallbackData = SecondButton},
                    },
                    new[]
                    {
                        new InlineKeyboardButton($"☛{current_Counterpeaks[2].Name}☚") { CallbackData = ThirdButton }
                    },
                    new[]
                    {
                        new InlineKeyboardButton($"☛{current_Counterpeaks[3].Name}☚") { CallbackData = FourtButton }
                    },
                    new[]
                    {
                        new InlineKeyboardButton($"☛{current_Counterpeaks[4].Name}☚") { CallbackData = FifthButton}
                    }
                });

                await bot.SendTextMessageAsync(message.Chat.Id, "ㅤㅤㅤ▞▞▞►Контрпіки◄▞▞▞", replyMarkup: keyboard);
            }
        }

        private string Align(string text) 
        {
            int msg_width = 18;
            string result = "";

            // Determining the number of indents
            for (int i = 0; i < (msg_width - text.Length/2)/2; i++)
            {
                result += "ㅤ";
            }

            // Adding indentation for alignment
            string str = result.Substring(0, result.Length - 4);
            result = text.Insert(0, result) + str;

            return result;
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Error message output
           Console.WriteLine(exception.Message);
        }
    }
}
