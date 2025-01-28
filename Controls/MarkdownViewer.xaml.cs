using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Controls
{
    public class MarkdownViewer : RichTextBox
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                "Markdown",
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        public MarkdownViewer()
        {
            IsReadOnly = true;
            BorderThickness = new Thickness(0);
            Background = Brushes.Transparent;
            Document.LineHeight = 1.5;
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer)
            {
                viewer.RenderMarkdown();
            }
        }

        private void RenderMarkdown()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var document = new FlowDocument();
            
            // 解析Markdown
            var html = Markdig.Markdown.ToHtml(Markdown ?? string.Empty, pipeline);

            // 处理HTML
            ProcessHtml(html, document);

            Document = document;
        }

        private void ProcessHtml(string html, FlowDocument document)
        {
            // 解码HTML实体
            html = HttpUtility.HtmlDecode(html);

            // 分割代码块和普通文本
            var parts = Regex.Split(html, @"(<pre><code.*?>.*?</code></pre>|<ul>.*?</ul>|<ol>.*?</ol>|<h[1-6]>.*?</h[1-6]>|<blockquote>.*?</blockquote>)", RegexOptions.Singleline);

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                if (part.StartsWith("<pre><code"))
                {
                    AddCodeBlock(part, document);
                }
                else if (part.StartsWith("<ul>"))
                {
                    AddUnorderedList(part, document);
                }
                else if (part.StartsWith("<ol>"))
                {
                    AddOrderedList(part, document);
                }
                else if (part.StartsWith("<h"))
                {
                    AddHeading(part, document);
                }
                else if (part.StartsWith("<blockquote>"))
                {
                    AddBlockquote(part, document);
                }
                else
                {
                    AddTextBlock(part, document);
                }
            }
        }

        private void AddHeading(string html, FlowDocument document)
        {
            var match = Regex.Match(html, @"<h(\d)>(.*?)</h\1>");
            if (match.Success)
            {
                int level = int.Parse(match.Groups[1].Value);
                var text = match.Groups[2].Value;

                var paragraph = new Paragraph(new Run(text))
                {
                    FontSize = 24 - ((level - 1) * 2),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5)
                };

                document.Blocks.Add(paragraph);
            }
        }

        private void AddUnorderedList(string html, FlowDocument document)
        {
            var list = new List();
            var items = Regex.Matches(html, @"<li>(.*?)</li>", RegexOptions.Singleline);

            foreach (Match item in items)
            {
                var listItem = new ListItem(new Paragraph(new Run(item.Groups[1].Value)));
                list.ListItems.Add(listItem);
            }

            list.Margin = new Thickness(0, 5, 0, 5);
            document.Blocks.Add(list);
        }

        private void AddOrderedList(string html, FlowDocument document)
        {
            var list = new List() { MarkerStyle = TextMarkerStyle.Decimal };
            var items = Regex.Matches(html, @"<li>(.*?)</li>", RegexOptions.Singleline);

            foreach (Match item in items)
            {
                var listItem = new ListItem(new Paragraph(new Run(item.Groups[1].Value)));
                list.ListItems.Add(listItem);
            }

            list.Margin = new Thickness(0, 5, 0, 5);
            document.Blocks.Add(list);
        }

        private void AddBlockquote(string html, FlowDocument document)
        {
            var match = Regex.Match(html, @"<blockquote>(.*?)</blockquote>", RegexOptions.Singleline);
            if (match.Success)
            {
                var section = new Section
                {
                    BorderThickness = new Thickness(4, 0, 0, 0),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    Padding = new Thickness(10, 0, 0, 0),
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var paragraph = new Paragraph(new Run(match.Groups[1].Value))
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };

                section.Blocks.Add(paragraph);
                document.Blocks.Add(section);
            }
        }

        private void AddCodeBlock(string html, FlowDocument document)
        {
            var match = Regex.Match(html, @"<pre><code.*?class=""language-(\w+)"">(.*?)</code></pre>", RegexOptions.Singleline);
            string language = match.Success ? match.Groups[1].Value : string.Empty;
            string codeContent = match.Success ? match.Groups[2].Value : 
                Regex.Match(html, @"<pre><code>(.*?)</code></pre>", RegexOptions.Singleline).Groups[1].Value;

            var section = new Section
            {
                Background = new SolidColorBrush(Color.FromRgb(246, 248, 250)),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 5),
            };

            if (!string.IsNullOrEmpty(language))
            {
                var languageIndicator = new Paragraph(new Run(language))
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                section.Blocks.Add(languageIndicator);
            }

            var paragraph = new Paragraph(new Run(codeContent))
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                LineHeight = 1.4
            };

            section.Blocks.Add(paragraph);
            document.Blocks.Add(section);
        }

        private void AddTextBlock(string html, FlowDocument document)
        {
            var paragraph = new Paragraph();

            // 处理加粗
            html = ProcessInlineElement(html, "strong", text => new Bold(new Run(text)));
            
            // 处理斜体
            html = ProcessInlineElement(html, "em", text => new Italic(new Run(text)));
            
            // 处理行内代码
            html = ProcessInlineElement(html, "code", text => new Run(text)
            {
                Background = new SolidColorBrush(Color.FromRgb(246, 248, 250)),
                FontFamily = new FontFamily("Consolas")
            });

            // 移除其他HTML标签
            html = Regex.Replace(html, "<[^>]+>", "");

            // 添加剩余文本
            if (!string.IsNullOrWhiteSpace(html))
            {
                paragraph.Inlines.Add(new Run(html));
            }

            if (paragraph.Inlines.Count > 0)
            {
                document.Blocks.Add(paragraph);
            }
        }

        private string ProcessInlineElement(string html, string tag, Func<string, Inline> createInline)
        {
            var pattern = $"<{tag}>(.*?)</{tag}>";
            return Regex.Replace(html, pattern, match =>
            {
                var text = match.Groups[1].Value;
                return $"__INLINE_ELEMENT_{tag}_{text}__";
            });
        }
    }
} 