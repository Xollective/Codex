using System.Collections.Generic;
using System.Linq;
using Codex.ObjectModel;
using Codex.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Language.Xml;

using TextSpan = Microsoft.Language.Xml.TextSpan;

namespace Codex.Analysis
{
    public static class XmlSyntaxExtensions
    {
        public static IEnumerable<IXmlElement> Elements(this IXmlElement element, string name)
        {
            return element.Elements.Where(el => el.Name == name);
        }

        public static XmlNameSyntax NameNode(this IXmlElement element)
        {
            return element.AsSyntaxElement.NameNode;
        }

        public static TextSpan ValueSpan(this IXmlElement element)
        {
            return element.AsSyntaxElement.Content.FullSpan;
        }

        public static XmlAttributeSyntax Attribute(this IXmlElement element, string name)
        {
            return element.AsSyntaxElement.Attributes.Where(att => att.Name == name).FirstOrDefault();
        }

        public static int End(this XmlNodeSyntax node)
        {
            return node.Start + node.FullWidth;
        }

        public static T As<T>(this IXmlElement node) where T : XmlNodeSyntax
        {
            return node as T;
        }

        public static T As<T>(this XmlNodeSyntax node) where T : XmlNodeSyntax
        {
            return node as T;
        }

        public static TextSpan GetTextSpan(this XmlStringSyntax node)
        {
            return TextSpan.FromBounds(node.StartQuoteToken.End, node.EndQuoteToken.Start);
        }

        public static void AddAttributeValueReferences(
            this BoundSourceFileBuilder binder,
            XmlAttributeSyntax attribute,
            params ReferenceSymbol[] references)
        {
            var span = GetAttributeValueSpan(attribute);
            if (span != null)
            {
                binder.AnnotateReferences(span.Value.Start, span.Value.Length, references);
            }
        }

        private static Extent? GetAttributeValueSpan(XmlAttributeSyntax attribute)
        {
            var valueNode = attribute?.ValueNode.As<XmlStringSyntax>();
            if (valueNode != null)
            {
                return Extent.FromBounds(valueNode.StartQuoteToken.End, valueNode.EndQuoteToken.Start);
            }

            return null;
        }

        public static void AddAttributeNameReferences(
            this BoundSourceFileBuilder binder,
            XmlAttributeSyntax attribute,
            params ReferenceSymbol[] references)
        {
            var nameSpan = attribute?.NameNode.Span;
            if (nameSpan != null)
            {
                binder.AnnotateReferences(nameSpan.Value.Start, nameSpan.Value.Length, references);
            }
        }

        public static void AddAttributeValueDefinition(
            this BoundSourceFileBuilder binder,
            XmlAttributeSyntax attribute,
            DefinitionSymbol definition)
        {
            var span = GetAttributeValueSpan(attribute);
            if (span != null)
            {
                binder.AnnotateDefinition(span.Value.Start, span.Value.Length, definition);
            }
        }

        public static void AddElementNameReferences(
            this BoundSourceFileBuilder binder,
            IXmlElement element,
            params ReferenceSymbol[] references)
        {
            var nameSpan = element.NameNode().Span;
            binder.AnnotateReferences(nameSpan.Start, nameSpan.Length, references);
        }
    }
}