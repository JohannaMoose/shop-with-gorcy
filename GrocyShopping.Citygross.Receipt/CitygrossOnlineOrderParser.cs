using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace GrocyShopping.Citygross;

public class CitygrossOnlineOrderParser
{
    public IEnumerable<BoughtProduct> ParseOnlineOrder(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var header = doc.DocumentNode.Descendants().First(x => x.InnerText == "Varor" && x.Name == "thead");
        var table = header.ParentNode;
        var productsElements = table.ChildNodes.Where(x => x.Name == "tbody");

        return productsElements.Select(ParseProduct);
    }

    private static BoughtProduct ParseProduct(HtmlNode productElement)
    {
        var productPartsText = productElement.ChildNodes
            .Where(x => !string.IsNullOrWhiteSpace(x.InnerText) && x.InnerText != "&nbsp;")
            .SelectMany(x => x.ChildNodes).Select(x => x.InnerText).ToList();

        var name = productPartsText[0];
        var nbrOfProducts = Convert.ToInt32(productPartsText[1].Replace(" st", "", StringComparison.OrdinalIgnoreCase));
        var price = Convert.ToDouble(productPartsText[3].Replace(" kr", "", StringComparison.OrdinalIgnoreCase));
        var brand = Regex.Match(productPartsText[2], @"(.*) - ").Groups[1].Value;
        var rawAmount = Regex.Match(productPartsText[2], @".* - (.*)").Groups[1].Value;

        var product = new BoughtProduct(name, brand, price, nbrOfProducts, rawAmount);
        return product;
    }
}