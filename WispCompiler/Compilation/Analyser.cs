using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using WispAOT.Nodes;

namespace WispAOT
{
    public static class Analyser
    {
        private static Dictionary<string, Symbol> variables = new();
        private static Dictionary<string, MethodData> methods = new();
        private static Dictionary<string, CustomObject> objects = new();
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
                if(node is ObjectDeclareNode)
                {
                    analyseObjectDeclaration((ObjectDeclareNode)node);
                    continue;
                }
                if (node is not Nodes.MethodDeclarationNode m)
                {
                    Error.Log($"Only method declarations are allowed at the top level for now insead of '{node.GetType().Name}'");
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
                if(node is MethodDeclarationNode) analyseMethodDeclaration((MethodDeclarationNode)node);
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
                case DeleteStatement deleteNode:
                    analyseDeleteStatement(deleteNode);
                    break;
                case IndexAssignNode indexAssign:
                    analyseIndexAssignment(indexAssign);
                    break;
                case ObjectDeclareNode objDecl:
                    analyseObjectDeclaration(objDecl);
                    break;
                case NullNode:
                    Error.Log("NullNode found by analyser. a silent error occured?");
                    break;
                default:
                    Error.Log($"Unsupported node type '{node.GetType().Name}'");
                    break;
            }
        }

        private static bool objectHasMember(List<ObjectVariable> vars, string name)
        {
            foreach(var member in vars)
                if(member.name == name) return true;
            return false;
        }
        private static uint getObjectOffset(List<ObjectVariable> vars, uint baseOffset)
        {
            foreach(var member in vars)
            {
                baseOffset += member.offset;
            }
            return baseOffset;
        }

        private static void analyseObjectDeclaration(ObjectDeclareNode objectNode)
        {
            string className = objectNode.name!;
            if (objects.ContainsKey(className))
            {
                Error.Log($"Type '{className}' already exists");
                return;
            }

            List<ObjectVariable> objectVariables = new();

            uint offset = 0;

            foreach(Node node in objectNode.body)
            {
                if(node is DeclareVariableNode variable)
                {
                    if(variable.expression != null)
                    {
                        Error.Log($"Variable cant have an initialiser yet");
                        return;
                    }
                    if (objectHasMember(objectVariables, variable.variableName))
                    {
                        Error.Log("Variables in an object cannot share the same name");
                        return;
                    }

                    VariableData type = analyseTypeNode(variable.type!);
                    if(type.type == DT.Void)
                    {
                        Error.Log($"Member '{variable.variableName}' cannot be type void");
                        return;
                    }

                    objectVariables.Add(new ObjectVariable {
                        name = variable.variableName,
                        type = type,
                        offset = offset
                    });
                    offset += type.bytes;  
                }
                else if(node is MethodDeclarationNode)
                {
                    Error.Log("Method declaration inside a class/struct is not supported yet", ConsoleColor.DarkMagenta);   
                    return;
                }
                else
                {
                    Error.Log($"Unsupported node type of '{node.GetType().Name}' in a class/struct");
                    return;
                }
            }

            if (Settings.Debug)
            {
                Console.WriteLine($"object {className} ({objectNode.totalBytes}):");
                foreach(ObjectVariable objVar in objectVariables)
                {
                    Console.WriteLine($"    ({objVar.offset}): {objVar.name}");
                }
                
            }

            objectNode.totalBytes = offset;

            objects[className] = new CustomObject {
                totalMemory = offset,
                variables = objectVariables
            };

        }

        private static DeclareVariableNode analyseDeclaration(Nodes.DeclareVariableNode declare, bool inClass = false)
        {
            string name = declare.variableName;

            if(!inClass){
                if (variables.ContainsKey(name))
                {
                    Error.Log($"Variable '{name}' already exists");
                    return new DeclareVariableNode();
                }
            }

            VariableData declared = analyseTypeNode(declare.type!);
            if (declared.type == DT.Void)
            {
                Error.Log($"Variable '{name}' cannot be of type void");
                return new DeclareVariableNode();;
            }

            if (declare.expression != null)
            {
                if (declared.isReference)
                {
                    if (declare.expression is not Nodes.VariableReferenceNode)
                    {
                        Error.Log("Reference must be initialized with a variable reference");
                        return new DeclareVariableNode();;
                    }

                    declare.expression = new Nodes.UnaryExpressionNode 
                    { 
                        oper = new Token { type = TokenType.Operator, value = "&" }, 
                        operand = declare.expression,
                        resolvedType = declared
                    };
                }
                
                ExpressionNode? value = analyseAndCoerce(declare.expression, declared, declare.type!.ToString()!);
                if (value != null) declare.expression = value;
            }

            if(inClass)
            {
                return declare;
            }

            declare.offset = methodStackSizes[currentMethod];
            variables[name] = new Symbol { type = declared, offset = declare.offset };
            methodStackSizes[currentMethod] += declared.bytes;

            return declare;
        }
        private static void analyseReassignment(VariableReassignNode res)
        {
            if (!variables.TryGetValue(res.variableName, out Symbol? sym))
            {
                Error.Log($"Variable '{res.variableName}' does not exist or is out of scope");
                return;
            }
            if (res.isIndirect && sym.type.type != DT.Pointer)
            {
                Error.Log($"Cannot dereference '{res.variableName}': it is not a pointer");
                return;
            }

            res.offset = sym.offset;
            res.varType = sym.type;

            VariableData targetType = (res.isIndirect || sym.type.isReference) ? sym.type.pointeeType! : sym.type;

            ExpressionNode? value = analyseAndCoerce(res.expression!, targetType, nameOf(targetType));
            if (value != null) res.expression = value;
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
            bool hasReturn = false;
            foreach (Node node in m.body)
            {
                if (Error.hasError) return;
                analyseNode(node);
                if (node is ReturnNode) hasReturn = true;
            }

            if(m.returnType != "void" && !hasReturn)
            {
                Error.Log("A method with a return type must have a return statement");
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

            for (int i = 0; i < call.args.Count; i++)
            {
                var param = target.parameters[i];
                ExpressionNode arg = call.args[i];

                if (param.data.isReference)
                {
                    arg = analyseExpression(arg);
                    if (arg is not Nodes.VariableReferenceNode)
                    {
                        Error.Log($"Cannot pass a non-lvalue expression to reference parameter '{param.name}'");
                        return call;
                    }
                    arg = new Nodes.UnaryExpressionNode
                    {
                        oper = new Token { type = TokenType.Operator, value = "&" },
                        operand = arg,
                        resolvedType = param.data
                    };
                }
                else
                {
                    var coercedArg = analyseAndCoerce(arg, param.data, nameOf(param.data));
                    if (coercedArg != null) arg = coercedArg;
                }

                call.args[i] = arg;
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

        private static VariableData analyseTypeNode(TypeNode typeNode)
        {
            if (typeNode is Nodes.PrimitiveTypeNode p)
            {
                return Data.primativeTypes[p.value];
            }
            if (typeNode is Nodes.PointerTypeNode ptr)
            {
                VariableData pointee = analyseTypeNode(ptr.pointeeType);
                return Data.MakePointer(pointee);
            }
            if (typeNode is Nodes.ReferenceTypeNode refNode)
            {
                VariableData pointee = analyseTypeNode(refNode.pointeeType);
                return Data.MakeReference(pointee);
            }
            Error.Log("Unknown type node representation");
            return Data.primativeTypes["int"];
        }

        // sets resolvedType so it stays null if there was an error.
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

                case Nodes.UnaryExpressionNode u:
                {
                    u.operand = analyseExpression(u.operand!);
                    if (u.oper!.value == "&")
                    {
                        if (u.operand is not Nodes.VariableReferenceNode)
                        {
                            Error.Log("Cannot take the address of a non-lvalue expression");
                            return u;
                        }
                        u.resolvedType = Data.MakePointer(u.operand.resolvedType!);
                    }
                    else if (u.oper!.value == "*")
                    {
                        if (u.operand.resolvedType!.type != DT.Pointer)
                        {
                            Error.Log("Cannot dereference a non-pointer type");
                            return u;
                        }
                        u.resolvedType = u.operand.resolvedType!.pointeeType;
                    }
                    return u;
                }
                case NewStatement New:
                    return analyseNewStatement(New);

                case IndexNode Index:
                    {
                        Index.baseExpression = analyseExpression(Index.baseExpression!);
                        Index.indexExpression = analyseExpression(Index.indexExpression!);
                        var baseType = Index.baseExpression.resolvedType;
                        var indexType = Index.indexExpression.resolvedType;
                        if(baseType == null || indexType == null)
                        {
                            return Index;
                        }

                        if(baseType.type != DT.Pointer)
                        {
                            Error.Log($"Cannot index to '{nameOf(baseType)}' because its not a pointrer");
                            return Index;
                        }

                        if(indexType.type != DT.Int && indexType.type != DT.Uint)
                        {
                            Error.Log($"Array indexes are integer.");
                            return Index;
                        }

                        Index.resolvedType = baseType.pointeeType;
                        return Index;
                    }

                default:
                    Error.Log($"Unsupported expression '{expr.GetType().Name}'");
                    return expr;
            }
        }

        private static NewStatement analyseNewStatement(NewStatement newS)
        {
            // im sure its safe and that...
            newS.resolvedType = Data.MakePointer(newS.data!);

            if (newS.count != null)
            {
                newS.count = analyseExpression(newS.count);
                var ct = newS.count.resolvedType;
                if (ct != null && ct.type != DT.Int && ct.type != DT.Uint) Error.Log("The size in 'new TYPE[size]' must be an integer");
            }

            return newS;
        }
        private static void analyseDeleteStatement(DeleteStatement deleteS)
        {
            string ptrName = deleteS.deletedPointer!;
            if (!variables.ContainsKey(ptrName))
            {
                Error.Log($"Pointer '{ptrName}' does not exist");
                return;
            }
            if (!variables[ptrName].type.isPointer)
            {
                Error.Log($"Variable '{ptrName}' is not a pointer. You can only delete pointers that are pointing to the heap");
            }

            deleteS.pointerAddress = variables[ptrName].offset;
        }

        private static void analyseIndexAssignment(IndexAssignNode indexAssign)
        {
            IndexNode target = (IndexNode) analyseExpression(indexAssign.target!);
            indexAssign.target = target;
            if(target.resolvedType == null) return;

            ExpressionNode value = analyseAndCoerce(indexAssign.expression!, target.resolvedType, nameOf(target.resolvedType))!;
            if (value != null) indexAssign.expression = value;
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

        // readability + future understanding < small
        private static bool SamePointee(VariableData a, VariableData b) => a.pointeeType!.type == b.pointeeType!.type && a.pointeeType.bytes == b.pointeeType.bytes && (a.pointeeType.type != DT.Pointer || SamePointee(a.pointeeType, b.pointeeType));

        private static ExpressionNode? coerce(ExpressionNode e, VariableData target)
        {
            VariableData from = e.resolvedType!;

            if (from.type == DT.Void || target.type == DT.Void) return null;

            if (from.type == DT.Pointer || target.type == DT.Pointer)
            {
                if (target.type == DT.Pointer && e is Nodes.LiteralNode { value: "0" }) return e;
                return (from.type == DT.Pointer && target.type == DT.Pointer && SamePointee(from, target)) ? e : null;
            }

            if(target.type == DT.Bool){
                if(from.type == DT.Bool) return e;

                if(e is Nodes.LiteralNode {type: DT.Int} literal && (literal.value == "0" || literal.value == "1"))
                {
                    literal.type = DT.Bool;
                    literal.resolvedType = target;
                    return literal;
                }
                return null;
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


