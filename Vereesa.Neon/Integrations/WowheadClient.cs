using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Integrations.Interfaces;
using Vereesa.Neon.Data.Models.Wowhead;
using Vereesa.Neon.Extensions;

namespace Vereesa.Neon.Integrations
{
    public class WowheadClient : IWowheadClient
    {
        public TodayInWow GetTodayInWow()
        {
            string html = string.Empty;
            using (var webClient = new WebClient())
            {
                html = webClient.DownloadString("https://www.wowhead.com");
                Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
                html = rRemScript.Replace(html, string.Empty);
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tiwSections = doc.DocumentNode.SelectNodes(
                "//div[contains(@class,'tiw-region-EU')]//table[contains(@class,'tiw-group') and not( contains(@class,'tiw-blocks-warfront'))]"
            );
            var warfronts = doc.DocumentNode.SelectNodes(
                "//div[contains(@class,'tiw-region-EU')]//table[contains(@class, 'tiw-group tiw-blocks-warfront')]"
            );
            var assaults = doc.DocumentNode.SelectNodes("//*[contains(@class,'tiw-assault-EU-wrapper')]");

            var todayInWow = new TodayInWow();
            todayInWow.Sections = tiwSections
                .Select(
                    c =>
                        new TodayInWowSection
                        {
                            Title = c.ChildNodes.First(n => n.Name == "tr").InnerText.StripTrim(),
                            Entries = c.ChildNodes
                                .Where(n => n.Name == "tr")
                                .Skip(1)
                                .Select(n => n.InnerText.StripTrim())
                                .ToList()
                        }
                )
                .ToList();

            var warfrontSections = warfronts.Select(
                n =>
                    new TodayInWowSection
                    {
                        Title = n.SelectSingleChildNode("//div[contains(@class,'tiw-blocks-status-name')]")
                            .InnerText.StripTrim(),
                        Entries = new List<string>
                        {
                            n.SelectSingleChildNode("//div[contains(@class,'status-state')]").InnerText.StripTrim(),
                            n.SelectSingleChildNode("//div[contains(@class,'status-progress')]").InnerText.StripTrim()
                        }
                    }
            );

            // var assaultSections = assaults.Select(n => new TodayInWowSection {
            //     Title = n.SelectSingleChildNode("//*[contains(@class,'tiw-blocks-status-name')]").InnerText.StripTrim(),
            //     Entries = new List<string> {
            //         ""
            //     }
            // });

            todayInWow.Sections.AddRange(warfrontSections);

            return todayInWow;
        }
    }
}
