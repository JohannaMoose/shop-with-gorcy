using FluentAssertions;
using GrocyShopping.Citygross;
using NUnit.Framework;

namespace GrocyShopping.Tests.Citygross.UnitTests;

[TestFixture]
public class CitygrossOnlineOrderParser_Specs
{
    private CitygrossOnlineOrderParser SUT;

    private string LoadTestHtml()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var mainFolder = currentDir.Substring(0, currentDir.Length - 16);
        var testFile = mainFolder + @"Citygross\TestData\StandardOrderEmail.txt";

        var html = File.ReadAllText(testFile);
        return html;
    }

    [Test]
    public void All_products_parsed()
    {
        var html = LoadTestHtml();
        SUT = new CitygrossOnlineOrderParser();

        var products = SUT.ParseOnlineOrder(html);

        products.Should().HaveCount(41);
    }
}