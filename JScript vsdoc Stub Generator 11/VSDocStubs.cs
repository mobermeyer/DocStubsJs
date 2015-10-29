using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using JScriptStubOptions;
using System.Linq;

namespace JScript_vsdoc_Stub_Generator_11
{
    ///<summary>
    ///Auto generates vsdoc comments on triple slash in JavaScript files.
    ///</summary>
    public class VSDocStubs
    {
        IWpfTextView _view;
        ITextBuffer editor;
        const string SUMMARY_OPEN = "<summary>";
        const string SUMMARY_CLOSE = "</summary>";
        string tabs = "";
        private const string errorMsgPrefix = "JScript vsdoc Stub Generator has encountered an error:\n";

        public VSDocStubs(IWpfTextView view)
        {
            _view = view;
            this.editor = _view.TextBuffer;
            //Listen for text changed event.
            this.editor.Changed += OnTextChanged;
        }

        /// <summary>
        /// On text change, check for the /// comment.
        /// </summary>
        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                if (!StubUtils.Options.VSDocEnabled) { return; }

                INormalizedTextChangeCollection changes = e.Changes;
                
                foreach (ITextChange change in changes)
                {
                    if (change.NewText.EndsWith("/") && EndsInDoubleSlash(change.OldEnd))
                        CreateStub(change.NewEnd, change);
                    else if (StubUtils.Options.AutoNewLine && change.NewText.EndsWith(Environment.NewLine))
                        CreateNewCommentLine(change);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(errorMsgPrefix + ex.Message);
            }
        }

        private void CreateNewCommentLine(ITextChange change)
        {
            using (ITextEdit editor = this._view.TextBuffer.CreateEdit())
            {
                try
                {
                    ITextSnapshotLine line = this._view.TextSnapshot.GetLineFromPosition(change.OldEnd);
                    string lineText = line.GetText();
                    string nextLine = this._view.TextSnapshot.GetLineFromLineNumber(line.LineNumber + 1).GetText();
                    if (lineText.Trim().StartsWith("///") && (nextLine.Trim().StartsWith("///") || change.OldEnd != line.End.Position))
                    {
                        int slashIndex = lineText.IndexOf('/');
                        //Only add a new comment line if the newline char is after the triple slash
                        //(how Visual Studio in C# works)
                        if ((line.Start.Position + 3 + slashIndex) > change.OldEnd)
                            return;

                        string newTabs = lineText.Substring(0, slashIndex);
                        editor.Replace(change.NewSpan, Environment.NewLine + newTabs + "/// ");
                        editor.Apply();
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Returns true if the line at the given position ends with a double slash prior to any pending changes.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool EndsInDoubleSlash(int i)
        {
            return StubUtils.GetLineTextFromPosition(i, this._view.TextSnapshot).EndsWith("//");
        }

        /// <summary>
        /// Builds the comment stub and inserts it into the editor.
        /// </summary>
        /// <param name="position">The position of the last slash.</param>
        private void CreateStub(int position, ITextChange change)
        {
            string text = this._view.TextSnapshot.ToString();
            using (ITextEdit editor = this._view.TextBuffer.CreateEdit())
            {
                try
                {
                    this.tabs = StubUtils.GetIndention(position, this._view.TextSnapshot);
                    string summaryTag = generateSummaryTag();
                    string parameters = getFunctionParameters(position);
                    string returnTag = getReturnTag(position);
                    string autoComment = summaryTag + parameters + returnTag;

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

        private string generateSummaryTag()
        {
            string result = this.tabs + "/// " + SUMMARY_OPEN;

            if (StubUtils.Options.MultiLineSummary)
            {
                result += NewLine() + NewLine();
            }

            result += SUMMARY_CLOSE;

            return result;
        }

        /// <summary>
        /// Returns a string for a return tag if one is necessary.
        /// </summary>
        /// <param name="position">Position of the last slash in the triple slash comment</param>
        /// <returns>Return tag line as a string.</returns>
        private string getReturnTag(int position)
        {
            string result = "";
            bool shouldCreate = StubUtils.ShouldCreateReturnTag(position, this._view.TextSnapshot);

            if (shouldCreate)
                result = NewLine() + createReturnString();

            return result;
        }

        private string createReturnString()
        {
            var attrs = StubUtils.Options.ReturnAttributes;
            var result = "<returns";

            if (!String.IsNullOrEmpty(StubUtils.Options.ReturnAttributes))
            {
                result += " " + attrs;
            }

            result += ">";

            if (StubUtils.Options.MultiLineReturn)
            {
                result += NewLine() + NewLine();
            }

            return result + "</returns>";
        }

        /// <summary>
        /// Creates a new comment line with appropriate spacing.
        /// </summary>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private string NewLine()
        {
            return Environment.NewLine + this.tabs + "/// ";
        }

        private string getFunctionParameters(int position)
        {
            var parameters = StubUtils.GetFunctionParameters(position, this._view.TextSnapshot);
            var result = "";

            foreach (var param in parameters)
            {
                if (!String.IsNullOrEmpty(param.Name))
                    result += NewLine() + createParamString(param.Name, param.Type);
            }

            return result;
        }

        Regex typeAttributeTest = new Regex("\\btype=\"([^\"]*\")\\s?", RegexOptions.Compiled);
        private string createParamString(string name, string type)
        {
            var result = "<param name=\"" + name + "\"";
            var defaultParamAttributes = StubUtils.Options.ParamAttributes;
            if (!String.IsNullOrEmpty(type))
            {
                result += " type=\"" + type + "\"";
                if (defaultParamAttributes.Contains("type="))
                {
                    defaultParamAttributes = typeAttributeTest.Replace(defaultParamAttributes, "");
                }
            }
            if (!String.IsNullOrEmpty(defaultParamAttributes))
            {
                result += " " + StubUtils.Options.ParamAttributes;
            }

            result += ">";

            if (StubUtils.Options.MultiLineParam)
            {
                result += NewLine() + NewLine();
            }

            return result + "</param>";
        }
    }
}
