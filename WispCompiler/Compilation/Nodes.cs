using System.Reflection.Metadata;
using System.Text;

namespace WispAOT
{
    public class Node;
    public class TypeNode;
    public class NullNode:Node;
    public abstract class ExpressionNode : Node
    {
        public VariableData? resolvedType;
    }

    namespace Nodes{        
        public class PrimitiveTypeNode : TypeNode
        {
            public string value = "";
        }
        public class CustomTypeNode : TypeUnloadedException
        {
            public string value = "";
        }

        public class LiteralNode : ExpressionNode
        {
            public object? value;
            public DT? type;
        }

        

        public class BinaryExpressionNode : ExpressionNode
        {
            public ExpressionNode? left;
            public Token? oper;
            public ExpressionNode? right;
        }

        public class VariableReferenceNode : ExpressionNode
        {
            public string? name;
            public uint offset;                  
        }

        public class CastNode : ExpressionNode 
        {
            public ExpressionNode? operand; 
        }

        public class DeclareVariableNode : Node
        {
            public TypeNode? type;
            public string variableName = "";
            public ExpressionNode? expression;
            public uint offset;
        }
    }

}