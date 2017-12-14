using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using HtmlAgilityPack;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData(string search, int? pageToShow)
        {
            if ((pageToShow ?? 0) == 0) pageToShow = 1;

            var q = HttpUtility.UrlEncode(search);
            var model = new List<SearchResultModel>();
            string url = $"https://www.upwork.com/o/jobs/browse/?page={(pageToShow.Value + 1)}&q={q}";
            var web = new HtmlWeb();
            var doc = await Task.Factory.StartNew(() => web.Load(url));
            var nodes = doc.DocumentNode.SelectNodes("//*[@id=\"jobs-list\"]//section");

            foreach (var node in nodes)
            {
                var jobUrl = doc.DocumentNode.SelectNodes(node.XPath + "//div[1]//div//h4//a")
                    .Select(x => { try { return x.Attributes["href"].Value; } catch { return ""; } }).FirstOrDefault();

                var header = GetInnerText(doc.DocumentNode, node.XPath + "//div//div//h4//a");
                var jobType = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//strong[contains(@class,\"js-type\")]");
                var contractorLevel = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-contractor-tier\")]");
                var budget = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-budget\")]");
                var postedDuration = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-duration\")]");
                var description = doc.DocumentNode.SelectNodes(node.XPath + "//div[2]//div//div[2]").Select(x => { try { return x.Attributes["data-ng-init"].Value; } catch { return ""; } }).FirstOrDefault();
                description = description.Replace("jobDescriptionController.description = ", "");
                description = StripHTML(description)
                        .Replace("\\n", " ")
                        .Replace("\\r", " ")
                        .Replace("\\u2022", "•")
                        .Replace("\\t", " ");
                model.Add(new SearchResultModel()
                {
                    Url = "https://www.upwork.com" + jobUrl,
                    Header = header,
                    ContractorLevel = contractorLevel,
                    JobType = jobType,
                    Budget = budget,
                    Description = description,
                    PostedDuration = postedDuration,
                });
            }
            var hasPaging = false;
            try
            {
                var nodePaging = doc.DocumentNode.SelectNodes("//*[@id=\"jobs-list\"]/footer/div/ul[contains(@class, \"pagination\")]");
                hasPaging = nodePaging.Any();
            }
            catch { }

            return Json(new { success = true, data = model, hasPaging });
        }

        public string StripHTML(string input)
        {
            string result =  Regex.Replace(input, "<.*?>", String.Empty);

            return Regex.Replace(result, "&lt;.*?&gt;", String.Empty);
        }

        private string GetInnerText(HtmlNode node, string path)
        {
            try
            {
                return node.SelectNodes(path).FirstOrDefault().InnerText.Trim();
            }
            catch
            {
                return "";
            }
        }

    }
}