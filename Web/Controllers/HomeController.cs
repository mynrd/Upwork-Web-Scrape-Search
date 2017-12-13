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
            int totalCountLoop = 0;
            for (int i = 0; i < pageToShow; i++)
            {
                totalCountLoop++;
                string url = $"https://www.upwork.com/o/jobs/browse/?page={(i + 1)}&q={q}";
                var web = new HtmlWeb();
                var doc = await Task.Factory.StartNew(() => web.Load(url));
                var nodes = doc.DocumentNode.SelectNodes("//*[@id=\"jobs-list\"]//section");

                foreach (var node in nodes)
                {
                    var header = GetInnerText(doc.DocumentNode, node.XPath + "//div//div//h4//a");
                    var jobType = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//strong[contains(@class,\"js-type\")]");
                    var contractorLevel = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-contractor-tier\")]");
                    var budget = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-budget\")]");
                    var postedDuration = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//small[1]//span[contains(@class,\"js-duration\")]");
                    //var description = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//span[1]//span");
                    var description = doc.DocumentNode.SelectNodes(node.XPath + "//div[2]//div//div[2]").Select(x => { try { return x.Attributes["data-ng-init"].Value; } catch { return ""; } }).FirstOrDefault();
                    //var description = GetInnerText(doc.DocumentNode, node.XPath + "//div[2]//div//div[2]//div//span[1]//span");

                    //description = DecodeEncodedNonAsciiCharacters();
                    description = description.Replace("jobDescriptionController.description = ", "");
                    //description = description.Replace("\n", " ");

                    //if (description.Length > 150)
                    //{
                    //    description = description.Substring(0, 149);
                    //}
                    description = StripHTML(description)
                            .Replace("\\n", " ")
                            .Replace("\\r", " ")
                            .Replace("\\t", " ");
                    model.Add(new SearchResultModel()
                    {
                        Header = header,
                        ContractorLevel = contractorLevel,
                        JobType = jobType,
                        Budget = budget,
                        Description = description,
                        PostedDuration = postedDuration,
                    });
                }

                try
                {
                    var nodePaging = doc.DocumentNode.SelectNodes("//*[@id=\"jobs-list\"]/footer/div/ul[contains(@class, \"pagination\")]");

                    if (!nodePaging.Any())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    break;
                }

            }

            return Json(new { success = true, data = model, totalCountLoop });
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