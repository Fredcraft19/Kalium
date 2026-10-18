using System.Text;

namespace WispAOT
{
    public static class CodeGen
    {
        private static StringBuilder sb;
        public static string Generate(List<Node> nodes)
        {
            if(Analyser.hadError) return "";
            sb = new();

            writeHeader();

            foreach(Node node in nodes)
                generateNode(node);

            emit("ret");

            return sb.ToString();
        }

        private static void generateNode(Node node)
        {
            switch (node)
            {
                case Nodes.DeclareVariableNode d:
                    {
                        if(d.expression != null)
                        {
                            generateExpression(d.expression);
                            emit($"{storeOp(declaredType(d))} {d.offset}");
                        }
                        break;
                    }
            }
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
                
                default:
                    Error.Log($"CodeGen: unsupported expression: '{expression.GetType().Name}'");
                    break;
            }
        }

        private static string loadOp(VariableData t) => t.type == DT.Float ? $"loadf{t.bytes * 8}" : $"load{(t.type == DT.Uint ? "u" : "s")}{t.bytes * 8}";

        private static string storeOp(VariableData t) => t.type == DT.Float ? $"storef{t.bytes * 8}" : $"store{t.bytes * 8}";
        private static string operatorOp(string op, VariableData t) => op switch
        {
            "+" => $"add-{suffix(t.type)}",
            "-" => $"sub-{suffix(t.type)}",
            "*" => $"mul-{suffix(t.type)}",
            "/" => $"div-{suffix(t.type)}",
            "/?" => $"safe-div-{suffix(t.type)}",
            _   => throw new Exception($"unknown operator {op}")
        };
        private static string suffix(DT t) => t switch
        {
            DT.Uint => "u", DT.Int => "s", DT.Float => "f",
            _ => throw new Exception("void has no operations")
        };

        private static string pushSuffix(DT? t) => t switch
        {
            DT.Uint => "", DT.Int => "s", DT.Float => "f",
            _ => throw new Exception("void not able to be pushed")
        };

        private static void emit(string text) => sb.AppendLine($"   {text}");
        private static void label(string label_name) => sb.AppendLine($"{label_name}:");
        private static VariableData declaredType(Nodes.DeclareVariableNode d) => Data.primativeTypes[((Nodes.PrimitiveTypeNode)d.type!).value];
        private static void writeHeader()
        {
            emit("call _main");
            emit("exit");

            label("_main");
            emit($"enter {Analyser.methodStackSizes["_main"]}");
        }
    }
}