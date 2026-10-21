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

            // pointer
            while (check(TokenType.Operator) && peek().value == "*")
            {
                advance();
                baseType = new PointerTypeNode { pointeeType = baseType };
            }

            // reference
            if (check(TokenType.Operator) && peek().value == "&")
            {
                advance();
                baseType = new ReferenceTypeNode { pointeeType = baseType };
            }

            return baseType;
        }
        private static Node parseStatement()
        {
            if (check(TokenType.Type)) return parseVariableDeclaration();
            else if (check(TokenType.Identifyer) || (check(TokenType.Operator) && peek().value == "*")) return parseVariableReassignment();
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
                left = new BinaryExpressionNode { left = left, oper = op, right = parseAddition() };
            }
            return left;
        }

        private static ExpressionNode parseAddition()
        {
            ExpressionNode left = parseMultiplication();
            while (check(TokenType.Operator) && (peek().value == "+" || peek().value == "-") && !Error.hasError)
            {
                Token op = advance();
                left = new BinaryExpressionNode { left = left, oper = op, right = parseMultiplication() };
            }
            return left;
        }

        private static ExpressionNode parseMultiplication()
        {
            ExpressionNode left = parsePrimary();
            while (check(TokenType.Operator) && (peek().value == "*" || peek().value == "/" || peek().value == "/?")&& !Error.hasError)
            {
                Token op = advance();
                left = new BinaryExpressionNode { left = left, oper = op, right = parsePrimary() };
            }
            return left;
        }
        private static ExpressionNode parsePrimary()
        {
            ExpressionNode expr = parseAtom();

            while (match(TokenType.OpenSquareBracket) != null)
            {
                ExpressionNode index = parseExpression();
                if (match(TokenType.CloseSquareBracket) == null)
                    Error.Log("Expected ']' after the index");
                expr = new IndexNode { baseExpression = expr, indexExpression = index };
            }
            return expr;
        }
        private static ExpressionNode parseAtom()
        {
            if (check(TokenType.Operator) && (peek().value == "*" || peek().value == "&"))
            {
                Token op = advance();
                ExpressionNode operand = parsePrimary();
                return new UnaryExpressionNode { oper = op, operand = operand };
            }

            if (match(TokenType.Integer) != null) return new LiteralNode { value = tokens[current - 1].value, type = DT.Int };
            if (match(TokenType.Boolean) != null) return new LiteralNode { value = tokens[current - 1].value, type = DT.Bool };
            if (match(TokenType.Float) != null) return new LiteralNode { value = tokens[current - 1].value, type = DT.Float };
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

            if(check(TokenType.Keyword))
            {
                string keyword = peek().value;
                if(keyword == "new")
                {
                    advance(); // consume 'new'
                    if (check(TokenType.Type))
                    {
                        string dataType = peek().value;
                        advance(); // consume type
                        ExpressionNode? count = null;
                        if (!Data.primativeTypes.ContainsKey(dataType))
                        {
                            Error.Log("Unrecognised type for 'new'. Custom types aren't supported");
                            return new LiteralNode{value = "0", type = DT.Uint};  // basically 'nullptr' (even though this will never compile)
                        }
                        
                        if (match(TokenType.OpenSquareBracket) != null)
                        {
                            count = parseExpression();
                            if (match(TokenType.CloseSquareBracket) == null)
                                Error.Log("Expected ']' after the array size");
                        }
                        return new NewStatement { data = Data.primativeTypes[dataType], count = count };
                    }
                    else
                    {
                        Error.Log("Expected a type for 'new'.");
                    }
                }
                else if(keyword == "nullptr")
                {
                    advance(); // consume 'nullptr'
                    return new LiteralNode // 'nullptr' is just 0 uint64
                    {
                        value = "0",
                        type = DT.Uint
                    };
                }
                else
                {
                    Error.Log($"Unsupported keyword in expression '{Data.FormatToken(peek())}'");
                }
            }

            Error.Log($"Unexpected token '{peek().value}', when in parsePrimary");
            return new LiteralNode { value = "0", type = DT.Int };
        }

        private static Node parseVariableDeclaration()
        {
            TypeNode vartype = parseType();

            TypeNode checkNode = vartype;
            while (true)
            {
                if (checkNode is PointerTypeNode ptr) checkNode = ptr.pointeeType;
                else if (checkNode is ReferenceTypeNode refNode) checkNode = refNode.pointeeType;
                else break;
            }

            string typeValue = "";
            if (checkNode is PrimitiveTypeNode prim)
            {
                typeValue = prim.value;
            }

            if (!Data.primativeTypes.ContainsKey(typeValue) && !Data.customTypes.ContainsKey(typeValue))
            {
                Error.Log($"Unknown type '{typeValue}' in variable declaration", ConsoleColor.DarkMagenta);
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
                expr = parseExpression();
            }

            if (match(TokenType.SemiColon) == null)
            {
                Error.Log("Expected ';' at the end of the variable declaration");
                return new NullNode();
            }

            return new DeclareVariableNode
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
                    while (!check(TokenType.CloseBracket) && !isAtEnd() && !Error.hasError)
                    {
                        TypeNode paramTypeNode = parseType();
                        Token? nameTok = match(TokenType.Identifyer);
                        
                        if (nameTok == null)
                        {
                            Error.Log($"Expected an identifier for parameter, got '{peek().value}'");
                            break;
                        }

                        VariableData paramData = resolveTypeNode(paramTypeNode);
                        parameterData.Add((nameTok.value, paramData));

                        if (check(TokenType.Comma))
                        {
                            advance();
                        }
                        else if (!check(TokenType.CloseBracket))
                        {
                            Error.Log($"Expected a ',' or ')'. Not {peek().value}");
                            break;
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
            if(Settings.Debug) Console.WriteLine($"{name} ({stackCounter}):");
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
            bool isIndirect = false;
            string variableName = "";
            if (check(TokenType.Operator) && peek().value == "*")
            {
                advance(); // consume '*'
                isIndirect = true;
                
                Token? nameTok = match(TokenType.Identifyer);
                if (nameTok == null)
                {
                    Error.Log("Expected an identifier after '*' in assignment");
                    return new NullNode();
                }
                variableName = nameTok.value;
            }
            else
            {
                variableName = advance().value;
            }

            // reassignning a value to an object member
            if (check(TokenType.Dot))
            {
                advance(); // consume "."
                if(peek().type != TokenType.Identifyer)
                {
                    Error.Log($"Expected an identifyer (member names) after '{variableName}.'");
                    return new NullNode();
                }
                string memberName = peek().value;

                
            }

            if (check(TokenType.OpenBracket))
            {
                if (isIndirect)
                {
                    Error.Log("Cannot call a method on a dereferenced expression this way");
                    return new NullNode();
                }
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

                VariableReassignNode reassignNode = new()
                {
                    variableName = variableName,
                    expression = expr,
                    isIndirect = isIndirect
                };

                if(inFor) return reassignNode;

                if(match(TokenType.SemiColon) == null)
                {
                    Error.Log("Expected a ';' after variable reassignment");
                    return new NullNode();
                }
                return reassignNode;
            }
            else if (check(TokenType.OpenSquareBracket))
            {
                if (isIndirect)
                {
                    Error.Log("Use (*p)[i] to index through a dereferenced pointer");
                    return new NullNode();
                }

                ExpressionNode target = new VariableReferenceNode { name = variableName };
                while (match(TokenType.OpenSquareBracket) != null)
                {
                    ExpressionNode index = parseExpression();
                    if (match(TokenType.CloseSquareBracket) == null)
                    {
                        Error.Log("Expected ']' after the index");
                        return new NullNode();
                    }
                    target = new IndexNode { baseExpression = target, indexExpression = index };
                }

                if (match(TokenType.Equals) == null)
                {
                    Error.Log("Expected '=' after the indexed element");
                    return new NullNode();
                }

                ExpressionNode value = parseExpression();
                var node = new IndexAssignNode { target = (IndexNode)target, expression = value };

                if (inFor) return node;
                if (match(TokenType.SemiColon) == null)
                {
                    Error.Log("Expected ';' after the assignment");
                    return new NullNode();
                }
                return node;
            }
            else
            {
                Error.Log("Unknown action to identifyer");
            }
            isIndirect = false;

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
                case "delete":
                    return parseDeleteStatement();
                case "struct":
                case "class":
                    return parseObject();
                default:
                    Error.Log("Unsupported keyword used");
                    break;
                    
            }
            return new NullNode();
        }

        private static Node parseObject()
        {
            // unused for now. (except for consuming the keyword token)
            // will be used for default public/private members
            bool isClass = advance().value == "class";
            Token name = advance();
            if(name.type != TokenType.Identifyer)
            {
                Error.Log("Expected an identifyer after 'class' keyword");
                return new NullNode();
            }
            if(match(TokenType.OpenCurlyBracket) == null)
            {
                Error.Log("Expected a '{' to start the class body");
            }

            List<Node> class_body = new();
            while (!check(TokenType.CloseCurlyBracket) && !isAtEnd() && !Error.hasError)
            {
                int before = current;
                var nodeToAdd = parseStatement();
                class_body.Add(nodeToAdd);

                if (current == before) // parse statement did nothing.. error occured?
                {
                    Error.Log($"Unexpected token {peek().type}('{peek().value}') in class body");
                    advance();
                }
            }
            if (!Error.hasError && match(TokenType.CloseCurlyBracket) == null) Error.Log("Expected '}' to close the class body");
            
            uint totalMemoryFootprint = 0;
            Dictionary<string, VariableData> variablesInClass = new();

            foreach(Node node in class_body)
            {
                if(node is DeclareVariableNode)
                {
                    DeclareVariableNode decl = (DeclareVariableNode)node;
                    if(decl.type is PrimitiveTypeNode primType)
                    {
                        var data = Data.primativeTypes[primType.value];
                        totalMemoryFootprint += data.bytes;
                        variablesInClass[decl.variableName] = data;
                    }
                    else
                    {
                        Error.Log("Cannot use custom data types (including pointers) when declaring a class (for now its unsupported..)");
                        return new NullNode();
                    }
                }
                else
                {
                    Error.Log("Classes/Structs only support variable declaration inside them.");
                    return new NullNode();
                }
            }

            return new ObjectDeclareNode
            {
                name = name.value,
                totalBytes = totalMemoryFootprint,
                body = class_body,
                variables = variablesInClass
            };
        }
        private static Node parseDeleteStatement()
        {
            advance(); // consume 'delete'

            if (!check(TokenType.Identifyer))
            {
                Error.Log("Expected a pointer to delete");
                return new NullNode();
            }

            string name = advance().value;

            if (match(TokenType.SemiColon) == null)
            {
                Error.Log("Expected ';' after delete");
                return new NullNode();
            }
            
            return new DeleteStatement { deletedPointer = name };
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
                    return new IfNode {expression = expr, body = if_body, elseBody = else_body};
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
                    
                    return new IfNode
                    {
                        expression = expr,
                        body = if_body,
                        elseBody = else_body
                    };
                }
            }
            else
            {
                return new IfNode
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

        private static VariableData resolveTypeNode(TypeNode typeNode)
        {
            if (typeNode is PrimitiveTypeNode p)
            {
                return Data.primativeTypes[p.value];
            }
            if (typeNode is PointerTypeNode ptr)
            {
                VariableData pointee = resolveTypeNode(ptr.pointeeType);
                return Data.MakePointer(pointee);
            }
            if (typeNode is ReferenceTypeNode refNode)
            {
                VariableData pointee = resolveTypeNode(refNode.pointeeType);
                return Data.MakeReference(pointee);
            }
            return Data.primativeTypes["int"];
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