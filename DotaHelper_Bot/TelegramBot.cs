using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Message = Telegram.Bot.Types.Message;

namespace DotaHelper_Bot
{
    public class TelegramBot
    {
        // Сервіс з якого ми витягуємо данні
        private DotabuffServer dotabuff;
        // Клієнт бота
        private TelegramBotClient botClient;

        // Час останнього використання команди
        private readonly Dictionary<long, DateTime> lastCommandUsage = new Dictionary<long, DateTime>();
        // Тривалість перезарядки
        private readonly TimeSpan commandCooldown = TimeSpan.FromSeconds(1); 

        private const string COMANDS_INFO =
        "Command List: \n" +
        "/start - Running the bot \n" +
        "/find [character name] - Search for character counterpicks \n" +
        "( Example: /find Axe )\n\n" +
        "/help - View the commands list";


        public TelegramBot()
        {
            SetupBot();
            Console.ReadLine();
        }

        private void SetupBot()
        {
            Console.WriteLine(" -- Bot start setuping -- \n");
            botClient = new TelegramBotClient("5939745151:AAHcL4_XqY42DO9JVkm57h9WFNZa2uNofsM");
            dotabuff = new DotabuffServer();
            StartReceiver();
            Console.WriteLine("\n -- Bot ready to use -- ");
        }

        public async void StartReceiver()
        {
            var token = new CancellationTokenSource();
            var cancelToken = token.Token;
            var ReOpt = new ReceiverOptions { AllowedUpdates = { } };
            await botClient.ReceiveAsync(OnUpdate, ErrorMessage, ReOpt, cancelToken);
        }

        /// <summary>
        /// Обробляє помилки, які виникають під час взаємодії з API Telegram.
        /// </summary>
        /// <param name="botClient">Клієнт бота Telegram</param>
        /// <param name="exception">Об'єкт винятку, який потрібно обробити</param>
        /// <param name="cancellationToken">Токен для можливості відміни операції</param>
        public async Task ErrorMessage(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException requestException)
            {
                await botClient.SendTextMessageAsync("", requestException.Message.ToString());
            }
        }

        /// <summary>
        /// Обробка вхідних повідомлень та обробка подій від користувачів, включаючи текстові повідомлення
        /// та відповіді на натискання кнопок.
        /// </summary>
        /// <param name="botClient">Клієнт бота Telegram</param>
        /// <param name="update">Оновлення від Telegram, яке містить інформацію про подію</param>
        /// <param name="cancellationToken">Токен для відміни операції</param>
        public async Task OnUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message != null)
            {
                await HandleMessage(update.Message);
            }
            else if (update.CallbackQuery != null)
            {
                await HandleCallbackQuery(update.CallbackQuery);
            }
        }

        public async Task HandleMessage(Message message)
        {
            var user = message.From;
            var text = message.Text ?? string.Empty;

            if (user is null)
                return;

            if (text.Equals("/start"))
            {
                await botClient.SendTextMessageAsync(message.Chat, $"👋DotaHelper welcomes you {message.Chat.FirstName}👋\n" + COMANDS_INFO);
            }
            else if (text.Equals("/help"))
            {
                await botClient.SendTextMessageAsync(message.Chat, COMANDS_INFO);
            }
            else if (text.Contains("/find "))
            {
                if (lastCommandUsage.ContainsKey(user.Id) && (DateTime.Now - lastCommandUsage[user.Id]) < commandCooldown)
                {
                    // Користувач вже використовував команду /find і не минула перезарядка
                    await botClient.SendTextMessageAsync(message.Chat, "Ви не можете використовувати цю команду зараз.");
                }
                else
                {
                    lastCommandUsage[user.Id] = DateTime.Now;
                    string name = text.Replace("/find ", "");
                    long id = message.Chat.Id;

                    await SendHeroWithButtons(name, id);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, "This command is unknown");
            }
        }

        private async Task SendHeroWithButtons(string name, long chatId)
        {
            var hero = dotabuff.GetHero(name);

            if (hero.Name != null && name.Length > 0) // Character found
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(hero.Path))
                    {
                        // Опис тексту з використанням HTML-підтримки
                        var caption = $"<b>{hero.Name.ToUpper()}</b>\n\nClick a button to see the counterpicks:";

                        List<Hero> counterpeaks = dotabuff.GetHeroCounterpeaks(name);

                        if (counterpeaks != null && counterpeaks.Count > 0)
                        {
                            await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: new InputOnlineFile(stream),
                                caption: caption,
                                parseMode: ParseMode.Html,
                                replyMarkup: CreateKeybord(counterpeaks)
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "The character is not found! \nCheck the indicated name");
            }
        }

        private InlineKeyboardMarkup CreateKeybord(List<Hero> counterpeaks)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var counterpeak in counterpeaks)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton(counterpeak.Name)
                    {
                        CallbackData = counterpeak.Name
                    }
                });
            }

            return new InlineKeyboardMarkup(buttons);
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            string Name = callbackQuery.Data;
            long ChatId = callbackQuery.Message.Chat.Id;

            await SendHeroWithButtons(Name, ChatId);
        }
    }
}


//private async Task SendHero(string name, long chatId)
//{
//    var hero = dotabuff.GetHero(name);

//    if (hero.Name != null && name.Length > 0) // Character found
//    {
//        try
//        {
//            using (var stream = System.IO.File.OpenRead(hero.Path))
//            {
//                var fileStream = new InputOnlineFile(stream);
//                // Character output
//                await botClient.SendPhotoAsync(
//                    chatId: chatId,
//                    photo: fileStream,
//                    caption: hero.Name.ToUpper()
//                );
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine(ex.Message);
//        }
//    }
//    else
//    {
//        await botClient.SendTextMessageAsync(chatId, "The character is not found! \nCheck the indicated name");
//    }
//}

//private async Task SendHeroDrafts(string name, long chatId)
//{
//    List<Hero> current_Counterpeaks = dotabuff.GetHeroCounterpeaks(name);

//    if (current_Counterpeaks != null && current_Counterpeaks.Count > 0)
//    {
//        var buttons = new List<List<InlineKeyboardButton>>();

//        foreach (var counterpeak in current_Counterpeaks)
//        {
//            buttons.Add(new List<InlineKeyboardButton>
//                    {
//                        new InlineKeyboardButton($"☛{counterpeak.Name}☚")
//                        {
//                            CallbackData = counterpeak.Name
//                        }
//                    });
//        }

//        var keyboard = new InlineKeyboardMarkup(buttons);

//        await botClient.SendTextMessageAsync(chatId, "ㅤㅤㅤ▞▞▞►Контрпіки◄▞▞▞", replyMarkup: keyboard);
//    }
//}
