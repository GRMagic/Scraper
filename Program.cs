using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            UsarHtmlAgilityPack();
            Console.WriteLine("-----------");
            UsarScrapySharp();
            Console.WriteLine("-----------");
            var times = UsarSelenium();
            Console.WriteLine("-----------");
            ExportarUsandoCsvHelper(times);
            Console.ReadKey();
        }

        static void UsarHtmlAgilityPack()
        {
            Console.WriteLine("Scrapping com HtmlAgilityPack!");
            Console.WriteLine("Funciona bem quando o site retorna os dados no html logo de cara.");
            var web = new HtmlWeb();
            var doc = web.Load(@"https://example.com/");
            var titulo = doc.DocumentNode.SelectNodes(@"/html/body/div/h1")[0];
            Console.WriteLine(titulo.InnerText);
        }

        static void UsarScrapySharp()
        {
            Console.WriteLine("Scrapping com ScrapySharp!");
            Console.WriteLine("Tenta simula um navegador controlando cookies e permitindo clicar, submeter forms, etc., mas se o conteúdo é carregado dinamicamente não é uma boa opção.");
            Console.WriteLine("Não manipula bem alguns cookies.");

            var browser = new ScrapingBrowser();

            var homePage = browser.NavigateToPage(new Uri("http://www.bing.com/"));

            var form = homePage.FindFormById("sb_form");
            form["q"] = "example domain";
            form.Method = HttpVerb.Get;
            WebPage resultsPage = form.Submit();

            var examplePage = resultsPage.FindLinks(ScrapySharp.Html.By.Text("Example.com - Wikipedia")).First().Click();

            var titulo = examplePage.Html.SelectNodes(@"/html/body/div/h1")[0];
            Console.WriteLine(titulo.InnerText);
        }

        static Dictionary<string, Time> UsarSelenium()
        {
            Console.WriteLine("Scrapping com Selemium web driver!");
            Console.WriteLine("Controla um navegador usando um driver. O site tem como saber que o navegador está sendo controlado, mesmo assim funciona bem para a grande maioria dos sites.");

            using var navegador = new ChromeDriver(@"C:\WebDriver");
            navegador.Manage().Window.Maximize();
            navegador.Url = @"https://ge.globo.com/futebol/brasileirao-serie-a/";

            var spanRodada = navegador.FindElementByClassName("lista-jogos__navegacao--rodada");
            var rodadaAtual = int.Parse(spanRodada.Text.Split("ª").FirstOrDefault());

            var numeroRodada = rodadaAtual;
            var numeroProximaRodada = rodadaAtual + 1;
            var botaoRodadaAnterior = navegador.FindElementByClassName("lista-jogos__navegacao--seta-esquerda");
            var botaoRodadaProxima = navegador.FindElementByClassName("lista-jogos__navegacao--seta-direita");
            while (numeroRodada > 1)
            {
                botaoRodadaAnterior.Click();
                numeroRodada = int.Parse(spanRodada.Text.Split("ª").FirstOrDefault());
            }

            var rodadas = new List<Rodada>();
            var times = new Dictionary<string,Time>();

            numeroRodada = 0; numeroProximaRodada = 1;

            while (numeroRodada != numeroProximaRodada)
            {
                numeroRodada = numeroProximaRodada;
                var rodada = new Rodada { Numero = numeroRodada };
                Console.WriteLine("Coletando dados da " + spanRodada.Text);

                var ulJogos = navegador.FindElementByClassName("lista-jogos");
                foreach (var li in ulJogos.FindElements(OpenQA.Selenium.By.TagName("li")))
                {
                    var jogo = new Jogo();
                    var data = li.FindElements(OpenQA.Selenium.By.TagName("meta"))[1].GetAttribute("content");
                    if(!string.IsNullOrWhiteSpace(data)) jogo.Data = DateTime.Parse(data);
                    jogo.Local = li.FindElement(OpenQA.Selenium.By.ClassName("jogo__informacoes--local")).Text;
                    var divMandante = li.FindElement(OpenQA.Selenium.By.ClassName("placar__equipes--mandante"));
                    var siglaMantante = divMandante.FindElement(OpenQA.Selenium.By.ClassName("equipes__sigla")).Text;
                    if (!times.ContainsKey(siglaMantante))
                    {
                        var time = new Time();
                        time.Sigla = siglaMantante;
                        time.Nome = divMandante.FindElement(OpenQA.Selenium.By.ClassName("equipes__sigla")).GetAttribute("title");
                        time.Escudo = new Uri(divMandante.FindElement(OpenQA.Selenium.By.ClassName("equipes__escudo")).GetAttribute("src"));
                        times[time.Sigla] = time;
                    }
                    jogo.Mandante = times[siglaMantante];
                    var divVisitante = li.FindElement(OpenQA.Selenium.By.ClassName("placar__equipes--visitante"));
                    var siglaVisitante = divVisitante.FindElement(OpenQA.Selenium.By.ClassName("equipes__sigla")).Text;
                    if (!times.ContainsKey(siglaVisitante))
                    {
                        var time = new Time();
                        time.Sigla = siglaVisitante;
                        time.Nome = divVisitante.FindElement(OpenQA.Selenium.By.ClassName("equipes__sigla")).GetAttribute("title");
                        time.Escudo = new Uri(divVisitante.FindElement(OpenQA.Selenium.By.ClassName("equipes__escudo")).GetAttribute("src"));
                        times[time.Sigla] = time;
                    }
                    jogo.Visitante = times[siglaVisitante];
                    var divPlacar = li.FindElement(OpenQA.Selenium.By.ClassName("placar-box"));
                    var mandanteGols = divPlacar.FindElement(OpenQA.Selenium.By.XPath("span[1]")).Text.Trim();
                    if (mandanteGols.Any()) jogo.MandanteGols = int.Parse(mandanteGols);
                    var visitanteGols = divPlacar.FindElement(OpenQA.Selenium.By.XPath("span[5]")).Text.Trim();
                    if (visitanteGols.Any()) jogo.VisitanteGols = int.Parse(visitanteGols);
                    var mandanteGolsPenaltis = divPlacar.FindElement(OpenQA.Selenium.By.XPath("span[2]")).Text.Trim();
                    if (mandanteGolsPenaltis.Any()) jogo.MandanteGolsPenaltis = int.Parse(mandanteGolsPenaltis);
                    var visitanteGolsPenaltis = divPlacar.FindElement(OpenQA.Selenium.By.XPath("span[4]")).Text.Trim();
                    if (visitanteGolsPenaltis.Any()) jogo.VisitanteGolsPenaltis = int.Parse(visitanteGolsPenaltis);
                    rodada.Jogos.Add(jogo);
                }
                rodadas.Add(rodada);
                for (int i = 0; i < 3 && numeroProximaRodada == numeroRodada; i++)
                {
                    botaoRodadaProxima.Click();
                    System.Threading.Thread.Sleep(500);
                    numeroProximaRodada = int.Parse(spanRodada.Text.Split("ª").FirstOrDefault());
                }
            }

            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
                Campeonato = "Brasileirão",
                RodadaAtual = rodadaAtual,
                Rodadas = rodadas
            }, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            navegador.Close();
            return times;
        }

        static void ExportarUsandoCsvHelper(Dictionary<string, Time> times)
        {
            Console.WriteLine("Exportando arquivo csv com CsvHelper!");
            Console.WriteLine("É bem prático.");

            using var csv = new CsvHelper.CsvWriter(Console.Out, CultureInfo.InvariantCulture);
            csv.WriteRecords(times.Select(t => t.Value));
        }

    }
}
