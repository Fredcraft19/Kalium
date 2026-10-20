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
        public class CustomTypeNode : TypeNode
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
            public uint arrayLength = 0; // 0 is not an array
        }
        public class VariableReassignNode : DeclareVariableNode
        {
            public VariableData? varType;
            public bool isIndirect;
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

        public class UnaryExpressionNode : ExpressionNode
        {
            public Token? oper;
            public ExpressionNode? operand;
        }

        public class PointerTypeNode : TypeNode
        {
            public TypeNode pointeeType = null!;
        }
        public class ReferenceTypeNode : TypeNode
        {
            public TypeNode pointeeType = null!;
        }

        public class NewStatement : ExpressionNode
        {
            public VariableData? data;
            public ExpressionNode? count;
        }
        public class DeleteStatement : Node
        {
            public string? deletedPointer;
            public UInt64 pointerAddress;
            public VariableData? data;
        }

        public class IndexNode : ExpressionNode
        {
            public ExpressionNode? baseExpression;
            public ExpressionNode? indexExpression;
        }
        public class IndexAssignNode : Node
        {
            public IndexNode? target;
            public ExpressionNode? expression;
        }

        public class ObjectDeclareNode : Node
        {
            public string? name;
            public uint totalBytes;
            public Dictionary<string, VariableData> variables = new();
            public List<Node> body = new();
        }
    }

}