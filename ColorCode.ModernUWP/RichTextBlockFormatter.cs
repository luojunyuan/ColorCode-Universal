// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using ColorCode.Parsing;
using ColorCode.Styling;
using Windows.UI.Text;
using ColorCode.Common;
#if WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using ColorCode.WinUI.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using ColorCode.WinUI.Common;
using Windows.UI.Xaml;
#endif

namespace ColorCode
{
    public class RichTextBlockFormatter : CodeColorizerBase
    {
        public RichTextBlockFormatter(ElementTheme Theme, ILanguageParser languageParser = null) : this(Theme == ElementTheme.Dark ? StyleDictionary.DefaultDark : StyleDictionary.DefaultLight, languageParser)
        {
        }

        public RichTextBlockFormatter(StyleDictionary Style = null, ILanguageParser languageParser = null) : base(Style, languageParser)
        {
        }

        public void FormatRichTextBlock(string sourceCode, ILanguage Language, RichTextBlock RichText)
        {
            var paragraph = new Paragraph();
            RichText.Blocks.Add(paragraph);
            FormatInlines(sourceCode, Language, paragraph.Inlines);
        }

        public void FormatInlines(string sourceCode, ILanguage Language, InlineCollection InlineCollection)
        {
            this.InlineCollection = InlineCollection;
            languageParser.Parse(sourceCode, Language, (parsedSourceCode, captures) => Write(parsedSourceCode, captures));
        }

        private InlineCollection InlineCollection { get; set; }

        protected override void Write(string parsedSourceCode, IList<Scope> scopes)
        {
            var styleInsertions = new List<TextInsertion>();

            foreach (Scope scope in scopes)
                GetStyleInsertionsForCapturedStyle(scope, styleInsertions);

            styleInsertions.SortStable((x, y) => x.Index.CompareTo(y.Index));

            int offset = 0;

            Scope PreviousScope = null;

            foreach (var styleinsertion in styleInsertions)
            {
                var text = parsedSourceCode.Substring(offset, styleinsertion.Index - offset);
                CreateSpan(text, PreviousScope);
                if (!string.IsNullOrWhiteSpace(styleinsertion.Text))
                {
                    CreateSpan(text, PreviousScope);
                }
                offset = styleinsertion.Index;

                PreviousScope = styleinsertion.Scope;
            }

            var remaining = parsedSourceCode.Substring(offset);
            if (remaining != "\r")
            {
                CreateSpan(remaining, null);
            }
        }

        private void CreateSpan(string Text, Scope scope)
        {
            var span = new Span();
            var run = new Run
            {
                Text = Text
            };

            if (scope != null) StyleRun(run, scope);
            span.Inlines.Add(run);

            InlineCollection.Add(span);
        }

        private void StyleRun(Run Run, Scope Scope)
        {
            string foreground = null;
            string background = null;
            bool italic = false;
            bool bold = false;

            if (Styles.Contains(Scope.Name))
            {
                Styling.Style style = Styles[Scope.Name];

                foreground = style.Foreground;
                background = style.Background;
                italic = style.Italic;
                bold = style.Bold;
            }

            if (!string.IsNullOrWhiteSpace(foreground))
                Run.Foreground = foreground.GetSolidColorBrush();

            if (italic)
                Run.FontStyle = FontStyle.Italic;

            if (bold)
                Run.FontWeight = FontWeights.Bold;
        }

        private void GetStyleInsertionsForCapturedStyle(Scope scope, ICollection<TextInsertion> styleInsertions)
        {
            styleInsertions.Add(new TextInsertion
            {
                Index = scope.Index,
                Scope = scope
            });

            foreach (Scope childScope in scope.Children)
                GetStyleInsertionsForCapturedStyle(childScope, styleInsertions);

            styleInsertions.Add(new TextInsertion
            {
                Index = scope.Index + scope.Length
            });
        }
    }
}
