#pragma once

#include <cstdint>
#include <cstring>

#include <vector>
#include <iostream>

// Frame....bro
struct frame{
    // Old Stack Pointer
    uint32_t oldVsp = 0;

    uint32_t oldFp = 0;

    // Where to Return To
    uint64_t oldIp = 0;

    frame() = default;
    frame(uint32_t vsp,uint32_t fp,  uint64_t ip){
        oldVsp = vsp;
        oldIp = ip;
        oldFp = fp;
    }
};

// Call Stack
class c_stack{
private:
    // 1 Frame = 1 Method call
    // So up to 254 nested method calls
    frame frames[255];
public:
    // Call Stack Pointer as 1 byte
    uint8_t csp = 0; 
    // frame pointer used by vStack
    uint32_t fp = 0;
    
    // vsp == vStackPointer (v_stack.ptr)
    // ip = bytecode ip

    void push(uint32_t vsp, uint32_t ip){
        frames[csp++] = frame(vsp, fp, ip);
    }

    void pop(uint32_t& vsp, uint64_t& ip) {
        if (csp == 0) throw std::runtime_error("Call stack underflow");
        const frame& f = frames[--csp];
        vsp = f.oldVsp;
        fp  = f.oldFp;
        ip  = f.oldIp;
    }
};


// Operation Stack
class o_stack{
private:
    uint64_t _ptr = 0;
    uint64_t _stack[128];   // max operation stack is 1kb (128 * 8)
public:
    void push(uint64_t val){
        _stack[_ptr++] = val;
    }
    uint64_t pop(){
        return _stack[--_ptr];
    }
};

// Variable Stack
class v_stack {
private:
    uint8_t _mem[1048576]; // 1 MB
public:
    uint32_t ptr = 0;

    template <typename T>
    T read(uint64_t addr) const {
        if (addr + sizeof(T) > sizeof(_mem)) {
            throw std::runtime_error("Stack out of bounds read");
        }
        T val;
        std::memcpy(&val, _mem + addr, sizeof(T));
        return val;
    }

    uint32_t getAddress(uint8_t fp, uint32_t offset){
        
    }

    void pop(size_t length) {
        if (ptr < length) {
            throw std::runtime_error("vStack underflow");
        }
        ptr -= length;
    }

    template <typename T>
    void push(const T& val) {
        if (ptr + sizeof(T) > sizeof(_mem)) {
            throw std::runtime_error("vStack overflow");
        }
        std::memcpy(_mem + ptr, &val, sizeof(T));
        ptr += sizeof(T);
    }

    // Set the vStacks Pointer
    void set_ptr(uint32_t t_ptr){
        if (t_ptr > ptr) {
            throw std::runtime_error("Invalid stack reset pointer");
        }
        ptr = t_ptr;
    }
};

// Variable Heap
class v_heap {
private:
    uint8_t _mem[1048576]; // Fixed 1 MB heap
    uint64_t ptr = 0;

    void check(uint64_t addr, size_t length) const {
        if (addr + length > sizeof(_mem)) {
            throw std::runtime_error("vHeap out of bounds");
        }
    }

public:
    uint64_t allocate(size_t size) {
        size = (size + 7) & ~size_t(7); // 8-byte alignment
        if (ptr + size > sizeof(_mem)) {
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


enum OPCODE : uint8_t{
    Null            = 0x00,

    // Push 8 bytes into OStack
    PUSH_CONST      = 0x01,

    // Store into VStack
    STORE_8         = 0x02,
    STORE_16        = 0x03,
    STORE_32        = 0x04,
    STORE_64        = 0x05,
    STORE_F32       = 0x06,
    STORE_F64       = 0x07,

    // Load from VStack
    LOAD_U8          = 0x08,
    LOAD_U16         = 0x09,
    LOAD_U32         = 0x0A,
    LOAD_U64         = 0x0B,
    LOAD_S8          = 0x0C,
    LOAD_S16         = 0x0D,
    LOAD_S32         = 0x0E,
    LOAD_S64         = 0x0F,
    LOAD_F32        = 0x10,
    LOAD_F64        = 0x11,

    // Store into VHeap
    HSTORE_8        = 0x12,
    HSTORE_16       = 0x13,
    HSTORE_32       = 0x14,
    HSTORE_64       = 0x15,
    HSTORE_F32      = 0x16,
    HSTORE_F64      = 0x17,

    // Load from VHeap
    HLOAD_U8         = 0x18,
    HLOAD_U16        = 0x19,
    HLOAD_U32        = 0x1A,
    HLOAD_U64        = 0x1B,
    HLOAD_S8         = 0x1C,
    HLOAD_S16        = 0x1D,
    HLOAD_S32        = 0x1E,
    HLOAD_S64        = 0x1F,
    HLOAD_F32        = 0x20,
    HLOAD_F64        = 0x21,

    // Allocate memory in vHeap
    MALLOC           = 0x22,

    // Pushes vStack address, of a variable in stack, to OStack (as uint64_t)
    REF              = 0x23,

    // Later bro..
    FREE             = 0x24,


    CALL             = 0x25,
    RET              = 0x26,

    
    EXIT        = 0xFF
};