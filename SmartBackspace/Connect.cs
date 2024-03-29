using System;
using System.Collections.Generic;
using System.Globalization;
using Extensibility;
using EnvDTE;
using EnvDTE80;

namespace SmartBackspace
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2
    {
        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
        /// <param name="application">Root object of the host application.</param>
        /// <param name='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param name='addInInst'>Object representing this Add-in.</param>
        /// <param name='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            _outputWindowPane = _applicationObject.ToolWindows.OutputWindow.OutputWindowPanes.Add("SmartBackspace");
            _outputWindowPane.OutputString("SmartBackspace loaded" + Environment.NewLine);

            _textDocumentKeyPressEvents = ((Events2)_applicationObject.Events).get_TextDocumentKeyPressEvents(null);
            _textDocumentKeyPressEvents.BeforeKeyPress += _OnTextDocumentBeforeKeyPress;
        }

        private void _OnTextDocumentBeforeKeyPress(string keypress, TextSelection selection, bool inStatementCompletion, ref bool cancelKeypress)
        {
            const char BACKSPACE_CHAR_ASCII = (char)8;
            if (keypress != BACKSPACE_CHAR_ASCII.ToString(CultureInfo.InvariantCulture))
                return;

            var textDocument = (TextDocument)_applicationObject.ActiveDocument.Object("TextDocument");
            if (textDocument == null)
                return;

            IndentOptions indentOptions;
            if (_languageIndentOptions.ContainsKey(textDocument.Language) == false)
            {
                Properties properties = _applicationObject.get_Properties("TextEditor", textDocument.Language);
                indentOptions = new IndentOptions
                                    {
                                        InsertTabs = bool.Parse(properties.Item("InsertTabs").Value.ToString()),
                                        IndentSize = int.Parse(properties.Item("IndentSize").Value.ToString())
                                    };

                _languageIndentOptions.Add(textDocument.Language, indentOptions);
            }
            else
                indentOptions = _languageIndentOptions[textDocument.Language];
            

            // if tabs are used for indentation lets not do anything
            if (indentOptions.InsertTabs)
                return;

            var lineStartPoint = selection.ActivePoint.CreateEditPoint();
            lineStartPoint.StartOfLine();

            var activeEditPoint = selection.ActivePoint.CreateEditPoint();
            string currentLine = activeEditPoint.GetText(lineStartPoint);

            if (currentLine.Length == 0)
                return;

            // if the current line full of white space chars and multiple of indent size then left indent by deleting
            // indent amount of white space character
            const char WHITESPACE_CHAR = ' ';
            if (IsLineFullOf(currentLine, WHITESPACE_CHAR) && (currentLine.Length % indentOptions.IndentSize) == 0)
            {
                activeEditPoint.CharLeft(indentOptions.IndentSize);
                activeEditPoint.Delete(indentOptions.IndentSize);
                cancelKeypress = true;
            }
        }

        /// <summary>
        /// returns 'true' if the 'line' if full of character 'ch'
        /// </summary>
        /// <param name="line"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        static bool IsLineFullOf(string line, char ch)
        {
            // we start from last character as we can quickly return 'false' as soon as we detect non 'ch' character
            for (int i = line.Length - 1; i > 0; --i)
            {
                if (line[i] != ch)
                    return false;
            }

            return true;
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param name='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param name='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            _textDocumentKeyPressEvents.BeforeKeyPress -= _OnTextDocumentBeforeKeyPress;
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
        /// <param name='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
        /// <param name='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
        /// <param name='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }
 
        private struct IndentOptions
        {
            public bool InsertTabs;
            public int IndentSize;
        }

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private OutputWindowPane _outputWindowPane;

        private readonly Dictionary<string, IndentOptions> _languageIndentOptions = new Dictionary<string,IndentOptions>();
        private TextDocumentKeyPressEvents _textDocumentKeyPressEvents;
    }
}