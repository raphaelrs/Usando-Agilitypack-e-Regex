using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using Lucrando.Models;

namespace Lucrando.Controllers
{
    public class HomeController : Controller
    {
        static List<ObjetoDeBusca> ObjetosDeBusca = new List<ObjetoDeBusca>
            {                
                new ObjetoDeBusca(){
                    SitesXPaths = new Dictionary<string,List<string>>
                    {
                        {"https://www.peixeurbano.com.br/recife?categoria=restaurantes", new List<string>(){"//div[@class='other_deals span4']", "//div[@class='other_deals span3']"}}

                        //{"http://www.regateio.com.br/gastronomia", new List<string>(){"//div[contains(concat(' ',normalize-space(@class),' '),' mainOferta ')]", "//div[contains(concat(' ',normalize-space(@class),' '),' bonus ')]"}},
                        //{"http://www.groupon.com.br/browse/recife?category=bares-restaurantes", new List<string>(){"//figure[contains(concat(' ',normalize-space(@class),' '),' deal-tile-standard ')]"}},
                        //{"http://www.pechinchadavez.com.br/?p=gastronomia", new List<string>(){"//*[@id='sem-oferta']/article", "//article[contains(concat(' ',normalize-space(@class),' '),' oferta-container ')]"}}
                    },
                    Categoria = Categoria.Restaurantes
                }
                ,
                new ObjetoDeBusca(){
                    SitesXPaths = new Dictionary<string,List<string>>
                    {                        
                        //, "//*[@id='bc_0_99TB']"
                        {"http://economiaedescontos.blogspot.com.br/2012/09/peixeurbano.html", new List<string>(){"//*[@id='comment-holder']","//*[@id='Blog1']/div[1]/div/div/div[1]/div[1]" }}

                        //{"https://www.peixeurbano.com.br/recife?categoria=diversao&subcategoria=passeios", new List<string>(){"//div[@class='other_deals span4']", "//div[@class='other_deals span3']"}},
                        //{"http://www.regateio.com.br/viagens", new List<string>(){"//div[contains(concat(' ',normalize-space(@class),' '),' mainOferta ')]", "//div[contains(concat(' ',normalize-space(@class),' '),' bonus ')]"}},
                        //{"http://www.groupon.com.br/getaways", new List<string>(){"//figure[contains(concat(' ',normalize-space(@class),' '),' deal-tile-standard ')]"}}
                    },
                    Categoria = Categoria.Cupons
                }
            };

        static List<string> bairros = new List<string> { "Boa Viagem", "Piedade" };
        List<OfertaViewModel> lista = new List<OfertaViewModel>();

        public void ListarOfertas(string lugar, Categoria categoria, IndexModel model)
        {
            lista = new List<OfertaViewModel>();

            foreach (var item in ObjetosDeBusca.Where(x => x.Categoria == categoria).First().SitesXPaths)
            {
                HtmlWeb webget = new HtmlWeb();
                try
                {
                    WebProxy wp = new WebProxy(new Uri("http://proxy-rf.infranet.gov.br:8080"), true);
                    wp.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

                    var doc = webget.Load(item.Key, "GET", wp, System.Net.CredentialCache.DefaultNetworkCredentials);
                    //var doc = webget.Load(item.Key, "GET", (WebProxy)WebProxy.GetDefaultProxy(), System.Net.CredentialCache.DefaultNetworkCredentials);
                    //var doc = webget.Load(item.Key);


                    if (item.Key.Contains("economiaedescontos"))
                    {
                        LerCupons(doc, item.Value, lugar, model);
                    }
                    else if (item.Key.Contains("peixeurbano"))
                    {
                        LerPeixeUrbano(doc, item.Value, lugar, model);
                    }
                    else if (item.Key.Contains("regateio"))
                    {
                        //LerRegateio(doc, item.Value, lugar);
                    }
                    else if (item.Key.Contains("pechincha"))
                    {
                        //LerPechincha(doc, item.Value, lugar);
                    }
                    else
                    {
                        LerGroupon(doc, item.Value);
                    }
                }
                catch (Exception)
                {
                    //faça porra nenhuma
                }
            }
        }
        
        public static Categoria Categoria { get; set; }

        public ActionResult Index()
        {
            IndexModel im = new IndexModel();
            ListarOfertas("Boa Viagem|Piedade", Categoria, im);
            return View(im);
        }

        //public ActionResult BuscarOfertas(IndexModel model)
        //{
        //    if (!Regex.IsMatch(model.Lugar, "Boa Viagem|Piedade") | model.Categoria == Categoria.Viagens)
        //    {
        //        model.Lugar = "";
        //    }

        //    ListarOfertas(model.Lugar, model.Categoria, model);
        //    return PartialView("OfertasListControl", lista);
        //}

        public ActionResult BuscarPorCategoria(string lugar, Categoria categoria)
        {
            IndexModel im = new IndexModel();
            Categoria = categoria;

            if (!Regex.IsMatch(lugar, "Boa Viagem|Piedade") || categoria == Categoria.Cupons)
            {
                lugar = "";
            }

            ListarOfertas(lugar, categoria, im);
            return PartialView("OfertasListControl", im);
        }
        
        private void LerCupons(HtmlDocument doc, List<string> xPaths, string lugar, IndexModel model)
        {
            model.Cupom = true;

            foreach (var item in xPaths)
            {
                ObjetoCupom c = null;
                string s = String.Empty;
                HtmlNode node = null;
                List<HtmlNode> lis = null;
                string padrao = String.Empty;

                if (item == "//*[@id='comment-holder']")
                {
                    node = doc.DocumentNode.Descendants().Where(x => x.Name == "div"
                    && x.Attributes.Contains("id")
                    && x.Attributes["id"].Value == "comment-holder").FirstOrDefault();

                    lis = node.Descendants().Where(x => x.Name == "li"
                     && x.Attributes.Contains("class")
                    && x.Attributes["class"].Value.Contains("comment")
                    && (Convert.ToInt32(
                        Enum.Parse
                        (typeof(Meses),
                                Regex.Match
                                (x.InnerHtml, ">([0-9]{1,2} de (janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro))"
                                ).Groups[1].ToString().Split(' ')[2]
                        )
                    ) == DateTime.Now.Month
                    && Convert.ToInt32
                        (
                            Regex.Match
                                (x.InnerHtml, ">([0-9]{1,2} de (janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro))"
                                ).Groups[1].ToString().Split(' ')[0]
                        ) > (DateTime.Now.Day - 5)
                    
                    )).ToList();
                    
                    bool ultimoEraPai = false;

                    for (int i = 0; i < lis.Count; i++)
                    {
                        bool ehPai = lis[i].Descendants().Any(x => x.Name == "a" && x.Attributes.Contains("o") && x.Attributes["o"].Value == "r");

                        if (model.Comentarios.Count == 0 || ehPai || (!ehPai && !ultimoEraPai))
                        {
                            ultimoEraPai = ehPai;
                            model.Comentarios.Add(ExtrairComentario(lis[i], model));
                        }
                        else
                        {
                            model.Comentarios.LastOrDefault().Filhos.Add(ExtrairComentario(lis[i], model, true));
                        }
                        
                    }

                    model.Comentarios.Reverse();
                }
                else
                {
                    node = doc.DocumentNode.Descendants().Where(x => x.Name == "div"
                    && x.Attributes.Contains("id")
                    && x.Attributes["id"].Value == "Blog1").FirstOrDefault();

                    lis = node.Descendants().Where(x => x.Name == "li"
                     && x.Attributes.Contains("style")
                    && x.Attributes["style"].Value.Contains("justify")).ToList();
                    
                    foreach (HtmlNode li in lis)
                    {
                        c = new ObjetoCupom();
                        c.CodigoCupom = Extrair(li.InnerHtml, ">([A-Z0-9]{5})<");
                        c.ValorCupom = Extrair(li.InnerHtml,">R\\$([0-9]{1,2}|[0-9]{1,2},[0-9]{2}|[0-9]{1,2}% OFF)<");

                        if (String.IsNullOrEmpty(c.ValorCupom))
                        {
                            c.ValorCupom = Extrair(li.InnerHtml,">([0-9]{1,2}% OFF)<");
                        }

                        c.AcimaDe = Extrair(li.InnerHtml,"red;\">R\\$([0-9]{1,3})<");
                        c.AcimaDe = String.IsNullOrEmpty(c.AcimaDe) ? "???" : s;
                        c.Validade = Extrair(li.InnerHtml, ">(([0-9]{2})(/)([0-9]{2}))<");
                        c.Validade = String.IsNullOrEmpty(c.Validade) ? "???" : c.Validade;

                        if (li.InnerHtml.Contains("facebook"))
                        {
                            c.LogandoNoFace = true;
                        }

                        model.Cupons.Add(c);
                    }
                }
            }
        }

        private Comentario ExtrairComentario(HtmlNode li, IndexModel model, bool ehFilho = false)
        {
            Comentario co = new Comentario();

            co.EhFilho = ehFilho;

            co.Titulo = Regex.Match(li.InnerHtml, ">(\\w*(\\s\\w+)*)</cite>").Groups[1].ToString();

            if (String.IsNullOrEmpty(co.Titulo))
            {
                co.Titulo = Regex.Match(li.InnerHtml, ">(\\w*(\\s\\w+)*)</a></cite>").Groups[1].ToString();
            }

            co.Titulo = string.IsNullOrEmpty(co.Titulo) ? "" : co.Titulo + " ";


            co.Titulo += Regex.Match(li.InnerHtml, ">([0-9]{1,2} de (janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro) de ([0-9]{4}) ([0-9]{2})(:)([0-9]{2}))")
                .Groups[1].ToString();

            co.Texto = li.Descendants().Where(x => x.Name == "p"
                && x.Attributes.Contains("class")
                && x.Attributes["class"].Value.Contains("comment-content")).FirstOrDefault().InnerHtml;

            //if (DateTime.Now.Day == Convert.ToInt32(co.Titulo.Split(' ')[0]))
            //{
            //    MailMessage msg = new MailMessage("raphaelninoo@gmail.com","eshenriques@infraero.gov.br","teste",co.Texto);
            //    // build message contents
            //    SmtpClient client = new SmtpClient("10.5.17.238");
            //    client.Credentials = new System.Net.NetworkCredential("t072911934", "Rv059401", "D_SEDE");
            //    client.Send(msg);
            //}

            return co;
        }

        private string Extrair(string li, string padrão)
        {
            return Regex.Match(li, padrão).Groups[1].ToString();
        }

        private void LerPeixeUrbano(HtmlDocument doc, List<string> xPaths, string lugar, IndexModel model)
        {
            int i = 0;
            int seletorExtra = 2;
            string desconto = String.Empty;

            foreach (var item in xPaths)
            {
                i = 0;

                if (item.Contains("other_deals span4"))
                {
                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes(item))
                    {
                        i++;
                        seletorExtra = 2;

                        if (node.InnerHtml.Contains("automatic_promocode_tiny_bar"))
                        {
                            seletorExtra = 3;
                        }

                        try
                        {
                            if (Regex.IsMatch(node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[1]/div[" + seletorExtra + "]/div/span").InnerHtml, "Boa Viagem|Piedade"))
                            {//*[@id="deals-div"]/div[3]/div[3]/div[12]/div/a/div[1]/div[3]/div[1]/span
                                OfertaViewModel ov = new OfertaViewModel();

                                ov.UrlSite = "http://www.peixeurbano.com.br";
                                ov.Url = ov.UrlSite + node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a").Attributes["href"].Value;
                                ov.Titulo = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[2]/h3/span").InnerHtml;
                                ov.DescricaoResumida = ov.Titulo;
                                ov.UrlImagem = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[1]/img").Attributes["data-original"].Value;
                                ov.Estabelecimento = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[1]/div[" + seletorExtra + "]/div/h4").InnerHtml
                                    .Replace("Bar e Restaurante", "").Replace("Restaurante e Bar", "").Replace("Bar", "").Replace("Restaurante", "");

                                ov.NomeSiteCompraColetiva = "Peixe Urbano";
                                ov.PrecoComDesconto = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[2]/div/div[2]/span").InnerHtml.Trim();
                                //*[@id="div_discount"]/span
                                if (node.InnerHtml.Contains("price_from"))
                                {
                                    ov.ApartirDe = true;
                                    ov.PrecoOriginal = "a partir de";
                                }
                                else
                                {
                                    if (node.InnerHtml.Contains("old_price"))
                                    {
                                        ov.PrecoOriginal = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[2]/div/span").InnerHtml.Replace("<abbr title=\"BRL\">R$</abbr> ", "");
                                    }
                                    else
                                    {
                                        ov.PrecoOriginal = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[16]/div/a/div[2]/div/div[1]/del/span").InnerHtml.Replace("<abbr title=\"BRL\">R$</abbr> ", "");
                                    }
                                }

                                desconto = node.SelectSingleNode("//*[@id=\"deals-div\"]/div[3]/div[3]/div[" + i + "]/div/a/div[1]/div[1]/span").InnerHtml;

                                ov.PorcentagemDesconto = String.IsNullOrEmpty(desconto) ? 0 : Convert.ToInt32(desconto.Replace("%", ""));

                                if (!ov.ApartirDe)
                                {
                                    ov.PrecoOriginal += ov.PrecoOriginal.Split(',').Length == 1 ? ",00" : "";
                                }

                                string[] precoPartes = ov.PrecoComDesconto.ToString().Split(',');
                                ov.PrecoParteEmReal = precoPartes[0];

                                if (precoPartes.Length > 1)
                                {
                                    ov.PrecoParteEmCentavo = precoPartes[1];
                                }
                                else
                                {
                                    ov.PrecoParteEmCentavo += "00";
                                }

                                lista.Add(ov);
                            }
                        }
                        catch (Exception)
                        {
                            //throw;
                        }
                    }
                }
            }

            model.Ofertas = lista;
        }

        private void LerRegateio(HtmlDocument doc, List<string> xPaths, string lugar, string url = null)
        {
            foreach (var item in xPaths)
            {
                if (item.Contains("mainOferta"))
                {
                    var nodes = doc.DocumentNode.SelectNodes(item);

                    foreach (HtmlNode node in nodes)
                    {
                        try
                        {
                            if (Regex.IsMatch(node.SelectSingleNode(@"//*[@id='saibamais-da-oferta']/div[2]/address").InnerHtml, lugar))
                            {
                                OfertaViewModel ov = new OfertaViewModel();
                                ov.PorcentagemDesconto = Convert.ToInt32(node.SelectSingleNode(@"/html/body/div[4]/div[1]/article/div/header/div[1]").InnerHtml.Replace("% OFF", ""));
                                ov.Estabelecimento = node.SelectSingleNode(@"//*[@id='saibamais-da-oferta']/div[1]/h5").InnerHtml.Replace("Bar e Restaurante", "").Replace("Restaurante e Bar", "").Replace("Bar", "").Replace("Restaurante", "");


                                ov.UrlSite = "http://www.regateio.com.br";
                                ov.Url = url != null ? url : "http://www.regateio.com.br/gastronomia";

                                ov.Titulo = node.SelectSingleNode(@"/html/body/div[4]/div[1]/article/div/header/h3").InnerHtml.Replace("<span class=\"tipo1\">", "").Replace("</span>", "");
                                ov.DescricaoResumida = ov.Titulo;
                                ov.UrlImagem = "http://www.regateio.com.br" + node.SelectSingleNode(@"//*[@id='slider']/div[1]").ChildNodes[0].Attributes[1].Value;
                                ov.NomeSiteCompraColetiva = "Regateio";

                                string preco = node.SelectSingleNode(@"/html/body/div[4]/div[1]/article/div/div[2]/div[2]/p").InnerHtml;
                                string[] precos = null;

                                if (preco.Contains("span"))
                                {
                                    precos = node.SelectSingleNode(@"/html/body/div[4]/div[1]/article/div/div[2]/div[2]/p").InnerHtml.Replace("<span>", "*").Replace("</span>", "*").Split('*');

                                    ov.PrecoOriginal = precos[1].Replace("De R$ ", "");
                                    ov.PrecoComDesconto = precos[2].Replace("por R$ ", "");
                                }
                                else
                                {
                                    precos = node.SelectSingleNode(@"/html/body/div[4]/div[1]/article/div/div[2]/div[2]/p").InnerHtml.Replace(" <br> R$ ", "*").Split('*');
                                    ov.PrecoOriginal = "A partir de";
                                    ov.PrecoComDesconto = precos[1];
                                    ov.ApartirDe = true;
                                }

                                if (!ov.ApartirDe)
                                {
                                    ov.PrecoOriginal += ov.PrecoOriginal.Split(',').Length == 1 ? ",00" : "";
                                }

                                string[] precoPartes = ov.PrecoComDesconto.ToString().Split(',');
                                ov.PrecoParteEmReal = precoPartes[0];

                                if (precoPartes.Length > 1)
                                {
                                    ov.PrecoParteEmCentavo = precoPartes[1];
                                }
                                else
                                {
                                    ov.PrecoParteEmCentavo += "00";
                                }
                                lista.Add(ov);
                            }
                        }
                        catch (Exception)
                        {

                            //throw;
                        }
                    }
                }
                else
                {
                    HtmlNodeCollection innerNodes = doc.DocumentNode.SelectNodes(item);
                    int i = 0;
                    int limit = innerNodes.Count / 2;

                    foreach (HtmlNode innerNode in innerNodes)
                    {
                        i++;

                        if (i <= limit)
                        {
                            string urlOferta = "http://www.regateio.com.br" + innerNode.SelectSingleNode(@"/html/body/div[4]/div[2]/aside/div[4]/div[" + i + "]/div[2]/a").Attributes[0].Value;

                            HtmlWeb webget = new HtmlWeb();
                            WebProxy wp = new WebProxy("http://proxy-rf.infranet.gov.br:8080/", true);
                            wp.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                            wp.UseDefaultCredentials = true;

                            //var innerDoc = webget.Load(url, "GET", (WebProxy)WebProxy.GetDefaultProxy(), System.Net.CredentialCache.DefaultNetworkCredentials);
                            var innerDoc = webget.Load(urlOferta, "GET", wp, System.Net.CredentialCache.DefaultNetworkCredentials);
                            //var innerDoc = webget.Load(url);

                            LerRegateio(innerDoc, new List<string>()
                            {
                                "//div[contains(concat(' ',normalize-space(@class),' '),' mainOferta ')]"
                            }, lugar, urlOferta);
                        }
                    }
                }
            }
        }

        private void LerGroupon(HtmlDocument doc, List<string> xPaths)
        {
            foreach (var item in xPaths)
            {
                int cont = 0;
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes(item))
                {
                    try
                    {
                        OfertaViewModel ov = new OfertaViewModel();

                        if (node.ParentNode.Attributes[0].Value.Contains("browse-deals"))
                        {
                            cont++;

                            ov.UrlSite = "http://www.groupon.com.br/browse/recife?category=bares-restaurantes&category2=restaurante";
                            ov.Url = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a").Attributes[0].Value;
                            ov.Titulo = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a/figcaption/div[1]/p[1]").InnerHtml;
                            ov.DescricaoResumida = ov.Titulo;
                            ov.UrlImagem = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a/img").Attributes[0].Value;
                            ov.Estabelecimento = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a/figcaption/div[1]/p[2]").InnerHtml
                                .Replace("Bar e Restaurante", "").Replace("Restaurante e Bar", "").Replace("Bar", "").Replace("Restaurante", "");
                            ov.NomeSiteCompraColetiva = "Groupon";

                            ov.PrecoComDesconto = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a/figcaption/div[2]/p/s[2]").InnerHtml.Replace("R$", "").Replace("Por ", "");
                            ov.PrecoComDesconto += ov.PrecoComDesconto.Split(',').Length == 1 ? ",00" : "";
                            ov.PrecoOriginal = node.SelectSingleNode("//*[@id='browse-deals']/figure[" + cont + "]/a/figcaption/div[2]/p/s[1]").InnerHtml.Replace("R$", "");

                            string[] precoPartes = ov.PrecoComDesconto.ToString().Split(',');
                            ov.PrecoParteEmReal = precoPartes[0];
                            ov.PrecoParteEmCentavo = precoPartes[1];
                            ov.PorcentagemDesconto = Convert.ToInt32(((Convert.ToDouble(ov.PrecoOriginal) - Convert.ToDouble(ov.PrecoComDesconto)) / Convert.ToDouble(ov.PrecoOriginal)) * 100);

                            lista.Add(ov);

                        }
                        else
                        {
                            ov.UrlSite = "http://www.groupon.com.br/browse/recife?category=bares-restaurantes&category2=restaurante";
                            ov.Url = node.SelectSingleNode("//*[@id='browse-deals']/figure[1]/a").Attributes[0].Value;
                            ov.Titulo = node.SelectSingleNode("//*[@id='hero-tile']/figcaption/div[1]/div/p").InnerHtml;
                            ov.DescricaoResumida = ov.Titulo;
                            ov.UrlImagem = node.SelectSingleNode("//*[@id='hero-tile']/a/img").Attributes[0].Value;
                            ov.Estabelecimento = node.SelectSingleNode("//*[@id='hero-tile']/figcaption/div[1]/h3").InnerHtml
                                .Replace("Bar e Restaurante", "").Replace("Restaurante e Bar", "").Replace("Bar", "").Replace("Restaurante", "");
                            ov.NomeSiteCompraColetiva = "Groupon";

                            ov.PrecoComDesconto = node.SelectSingleNode("//*[@id='hero-tile']/figcaption/div[2]/p/s[2]").InnerHtml.Replace("R$", "").Replace("Por ", "");
                            ov.PrecoComDesconto += ov.PrecoComDesconto.Split(',').Length == 1 ? ",00" : "";
                            ov.PrecoOriginal = node.SelectSingleNode("//*[@id='hero-tile']/figcaption/div[2]/p/s[1]").InnerHtml.Replace("R$", "");

                            string[] precoPartes = ov.PrecoComDesconto.ToString().Split(',');
                            ov.PrecoParteEmReal = precoPartes[0];
                            ov.PrecoParteEmCentavo = precoPartes[1];
                            ov.PorcentagemDesconto = Convert.ToInt32(((Convert.ToDouble(ov.PrecoOriginal) - Convert.ToDouble(ov.PrecoComDesconto)) / Convert.ToDouble(ov.PrecoOriginal) * 100));


                            lista.Add(ov);
                        }
                    }
                    catch (Exception)
                    {

                        //throw;
                    }
                }
            }
        }
    }
}
