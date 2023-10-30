using AngleSharp.Html.Parser;
using AngleSharp.Io;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace genshinwiki
{
    public class DataFields
    {
        public int index { get; set; }
        public string ogg { get; set; }
        public string title { get; set; }
        public string romaji { get; set; }
        public string engt { get; set; }
        public string jpt { get; set; }
    }
    public partial class MainWindow : Window
    {
        IBrowser browser;
        IPage page;
        IPlaywright playwright;
        private readonly HttpClient cli;

        public MainWindow()
        {
            cli = new HttpClient();
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> containing = new()
            {
                ".png",
                "data:image",
                "shopGoodsRec",
                "unread",
                "userTaskCount",
                "userRewardChatCount",
                "getUserInfo",
                "frame-modern",
                "countRepairPack",
                "balance",
                "nums",
                "staticsUnread",
                "getCustomizedInfo",
                "getOrderCount",
                "to0et"
            };

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false,
                Timeout = 100000,
                //UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36",
                //Locale = "en-US",
                //TimezoneId = "America/Santiago",
            });
            page = await browser.NewPageAsync();
            //page = browser.Pages[0];
            await page.RouteAsync("**/*", async route =>
            {
                bool ok = true;
                if (
                route.Request.ResourceType == "image"
                || route.Request.ResourceType == "stylesheet"
                || route.Request.ResourceType == "media"
                || route.Request.ResourceType == "font"
                || route.Request.ResourceType == "script"
                || route.Request.ResourceType == "websocket"
                || route.Request.ResourceType == "other"
                || route.Request.ResourceType == "fetch"
                || route.Request.ResourceType == "xhr"
                || route.Request.ResourceType == "eventsource"
                || route.Request.ResourceType == "manifest"
                || route.Request.ResourceType == "texttrack")
                {
                    ok = false;
                    await route.AbortAsync();
                }

                for (int i = 0; i < containing.Count; i++)
                {

                    if (route.Request.Url.Contains(containing[i]))
                    {
                        ok = false;
                        await route.AbortAsync();
                    }
                }
                if (ok)
                {
                    await route.ContinueAsync();
                }
            });
        }
        private static HttpClient client = null;
        HtmlParser pa = new HtmlParser();
        string play = "Baizhu";
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory($"..\\..\\..\\VO\\{play}");
            Directory.CreateDirectory($"..\\..\\..\\VO\\{play}\\ogg\\");
            await page.GotoAsync($"https://genshin-impact.fandom.com/wiki/{play}/Voice-Overs/Japanese");
            var c = await page.ContentAsync();
            var p = await pa.ParseDocumentAsync(c);

            var t1 = p.QuerySelector(".wikitable");
            var r = t1.QuerySelectorAll("tr");
            List<DataFields> fields = new List<DataFields>();
            for (int i = 0; i < r.Length; i++)
            {
                DataFields o = new DataFields();
                var th = r[i].QuerySelector("th");
                var td = r[i].QuerySelector("td");
                if (td == null) continue;
                var aud = td.QuerySelector(".internal");
                var id = td.QuerySelector("i");
                //var tr = evi[i].QuerySelector("tr");
                var text = td.QuerySelectorAll("span");
                var text2 = th.QuerySelectorAll("span");
                foreach (var item in text)
                {
                    if(item.HasAttribute("lang"))
                    {
                        ///if (text != null)
                        {
                            var f = item.TextContent;
                            o.jpt = f;
                        }
                    }
                }
                foreach (var item in text2)
                {
                    if (item.HasAttribute("lang"))
                    {
                        ///if (text != null)
                        {
                            var f = item.TextContent;
                            o.title = f;
                        }
                    }
                }
                if (th != null)
                {
                    var v = th.QuerySelector("small");
                    if (v != null)
                    {
                        o.engt = v.TextContent;
                    }
                }
                if (id != null)
                {
                    o.romaji = id.TextContent;
                }
                if (aud!= null)
                {
                    var f = aud.GetAttribute("href");
                    o.ogg = f;
                }        
                if(o.ogg != null)
                {
                    o.index = i;
                    fields.Add(o);
                }
            }
            var z = p.QuerySelectorAll(".internal");
            string cx = $"..\\..\\..\\VO\\{play}\\ogg\\";
            File.WriteAllText($"..\\..\\..\\VO\\{play}\\wiki.json", JsonSerializer.Serialize(fields));
            for (int i = 0; i < fields.Count; i++)
            {
                var response = await cli.GetAsync(fields[i].ogg);
                var stream = await response.Content.ReadAsStreamAsync();
                var fileInfo = new FileInfo($"{cx}{i}.ogg");
                using (var fileStream = fileInfo.OpenWrite())
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
    }
}
