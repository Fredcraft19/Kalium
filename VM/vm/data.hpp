#pragma once

#include <cstdint>
#include <cstring>

#include <vector>
#include <iostream>
#include <stdexcept>

// One saved call: caller's frame pointer + where to return to
struct frame {
    uint32_t oldFp = 0;
    uint64_t oldIp = 0;
};

// Call Stack
class c_stack {
private:
    static constexpr uint32_t MAX_DEPTH = 255;
    frame frames[MAX_DEPTH];
public:
    uint32_t csp = 0;   // uint8_t can't safely represent 255

    void push(uint32_t fp, uint64_t ip) {
        if (csp >= MAX_DEPTH) throw std::runtime_error("Call stack overflow");
        frames[csp++] = {fp, ip};
    }

    void pop(uint32_t& fp, uint64_t& ip) {
        if (csp == 0) throw std::runtime_error("Call stack underflow");
        const frame& f = frames[--csp];
        fp = f.oldFp;
        ip = f.oldIp;
    }
};

// Operation Stack
class o_stack {
private:
    static constexpr uint64_t MAX = 128;   // 128 * 8 = 1 KB
    uint64_t _ptr = 0;
    uint64_t _stack[MAX];
public:
    void push(uint64_t val) {
        if (_ptr >= MAX) throw std::runtime_error("OStack overflow");
        _stack[_ptr++] = val;
    }
    uint64_t pop() {
        if (_ptr == 0) throw std::runtime_error("OStack underflow");
        return _stack[--_ptr];
    }
};

// Variable Stack
class v_stack {
private:
    uint8_t _mem[1048576]; // 1 MB
public:
    uint32_t ptr = 0;   // top of the live stack
    uint32_t fp  = 0;   // base of the current frame

    // Only live memory (below ptr) is readable/writable.
    // Written as `addr > ptr || size > ptr - addr` so a huge addr can't wrap around.
    template <typename T>
    T read(uint64_t addr) const {
        if (addr > ptr || sizeof(T) > ptr - addr) {
            throw std::runtime_error("vStack out of bounds read");
        }
        T val;
        std::memcpy(&val, _mem + addr, sizeof(T));
        return val;
    }

    template <typename T>
    void write(uint64_t addr, const T& val) {
        if (addr > ptr || sizeof(T) > ptr - addr) {
            throw std::runtime_error("vStack out of bounds write");
        }
        std::memcpy(_mem + addr, &val, sizeof(T));
    }

    uint32_t getAddress(uint32_t offset) const {
        return fp + offset;
    }

    // Kept for later use; the VM currently reserves frames via ENTER instead
    void pop(size_t length) {
        if (ptr < length) throw std::runtime_error("vStack underflow");
        ptr -= length;
    }

    template <typename T>
    void push(const T& val) {
        if (ptr > sizeof(_mem) || sizeof(T) > sizeof(_mem) - ptr) {
            throw std::runtime_error("vStack overflow");
        }
        std::memcpy(_mem + ptr, &val, sizeof(T));
        ptr += sizeof(T);
    }

    void set_ptr(uint32_t t_ptr) {
        if (t_ptr > ptr) throw std::runtime_error("Invalid stack reset pointer");
        ptr = t_ptr;
    }
};

// Variable Heap
class v_heap {
private:
    uint8_t _mem[1048576]; // Fixed 1 MB heap
    uint64_t ptr = 0;

    void check(uint64_t addr, size_t length) const {
        if (addr > sizeof(_mem) || length > sizeof(_mem) - addr) {
            throw std::runtime_error("vHeap out of bounds");
        }
    }

public:
    uint64_t allocate(size_t size) {
        size = (size + 7) & ~size_t(7); // 8-byte alignment
        if (size > sizeof(_mem) - ptr) {
            throw std::runtime_error("vHeap out of memory");
        }
        uint64_t addr = ptr;
        ptr += size;
        return addr;
    }

    template <typename T>
    T read(uint64_t addr) const {
        check(addr, sizeof(T));
        T val;
        std::memcpy(&val, _mem + addr, sizeof(T));
        return val;
    }

    template <typename T>
    void write(uint64_t addr, const T& val) {
        check(addr, sizeof(T));
        std::memcpy(_mem + addr, &val, sizeof(T));
    }
};

enum OPCODE : uint8_t {
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

    // Pops value from oStack
    POP             = 0x57,

    LOAD_ADDR_U8    = 0x58,
    LOAD_ADDR_U16   = 0x59,
    LOAD_ADDR_U32   = 0x5A,
    LOAD_ADDR_U64   = 0x5B,
    LOAD_ADDR_S8    = 0x5C,
    LOAD_ADDR_S16   = 0x5D,
    LOAD_ADDR_S32   = 0x5E,
    LOAD_ADDR_S64   = 0x5F,
    LOAD_ADDR_F32   = 0x60,
    LOAD_ADDR_F64   = 0x61,

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
};
