using JScript_vsdoc_Stub_Generator_11.Symbols;
using JScriptStubOptions;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JScript_vsdoc_Stub_Generator_11
{
    class StubUtils
    {
        public static readonly Regex returnRegex = new Regex("return ");
        public static readonly Regex javaScriptFnRegex = new Regex(@"function(\(|\s)");

        public static readonly ReturnOptions Options = new ReturnOptions();

        private static string contentTypeName = string.Empty;

        public static string ContentTypeName
        {
            get { return contentTypeName; }
            set { contentTypeName = value; }
        }

        public static string GetLineTextFromPosition(int position, ITextSnapshot snapshot)
        {
            return snapshot.GetLineFromPosition(position - 1).GetText();
        }

        /// <summary>
        /// Gets the number of tabs from the beginning of the line.
        /// </summary>
        /// <param name="lastSlashPosition"></param>
        /// <param name="capture">The snapshot to use as the context of the line.</param>
        /// <returns></returns>
        public static string GetIndention(int lastSlashPosition, ITextSnapshot capture, bool isAboveFunction = false)
        {
            int lineNum = capture.GetLineNumberFromPosition(lastSlashPosition);
            if (isAboveFunction) { lineNum++; }
            else { lineNum--; }

            lineNum = GetFunctionDeclarationLineNumber(capture, lineNum, isAboveFunction);
            string space = capture.GetLineFromLineNumber(lineNum).GetText();
            int leadingSpace = space.Length - space.TrimStart().Length;
            space = space.Substring(0, leadingSpace);

            if (isAboveFunction) { return space; }

            return space + GetTab();
        }

        /// <summary>
        /// Returns the equivalent string reprenstation of a tab based on the Visual Studio environment settings.
        /// </summary>
        /// <returns></returns>
        public static string GetTab()
        {
            if (Options.UseSpacesForTabs)
            {
                return String.Join("", Enumerable.Range(0, Options.SpacesForTabsCount).Select(i => " "));
            }
            else
            {
                return "\t";
            }
        }

        private static bool IsJSFunctionLine(string lineText)
        {
            return javaScriptFnRegex.IsMatch(lineText);
        }

        /// <summary>
        /// Returns the line on which the word "function" or other function initializers appear.
        /// </summary>
        /// <param name="capture">The text snapshot.</param>
        /// <param name="lineNumber">The line that should contain the open curlybrace for the function if one exists
        /// in the context of the comment, or the first line of the function itself.</param>
        /// <returns>Returns the line of the function declaration. -1 if one is not found that corresponds to the given
        /// line number.</returns>
        public static int GetFunctionDeclarationLineNumber(ITextSnapshot capture, int lineNumber, bool isAboveFunction = false)
        {
            string lineText = capture.GetLineFromLineNumber(lineNumber).GetText();
            string unCommentedLine = RemoveComments(lineText);
            
            //Ignore inline functions if this is an "inside-the-function" doc (i.e, vsdoc)
            if (!isAboveFunction && !unCommentedLine.Trim().EndsWith("{")) return -1;

            if (capture.ContentType.TypeName == "TypeScript")
            {
                return GetTypeScriptFunctionLine(capture, lineNumber, isAboveFunction, lineText);
            }
            else
            {
                return GetJavaScriptFunctionLine(capture, lineNumber, isAboveFunction, lineText);
            }
        }

        private static int GetJavaScriptFunctionLine(ITextSnapshot capture, int lineNumber, bool isAboveFunction, string lineText)
        {
            if (!isAboveFunction)
            {
                while (!IsJSFunctionLine(lineText))
                {
                    lineNumber--;
                    lineText = capture.GetLineFromLineNumber(lineNumber).Extent.GetText();
                    //There is no function declaration associated with the curly brace.
                    if (lineText.Contains("{")) return -1;
                }
            }
            else if (!IsJSFunctionLine(lineText)) { return -1; }

            return lineNumber;
        }

        private static readonly Regex hasMatchingParens = new Regex(@"\(.*\)");
        private static readonly Regex whitespace = new Regex(@"\s");
        private static Regex keywordWithParen = new Regex(@"(if|else if|while|for)\(");
        public static readonly Regex typeScriptFnRegex = new Regex(@":\s?\([a-z_$]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // This function could use some refinement as it will return some false positives,
        // but hopefully they are rare enough to not cause any major issues. I would much 
        // rather return a couple false positives than false negatives.
        private static int GetTypeScriptFunctionLine(ITextSnapshot capture, int lineNumber, bool isAboveFunction, string lineText)
        {
            if (!isAboveFunction)
            {
                while (!lineText.Contains('(') && !typeScriptFnRegex.IsMatch(lineText))
                {
                    lineNumber--;
                    lineText = capture.GetLineFromLineNumber(lineNumber).Extent.GetText();
                    //There is no function declaration associated with the curly brace.
                    if (lineText.Contains('{')) return -1;
                }

                return lineNumber;
            }
            else
            {
                if (typeScriptFnRegex.IsMatch(lineText)) { return lineNumber; }
                if (keywordWithParen.IsMatch(lineText)) { return -1; }
                
                var line = capture.GetLineFromLineNumber(lineNumber);
                var parenOpen = lineText.IndexOf("(");
                var parenBlock = GetCompleteParenBlock(capture, lineNumber, line.Start + lineText.IndexOf("("));
                if (parenBlock == null) { return -1; }

                // add one because GetCompleteParenBlock excludes closing parenthesis.
                var endParamsPosition = line.Start.Position + parenOpen + parenBlock.Length + 1;

                var lineEnd = capture.GetLineFromPosition(endParamsPosition);
                var startTextAfterParams = endParamsPosition + 1;
                var textAfterParams = capture.GetText(startTextAfterParams, lineEnd.End.Position - startTextAfterParams);
                var lineCounter = lineEnd.LineNumber;

                while (!lineText.Contains('{') && lineCounter < capture.LineCount)
                {
                    lineCounter++;
                    lineText = capture.GetLineFromLineNumber(lineCounter).Extent.GetText();
                    textAfterParams += lineText;
                }

                if (!lineText.Contains('{'))
                {
                    return -1;
                }

                textAfterParams = textAfterParams.Substring(0, textAfterParams.LastIndexOf('{'));
                // If there is no text between the ) and {, then we know it is a valid function header.
                if (String.IsNullOrWhiteSpace(textAfterParams)) { return lineNumber; }

                // If there is text between ) {, check if it is a return type declaration.
                if (textAfterParams.Trim().StartsWith(":")) { return lineNumber; }

                return -1;
            }
        }

        /// <summary>
        /// Returns the given string with text that falls in a comment block removed.
        /// </summary>
        /// <param name="text">The string that may or maynot contain comments.</param>
        /// <returns></returns>
        public static string RemoveComments(string text)
        {
            if (text.Contains("//"))
            {
                return text.Substring(0, text.IndexOf("//"));
            }
            else if (text.Contains("/*"))
            {
                if (!text.Contains("*/"))
                    return text.Substring(0, text.IndexOf("/*"));
                else
                {
                    string result = text.Substring(0, text.IndexOf("/*"));
                    //Add 2 to only include characters after the */ string.
                    result += text.Substring(text.IndexOf("*/") + 2);
                    return RemoveComments(result);
                }
            }
            else
                return text;
        }

        public static Parameter[] GetFunctionParameters(int position, ITextSnapshot capture, bool isAboveFunction = false)
        {
            int openFunctionLine = capture.GetLineNumberFromPosition(position - 1);
            if (isAboveFunction)
            {
                openFunctionLine += 1;
            }
            else
            {
                openFunctionLine -= 1;
            }

            ITextSnapshotLine line = capture.GetLineFromLineNumber(openFunctionLine);
            string curLine = line.Extent.GetText();
            openFunctionLine = StubUtils.GetFunctionDeclarationLineNumber(capture, openFunctionLine, isAboveFunction);
            //Not immediately after a function declaration
            if (openFunctionLine == -1) return new Parameter[0];

            curLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();

            int ftnIndex = StubUtils.javaScriptFnRegex.Match(curLine).Index;
            int firstParenPosition = -1;
            if (curLine.IndexOf('(', ftnIndex) > -1)
            {
                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start +
                                 curLine.IndexOf('(', ftnIndex) + 1;
            }
            else
            {
                do
                {
                    openFunctionLine++;
                    curLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();
                } while (!curLine.Contains("("));

                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start
                                     + curLine.IndexOf('(')
                                     + 1;
            }

            var parenBlock = GetCompleteParenBlock(capture, openFunctionLine, firstParenPosition);
            if (parenBlock == null)
            {
                return new Parameter[0];
            }

            parenBlock = RemoveComments(parenBlock);
            return parenBlock
                .Split(',')
                .Select(param =>
                {
                    var result = Parameter.Parse(param);
                    if (StubUtils.contentTypeName.Equals("JavaScript"))
                    {
                        result.Type = "type";
                    }

                    return result;
                })
                .ToArray();
        }

        /// <summary>
        /// Returns a parenthetical block of code, excluding the parentheses. Returns null if there is no complete block.
        /// </summary>
        /// <param name="capture"></param>
        /// <param name="startLine">The line the parenthetical block starts.</param>
        /// <param name="openParenPosition">The position in the ITextSnapshot of the first open parenthesis.</param>
        /// <returns></returns>
        private static string GetCompleteParenBlock(ITextSnapshot capture, int startLine, int openParenPosition)
        {
            int lastParenPosition = -1;
            var openParens = 0;
            var curLineNumber = startLine;
            while (lastParenPosition < 0 && curLineNumber < capture.LineCount)
            {
                var curLine = capture.GetLineFromLineNumber(curLineNumber).GetText();
                for (var i = 0; i < curLine.Length; i++)
                {
                    var c = curLine[i];
                    if (c == '(')
                    {
                        openParens++;
                    }
                    else if (c == ')')
                    {
                        openParens--;
                        if (openParens == 0)
                        {
                            lastParenPosition = capture.GetLineFromLineNumber(curLineNumber).Start + i;
                            break;
                        }
                    }
                }

                curLineNumber++;
            }

            if (lastParenPosition == -1)
            {
                // no matching parens found - return no params
                return null;
            }

            return capture
                    .GetText()
                    .Substring(openParenPosition, (lastParenPosition - openParenPosition));
        }

        public static bool ShouldCreateReturnTag(int position, ITextSnapshot capture, bool isAboveFunction = false)
        {
            if (Options.ReturnGenerationOption == ReturnTagGenerationSetting.Always) return true;
            if (Options.ReturnGenerationOption == ReturnTagGenerationSetting.Never) return false;

            bool hasReturn = false;
            bool newFunction = false;
            bool functionClosed = false;
            bool hasComment = false;
            int lineNumber = capture.GetLineNumberFromPosition(position - 1);
            string lineText = capture.GetLineFromLineNumber(lineNumber).GetText();

            if (isAboveFunction) { lineNumber++; }
            else { lineNumber--; }

            bool inFunction = GetFunctionDeclarationLineNumber(capture, lineNumber, isAboveFunction) >= 0;
            if (!inFunction) return false;

            if (isAboveFunction) { lineNumber = GetNextOpenCurlyBrace(lineNumber, capture); }

            if (lineNumber == -1) { return false; }

            // If vsdoc (inside function) we need to initialize to 1 to account for the function we are currently attempting to document.
            int functionsOpen = 0;
            int openBracket = 0;

            for (int i = lineNumber; i < capture.LineCount; i++)
            {
                lineText = capture.GetLineFromLineNumber(i).GetText();
                //HANDLE COMMENTS
                if (lineText.Contains("/*") && lineText.Contains("*/") && lineText.LastIndexOf("/*") > lineText.LastIndexOf("*/"))
                {
                    hasComment = true;
                }
                else if (lineText.Contains("/*") && lineText.Contains("*/"))
                {
                    hasComment = false;
                }
                else if (lineText.Contains("/*"))
                {
                    hasComment = true;
                }

                if (hasComment && lineText.Contains("*/"))
                {
                    if (!lineText.Contains("/*") || lineText.LastIndexOf("/*") <= lineText.LastIndexOf("*/"))
                        hasComment = false;
                }
                else if (hasComment || String.IsNullOrEmpty(lineText.Trim())) { continue; }

                lineText = RemoveComments(lineText);

                //END COMMENT HANDLING

                //HANDLE BRACKETS - "{ }"
                if (i == lineNumber || javaScriptFnRegex.IsMatch(lineText))
                {
                    //adds an open function and an open bracket.
                    functionsOpen++;
                }
                else if (javaScriptFnRegex.IsMatch(lineText))
                {
                    //states that there is a new function open without an open bracket.
                    newFunction = true;
                }
                else if (newFunction && lineText.Contains("{"))
                {
                    //states that there is no longer a new function and adds an open
                    //bracket and open function.
                    newFunction = false;
                    functionsOpen++;
                }

                if (lineText.Contains("{"))
                {
                    //Adds an open bracket.
                    openBracket++;
                }
                bool isInlineFunction = false;

                if (lineText.Contains("}"))
                {
                    //If function is closed on same line as closing bracket
                    if (functionsOpen == 1 && returnRegex.IsMatch(lineText))
                    {
                        hasReturn = true;
                        break;
                    }
                    else if (returnRegex.IsMatch(lineText))
                    {
                        isInlineFunction = true;
                    }
                        
                    //Decrements both the number of open brackets and functions if they are equal.
                    //This means the number of open brackets are the same as the number of open functions.
                    //Otherwise it just decrements the number of open brackets.
                    if (openBracket == functionsOpen)
                    {
                        functionsOpen--;
                    }

                    openBracket--;
                }
                if (functionsOpen == 0) functionClosed = true;

                if (functionsOpen == 1 && returnRegex.IsMatch(lineText) && !isInlineFunction)
                {
                    hasReturn = true;
                    break;
                }
                else if (functionClosed)
                {
                    break;
                }
            }
            return hasReturn;
        }

        // Searches down the file for the next line with an open curly brace, including the given line number.
        // Returns the line number.
        private static int GetNextOpenCurlyBrace(int lineNumber, ITextSnapshot capture)
        {
            var found = false;
            while (lineNumber < capture.LineCount)
            {
                var text = capture.GetLineFromLineNumber(lineNumber).GetText();
                if (text.Contains("{"))
                {
                    found = true;
                    break;
                }

                lineNumber++;
            }

            if (found == false) { return -1; }

            return lineNumber;
        }
    }
}
