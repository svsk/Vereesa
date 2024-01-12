using HtmlAgilityPack;

namespace Vereesa.Neon.Extensions
{
    public static class HtmlNodeExtensions
    {
        public static HtmlNodeCollection SelectChildNodes(this HtmlNode node, string xpath)
        {
            return HtmlNode.CreateNode(node.InnerHtml).SelectNodes(xpath);
        }

        public static HtmlNode SelectSingleChildNode(this HtmlNode node, string xpath)
        {
            return HtmlNode.CreateNode(node.InnerHtml).SelectSingleNode(xpath);
        }
    }
}
