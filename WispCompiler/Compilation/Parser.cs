using System.Diagnostics;

namespace WispAOT
{
    public static class Parser
    {
        private static int current = 0;
        private static List<Token> tokens = new();
        private static void reset()
        {
            current = 0;
        }
        public static void GenerateNodes(ref List<Node> nodes, List<Token> _tokens)
        {
            reset();
            tokens = _tokens;

            while(tokens[current].type != TokenType.END)
            {
                nodes.Add(parseStatement());
            }
            
        }
        private static Node parseStatement()
        {
            if(check(TokenType.Type)) return parseVariableDeclaration();
            else if(check(TokenType.Keyword)) return parseKeywordStatement();
            return new NullNode();
        }

        private static ExpressionNode parseExpression() => parseAddition();

        private static ExpressionNode parseAddition()
        {
            ExpressionNode left = parseMultiplication();
            while (check(TokenType.Operator) && (peek().value == "+" || peek().value == "-"))
            {
                Token op = advance();
                left = new Nodes.BinaryExpressionNode { left = left, oper = op, right = parseMultiplication() };
            }
            return left;
        }

        private static ExpressionNode parseMultiplication()
        {
            ExpressionNode left = parsePrimary();
            while (check(TokenType.Operator) && (peek().value == "*" || peek().value == "/" || peek().value == "/?"))
            {
                Token op = advance();
                left = new Nodes.BinaryExpressionNode { left = left, oper = op, right = parsePrimary() };
            }
            return left;
        }
        private static ExpressionNode parsePrimary()
        {
            if (match(TokenType.Integer) != null) return new Nodes.LiteralNode { value = tokens[current - 1].value, type = DT.Int};
            if (match(TokenType.Float) != null) return new Nodes.LiteralNode {value = tokens[current-1].value, type = DT.Float};
            if (match(TokenType.Identifyer) != null) return new Nodes.VariableReferenceNode { name = tokens[current - 1].value };
            if (match(TokenType.OpenBracket) != null) {
                ExpressionNode expr = parseExpression();
                match(TokenType.CloseBracket);
                return expr;
            }
            Error.Log($"Unexpected token '{peek().value}', when in parsePrimary");
            return null;
        }

        private static Node parseVariableDeclaration()
        {
            TypeNode vartype;
            string typeValue = advance().value;

            if (Data.primativeTypes.ContainsKey(typeValue))
            {
                vartype = new Nodes.PrimitiveTypeNode { value = typeValue };
            }
            else if (Data.customTypes.ContainsKey(typeValue))
            {
                Error.Log($"type '{typeValue}' not supported yet", ConsoleColor.DarkMagenta);
                return new NullNode();
            }
            else
            {
                Error.Log("unknown type in variable declaration", ConsoleColor.DarkMagenta);
                return new NullNode();
            }

            Token? nameTok = match(TokenType.Identifyer);
            if (nameTok == null)
            {
                Error.Log("expected an identifier when declaring a method/variable");
                return new NullNode();
            }
            string varName = nameTok.value;

            if (check(TokenType.OpenBracket))
            {
                advance(); // '('
                return parseMethodDeclaration(typeValue, varName);
            }

            ExpressionNode? expr = null;

            if (match(TokenType.Equals) != null)
            {
                // parse the expression right after '='
                expr = parseExpression();
            }

            if (match(TokenType.SemiColon) == null)
            {
                Error.Log("Expected ';' at the end of the variable declaration");
                return new NullNode();
            }

            return new Nodes.DeclareVariableNode
            {
                type = vartype,
                variableName = varName,
                expression = expr
            };
        }

        private static Node parseMethodDeclaration(string type, string name)
        {
            Error.Log("Method Declaration not supported yet", ConsoleColor.DarkMagenta);
            return new NullNode();
        }

        private static Node parseKeywordStatement()
        {
            Error.Log("Keyword Statements not supported yet", ConsoleColor.DarkMagenta);
            return new NullNode();
        }


        private static Token peek() => tokens[current];
        private static Token advance() {
            if (!isAtEnd()) current++;
            return tokens[current - 1];
        }
        private static bool isAtEnd() => peek().type == TokenType.END;
        private static bool check(TokenType type) {
            if (isAtEnd()) return false;
            return peek().type == type;
        }
        private static Token? match(TokenType type) {
            if (check(type)) return advance();
            return null;
        }
    }
}