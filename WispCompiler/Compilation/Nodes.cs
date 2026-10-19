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
            public string? value;
            public DT? type;
        }

        

        public class BinaryExpressionNode : ExpressionNode
        {
            public ExpressionNode? left;
            public Token? oper;
            public ExpressionNode? right;
            public VariableData? operandType; 
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
        public class VariableReassignNode : DeclareVariableNode
        {
            public VariableData? varType;
        }

        public class IfNode : Node
        {
            public ExpressionNode? expression;
            public List<Node> body = new();
            public List<Node> elseBody = new();
        }

        public class WhileNode : Node
        {
            public ExpressionNode? expression;
            public List<Node> body = new();
        }

        public class ForNode : Node
        {
            public ExpressionNode? expression;
            public DeclareVariableNode? init;
            public VariableReassignNode? step;
            public List<Node> body = new();
        }

        public class MethodDeclarationNode : Node
        {
            public string returnType = "";
            public string methodName = "";
            public List<(string paremName, VariableData paremData)> parameters = new();
            public uint stackSize = 0; 
            public List<Node> body = new();
        }
        public class CallNode : ExpressionNode
        {
            public string name = "";
            public List<ExpressionNode> args = new();
        }

        public class ExpressionStatementNode : Node
        {
            public ExpressionNode? expression;
        }

        public class ReturnNode : Node
        {
            public ExpressionNode? expression;
        }

        public class PointerTypeNode : TypeNode
        {
            public TypeNode pointeeType = null!;
        }
    }

}
