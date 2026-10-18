namespace WispAOT
{
    public class Token
    {
        public TokenType type;
        public string value = "";
    }

    public enum TokenType : byte
    {
        Null = 0x00,

        Type,
        Identifyer,
        Keyword,
        Equals,
        Float,
        Integer,
        Operator,
        SemiColon,


        OpenBracket,
        CloseBracket,

        OpenCurlyBracket,
        CloseCurlyBracket,

        END,
        Error = 0xFF,
    }
}