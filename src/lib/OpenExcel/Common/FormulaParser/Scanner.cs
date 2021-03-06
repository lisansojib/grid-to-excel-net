// Generated by TinyPG v1.2 available at www.codeproject.com

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenExcel.Common.FormulaParser
{
    #region Scanner

    public partial class Scanner
    {
        public string Input;
        public int StartPos = 0;
        public int EndPos = 0;
        public int CurrentLine;
        public int CurrentColumn;
        public int CurrentPosition;
        public List<Token> Skipped; // tokens that were skipped
        public Dictionary<TokenType, Regex> Patterns;

        private Token LookAheadToken;
        private List<TokenType> Tokens;
        private List<TokenType> SkipList; // tokens to be skipped

        public Scanner()
        {
            Regex regex;
            Patterns = new Dictionary<TokenType, Regex>();
            Tokens = new List<TokenType>();
            LookAheadToken = null;

            SkipList = new List<TokenType>();
            SkipList.Add(TokenType.WSPC);

            regex = new Regex(@"\(", RegexOptions.Compiled);
            Patterns.Add(TokenType.PARENOPEN, regex);
            Tokens.Add(TokenType.PARENOPEN);

            regex = new Regex(@"\)", RegexOptions.Compiled);
            Patterns.Add(TokenType.PARENCLOSE, regex);
            Tokens.Add(TokenType.PARENCLOSE);

            regex = new Regex(@"\{", RegexOptions.Compiled);
            Patterns.Add(TokenType.BRACEOPEN, regex);
            Tokens.Add(TokenType.BRACEOPEN);

            regex = new Regex(@"\}", RegexOptions.Compiled);
            Patterns.Add(TokenType.BRACECLOSE, regex);
            Tokens.Add(TokenType.BRACECLOSE);

            regex = new Regex(@",", RegexOptions.Compiled);
            Patterns.Add(TokenType.COMMA, regex);
            Tokens.Add(TokenType.COMMA);

            regex = new Regex(@"\:", RegexOptions.Compiled);
            Patterns.Add(TokenType.COLON, regex);
            Tokens.Add(TokenType.COLON);

            regex = new Regex(@";", RegexOptions.Compiled);
            Patterns.Add(TokenType.SEMICOLON, regex);
            Tokens.Add(TokenType.SEMICOLON);

            regex = new Regex(@"@?[A-Za-z_][A-Za-z0-9_]*(?=\()", RegexOptions.Compiled);
            Patterns.Add(TokenType.FUNC, regex);
            Tokens.Add(TokenType.FUNC);

            regex = new Regex(@"\#((NULL|DIV\/0|VALUE|REF|NUM)\!|NAME\?|N\/A)", RegexOptions.Compiled);
            Patterns.Add(TokenType.ERR, regex);
            Tokens.Add(TokenType.ERR);

            regex = new Regex(@"(?x)(  '([^*?\[\]\/\\\'\\]+)'  |  ([^*?\[\]\/\\\(\)\!\+\-\&\,]+)  )\!", RegexOptions.Compiled);
            Patterns.Add(TokenType.SHEETNAME, regex);
            Tokens.Add(TokenType.SHEETNAME);

            regex = new Regex(@"(\$)?([A-Za-z]+)(\$)?[0-9]+", RegexOptions.Compiled);
            Patterns.Add(TokenType.ADDRESS, regex);
            Tokens.Add(TokenType.ADDRESS);

            regex = new Regex(@"(?i)(NULL)", RegexOptions.Compiled);
            Patterns.Add(TokenType.NULL, regex);
            Tokens.Add(TokenType.NULL);

            regex = new Regex(@"(?i)(TRUE|FALSE)", RegexOptions.Compiled);
            Patterns.Add(TokenType.BOOL, regex);
            Tokens.Add(TokenType.BOOL);

            regex = new Regex(@"(\+|-)?[0-9]+(\.[0-9]+)?", RegexOptions.Compiled);
            Patterns.Add(TokenType.NUMBER, regex);
            Tokens.Add(TokenType.NUMBER);

            regex = new Regex(@"\""(\""\""|[^\""])*\""", RegexOptions.Compiled);
            Patterns.Add(TokenType.STRING, regex);
            Tokens.Add(TokenType.STRING);

            regex = new Regex(@"\*|/|\+|-|&|==|!=|<>|<=|>=|>|<|=", RegexOptions.Compiled);
            Patterns.Add(TokenType.OP, regex);
            Tokens.Add(TokenType.OP);

            regex = new Regex(@"^$", RegexOptions.Compiled);
            Patterns.Add(TokenType.EOF, regex);
            Tokens.Add(TokenType.EOF);

            regex = new Regex(@"\s+", RegexOptions.Compiled);
            Patterns.Add(TokenType.WSPC, regex);
            Tokens.Add(TokenType.WSPC);


        }

        public void Init(string input)
        {
            this.Input = input;
            StartPos = 0;
            EndPos = 0;
            CurrentLine = 0;
            CurrentColumn = 0;
            CurrentPosition = 0;
            Skipped = new List<Token>();
            LookAheadToken = null;
        }

        public Token GetToken(TokenType type)
        {
            Token t = new Token(this.StartPos, this.EndPos);
            t.Type = type;
            return t;
        }

         /// <summary>
        /// executes a lookahead of the next token
        /// and will advance the scan on the input string
        /// </summary>
        /// <returns></returns>
        public Token Scan(params TokenType[] expectedtokens)
        {
            Token tok = LookAhead(expectedtokens); // temporarely retrieve the lookahead
            LookAheadToken = null; // reset lookahead token, so scanning will continue
            StartPos = tok.EndPos;
            EndPos = tok.EndPos; // set the tokenizer to the new scan position
            return tok;
        }

        /// <summary>
        /// returns token with longest best match
        /// </summary>
        /// <returns></returns>
        public Token LookAhead(params TokenType[] expectedtokens)
        {
            int i;
            int startpos = StartPos;
            Token tok = null;
            List<TokenType> scantokens;


            // this prevents double scanning and matching
            // increased performance
            if (LookAheadToken != null 
                && LookAheadToken.Type != TokenType._UNDETERMINED_ 
                && LookAheadToken.Type != TokenType._NONE_) return LookAheadToken;

            // if no scantokens specified, then scan for all of them (= backward compatible)
            if (expectedtokens.Length == 0)
                scantokens = Tokens;
            else
            {
                scantokens = new List<TokenType>(expectedtokens);
                scantokens.AddRange(SkipList);
            }

            do
            {

                int len = -1;
                TokenType index = (TokenType)int.MaxValue;
                string input = Input.Substring(startpos);

                tok = new Token(startpos, EndPos);

                for (i = 0; i < scantokens.Count; i++)
                {
                    Regex r = Patterns[scantokens[i]];
                    Match m = r.Match(input);
                    if (m.Success && m.Index == 0 && ((m.Length > len) || (scantokens[i] < index && m.Length == len )))
                    {
                        len = m.Length;
                        index = scantokens[i];  
                    }
                }

                if (index >= 0 && len >= 0)
                {
                    tok.EndPos = startpos + len;
                    tok.Text = Input.Substring(tok.StartPos, len);
                    tok.Type = index;
                }
                else
                {
                    if (tok.StartPos < tok.EndPos - 1)
                        tok.Text = Input.Substring(tok.StartPos, 1);
                }

                if (SkipList.Contains(tok.Type))
                {
                    startpos = tok.EndPos;
                    Skipped.Add(tok);
                }
            }
            while (SkipList.Contains(tok.Type));

            LookAheadToken = tok;
            return tok;
        }
    }

    #endregion

    #region Token

    public enum TokenType
    {

            //Non terminal tokens:
            _NONE_  = 0,
            _UNDETERMINED_= 1,

            //Non terminal tokens:
            Start   = 2,
            ComplexExpr= 3,
            Params  = 4,
            ArrayElems= 5,
            FuncCall= 6,
            Array   = 7,
            Range   = 8,
            Expr    = 9,

            //Terminal tokens:
            PARENOPEN= 10,
            PARENCLOSE= 11,
            BRACEOPEN= 12,
            BRACECLOSE= 13,
            COMMA   = 14,
            COLON   = 15,
            SEMICOLON= 16,
            FUNC    = 17,
            ERR     = 18,
            SHEETNAME= 19,
            ADDRESS = 20,
            NULL    = 21,
            BOOL    = 22,
            NUMBER  = 23,
            STRING  = 24,
            OP      = 25,
            EOF     = 26,
            WSPC    = 27
    }

    public class Token
    {
        private int startpos;
        private int endpos;
        private string text;
        private object value;

        public int StartPos { 
            get { return startpos;} 
            set { startpos = value; }
        }

        public int Length { 
            get { return endpos - startpos;} 
        }

        public int EndPos { 
            get { return endpos;} 
            set { endpos = value; }
        }

        public string Text { 
            get { return text;} 
            set { text = value; }
        }

        public object Value { 
            get { return value;} 
            set { this.value = value; }
        }

        public TokenType Type;

        public Token()
            : this(0, 0)
        {
        }

        public Token(int start, int end)
        {
            Type = TokenType._UNDETERMINED_;
            startpos = start;
            endpos = end;
            Text = ""; // must initialize with empty string, may cause null reference exceptions otherwise
            Value = null;
        }

        public void UpdateRange(Token token)
        {
            if (token.StartPos < startpos) startpos = token.StartPos;
            if (token.EndPos > endpos) endpos = token.EndPos;
        }

        public override string ToString()
        {
            if (Text != null)
                return Type.ToString() + " '" + Text + "'";
            else
                return Type.ToString();
        }
    }

    #endregion
}
