using System.Diagnostics;
using System.Linq.Expressions;
using WispAOT.Nodes;

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

            while(tokens[current].type != TokenType.END && !Error.hasError)
            {
                nodes.Add(parseStatement());
            }
            
        }
        private static TypeNode parseType()
        {
            string typeValue = advance().value;
            TypeNode baseType = new PrimitiveTypeNode { value = typeValue };

            if (check(TokenType.Operator) && peek().value == "*")
            {
                advance();
                return new PointerTypeNode { pointeeType = baseType };
            }
            return baseType;
        }
        private static Node parseStatement()
        {
            if (check(TokenType.Type)) return parseVariableDeclaration();
            else if (check(TokenType.Identifyer)) return parseVariableReassignment();
            else if (check(TokenType.Keyword)) return parseKeywordStatement();

            Error.Log($"Unexpected token '{Data.FormatToken(peek())}'");
            advance();
            return new NullNode();
        }

        private static ExpressionNode parseExpression() => parseBooleanExpression();

        private static ExpressionNode parseBooleanExpression()
        {
            ExpressionNode left = parseAddition();
            if (check(TokenType.Operator) && peek().value is "==" or "!=" or "<" or ">" or "<=" or ">=")
            {
                Token op = advance();
                left = new Nodes.BinaryExpressionNode { left = left, oper = op, right = parseAddition() };
            }
            return left;
        }

        private static ExpressionNode parseAddition()
        {
            ExpressionNode left = parseMultiplication();
            while (check(TokenType.Operator) && (peek().value == "+" || peek().value == "-") && !Error.hasError)
            {
                Token op = advance();
                left = new Nodes.BinaryExpressionNode { left = left, oper = op, right = parseMultiplication() };
            }
            return left;
        }

        private static ExpressionNode parseMultiplication()
        {
            ExpressionNode left = parsePrimary();
            while (check(TokenType.Operator) && (peek().value == "*" || peek().value == "/" || peek().value == "/?")&& !Error.hasError)
            {
                Token op = advance();
                left = new Nodes.BinaryExpressionNode { left = left, oper = op, right = parsePrimary() };
            }
            return left;
        }
        private static ExpressionNode parsePrimary()
        {
            if (match(TokenType.Integer) != null) return new LiteralNode { value = tokens[current - 1].value, type = DT.Int };
            if (match(TokenType.Boolean) != null) return new LiteralNode { value = tokens[current - 1].value, type = DT.Bool };
            if (match(TokenType.Float) != null)   return new LiteralNode { value = tokens[current - 1].value, type = DT.Float };

            if (check(TokenType.Identifyer) && tokens[current + 1].type == TokenType.OpenBracket)
            {
                string name = advance().value;
                Node call = parseMethodCall(name);
                if (call is CallNode c) return c;
                return new LiteralNode { value = "0", type = DT.Int };
            }

            if (match(TokenType.Identifyer) != null) return new VariableReferenceNode { name = tokens[current - 1].value };

            if (match(TokenType.OpenBracket) != null)
            {
                ExpressionNode expr = parseExpression();
                if (match(TokenType.CloseBracket) == null)
                    Error.Log("Expected ')' to close the expression");
                return expr;
            }
            

            Error.Log($"Unexpected token '{peek().value}', when in parsePrimary");
            return new Nodes.LiteralNode { value = "0", type = DT.Int };         // placeholder, see below
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
            List<(string name, VariableData data)> parameterData = new();
            if (Data.primativeTypes.ContainsKey(type))
            {
                if (check(TokenType.CloseBracket)) // no parameters
                {} // pro gamer coding
                else if(check(TokenType.Type))
                {
                    // get parameters
                    while (!check(TokenType.CloseBracket) && !Error.hasError)
                    {
                        Token dataTok = advance();
                        if(dataTok.type == TokenType.Type)
                        {
                            if (Data.primativeTypes.ContainsKey(dataTok.value))
                            {
                                Token nameTok = advance();
                                if(nameTok.type == TokenType.Identifyer)
                                {
                                    parameterData.Add((nameTok.value, Data.primativeTypes[dataTok.value]));
                                    if (check(TokenType.Comma))
                                    {
                                        advance();
                                    }
                                    else if(!check(TokenType.CloseBracket))
                                    {
                                        Error.Log($"Expected a ',' or ')'. Not {peek().value}");
                                    }
                                }
                                else
                                {
                                    Error.Log($"Expected an identifyer but got a '{Data.FormatToken(nameTok)}'");
                                }
                            }
                            else if (Data.customTypes.ContainsKey(dataTok.value))
                            {
                                Error.Log("Custom data types not supported");
                            }
                            else
                            {
                                Error.Log("Unknown data type");
                            }
                        }
                        else
                        {
                            Error.Log($"Expected a data type in method declaration instead of {Data.FormatToken(dataTok)}");
                        }
                    }
                }
                else
                {
                    Error.Log("Expected a ')' or a type after '(' for method declaration");
                    return new NullNode();
                }
            }
            else if (Data.customTypes.ContainsKey(type)){
                Error.Log("Custom types not supported yet..");
                return new NullNode();
            }
            else
            {
                Error.Log("Invalid type for method");
                return new NullNode();
            }

            // get stack size
            uint stackCounter = 0;
            foreach(var parem in parameterData)
            {
                stackCounter+= parem.data.bytes;
            }

            // debug parem logs
            Console.WriteLine($"{name} ({stackCounter}):");
            foreach(var parem in parameterData)
            {
                Console.WriteLine($"    {parem.name}: {parem.data.type}{parem.data.bytes*8}");
            }

            if(match(TokenType.CloseBracket) == null)
            {
                Error.Log("Expected ')' to close method parameters");
                return new NullNode();
            }
            if(match(TokenType.OpenCurlyBracket) == null)
            {
                Error.Log("Expected '{' to start method body");
                return new NullNode();
            }
            

            // get body
            List<Node> methodBody = new();

            // parse main if body
            while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
            {
                int before = current;
                methodBody.Add(parseStatement());

                if (current == before) // parse statement did nothing.. error occured?
                {
                    Error.Log($"Unexpected token {peek().type}('{peek().value}') in method declaration body");
                    advance();
                }
            }
            if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the method declaration body");


            return new MethodDeclarationNode
            {
                methodName = name,
                stackSize = stackCounter,
                parameters = parameterData,
                body = methodBody,
                returnType = type
            };
        }

        private static Node parseVariableReassignment(bool inFor = false)
        {
            string variableName = advance().value;

            if (check(TokenType.OpenBracket))
            {
                Node call = parseMethodCall(variableName);
                if (call is not CallNode c) return new NullNode();

                if (match(TokenType.SemiColon) == null)
                {
                    Error.Log("Expected ';' after the method call");
                    return new NullNode();
                }
                return new ExpressionStatementNode { expression = c };
            }
            else if (check(TokenType.Equals))
            {
                advance(); // consume '='
                var expr = parseExpression();

                Nodes.VariableReassignNode reassignNode = new()
                {
                    variableName = variableName,
                    expression = expr
                };

                if(inFor) return reassignNode;

                if(match(TokenType.SemiColon) == null)
                {
                    Error.Log("Expected a ';' after variable reassignment");
                    return new NullNode();
                }
                return reassignNode;
            }
            else
            {
                Error.Log("Unknown action to identifyer");
            }

            return new NullNode();
        }

        private static Node parseKeywordStatement()
        {
            switch (peek().value)
            {
                case "if":
                    return parseIfStatement();
                case "while":
                    return parseWhileStatement();
                case "for":
                    return parseForStatement();
                case "return":
                    return parseReturnStatement();
                default:
                    Error.Log("Unsupported keyword used");
                    break;
                    
            }
            return new NullNode();
        }

        private static Node parseIfStatement()
        {
            advance(); // keyword: 'if'

            if(match(TokenType.OpenBracket) == null)
            {
                Error.Log("Invalid use of If statement. Needs () surrounding the conditional expression");
                return new NullNode();
            }
            var expr = parseExpression();
            
            if (match(TokenType.CloseBracket) == null)
            {
                Error.Log("Closing Bracket required");
            }

            if (match(TokenType.OpenCurlyBracket) == null)
            {
                Error.Log("Openling Curly Bracket for IF body");
            }

            List<Node> if_body = new();

            // parse main if body
            while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
            {
                int before = current;
                if_body.Add(parseStatement());

                if (current == before) // parse statement did nothing.. error occured?
                {
                    Error.Log($"Unexpected token {peek().type}('{peek().value}') in if body");
                    advance();
                }
            }

            if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the if body");
            
            // parse else body (optional)
            if(peek().type == TokenType.Keyword && peek().value == "else")
            {
                advance(); // consume 'else'

                // else if body
                List<Node> else_body = new();

                if(peek().type == TokenType.Keyword && peek().value == "if")
                {
                    else_body.Add(parseStatement());
                    return new Nodes.IfNode {expression = expr, body = if_body, elseBody = else_body};
                }

                if(match(TokenType.OpenCurlyBracket) == null)
                {
                    Error.Log("Expected a '{' after else");
                    return new NullNode();
                }
                else
                {
                    

                    // parse (optional) else body
                    while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
                    {
                        int before = current;
                        else_body.Add(parseStatement());

                        if (current == before) // parse statement did nothing.. error occured?
                        {
                            Error.Log($"Unexpected token {peek().type}('{peek().value}') in else body");
                            advance();
                        }
                    }

                    if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the else body");
                    
                    return new Nodes.IfNode
                    {
                        expression = expr,
                        body = if_body,
                        elseBody = else_body
                    };
                }
            }
            else
            {
                return new Nodes.IfNode
                {
                    expression = expr,
                    body = if_body
                };
            }
        }

        private static Node parseWhileStatement()
        {
            advance(); // consume 'while'
            // swipe from if body and expression parsing 

            if(match(TokenType.OpenBracket) == null)
            {
                Error.Log("Invalid use of While statement. Needs () surrounding the conditional expression");
                return new NullNode();
            }
            var expr = parseExpression();
            
            if (match(TokenType.CloseBracket) == null)
            {
                Error.Log("Closing Bracket required");
            }

            if (match(TokenType.OpenCurlyBracket) == null)
            {
                Error.Log("Openling Curly Bracket for WHILE body");
            }

            List<Node> whileBody = new();

            // parse while body
            while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
            {
                int before = current;
                whileBody.Add(parseStatement());

                if (current == before) // parse statement did nothing.. error occured?
                {
                    Error.Log($"Unexpected token {peek().type}('{peek().value}') in if body");
                    advance();
                }
            }

            if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the if body");
            
            return new WhileNode
            {
                expression = expr,
                body = whileBody  
            };
        }
        private static Node parseForStatement()
        {
            advance(); //  consume 'for'
            if(match(TokenType.OpenBracket) == null)
            {
                Error.Log("Expected a '(' after 'for'");
                return new NullNode();
            }
            Node decl = parseVariableDeclaration();
            Node expr = parseExpression();
            advance(); // consume ';'
            Node step_ = parseVariableReassignment(true);

            if(decl is NullNode)
                Error.Log("Declaration expected in for Loop");
            if(expr is NullNode)
                Error.Log("Expression expected in for loop");
            if(step_ is NullNode)
                Error.Log("Step expected in for loop (step = variable re-assignment)");
            if(step_ is NullNode || expr is NullNode || decl is NullNode) return new NullNode();
            if(match(TokenType.CloseBracket) == null)
            {
                Error.Log("Expected a ')' to close off the 'for' loop");
                return new NullNode();
            }
            if(match(TokenType.OpenCurlyBracket) == null)
            {
                Error.Log("Expected '{' after () of for loop");
                return new NullNode();
            }

            // get for body
            List<Node> forBody = new();
            while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
            {
                int before = current;
                forBody.Add(parseStatement());

                if (current == before) // parse statement did nothing.. error occured?
                {
                    Error.Log($"Unexpected token {peek().type}('{peek().value}') in for body");
                    advance();
                }
            }
            if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the for body");

            return new ForNode
            {
                init = (DeclareVariableNode)decl,
                expression = (ExpressionNode)expr,
                step = (VariableReassignNode)step_,
                body = forBody
            };
        }

        private static Node parseMethodCall(string methodName)
        {
            advance(); // consume '('

            CallNode call = new() { name = methodName };

            if (!check(TokenType.CloseBracket))
            {
                do { call.args.Add(parseExpression()); }
                while (match(TokenType.Comma) != null && !Error.hasError);
            }

            if (match(TokenType.CloseBracket) == null)
            {
                Error.Log("Expected ')' to close off the method call arguments");
                return new NullNode();
            }

            return call;
        }

        private static Node parseReturnStatement()
        {
            advance(); // consume 'return'

            var expr = parseExpression();

            if(match(TokenType.SemiColon) == null)
            {
                Error.Log("Expected a ';' at the end");
                return new NullNode();
            }

            return new ReturnNode { expression = expr };
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
