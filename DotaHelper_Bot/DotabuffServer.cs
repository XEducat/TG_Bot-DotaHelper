using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace DotaHelper_Bot
{
    public class DotabuffServer
    {
        private string SiteUrl {get; } = "https://www.dotabuff.com";

        private HeroesDB database = new HeroesDB();

        public DotabuffServer()
        {
            UpdateInfo();
        }

        public List<Hero> GetHeroCounterpeaks(string text)
        {
            // Searching for a character in the database
            var hero = GetHero(text);
            if (hero.Name == null) return null;

            HtmlWeb web = new HtmlWeb();
            string url = $"{SiteUrl}/heroes/{hero.Name.ToLower().Replace(" ", "-").Replace("`", "")}/counters";
            HtmlDocument doc = web.Load(url); // (Example: https://www.dotabuff.com/heroes/night-stalker/counters)

            List<Hero> result = new List<Hero>();

            // Searching for counterpeaks in HTML and entering them into a variable result
            foreach (var item in doc.DocumentNode.SelectNodes("/html/body/div[2]/div[2]/div[3]/div[4]/div[1]/div[1]/section/article/table/tbody/tr/td[2]"))
            { 
                result.Add(new Hero { Name = item.InnerText.Trim().Replace("&#39;", "`") });
            }

            return result;
        }

        public void UpdateInfo()
        {
            List<Hero> heroes = new List<Hero>();
            
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load($"https://www.dotabuff.com/heroes/");

            // Finding the character and its image to add to the List<Hero>
            foreach (var item in doc.DocumentNode.SelectNodes("//div[@class='hero']"))
            {
                string ImageUrl = SiteUrl + item.Attributes["style"].Value.Replace("background: url(", "").Replace(")", "");
                string heroName = item.InnerText.Trim().Replace("&#39;", "`").Replace("-", " ");

                string fileLocation = $"../../Resources/Heroes_Icons/{heroName.Replace(" ", "_")}Icon.jpg";

                //If the image is on the device, then add it to the list
                if (DownloadWebsiteImage(ImageUrl, fileLocation))   
                {
                    heroes.Add(new Hero()
                    {
                        Path = fileLocation,
                        Name = heroName
                    });
                }
            }

            // Transfer the received data to the database
            database.AddHeroesData(heroes);
            Console.WriteLine("Information UPDATED");
        }

        public Hero GetHero(string text)
        {
            text = text.ToLower().Replace("-", " ");

            return database.GetHeroData(text);
        }
        
        private bool DownloadWebsiteImage(string Url, string FilePath)
        {
            // Checking if the file exists on the device
            try
            {
                var n = System.IO.File.Open(FilePath, FileMode.Open);
            }
            catch
            {
                // Making a request to the specified URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "Get";
                request.Accept = "*/*";
                request.Host = "www.dotabuff.com";
                request.UserAgent = "PostmanRuntime/7.29.2";
                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (Exception)
                {
                    Console.WriteLine("Image not found on the site");
                    return false;
                }

                // Check sure the deleted file from the site is found.
                // The ContentType check is performed since a request for 
                // a non-existent image file might be redirected to a 404-page,
                // which would yield the StatusCode "OK", even though the image
                // was not found.
                if ((response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Moved ||
                    response.StatusCode == HttpStatusCode.Redirect) &&
                    response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                    {

                    // if the remote file was found, download it to the device
                    using (Stream inputStream = response.GetResponseStream())
                    using (Stream outputStream = System.IO.File.OpenWrite(FilePath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        do
                        {
                            bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                            outputStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead != 0);
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine("Image could not be loaded");
                    return false;
                }
            }

            // We have an image on the device
            return true;
        }
    }
}