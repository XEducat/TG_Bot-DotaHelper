using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using HtmlAgilityPack;


namespace DotaHelper_Bot
{
    // TODO LIST:
    // Отделить логику от визуализации в методах:
    // - UpdateInfo()
    // - ExtractHeroesDataFromDotabuff()

    public class DotabuffServer
    {
        private readonly HttpClient httpClient;
        private const string SiteUrl = "https://www.dotabuff.com";

        private HeroesDB database;

        public DotabuffServer()
        {
            database = new HeroesDB();
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "YourUserAgent/1.0");

            UpdateInfo();
        }

        public void UpdateInfo()
        {
            List<Hero> heroes = ExtractHeroesDataFromDotabuff();

            if (heroes.Count > 0)
            {
                database.AddHeroesData(heroes);
                Console.WriteLine("Information UPDATED");
            }
            else
            {
                Console.WriteLine("No hero data found.");
            }
        }

        public Hero GetHero(string text)
        {
            text = text.ToLower().Replace("-", " ");

            return database.GetHeroData(text);
        }

        public List<Hero> GetHeroCounterpeaks(string text)
        {
            List<Hero> result = null;
            var hero = GetHero(text);

            if (hero.Name != null)
            {
                string url = BuildHeroCounterpeaksUrl(hero.Name);
                result = ExtractCounterpeaksFromURL(5, url);
            }

            return result;
        }

        private string BuildHeroCounterpeaksUrl(string heroName)
        {
            string normalizedHeroName = heroName.ToLower().Replace(" ", "-").Replace("`", "");
            return $"{SiteUrl}/heroes/{normalizedHeroName}/counters";
        }

        private List<Hero> ExtractCounterpeaksFromURL(int Count, string url)
        {
            List<Hero> result = new List<Hero>();
            HtmlDocument doc = LoadHtmlDocument(url);

            foreach (var item in doc.DocumentNode.SelectNodes("//table/tbody/tr/td[2][not(number(.) = number(.))]"))
            {
                result.Add(new Hero { Name = item.InnerText.Trim().Replace("&#39;", "`") });

                if (result.Count >= Count)
                {
                    break;
                }
            }

            return result;
        }

        // TODO: Отделить логику от визуализации
        private List<Hero> ExtractHeroesDataFromDotabuff()
        {
            List<Hero> heroes = new List<Hero>();
            HtmlDocument doc = LoadHtmlDocument($"{SiteUrl}/heroes/");

            foreach (var heroNode in doc.DocumentNode.SelectNodes("//div[@class='hero']"))
            {
                Hero hero = ExtractHero(heroNode);
                heroes.Add(hero);
            }

            return heroes;
        }

        private HtmlDocument LoadHtmlDocument(string url)
        {
            HtmlWeb web = new HtmlWeb();
            return web.Load(url);
        }

        private Hero ExtractHero(HtmlNode node)
        {
            string heroName = ExtractHeroName(node);
            string imageUrl = ExtractHeroImageUrl(node);
            string fileLocation = GetHeroIconFilePath(heroName);

            if (DownloadImage(imageUrl, fileLocation))
                Console.WriteLine($"Картинку {heroName} успішно завантажено та збережено.");

            return new Hero() { Path = fileLocation, Name = heroName };
        }

        // Витягує URL на зображення героя з HTML-елемента.
        private string ExtractHeroImageUrl(HtmlNode item)
        {
            string styleAttribute = item.Attributes["style"].Value;
            return SiteUrl + styleAttribute.Replace("background: url(", "").Replace(")", "");
        }

        // Витягує ім'я героя з HTML-елемента.
        private string ExtractHeroName(HtmlNode item)
        {
            return item.InnerText.Trim().Replace("&#39;", "`").Replace("-", " ");
        }

        // Генерує шлях до файлу іконки героя на основі імені героя.
        private string GetHeroIconFilePath(string heroName)
        {
            return $"../../Resources/Heroes_Icons/{heroName.Replace(" ", "_")}Icon.jpg";
        }

        // Завантажує зображення з вказаного URL та зберігає його на пристрої.
        private bool DownloadImage(string imageUrl, string fileLocation)
        {
            bool imageDownloaded = false;

            try
            {
                if (!File.Exists(fileLocation))
                {
                    byte[] imageBytes = httpClient.GetByteArrayAsync(imageUrl).Result;
                    File.WriteAllBytes(fileLocation, imageBytes);
                    imageDownloaded = true;
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine("Помилка завантаження зображення: " + ex.Message);
            }

            return imageDownloaded;
        }
    }
}