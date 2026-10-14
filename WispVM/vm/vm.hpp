#include <cstdint>
#include <vector>
#include <bit>

#include "data.hpp"


class vm{

private:

    // Variable Heap
    inline static v_heap vHeap;

    // Variable Stack
    inline static v_stack vStack;
    
    // Operation Stack
    inline static o_stack oStack;

    // Call Stack
    inline static c_stack cStack;

public:
    static void execute(std::vector<uint8_t>& bc){
        vStack.ptr = 0;
        uint64_t ip = 0;
        
        while(ip < bc.size()){

            switch(bc[ip++]){
                case OPCODE::Null:
                    std::cout << "Null Command. Skipping Line..";
                    break;
                case OPCODE::EXIT:
                    std::cout << "EXIT (0xFF) Reached\n";
                    return;
                
                case OPCODE::PUSH_CONST:
                {
                    uint64_t val = 0;
                    std::memcpy(&val, bc.data() + ip, 8);
                    oStack.push(val);
                    ip += 8;
                    break;
                }

                case OPCODE::STORE_8:
                    vStack.push <uint8_t>(oStack.pop()); break;
                case OPCODE::STORE_16:
                    vStack.push<uint16_t>(oStack.pop()); break;
                case OPCODE::STORE_32:
                    vStack.push<uint32_t>(oStack.pop()); break;
                case OPCODE::STORE_64:
                    vStack.push<uint64_t>(oStack.pop()); break;
                
                case OPCODE::STORE_F32:
                {
                    float val = 0.f;
                    uint32_t raw = static_cast<uint32_t>(oStack.pop());
                    std::memcpy(&val, &raw, sizeof(float));
                    vStack.push<float>(val);

                    // What does this do??
                    // 1. Pops top uint64 from OStack
                    // 2. Converts uint64 to uint32
                    // 3. converts uint32 to float
                    // 4. stores float in vStack
                    break;
                }
                case OPCODE::STORE_F64:
                {
                    double val = 0.0;
                    uint64_t raw = oStack.pop();
                    std::memcpy(&val, &raw, sizeof(double));
                    vStack.push<double>(val);

                    // What does this do??
                    // 1. Pops top uint64 from OStack
                    // 2. Converts uint64 to double
                    // 3. stores double in vStack
                    break;
                }

                case OPCODE::LOAD_U8:
                {
                    // Gets 32-bit offset in stack
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    
                    // debug value check
                    //std::cout << std::to_string(vStack.read<uint8_t>(addr)) << "\n";

                    oStack.push(static_cast<uint64_t>(vStack.read<uint8_t>(addr)));
                    // What does this do?
                    // 1. Reads uint8 from 'addr'.
                    // 2. Pushes the uint8 into the oStack
                    break;
                }
                case OPCODE::LOAD_U16:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(vStack.read<uint16_t>(addr))); break;
                }
                case OPCODE::LOAD_U32:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(vStack.read<uint32_t>(addr))); break;
                }
                case OPCODE::LOAD_U64:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(vStack.read<uint64_t>(addr)); break;
                }
                case OPCODE::LOAD_S8:
                {
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vStack.read<int8_t>(addr)))); break;
                }
                case OPCODE::LOAD_S16:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vStack.read<int16_t>(addr)))); break;
                }
                case OPCODE::LOAD_S32:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vStack.read<int32_t>(addr)))); break;
                }
                case OPCODE::LOAD_S64:{
                    uint32_t addr = 0;
                    std::memcpy(&addr, bc.data() + ip, 4); ip += 4;
                    oStack.push(static_cast<uint64_t>(vStack.read<int64_t>(addr))); break;
                }

                case OPCODE::HSTORE_8:{
                    uint8_t value = oStack.pop();
                    vHeap.write(oStack.pop(), value); break;

                    // What does this do?
                    // 1. Gets value to write on heap
                    // 2. Gets address to write to (oStack.pop())
                    // 3. Writes value to address in vHeap
                }
                case OPCODE::HSTORE_16:{
                    uint16_t value = oStack.pop();
                    vHeap.write(oStack.pop(), value); break;
                }
                case OPCODE::HSTORE_32:{
                    uint32_t value = oStack.pop();
                    vHeap.write(oStack.pop(), value); break;
                }
                case OPCODE::HSTORE_64:{
                    uint64_t value = oStack.pop();
                    vHeap.write(oStack.pop(), value); break;
                }
                case OPCODE::HSTORE_F32:{
                    double _d = 0.0;
                    uint64_t raw = oStack.pop();
                    std::memcpy(&_d, &raw, sizeof(double));
                    float value = static_cast<float>(_d);
                    vHeap.write(oStack.pop(), value); break;
                }
                case OPCODE::HSTORE_F64:{
                    double value = 0.0;
                    uint64_t raw = oStack.pop();
                    std::memcpy(&value, &raw, sizeof(double));
                    vHeap.write(oStack.pop(), value); break;
                }

                case OPCODE::HLOAD_U8:
                    oStack.push(vHeap.read<uint8_t>(oStack.pop()));
                    break;
                    // What does this do?
                    // 1. Gets address from oStack.pop()
                    // 2. Reads value from address in vHeap
                    // 3. Pushes value to oStack
                case OPCODE::HLOAD_U16:
                    oStack.push(vHeap.read<uint16_t>(oStack.pop()));
                    break;
                case OPCODE::HLOAD_U32:
                    oStack.push(vHeap.read<uint32_t>(oStack.pop()));
                    break;
                case OPCODE::HLOAD_U64:
                    oStack.push(vHeap.read<uint64_t>(oStack.pop()));
                    break;
                case OPCODE::HLOAD_S8:
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vHeap.read<int8_t>(oStack.pop()))));
                    break;
                    // What does this do?
                    // 1. oStack.pop() gets address to read from vHeap
                    // 2. Casts int8 into a int64
                    // 3. Casts int64 to uint64 for oStack
                    // 4. Pushes uint64 to oStack
                case OPCODE::HLOAD_S16:
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vHeap.read<int16_t>(oStack.pop()))));
                    break;
                case OPCODE::HLOAD_S32:
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vHeap.read<int32_t>(oStack.pop()))));
                    break;
                case OPCODE::HLOAD_S64:
                    oStack.push(static_cast<uint64_t>(static_cast<int64_t>(vHeap.read<int64_t>(oStack.pop()))));
                    break;
                case OPCODE::HLOAD_F32:
                    oStack.push(std::bit_cast<uint64_t>(static_cast<double>(vHeap.read<float>(oStack.pop()))));
                    break;
                    // What does this do?
                    // 1. Get address to read from heap
                    // 2. cast float to double
                    // 3. cast double to uint64
                    // 4. push uint64 to oStack
                case OPCODE::HLOAD_F64:
                    oStack.push(std::bit_cast<uint64_t>(vHeap.read<double>(oStack.pop())));
                    break;
                
                case OPCODE::MALLOC:
                    oStack.push(vHeap.allocate(static_cast<size_t>(oStack.pop())));
                    break;
                    // What does this do?
                    // 1. Gets size to allocate from oStack.pop
                    // 2. Casts uint64 into size_t
                    // 3. Pass size_t into vHeap.allocate()
                    // 4. Push allocated memory address in oStack

                case OPCODE::REF:{
                    uint32_t offset = 0;
                    std::memcpy(&offset, bc.data() + ip, 4);
                    ip += 4;
                    vStack.getAddress(cStack.csp, offset);

                }

                case OPCODE::CALL:{
                    cStack.push(vStack.ptr, ip);

                    break;
                }
                case OPCODE::RET:{
                    cStack.pop(vStack.ptr, ip);
                    break;
                }


            }
        }
    }

};
