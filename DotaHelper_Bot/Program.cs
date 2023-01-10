using System;
using System.Net;
using System.Runtime.InteropServices;

namespace DotaHelper_Bot
{

    internal class Program
    {
        static void Main(string[] args)
        {
            TelegramBot bot = new TelegramBot();
            bot.Run();
        }
    }
}