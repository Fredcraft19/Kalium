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
        Boolean,
        String,
        Operator,
        SemiColon,

        Comma,
        Dot,
        

        OpenBracket,
        CloseBracket,

        OpenCurlyBracket,
        CloseCurlyBracket,

        OpenSquareBracket,
        CloseSquareBracket,

        END,
        Error = 0xFF,
    }
}