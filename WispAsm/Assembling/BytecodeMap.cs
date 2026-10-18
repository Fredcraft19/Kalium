namespace WispAOT
{
    public static class BytecodeMap
    {
        public static Dictionary<string, byte> bytecode = new()
        {
            {"push",            0x01},
            {"store8",          0x02},
            {"store16",         0x03},
            {"store32",         0x04},
            {"store64",         0x05},
            {"storef32",        0x06},
            {"storef64",        0x07},

            {"store8*",         0x08},
            {"store16*",        0x09},
            {"store32*",        0x0A},
            {"store64*",        0x0B},
            {"storef32*",       0x0C},
            {"storef64*",       0x0D},

            {"loadu8",          0x0E},
            {"loadu16",         0x0F},
            {"loadu32",         0x10},
            {"loadu64",         0x11},
            {"loads8",          0x12},
            {"loads16",         0x13},
            {"loads32",         0x14},
            {"loads64",         0x15},
            {"loadf32",         0x16},
            {"loadf64",         0x17},

            {"loadu8*",          0x58},
            {"loadu16*",         0x59},
            {"loadu32*",         0x5A},
            {"loadu64*",         0x5B},
            {"loads8*",          0x5C},
            {"loads16*",         0x5D},
            {"loads32*",         0x5E},
            {"loads64*",         0x5F},
            {"loadf32*",         0x60},
            {"loadf64*",         0x61},

            {"heap-store8",     0x18},
            {"heap-store16",    0x19},
            {"heap-store32",    0x1A},
            {"heap-store64",    0x1B},
            {"heap-storef32",   0x1C},
            {"heap-storef64",   0x1D},

            {"heap-loadu8",     0x1E},
            {"heap-loadu16",    0x1F},
            {"heap-loadu32",    0x20},
            {"heap-loadu64",    0x21},
            {"heap-loads8",     0x22},
            {"heap-loads16",    0x23},
            {"heap-loads32",    0x24},
            {"heap-loads64",    0x25},
            {"heap-loadf32",    0x26},
            {"heap-loadf64",    0x27},

            {"malloc",          0x28},

            {"ref",             0x29},

            {"free",            0x2A},

            {"call",            0x2B},
            {"ret",             0x2C},

            {"enter",           0x2D},

            {"jump",            0x2E},
            {"jump-z",           0x2F},
            {"jump-nz",          0x30},


            {"add-u",           0x31},
            {"add-s",           0x32},
            {"add-f",           0x33},

            {"sub-u",           0x34},
            {"sub-s",           0x35},
            {"sub-f",           0x36},

            {"mul-u",           0x37},
            {"mul-s",           0x38},
            {"mul-f",           0x39},

            {"div-u",           0x3A},
            {"div-s",           0x3B},
            {"div-f",           0x3C},

            {"safe-div-u",      0x3D},
            {"safe-div-s",      0x3E},
            {"safe-div-f",      0x3F},

            
            {"eql-u",           0x40},
            {"eql-s",           0x41},
            {"eql-f",           0x42},
            
            {"neql-u",          0x43},
            {"neql-s",          0x44},
            {"neql-f",          0x45},

            {"gtr-u",           0x46},
            {"gtr-s",           0x47},
            {"gtr-f",           0x48},

            {"egtr-u",          0x49},
            {"egtr-s",          0x4A},
            {"egtr-f",          0x4B},

            {"les-u",           0x4C},
            {"les-s",           0x4D},
            {"les-f",           0x4E},

            {"eles-u",          0x4F},
            {"eles-s",          0x50},
            {"eles-f",          0x51},

            
            {"not",             0x52},


            {"cast-uf",         0x53},
            {"cast-sf",         0x54},

            {"cast-fu",         0x55},
            {"cast-fs",         0x56},

            {"pop",             0x57},


            {"print",           0xFE},
            {"printf",          0xFD},

            {"exit",            0xFF},

        };

        
    }
    public enum oc : byte
        {
            Null            = 0x00,

            // Push 8 bytes into OStack
            // operand: u64 value
            PUSH_CONST      = 0x01,

            // Store into VStack (frame-relative: addr = fp + offset)
            // operand: u32 offset | pops: value
            STORE_8         = 0x02,
            STORE_16        = 0x03,
            STORE_32        = 0x04,
            STORE_64        = 0x05,
            STORE_F32       = 0x06,
            STORE_F64       = 0x07,

            // Store into VStack through an address (pointers / references)
            // pops: addr (top), then value
            STORE_ADDR_8    = 0x08,
            STORE_ADDR_16   = 0x09,
            STORE_ADDR_32   = 0x0A,
            STORE_ADDR_64   = 0x0B,
            STORE_ADDR_F32  = 0x0C,
            STORE_ADDR_F64  = 0x0D,

            // Load from VStack (frame-relative: addr = fp + offset)

            LOAD_U8         = 0x0E,
            LOAD_U16        = 0x0F,
            LOAD_U32        = 0x10,
            LOAD_U64        = 0x11,
            LOAD_S8         = 0x12,
            LOAD_S16        = 0x13,
            LOAD_S32        = 0x14,
            LOAD_S64        = 0x15,
            LOAD_F32        = 0x16,
            LOAD_F64        = 0x17,

            // Store into VHeap
            // pops: addr (top), then value
            HSTORE_8        = 0x18,
            HSTORE_16       = 0x19,
            HSTORE_32       = 0x1A,
            HSTORE_64       = 0x1B,
            HSTORE_F32      = 0x1C,
            HSTORE_F64      = 0x1D,

            // Load from VHeap
            // pops: addr | pushes: value
            HLOAD_U8        = 0x1E,
            HLOAD_U16       = 0x1F,
            HLOAD_U32       = 0x20,
            HLOAD_U64       = 0x21,
            HLOAD_S8        = 0x22,
            HLOAD_S16       = 0x23,
            HLOAD_S32       = 0x24,
            HLOAD_S64       = 0x25,
            HLOAD_F32       = 0x26,
            HLOAD_F64       = 0x27,


            MALLOC          = 0x28,

            REF             = 0x29,

            // Later bro..
            FREE            = 0x2A,

            CALL            = 0x2B,

            RET             = 0x2C,

            ENTER           = 0x2D,

            JUMP            = 0x2E,
            JUMPZ           = 0x2F,
            JUMPNZ          = 0x30,

            // Operations
            // (U == Unsigned Int 8-64bits | S == Signed Int 8-64bits | F == Float 32-64bits)

            ADD_U           = 0x31,
            ADD_S           = 0x32,
            ADD_F           = 0x33,

            SUB_U           = 0x34,
            SUB_S           = 0x35,
            SUB_F           = 0x36,

            MUL_U           = 0x37,
            MUL_S           = 0x38,
            MUL_F           = 0x39,

            DIV_U           = 0x3A,
            DIV_S           = 0x3B,
            DIV_F           = 0x3C,

            // Safe Div has a IF check every division
            // to check if dividing by 0. if it is, then
            // it will throw an exception. whereas unsafe
            // division is much faster but will crash silently.

            SAFE_DIV_U      = 0x3D,
            SAFE_DIV_S      = 0x3E,
            SAFE_DIV_F      = 0x3F,


            // Boolean Operators
            // Pops 2 values, Pushes 1 uint8 (1 or 0 as bool)
            EQL_U           = 0x40,
            EQL_S           = 0x41,
            EQL_F           = 0x42,

            NEQL_U          = 0x43,
            NEQL_S          = 0x44,
            NEQL_F          = 0x45,

            GTR_U           = 0x46,
            GTR_S           = 0x47,
            GTR_F           = 0x48,

            EGTR_U          = 0x49,
            EGTR_S          = 0x4A,
            EGTR_F          = 0x4B,

            LES_U           = 0x4C,
            LES_S           = 0x4D,
            LES_F           = 0x4E,

            ELES_U          = 0x4F,
            ELES_S          = 0x50,
            ELES_F          = 0x51,

            // Flips boolean value from oStack
            // 1 pop, 1 push
            NOT             = 0x52,

            // Casting OpCodes
            U_F             = 0x53,
            S_F             = 0x54,
            F_U             = 0x55,
            F_S             = 0x56,

            //// Bitwise Operators
            //AND             = 0x59,
            //OR              = 0x5A,
            //XOR             = 0x5B,
            //BIT_NOT         = 0x5C,
            //SHL             = 0x5D,
            //SHR             = 0x5E,
            //MOD             = 0x5F,
            //NEG             = 0x60,



            DEBUG_PRINT     = 0xFE,
            DEBUG_PRINTF    = 0xFD,



            EXIT            = 0xFF
        }
}
