using System.Runtime.Intrinsics.X86;

namespace WispAOT
{
    public static class Analyser
    {
        private static Dictionary<string, Symbol> variables = new();
        private static Dictionary<string, MethodData> methods = new();
        public static Dictionary<string, uint> methodStackSizes = new();
        private static string currentMethod = "";

        public static void Analyse(ref List<Node> nodes)
        {
            variables.Clear();
            methods.Clear();
            methodStackSizes = new();
            currentMethod = "";

            // first pass. register signature
            foreach (Node node in nodes)
            {
                if (node is not Nodes.MethodDeclarationNode m)
                {
                    Error.Log("Only method declarations are allowed at the top level for now");
                    return;
                }
                if (methods.ContainsKey(m.methodName))
                {
                    Error.Log($"Method '{m.methodName}' is already defined");
                    return;
                }
                methods[m.methodName] = new MethodData
                {
                    returnType = Data.primativeTypes[m.returnType],
                    parameters = m.parameters
                };
            }

            if (!methods.ContainsKey("main")) { Error.Log("'main' function required"); return; }

            // 2 pass (the bodies)
            foreach (Node node in nodes)
            {
                if (Error.hasError) return;
                analyseMethodDeclaration((Nodes.MethodDeclarationNode)node);
            }
        }

        private static void analyseNode(Node node)
        {
            switch (node)
            {
                case Nodes.VariableReassignNode res:
                    analyseReassignment(res);
                    break;
                case Nodes.DeclareVariableNode declare:
                    analyseDeclaration(declare);
                    break;
                case Nodes.IfNode ifNode:
                    analyseIfStatement(ifNode);
                    break;
                case Nodes.WhileNode whileNode:
                    analyseWhileStatement(whileNode);
                    break;
                case Nodes.ForNode forNode:
                    analyseForStatement(forNode);
                    break;
                case Nodes.MethodDeclarationNode methodNode:
                    analyseMethodDeclaration(methodNode);
                    break;
                case Nodes.ExpressionStatementNode esn:
                    esn.expression = analyseExpression(esn.expression!);
                    break;
                case Nodes.CallNode callNode:
                    analyseMethodCall(callNode);
                    break;
                case Nodes.ReturnNode returnNode:
                    analyseReturnStatement(returnNode);
                    break;
                case NullNode:
                    Error.Log("NullNode found by analyser. a silent error occured?");
                    break;
                default:
                    Error.Log($"Unsupported node type '{node.GetType().Name}'");
                    break;
            }
        }

        private static void analyseDeclaration(Nodes.DeclareVariableNode declare)
        {
            string name = declare.variableName;

            if (variables.ContainsKey(name))
            {
                Error.Log($"Variable '{name}' already exists");
                return;
            }
            if (declare.type is not Nodes.PrimitiveTypeNode t)
            {
                Error.Log("Unsupported custom data type for variable declaration");
                return;
            }

            VariableData declared = Data.primativeTypes[t.value];
            if (declared.type == DT.Void)
            {
                Error.Log($"Variable '{name}' cannot be of type void");
                return;
            }

            if (declare.expression != null)
            {
                ExpressionNode? value = analyseAndCoerce(declare.expression, declared, t.value);
                if (value != null) declare.expression = value;
            }

            // ignore errors that could have happened. To avoid 'undeclared' errors
            declare.offset = methodStackSizes[currentMethod];
            variables[name] = new Symbol { type = declared, offset = declare.offset };
            methodStackSizes[currentMethod] += declared.bytes;
        }
        private static void analyseReassignment(Nodes.VariableReassignNode res)
        {
            if (!variables.TryGetValue(res.variableName, out Symbol? sym))
            {
                Error.Log($"Variable '{res.variableName}' does not exist or is out of scope");
                return;
            }

            res.offset = sym.offset;
            res.varType = sym.type;

            ExpressionNode? value = analyseAndCoerce(res.expression!, sym.type , nameof(sym.type));
            if( value != null) res.expression= value;

        }

        private static void analyseIfStatement(Nodes.IfNode ifNode)
        {

            // Analyse Condition
            var expr = analyseExpression(ifNode.expression!);
            ifNode.expression = expr;

            if(expr.resolvedType != null && expr.resolvedType.type != DT.Bool)
            {
                Error.Log($"If Condition must be bool. Current condition is of type '{nameOf(expr.resolvedType)}'");
                return;
            }

            analyseBody(ifNode.body);

            if(ifNode.elseBody.Count > 0) analyseBody(ifNode.elseBody);
        }

        private static void analyseWhileStatement(Nodes.WhileNode whileNode)
        {
            var expr = analyseExpression(whileNode.expression!);
            whileNode.expression = expr;

            if(expr.resolvedType != null && expr.resolvedType.type != DT.Bool)
            {
                Error.Log($"While Condition must be bool. Current condition is of type '{nameOf(expr.resolvedType)}'");
                return;
            }

            analyseBody(whileNode.body);
        }

        private static void analyseForStatement(Nodes.ForNode forNode)
        {
            // so for(int i) so i doesnt live past the body
            pushScope();

            analyseDeclaration(forNode.init!);
            var expr = analyseExpression(forNode.expression!);
            forNode.expression = expr;
            if(expr.resolvedType != null && expr.resolvedType.type != DT.Bool)
            {
                Error.Log($"For Condition must be bool. Current condition is of type '{nameOf(expr.resolvedType)}'");
                return;
            }

            analyseReassignment(forNode.step!);
            
            analyseBody(forNode.body);

            popScope();
        }

        private static void analyseMethodDeclaration(Nodes.MethodDeclarationNode m)
        {
            Console.WriteLine($"Making method '{m.methodName}'");
            string oldVal = currentMethod;
            currentMethod = m.methodName;
            variables.Clear();

            uint offset = 0;
            foreach (var (name, data) in m.parameters)
            {
                if (data.type == DT.Void) { Error.Log($"Parameter '{name}' can't be void"); return; }
                if (variables.ContainsKey(name)) { Error.Log($"Duplicate parameter '{name}'"); return; }

                variables[name] = new Symbol { type = data, offset = offset };
                offset += data.bytes;
            }

            methodStackSizes[m.methodName] = offset;

            foreach (Node node in m.body)
            {
                if (Error.hasError) return;
                analyseNode(node);
            }

            m.stackSize = methodStackSizes[m.methodName];
            currentMethod = oldVal;
            methods[m.methodName] = new MethodData
            {
                returnType = Data.primativeTypes.ContainsKey(m.returnType) ? Data.primativeTypes[m.returnType] : Data.customTypes[m.returnType],
                parameters = m.parameters
            };
        }

        private static void analyseBody(List<Node> body)
        {
            pushScope();

            // analyse body
            foreach(Node node in body)
            {
                if(Error.hasError) return;
                analyseNode(node);
            }

            popScope();
        }

        private static ExpressionNode analyseMethodCall(Nodes.CallNode call)
        {
            if(!methods.TryGetValue(call.name, out var target))
            {
                Error.Log($"Method '{call.name}' does not exist");
                return call;
            }
            if(call.args.Count != target.parameters.Count)
            {
                Error.Log($"'{call.name}' takes {target.parameters} parameters, not {call.args.Count}");
                return call;
            }

            for(int i = 0; i < call.args.Count; i++)
            {
                var parem = target.parameters[i];
                var arg = analyseAndCoerce(call.args[i], parem.data, nameOf(parem.data));
                if(arg != null) call.args[i] = arg;
            }

            call.resolvedType = target.returnType;

            return call;
        }

        private static void analyseReturnStatement(Nodes.ReturnNode returnNode)
        {
            returnNode.expression = analyseExpression(returnNode.expression!);
            if(currentMethod == "")
            {
                Error.Log("Can't return out of no method");
            }
            var mret = methods[currentMethod].returnType;
            var tret = returnNode.expression.resolvedType;
            if(tret!.bytes != mret.bytes && tret!.type != mret.type)
            {
                Error.Log("Return type isnt the same as the method return type");
            }
        }

        // ssets resolvedType so it stays null if there was an error.
        private static ExpressionNode analyseExpression(ExpressionNode expr)
        {
            switch (expr)
            {
                case Nodes.LiteralNode lit:
                    if(lit.value == "true" || lit.value == "false"){
                        lit.value = lit.value == "true" ? "1" : "0";
                        lit.resolvedType = Data.primativeTypes["bool"];
                    }
                    else
                        lit.resolvedType = Data.primativeTypes["int"];   
                    return lit;

                case Nodes.VariableReferenceNode v:
                    if (v.name != null && variables.TryGetValue(v.name, out Symbol? sym))
                    {
                        v.resolvedType = sym.type;
                        v.offset = sym.offset;
                    }
                    else
                    {
                        Error.Log($"Undeclared variable '{v.name}'");
                    }
                    return v;

                case Nodes.BinaryExpressionNode b:
                {
                    b.left = analyseExpression(b.left!);
                    b.right = analyseExpression(b.right!);

                    VariableData? lt = b.left.resolvedType;
                    VariableData? rt = b.right.resolvedType;
                    if (lt == null || rt == null) return b;  

                    if (lt.type == DT.Void || rt.type == DT.Void)
                    {
                        Error.Log("Cannot use a void value in an expression");
                        return b;
                    }

                    VariableData operandType = promote(lt, rt);
                    b.left = coerce(b.left, operandType)!;       
                    b.right = coerce(b.right, operandType)!;
                    b.operandType = operandType;
                    b.resolvedType = isComparison(b.oper!.value) ? Data.primativeTypes["bool"] : operandType;
                    return b;
                }
                case Nodes.CallNode call:
                    return analyseMethodCall(call);

                default:
                    Error.Log($"Unsupported expression '{expr.GetType().Name}'");
                    return expr;
            }
        }

        private static void pushScope()
        {
            
        }
        private static void popScope()
        {
            
        }

        private static VariableData promote(VariableData a, VariableData b)
        {
            if (a.type == DT.Float || b.type == DT.Float)
            {
                int bytes = (int)Math.Max(a.type == DT.Float ? a.bytes : 0, b.type == DT.Float ? b.bytes : 0);
                return find(bytes, DT.Float);
            }

            int width = (int)Math.Max(4, Math.Max(a.bytes, b.bytes));
            bool unsigned = (a.type == DT.Uint && a.bytes >= width) || (b.type == DT.Uint && b.bytes >= width);
            return find(width, unsigned ? DT.Uint : DT.Int);
        }

        private static ExpressionNode? coerce(ExpressionNode e, VariableData target)
        {
            VariableData from = e.resolvedType!;

            if (from.type == DT.Void || target.type == DT.Void) return null;

            if(target.type == DT.Bool){
                if(from.type == DT.Bool) return e;

                if(e is Nodes.LiteralNode {type: DT.Int} literal && (literal.value == "0" || literal.value == "1"))
                {
                    literal.type = DT.Bool;
                    literal.resolvedType = target;
                    return literal;
                }
            }

            bool fromFloat = from.type == DT.Float;
            bool toFloat = target.type == DT.Float;
            
            if (fromFloat == toFloat) return e; 
            
            if (toFloat) return new Nodes.CastNode { operand = e, resolvedType = target };
            return null; 
        }

        // analyses expression -> Converts it to target type
        private static ExpressionNode? analyseAndCoerce(ExpressionNode expr, VariableData target, string targetName)
        {
            ExpressionNode analysed = analyseExpression(expr);

            if (analysed.resolvedType == null) return null;   // the error was already reported inside

            ExpressionNode? coerced = coerce(analysed, target);
            if (coerced == null)
            {
                Error.Log($"Cannot implicitly convert '{nameOf(analysed.resolvedType)}' to '{targetName}'");
                return null;
            }
            return coerced;
        }
        private static bool isComparison(string op) => op is "==" or "!=" or "<" or ">" or "<=" or ">=";
        private static VariableData find(int bytes, DT type) => Data.primativeTypes.Values.First(v => v.bytes == bytes && v.type == type);

        private static string nameOf(VariableData v) => v.type == DT.Void ? "void" : v.type == DT.Bool ? "bool" : $"{v.type.ToString().ToLower()}{v.bytes * 8}";
    }
}


