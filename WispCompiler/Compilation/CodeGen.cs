using System.Text;
using WispAOT.Nodes;

namespace WispAOT
{
    public static class CodeGen
    {
        private static StringBuilder? sb;
        private static Dictionary<(string name, string returnLabel), List<Node>> pendingLabels = new();
        private static int labelCount = 0;
        private static bool prettyPrint = false;
        private static Dictionary<string, string> methodLabelNames = new();
        public static string Generate(List<Node> nodes, bool enablePrettyPrint = false)
        {
            if(Error.hasError) return "";
            //prettyPrint = enablePrettyPrint;
            prettyPrint = enablePrettyPrint;
            sb = new();
            pendingLabels = new();
            methodLabelNames = new();
            labelCount = 0;

            writeHeader();

            foreach(Node node in nodes)
                generateNode(node);

            generatePendingLabels();

            return sb.ToString();
        }

        private static void generatePendingLabels()
        {
            // key is (name, returnName)
            // name -> name of label we are making
            // returnLabel -> name of label to jump to after this (not using ret)
            // value is list<node> (labels nodes/code)
            while (pendingLabels.Count > 0)
            {
                var lbl = pendingLabels.First();
                pendingLabels.Remove(lbl.Key);

                label(lbl.Key.name!);
                foreach (var node in lbl.Value)
                {
                    generateNode(node);             
                }
                emit($"jump {lbl.Key.returnLabel}");
            }
        }

        private static void generateNode(Node node)
        {
            switch (node)
            {
                case Nodes.VariableReassignNode res:
                    {
                        if(res.expression != null)
                        {
                            generateExpression(res.expression);
                            emit($"{storeOp(res.varType!)} {res.offset}");
                        }
                        break;
                    }
                case Nodes.DeclareVariableNode d:
                    {
                        if(d.expression != null)
                        {
                            generateExpression(d.expression);
                            emit($"{storeOp(declaredType(d))} {d.offset}");
                        }
                        break;
                    }
                case Nodes.IfNode If:
                    {
                        if(If.expression != null)
                        {
                            generateIf(If);
                        }
                        break;
                    }
                case Nodes.WhileNode While:
                    {
                        if(While.expression != null)
                        {
                            generateWhile(While);
                        }
                        break;
                    }
                case Nodes.ForNode For:
                    {
                        if(For.expression != null)
                        {
                            generateFor(For);
                        }
                        break;
                    }
                case Nodes.MethodDeclarationNode MethodDecl:
                    {
                        generateMethodDeclaration(MethodDecl);
                        break;
                    }
                case Nodes.CallNode Call:
                    {
                        generateMethodCall(Call);
                        break;
                    }
                case Nodes.ExpressionStatementNode esn:
                    {
                        generateExpression(esn.expression!);
                        if (esn.expression!.resolvedType!.type != DT.Void) emit("pop");
                        break;
                    }
                case Nodes.ReturnNode Return:
                    {
                        generateReturn(Return);
                        break;
                    }
                default:
                    Error.Log($"Unsupported Node of type: '{node.GetType().Name}'");
                    break;
            }
        }

        private static void generateIf(Nodes.IfNode ifNode)
        {
            bool hasElse = ifNode.elseBody.Count > 0;

            labelCount++;
            string startIfName = $"if_{labelCount}";
            string endIfName = $"if_end_{labelCount}";
            string startElseName = $"else_{labelCount}";

            // generate expression
            generateExpression(ifNode.expression!);
            // ----> value in stack will be 1 if true and 0 in false <----

            pendingLabels[(startIfName, endIfName)] = ifNode.body;
            if (hasElse)
            {
                pendingLabels[(startElseName, endIfName)] = ifNode.elseBody;
            }

            // true?
            emit($"jump-nz {startIfName}");

            // false?
            if(hasElse) emit($"jump {startElseName}");

            // false?
            if(!hasElse) emit($"jump {endIfName}");

            label(endIfName);
        }

        private static void generateWhile(Nodes.WhileNode whileNode)
        {
            labelCount ++;
            string startWhileName = $"while_{labelCount}";
            string endWhileName = $"while_end_{labelCount}";

            generateExpression(whileNode.expression!);

            pendingLabels[(startWhileName, endWhileName)] = whileNode.body;

            emit($"jump-nz {startWhileName}");
            emit($"jump {endWhileName}");

            label(endWhileName);
            generateExpression(whileNode.expression!);
            emit($"jump-nz {startWhileName}");
        }

        private static void generateFor(Nodes.ForNode forNode)
        {
            // comments are example for -> for(int i = 0; i < 5; i++)
            labelCount++;
            string startForName = $"for_{labelCount}";
            string endForName = $"for_end_{labelCount}";

            pendingLabels[(startForName, endForName)] = forNode.body;

            generateNode(forNode.init!); // int i = 0
            generateExpression(forNode.expression!); // i < 5
            emit($"jump-nz {startForName}");
            emit($"jump {endForName}");

            label(endForName);
            generateNode(forNode.step!); // i++
            generateExpression(forNode.expression!); // i < 5
            emit($"jump-nz {startForName}");
        }

        private static void generateMethodDeclaration(Nodes.MethodDeclarationNode methodNode)
        {
            methodLabel(methodNode.methodName);

            emit($"enter {Analyser.methodStackSizes[methodNode.methodName]}");
            uint offset = 0;
            for(int i = methodNode.parameters.Count-1; i > -1; i--)
            {
                generateMethodParameter(methodNode.parameters[i].paremData, offset);
                offset += methodNode.parameters[i].paremData.bytes;
            }

            // generate body
            foreach(Node node in methodNode.body)
                generateNode(node);

            emit("ret");
        }
        private static void generateMethodParameter(VariableData parameterData, uint offset)
        {
            emit($"{storeOp(parameterData)} {offset}");
        }

        private static void generateMethodCall(Nodes.CallNode Call)
        {
            foreach(var parem in Call.args)  generateExpression(parem);
            emit($"call {getMethodLabel(Call.name)}");
        }

        private static void generateReturn(Nodes.ReturnNode Return)
        {
            generateExpression(Return.expression!);
        }

        private static void generateExpression(ExpressionNode expression)
        {
            switch (expression)
            {
                case Nodes.LiteralNode l:
                    emit($"push {l.value}{pushSuffix(l.type)}");
                    break;
                    
                case Nodes.VariableReferenceNode r:
                    emit($"{loadOp(r.resolvedType!)} {r.offset}");
                    break;
                case Nodes.BinaryExpressionNode bin:
                    generateExpression(bin.left!);
                    generateExpression(bin.right!);
                    emit(operatorOp(bin.oper!.value, bin.resolvedType!));
                    break;
                case Nodes.CastNode c:
                    generateExpression(c.operand!);
                    emit(c.operand!.resolvedType!.type == DT.Uint ? "cast-uf" : "cast-sf");
                    break;

                case Nodes.CallNode Call:
                    generateMethodCall(Call);
                    break;
                
                default:
                    Error.Log($"CodeGen: unsupported expression: '{expression.GetType().Name}'");
                    break;
            }
        }

        private static string loadOp(VariableData t) => t.type == DT.Float ? $"loadf{t.bytes * 8}" : $"load{((t.type == DT.Uint || t.type == DT.Bool) ? "u" : "s")}{t.bytes * 8}";

        private static string storeOp(VariableData t) => t.type == DT.Float ? $"storef{t.bytes * 8}" : $"store{t.bytes * 8}";
        private static string operatorOp(string op, VariableData t) => op switch
        {
            "+" => $"add-{suffix(t.type)}",
            "-" => $"sub-{suffix(t.type)}",
            "*" => $"mul-{suffix(t.type)}",
            "/" => $"div-{suffix(t.type)}",
            "/?" => $"safe-div-{suffix(t.type)}",
            "==" => $"eql-{suffix(t.type)}",
            "!=" => $"neql-{suffix(t.type)}",
            ">" => $"gtr-{suffix(t.type)}",
            ">=" => $"egtr-{suffix(t.type)}",
            "<" => $"les-{suffix(t.type)}",
            "<=" => $"eles-{suffix(t.type)}",
            _   => throw new Exception($"CodeGen: unknown operator {op}")
        };
        private static string suffix(DT t) => t switch
        {
            DT.Uint => "u", DT.Int => "s", DT.Float => "f", DT.Bool => "u",
            _ => throw new Exception("CodeGen: void has no operations")
        };

        private static string pushSuffix(DT? t) => t switch
        {
            DT.Uint => "", DT.Int => "s", DT.Float => "f", DT.Bool => "",
            _ => throw new Exception("CodeGen: void not able to be pushed")
        };

        private static void emit(string text) { 
            if(prettyPrint)
                sb?.AppendLine($"    {text}");
            else
                sb?.AppendLine($"{text}");
        }
        private static void label(string label_name){
            if(prettyPrint)
                sb?.AppendLine($"\n{label_name}:");
            else
                sb?.AppendLine($"{label_name}:");
        }
        private static void methodLabel(string method_name)
        {
            string output = "";

            output += method_name == "main" ? "main" : $"method_{method_name}_{labelCount}";
            methodLabelNames[method_name] = output;
            label(prettyPrint ? $"\n{output}" : output);
        }
        private static string getMethodLabel(string method_name)
        {
            if (!methodLabelNames.ContainsKey(method_name))
            {
                Error.Log($"Method '{method_name}' does not exist");
                return "null_method";
            }
            return methodLabelNames[method_name];
        }
        private static VariableData declaredType(Nodes.DeclareVariableNode d) => Data.primativeTypes[((Nodes.PrimitiveTypeNode)d.type!).value];
        private static void writeHeader()
        {
            sb?.AppendLine("call main");
            sb?.AppendLine("exit");
        }
    }
}
