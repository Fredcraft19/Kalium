namespace WispAOT
{
    public class Symbol
    {
        public VariableData type = null!;
        public uint offset;
    }

    public static class Analyser
    {
        private static Dictionary<string, Symbol> variables = new();
        public static Dictionary<string, uint> methodStackSizes = new() { { "_main", 0 } };
        private static string currentMethod = "_main";
        public static bool hadError = false;

        private static void error(string message)
        {
            hadError = true;
            Error.Log(message);
        }

        public static void Analyse(ref List<Node> nodes)
        {
            variables.Clear();
            methodStackSizes = new() {{ "_main", 0 }};
            hadError = false;

            foreach (Node node in nodes) analyseNode(node);
        }

        private static void analyseNode(Node node)
        {
            switch (node)
            {
                case Nodes.DeclareVariableNode declare:
                    analyseDeclaration(declare);
                    break;
                case NullNode:
                    break;
                default:
                    error($"Unsupported node type '{node.GetType().Name}'");
                    break;
            }
        }

        private static void analyseDeclaration(Nodes.DeclareVariableNode declare)
        {
            string name = declare.variableName;

            if (variables.ContainsKey(name))
            {
                error($"Variable '{name}' already exists");
                return;
            }
            if (declare.type is not Nodes.PrimitiveTypeNode t)
            {
                error("Unsupported custom data type for variable declaration");
                return;
            }

            VariableData declared = Data.primativeTypes[t.value];
            if (declared.type == DT.Void)
            {
                error($"Variable '{name}' cannot be of type void");
                return;
            }

            // make 'int x = x + 1;' an error
            if (declare.expression != null)
            {
                ExpressionNode expr = analyseExpression(declare.expression);

                if (expr.resolvedType != null)
                {
                    ExpressionNode? coerced = coerce(expr, declared);
                    if (coerced == null)
                        error($"Cannot implicitly convert '{nameOf(expr.resolvedType)}' to '{t.value}'");
                    else
                        declare.expression = coerced;
                }
            }

            // declare it even if the initializer had errors, avoids undeclared errors
            declare.offset = methodStackSizes[currentMethod];
            variables[name] = new Symbol { type = declared, offset = declare.offset };
            methodStackSizes[currentMethod] += (uint)declared.bytes;
        }

        // ssets resolvedType so it stays null if there was an error.
        private static ExpressionNode analyseExpression(ExpressionNode expr)
        {
            switch (expr)
            {
                case Nodes.LiteralNode lit:
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
                        error($"Undeclared variable '{v.name}'");
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
                        error("Cannot use a void value in an expression");
                        return b;
                    }

                    VariableData result = promote(lt, rt);
                    b.left = coerce(b.left, result)!;       
                    b.right = coerce(b.right, result)!;
                    b.resolvedType = result;
                    return b;
                }

                default:
                    error($"Unsupported expression '{expr.GetType().Name}'");
                    return expr;
            }
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

            bool fromFloat = from.type == DT.Float;
            bool toFloat = target.type == DT.Float;
            
            if (fromFloat == toFloat) return e; 
            
            if (toFloat) return new Nodes.CastNode { operand = e, resolvedType = target };
            return null; 
        }

        private static VariableData find(int bytes, DT type) => Data.primativeTypes.Values.First(v => v.bytes == bytes && v.type == type);

        private static string nameOf(VariableData v) => v.type == DT.Void ? "void" : $"{v.type.ToString().ToLower()}{v.bytes * 8}";


    }
}


