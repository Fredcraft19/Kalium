namespace WispAOT
{
    public static class Tokeniser
    {
        public static void Tokenise(ref List<Token> tokens, string source)
        {
            string word = "";
            
            for(int i = 0; i < source.Length; i++)
            {
                char ch = source[i];

                switch (ch)
                {
                    case '\n':
                    case '\r':
                    case ' ':
                        flushWord(ref word, ref tokens);
                        break;
                    
                    case ';':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.SemiColon, value = ";"});
                        break;

                    case '=':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.Equals, value="="});
                        break;

                    case '(':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.OpenBracket, value="("});
                        break;
                    case ')':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.CloseBracket, value=")"});
                        break;

                    case '{':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.OpenCurlyBracket, value="{"});
                        break;
                    case '}':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token{type = TokenType.CloseCurlyBracket, value="}"});
                        break;

                    case '/':
                        flushWord(ref word, ref tokens);
                        if (i + 1 < source.Length && source[i + 1] == '?')
                        {
                            tokens.Add(new Token { type = TokenType.Operator, value = "/?" });
                            i++;
                        }
                        else tokens.Add(new Token { type = TokenType.Operator, value = "/" });
                        break;

                    case '*':
                    case '-':
                    case '+':
                        flushWord(ref word, ref tokens);
                        tokens.Add(new Token { type = TokenType.Operator, value = ch.ToString() });
                        break;

                    case '?':
                        flushWord(ref word, ref tokens);
                        Error.Log("'?' is only valid as part of '/?' (safe division)");
                        break;

                    default:
                        word+=ch;
                        break;
                }
            }

            foreach(Token tok in tokens)
            {
                Console.WriteLine($"{tok.type}('{tok.value}')");
            }

            tokens.Add(new Token{type=TokenType.END,value="END"});
        }
        private static void flushWord(ref string word, ref List<Token> tokens)
        {

            if(string.IsNullOrWhiteSpace(word)) return;

            Token output = new Token {type = TokenType.Null, value = ""};
            // Word is a Data Type
            if (Data.primativeTypes.ContainsKey(word))
            {
                output = new Token{type=TokenType.Type, value=word};
            }
            // Word is a keyword
            else if (Data.keywords.Contains(word))
            {
                output = new Token{type=TokenType.Keyword, value=word};
            }
            // Word is an int
            else if (int.TryParse(word, out var _))
            {
                output = new Token {type = TokenType.Integer, value=word};
            }
            // word is float
            else if(word[word.Length-1] == 'f' && float.TryParse(word.Substring(0, word.Length-1), out var _))
            {
                output = new Token {type = TokenType.Float, value=word.Substring(0, word.Length-1)};
            }
            // Word is idenitfyer
            else if(!char.IsNumber(word[0]))
            {
                output = new Token{type = TokenType.Identifyer, value=word};
            }
            else
            {
                Console.WriteLine($"Unknown token for '{word}'");
            }

            if(output.type == TokenType.Null)
            {
                Console.WriteLine($"Error: Token type null when flushing word '{word}'");
            }
            else
            {
                tokens.Add(output);
            }
            
            word = "";
        }
    }
}