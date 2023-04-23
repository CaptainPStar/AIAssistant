using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;


namespace EVA.Controls
{
    public class HyperlinkTextBlock : TextBlock
    {
        public static readonly DependencyProperty CustomTextProperty =
           DependencyProperty.Register("CustomText", typeof(string), typeof(HyperlinkTextBlock), new PropertyMetadata(string.Empty, OnCustomTextChanged));

        public string CustomText
        {
            get => (string)GetValue(CustomTextProperty);
            set => SetValue(CustomTextProperty, value);
        }

        private static void OnCustomTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HyperlinkTextBlock hyperlinkTextBlock)
            {
                hyperlinkTextBlock.Inlines.Clear();
                AddLinks(hyperlinkTextBlock, hyperlinkTextBlock.CustomText);
            }
        }

        private static void AddLinks(HyperlinkTextBlock hyperlinkTextBlock, string text)
        {
            string[] parts = text.Split(' ');

            string urlPattern = @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
            string filePathPattern = @"[a-zA-Z]:\\(((?![<>:""\\|?*]).)+((?<![ .])\\)?)*";
            var urlMatches = Regex.Matches(text, urlPattern);
            var localFileMatches = Regex.Matches(text, filePathPattern);

            int cursorPosition = 0;

            foreach (Match match in urlMatches)
            {
                AddText(hyperlinkTextBlock, text, cursorPosition, match.Index);
                cursorPosition = match.Index + match.Length;

                string cleanUrl = CleanMatchedLink(match.Value);
                Hyperlink link = CreateHyperlink(cleanUrl, cleanUrl);
                hyperlinkTextBlock.Inlines.Add(link);
            }

            foreach (Match match in localFileMatches)
            {
                AddText(hyperlinkTextBlock, text, cursorPosition, match.Index);
                cursorPosition = match.Index + match.Length;

                string cleanPath = CleanMatchedLink(match.Value);
                Hyperlink link = CreateHyperlink(cleanPath, cleanPath);
                link.RequestNavigate += (s, e) =>
                {
                    if (Directory.Exists(e.Uri.LocalPath))
                    {
                        Process.Start("explorer.exe", e.Uri.LocalPath);
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo(e.Uri.LocalPath) { UseShellExecute = true });
                    }
                };
                hyperlinkTextBlock.Inlines.Add(link);
            }

            if (cursorPosition < text.Length)
            {
                hyperlinkTextBlock.Inlines.Add(text.Substring(cursorPosition));
            }
        }
        private static Hyperlink CreateHyperlink(string text, string navigateUri)
        {
            Hyperlink link = new Hyperlink(new Run(text)) { NavigateUri = new Uri(navigateUri, UriKind.RelativeOrAbsolute) };
            link.RequestNavigate += (s, e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            return link;
        }
        private static void AddText(HyperlinkTextBlock hyperlinkTextBlock, string text, int start, int end)
        {
            if (end > start)
            {
                hyperlinkTextBlock.Inlines.Add(text.Substring(start, end - start));
            }
        }
        private static string CleanMatchedLink(string matchedLink)
        {
            return matchedLink.Trim('\"', '\'').TrimEnd('\n',',', ';', '.', '\"', '\'');
        }

    }
}
