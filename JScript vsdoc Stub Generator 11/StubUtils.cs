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

                var textToCloseParen = lineText;

                // Though we are looking for a ), we need to break the loop if we find either { or },
                // because it could be the start of an object, class, or something else.
                var checkBreakLoop = new Regex(@"\)|\{|\}");

                // At this point, we know the given lineNumber is what we want to return as long
                // as it starts on a valid function declaration, so we create a separate counter.
                var lineCounter = lineNumber;
                while (!checkBreakLoop.IsMatch(lineText))
                {
                    lineCounter++;
                    lineText = capture.GetLineFromLineNumber(lineCounter).Extent.GetText();
                    textToCloseParen += lineText;
                }

                whitespace.Replace(textToCloseParen, "");
                if (!hasMatchingParens.IsMatch(textToCloseParen) || keywordWithParen.IsMatch(textToCloseParen)) 
                {
                    return -1;
                }

                var textToOpenBracket = textToCloseParen.Substring(textToCloseParen.IndexOf(')') + 1);
                while (!lineText.Contains('{'))
                {
                    lineCounter++;
                    lineText = capture.GetLineFromLineNumber(lineCounter).Extent.GetText();
                    textToOpenBracket += lineText;
                }

                textToOpenBracket = textToOpenBracket.Substring(0, textToOpenBracket.LastIndexOf('{'));
                // If there is no text between the ) and {, then we know it is a valid function header.
                if (String.IsNullOrWhiteSpace(textToOpenBracket)) { return lineNumber; }

                // If there is text between ) {, check if it is a return type declaration.
                if (textToOpenBracket.Trim().StartsWith(":")) { return lineNumber; }

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

        public static string[] GetFunctionParameters(int position, ITextSnapshot capture, bool isAboveFunction = false)
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
            string prevLine = line.Extent.GetText();
            openFunctionLine = StubUtils.GetFunctionDeclarationLineNumber(capture, openFunctionLine, isAboveFunction);
            //Not immediately after a function declaration
            if (openFunctionLine == -1) return new string[0];

            prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();

            int ftnIndex = StubUtils.javaScriptFnRegex.Match(prevLine).Index;
            int firstParenPosition = -1;
            if (prevLine.IndexOf('(', ftnIndex) > -1)
            {
                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start +
                                 prevLine.IndexOf('(', ftnIndex) + 1;
            }
            else
            {
                do
                {
                    openFunctionLine++;
                    prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();
                } while (!prevLine.Contains("("));

                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start
                                     + prevLine.IndexOf('(')
                                     + 1;
            }

            int lastParenPosition = -1;
            if (prevLine.IndexOf(')') > 0)
            {
                lastParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start
                                    + prevLine.IndexOf(')', prevLine.IndexOf('('));
            }
            else
            {
                do
                {
                    openFunctionLine++;
                    prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();
                } while (!prevLine.Contains(")"));

                lastParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start +
                                        prevLine.IndexOf(")");
            }


            return StubUtils
                .RemoveComments(capture
                    .GetText()
                    .Substring(firstParenPosition, (lastParenPosition - firstParenPosition)))
                .Split(',')
                .Select(param => param.Trim())
                .ToArray();
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

            int functionsOpen = 1;
            int openBracket = 1;

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
                if (javaScriptFnRegex.IsMatch(lineText) && lineText.Contains("{"))
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
                        isInlineFunction = true;
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

        /// <summary>
        /// Returns the param name from the given parameter definition regardless of whether it is for JavaScript or TypeScript.
        /// </summary>
        /// <param name="paramDefinition"></param>
        /// <returns></returns>
        public static string GetParamName(string paramDefinition)
        {
            if (!paramDefinition.Contains(':')) { return paramDefinition; }

            return paramDefinition.Split(':')[0].Trim();
        }

        /// <summary>
        /// Returns the param type from the given parameter definition. If it is not found, returns null.
        /// </summary>
        /// <param name="paramDefinition"></param>
        /// <returns></returns>
        public static string GetParamType(string paramDefinition)
        {
            if (!paramDefinition.Contains(':')) { return null; }

            return paramDefinition.Split(':')[1].Trim();
        }
    }
}
