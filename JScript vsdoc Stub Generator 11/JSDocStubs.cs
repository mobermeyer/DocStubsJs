using JScriptStubOptions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JScript_vsdoc_Stub_Generator_11
{
    class JSDocStubs
    {
        IWpfTextView _view;
        ITextBuffer editor;
        const string SUMMARY_OPEN = "<summary>";
        const string SUMMARY_CLOSE = "</summary>";
        string tabs = "";
        private const string errorMsgPrefix = "JScript vsdoc Stub Generator has encountered an error:\n";
        private string contentType = string.Empty;

        public JSDocStubs(IWpfTextView view)
        {
            _view = view;
            this.editor = _view.TextBuffer;
            
            //Listen for text changed event.
            this.editor.Changed += OnTextChanged;

            // Store the content type name.
            this.contentType = this.editor.ContentType.TypeName;

            // Pass content type name to StubUtils for further usage.
            // For example, for generating parameter type tag of JavaScript content type, we should not depend on ":" syntax.
            StubUtils.ContentTypeName = this.contentType;
        }

        //Microsoft.VisualStudio.Text.Impl
        /// <summary>
        /// On text change, check for the /// comment.
        /// </summary>
        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                if (!StubUtils.Options.JSDocEnabled) { return; }

                INormalizedTextChangeCollection changes = e.Changes;

                foreach (ITextChange change in changes)
                {
                    if (change.NewText.EndsWith("*") && LineIsJSDocOpening(change.OldEnd))
                        CreateStub(change.NewEnd, change);
                    else if (StubUtils.Options.AutoNewLine && StubUtils.Options.UseAsterisk && change.NewText.EndsWith(Environment.NewLine))
                        CreateNewCommentLine(change);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(errorMsgPrefix + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if the line at the given position ends with the /* characters prior to any pending changes.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool LineIsJSDocOpening(int i)
        {
            return StubUtils.GetLineTextFromPosition(i, this._view.TextSnapshot).EndsWith("/*");
        }

        private static Regex commentLineStart = new Regex(@"^\s*(\*)|(/\*\*)", RegexOptions.Compiled);
        private void CreateNewCommentLine(ITextChange change)
        {
            using (ITextEdit editor = this._view.TextBuffer.CreateEdit())
            {
                try
                {
                    ITextSnapshotLine line = this._view.TextSnapshot.GetLineFromPosition(change.OldEnd);
                    string lineText = line.GetText();
                    string nextLine = this._view.TextSnapshot.GetLineFromLineNumber(line.LineNumber + 1).GetText();
                    if (commentLineStart.IsMatch(lineText) && (commentLineStart.IsMatch(nextLine) || change.OldEnd != line.End.Position))
                    {
                        int asteriskIndex = lineText.IndexOf('*');
                        //Only add a new comment line if the newline char is after the triple slash
                        //(how Visual Studio in C# works)
                        if ((line.Start.Position + asteriskIndex) > change.OldEnd)
                            return;

                        int tabsStopIndex = -1;
                        if (asteriskIndex >= 0 || lineText.Contains("/**"))
                        {
                            // There's no slash, or its open-comment line, so it's a jsdoc comment. We need asteriskIndex here.
                            tabsStopIndex = asteriskIndex;
                        }

                        string newTabs = tabsStopIndex >= 0 ? lineText.Substring(0, tabsStopIndex) : "";
                        newTabs = newTabs.Replace('/', ' ');
                        editor.Replace(change.NewSpan, Environment.NewLine + newTabs + "* ");
                        editor.Apply();
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Creates a new comment line with appropriate spacing.
        /// </summary>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private string NewLine()
        {
            var result = Environment.NewLine + this.tabs;
            if (StubUtils.Options.UseAsterisk)
            {
                result += " * ";
            }

            return result;
        }

        private void CreateStub(int position, ITextChange change)
        {
            string text = this._view.TextSnapshot.ToString();
            using (ITextEdit editor = this._view.TextBuffer.CreateEdit())
            {
                try
                {
                    this.tabs = StubUtils.GetIndention(position, this._view.TextSnapshot, true);
                    string summaryString = StubUtils.Options.MultiLineSummary ? NewLine() : "";
                    string parameters = getFunctionParameters(position);
                    string returnTag = getReturnTag(position);
                    string commentBody = summaryString + parameters + returnTag;
                    string autoComment = this.tabs + "/**" + commentBody;
                    if (!String.IsNullOrEmpty(commentBody))
                    {
                        autoComment += Environment.NewLine + this.tabs;
                    }

                    autoComment += " */";

                    int lineStart = this._view.TextSnapshot.GetLineFromPosition(position).Start.Position;
                    Span firstLineSpan = new Span(lineStart, change.NewSpan.End - lineStart);
                    editor.Replace(firstLineSpan, autoComment);
                    ITextSnapshotLine prevLine = this._view.TextSnapshot.GetLineFromPosition(position);

                    var after = editor.Apply();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(errorMsgPrefix + ex.Message);
                }
            }
        }

        private string getFunctionParameters(int position)
        {
            var parameters = StubUtils.GetFunctionParameters(position, this._view.TextSnapshot, true);
            var result = "";

            foreach (string param in parameters)
            {
                string name = StubUtils.GetParamName(param);
                string type = StubUtils.GetParamType(param);
                if (!String.IsNullOrEmpty(name))
                    result += NewLine() + createParamString(name, type);
            }

            return result;
        }

        private string createParamString(string name, string type)
        {
            var result = "@param ";
            if (!String.IsNullOrEmpty(type))
            {
                result += "{" + type + "} ";
            }

            return result + name;
        }

        /// <summary>
        /// Returns a string for a return tag if one is necessary.
        /// </summary>
        /// <param name="position">Position of the last slash in the triple slash comment</param>
        /// <returns>Return tag line as a string.</returns>
        private string getReturnTag(int position)
        {
            string result = "";
            bool shouldCreate = StubUtils.ShouldCreateReturnTag(position, this._view.TextSnapshot, true);

            if (shouldCreate)
            {
                result = NewLine() + "@returns";

                if (contentType.Equals("JavaScript"))
                {
                    // Add {type} for JavaScript doc.
                    result += " {type} ";
                }
            }

            return result;
        }

    }
}
