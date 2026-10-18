#pragma once

#include <cstdint>
#include <cstring>
#include <vector>
#include <bit>
#include <iostream>
#include <stdexcept>

#include "data.hpp"

class vm {
private:
    inline static v_heap  vHeap;
    inline static v_stack vStack;
    inline static o_stack oStack;
    inline static c_stack cStack;

    template <typename To, typename From>
    static To bit_cast(const From& src) {
        static_assert(sizeof(To) == sizeof(From), "bit_cast size mismatch");
        To dst;
        std::memcpy(&dst, &src, sizeof(To));
        return dst;
    }

    template <typename T>
    static T fetch(const std::vector<uint8_t>& bc, uint64_t& ip) {
        if (ip + sizeof(T) > bc.size()) throw std::runtime_error("truncated bytecode");
        T v;
        std::memcpy(&v, bc.data() + ip, sizeof(T));
        ip += sizeof(T);
        return v;
    }

    static double popDouble() { return bit_cast<double>(oStack.pop()); }
    static void pushDouble(double d) { oStack.push(bit_cast<uint64_t>(d)); }

    // vstack helpers

    template <typename T>   // ints
    static void storeFrame(const std::vector<uint8_t>& bc, uint64_t& ip) {
        uint32_t addr = vStack.getAddress(fetch<uint32_t>(bc, ip));
        vStack.write(addr, static_cast<T>(oStack.pop()));
    }
    template <typename F>   // floats
    static void storeFrameF(const std::vector<uint8_t>& bc, uint64_t& ip) {
        uint32_t addr = vStack.getAddress(fetch<uint32_t>(bc, ip));
        vStack.write(addr, static_cast<F>(popDouble()));
    }
    template <typename T>
    static void loadFrame(const std::vector<uint8_t>& bc, uint64_t& ip) {
        uint32_t addr = vStack.getAddress(fetch<uint32_t>(bc, ip));
        oStack.push(static_cast<uint64_t>(vStack.read<T>(addr)));
    }
    template <typename F>
    static void loadFrameF(const std::vector<uint8_t>& bc, uint64_t& ip) {
        uint32_t addr = vStack.getAddress(fetch<uint32_t>(bc, ip));
        pushDouble(static_cast<double>(vStack.read<F>(addr)));
    }

    template <typename T>
    static void storeAddr() {
        uint64_t address = oStack.pop();
        vStack.write(address, static_cast<T>(oStack.pop()));
    }
    template <typename F>
    static void storeAddrF() {
        uint64_t address = oStack.pop();
        vStack.write(address, static_cast<F>(popDouble()));
    }
    // Load from stack address (not frame relative)
    template <typename T>
    static void loadAddr() {
        uint64_t address = oStack.pop();
        oStack.push(static_cast<uint64_t>(vStack.read<T>(address)));
    }
    template <typename F>
    static void loadAddrF() {
        uint64_t address = oStack.pop();
        pushDouble(static_cast<double>(vStack.read<F>(address)));
    }

    // vHeap helpers
    template <typename T>
    static void hstore() {
        uint64_t address = oStack.pop();
        vHeap.write(address, static_cast<T>(oStack.pop()));
    }
    template <typename T>
    static void hstoreF() {
        uint64_t address = oStack.pop();
        vHeap.write(address, static_cast<T>(popDouble()));
    }
    template <typename T>
    static void hload() {
        oStack.push(static_cast<uint64_t>(vHeap.read<T>(oStack.pop())));
    }
    template <typename T>
    static void hloadF() {
        pushDouble(static_cast<double>(vHeap.read<T>(oStack.pop())));
    }

public:
    static void execute(std::vector<uint8_t>& bc) {
        vStack.fp  = 0;
        vStack.ptr = 0;     // main must start with ENTER <frameSize>
        uint64_t ip = 0;

        while (ip < bc.size()) {

            //std::cout << "IP: " << std::to_string(ip) << "\n";
            
            switch (bc[ip++]) {
                case OPCODE::Null:
                    std::cout << "Null Command. Skipping Line..\n";
                    break;
                case OPCODE::EXIT:
                    //std::cout << "EXIT (0xFF) Reached\n";
                    return;

                case OPCODE::PUSH_CONST: oStack.push(fetch<uint64_t>(bc, ip)); break;
                case OPCODE::POP: oStack.pop(); break;

                // Frame-relative stores
                case OPCODE::STORE_8:   storeFrame<uint8_t>(bc, ip);  break;
                case OPCODE::STORE_16:  storeFrame<uint16_t>(bc, ip); break;
                case OPCODE::STORE_32:  storeFrame<uint32_t>(bc, ip); break;
                case OPCODE::STORE_64:  storeFrame<uint64_t>(bc, ip); break;
                case OPCODE::STORE_F32: storeFrameF<float>(bc, ip);   break;
                case OPCODE::STORE_F64: storeFrameF<double>(bc, ip);  break;

                // Stores through an address
                case OPCODE::STORE_ADDR_8:   storeAddr<uint8_t>();  break;
                case OPCODE::STORE_ADDR_16:  storeAddr<uint16_t>(); break;
                case OPCODE::STORE_ADDR_32:  storeAddr<uint32_t>(); break;
                case OPCODE::STORE_ADDR_64:  storeAddr<uint64_t>(); break;
                case OPCODE::STORE_ADDR_F32: storeAddrF<float>();   break;
                case OPCODE::STORE_ADDR_F64: storeAddrF<double>();  break;

                // Frame-relative loads
                case OPCODE::LOAD_U8:  loadFrame<uint8_t>(bc, ip);  break;
                case OPCODE::LOAD_U16: loadFrame<uint16_t>(bc, ip); break;
                case OPCODE::LOAD_U32: loadFrame<uint32_t>(bc, ip); break;
                case OPCODE::LOAD_U64: loadFrame<uint64_t>(bc, ip); break;
                case OPCODE::LOAD_S8:  loadFrame<int8_t>(bc, ip);   break;
                case OPCODE::LOAD_S16: loadFrame<int16_t>(bc, ip);  break;
                case OPCODE::LOAD_S32: loadFrame<int32_t>(bc, ip);  break;
                case OPCODE::LOAD_S64: loadFrame<int64_t>(bc, ip);  break;
                case OPCODE::LOAD_F32: loadFrameF<float>(bc, ip);   break;
                case OPCODE::LOAD_F64: loadFrameF<double>(bc, ip);  break;

                // Load from Stack Address
                case OPCODE::LOAD_ADDR_U8:  loadAddr<uint8_t>(); break;
                case OPCODE::LOAD_ADDR_U16: loadAddr<uint16_t>(); break;
                case OPCODE::LOAD_ADDR_U32: loadAddr<uint32_t>(); break;
                case OPCODE::LOAD_ADDR_U64: loadAddr<uint64_t>(); break;
                case OPCODE::LOAD_ADDR_S8:  loadAddr<int8_t>(); break;
                case OPCODE::LOAD_ADDR_S16: loadAddr<int16_t>(); break;
                case OPCODE::LOAD_ADDR_S32: loadAddr<int32_t>(); break;
                case OPCODE::LOAD_ADDR_S64: loadAddr<int64_t>(); break;
                case OPCODE::LOAD_ADDR_F32: loadAddrF<float>(); break;
                case OPCODE::LOAD_ADDR_F64: loadAddrF<double>(); break;

                // Heap
                case OPCODE::HSTORE_8:   hstore<uint8_t>();  break;
                case OPCODE::HSTORE_16:  hstore<uint16_t>(); break;
                case OPCODE::HSTORE_32:  hstore<uint32_t>(); break;
                case OPCODE::HSTORE_64:  hstore<uint64_t>(); break;
                case OPCODE::HSTORE_F32: hstoreF<float>();   break;
                case OPCODE::HSTORE_F64: hstoreF<double>();  break;

                case OPCODE::HLOAD_U8:  hload<uint8_t>();  break;
                case OPCODE::HLOAD_U16: hload<uint16_t>(); break;
                case OPCODE::HLOAD_U32: hload<uint32_t>(); break;
                case OPCODE::HLOAD_U64: hload<uint64_t>(); break;
                case OPCODE::HLOAD_S8:  hload<int8_t>();   break;
                case OPCODE::HLOAD_S16: hload<int16_t>();  break;
                case OPCODE::HLOAD_S32: hload<int32_t>();  break;
                case OPCODE::HLOAD_S64: hload<int64_t>();  break;
                case OPCODE::HLOAD_F32: hloadF<float>();   break;
                case OPCODE::HLOAD_F64: hloadF<double>();  break;

                case OPCODE::MALLOC:
                    oStack.push(vHeap.allocate(static_cast<size_t>(oStack.pop())));
                    break;

                case OPCODE::FREE:
                    oStack.pop();   // TODO: real free; for now just discards the address
                    break;

                case OPCODE::REF:
                    oStack.push(vStack.getAddress(fetch<uint32_t>(bc, ip)));
                    break;

                case OPCODE::CALL: {
                    uint32_t target = fetch<uint32_t>(bc, ip);
                    cStack.push(vStack.fp, ip);     // ip is already past the operand
                    vStack.fp = vStack.ptr;         // new frame starts at the top
                    ip = target;
                    break;
                }
                case OPCODE::ENTER:
                    vStack.ptr = vStack.fp + fetch<uint32_t>(bc, ip);
                    break;

                case OPCODE::RET:
                    vStack.ptr = vStack.fp;
                    cStack.pop(vStack.fp, ip);
                    break;

                case OPCODE::JUMP:{
                    ip = fetch<uint32_t>(bc, ip);
                    break;
                }
                case OPCODE::JUMPZ:{
                    uint32_t target =  fetch<uint32_t>(bc, ip);
                    if(static_cast<uint8_t>(oStack.pop()) == 0) ip = target;
                    break;
                }
                case OPCODE::JUMPNZ:{
                    uint32_t target =  fetch<uint32_t>(bc, ip);
                    if(static_cast<uint8_t>(oStack.pop()) != 0) ip = target;
                    break;
                }


                case OPCODE::ADD_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    oStack.push(a+b);                                   break;
                }
                case OPCODE::ADD_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    oStack.push(static_cast<uint64_t>(a+b));          break;
                }
                case OPCODE::ADD_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    oStack.push(std::bit_cast<uint64_t>(a+b));           break;
                }

                case OPCODE::SUB_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    oStack.push(a-b);                                   break;
                }
                case OPCODE::SUB_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    oStack.push(static_cast<uint64_t>(a-b));          break;
                }
                case OPCODE::SUB_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    oStack.push(std::bit_cast<uint64_t>(a-b));           break;
                }

                case OPCODE::MUL_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    oStack.push(a*b);                                   break;
                }
                case OPCODE::MUL_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    oStack.push(static_cast<uint64_t>(a*b));          break;
                }
                case OPCODE::MUL_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    oStack.push(std::bit_cast<uint64_t>(a*b));           break;
                }

                case OPCODE::DIV_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    oStack.push(a/b);                                   break;
                }
                case OPCODE::DIV_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    oStack.push(static_cast<uint64_t>(a/b));          break;
                }
                case OPCODE::DIV_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    oStack.push(std::bit_cast<uint64_t>(a/b));           break;
                }

                case OPCODE::SAFE_DIV_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    if(b==0) throw std::runtime_error("0 Division isnt allowed, IP: " + std::to_string(ip) + "\n");
                    oStack.push(a/b);                                   break;
                }
                case OPCODE::SAFE_DIV_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    if(b==0) throw std::runtime_error("0 Division isnt allowed, IP: " + std::to_string(ip) + "\n");
                    oStack.push(static_cast<uint64_t>(a/b));          break;
                }
                case OPCODE::SAFE_DIV_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    if(b==0) throw std::runtime_error("0 Division isnt allowed, IP: " + std::to_string(ip) + "\n");
                    oStack.push(std::bit_cast<uint64_t>(a/b));           break;
                }


                case OPCODE::EQL_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a == b);
                    oStack.push(res);                        break;
                }
                case OPCODE::EQL_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a == b);
                    oStack.push(res);                         break;
                }
                case OPCODE::EQL_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a == b);
                    oStack.push(res);                         break;
                }

                case OPCODE::NEQL_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a != b);
                    oStack.push(res);                        break;
                }
                case OPCODE::NEQL_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a != b);
                    oStack.push(res);                         break;
                }
                case OPCODE::NEQL_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a != b);
                    oStack.push(res);                         break;
                }

                case OPCODE::GTR_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a > b);
                    oStack.push(res);                        break;
                }
                case OPCODE::GTR_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a > b);
                    oStack.push(res);                         break;
                }
                case OPCODE::GTR_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a > b);
                    oStack.push(res);                         break;
                }
                case OPCODE::EGTR_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a >= b);
                    oStack.push(res);                        break;
                }
                case OPCODE::EGTR_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a >= b);
                    oStack.push(res);                         break;
                }
                case OPCODE::EGTR_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a >= b);
                    oStack.push(res);                         break;
                }

                case OPCODE::LES_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a < b);
                    oStack.push(res);                        break;
                }
                case OPCODE::LES_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a < b);
                    oStack.push(res);                         break;
                }
                case OPCODE::LES_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a < b);
                    oStack.push(res);                         break;
                }
                case OPCODE::ELES_U:{
                    uint64_t b = oStack.pop();
                    uint64_t a = oStack.pop();
                    uint8_t res = static_cast<uint8_t>(a <= b);
                    oStack.push(res);                        break;
                }
                case OPCODE::ELES_S:{
                    int64_t b = static_cast<int64_t>(oStack.pop());
                    int64_t a = static_cast<int64_t>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a <= b);
                    oStack.push(res);                         break;
                }
                case OPCODE::ELES_F:{
                    double b = std::bit_cast<double>(oStack.pop());
                    double a = std::bit_cast<double>(oStack.pop());
                    uint8_t res = static_cast<uint8_t>(a <= b);
                    oStack.push(res);                         break;
                }
                case OPCODE::NOT:{
                    bool val = oStack.pop() != 0;
                    oStack.push(static_cast<uint8_t>(!val));
                }

                case OPCODE::U_F: {
                    double F = static_cast<double>(oStack.pop());
                    oStack.push(std::bit_cast<uint64_t>(F));
                    break;
                }
                case OPCODE::S_F: {
                    double F = static_cast<double>(std::bit_cast<int64_t>(oStack.pop()));
                    oStack.push(std::bit_cast<uint64_t>(F));
                    break;
                }

                case OPCODE::F_S: {
                    int64_t S = static_cast<int64_t>(std::bit_cast<double>(oStack.pop()));
                    oStack.push(std::bit_cast<uint64_t>(S));
                    break;
                }
                case OPCODE::F_U: {
                    uint64_t U = static_cast<uint64_t>(std::bit_cast<double>(oStack.pop()));
                    oStack.push(U);
                    break;
                }

                

                case OPCODE::DEBUG_PRINT:
                    std::cout << static_cast<int64_t>(oStack.pop()) << "\n";
                    //oStack.pop();
                    break;
                case OPCODE::DEBUG_PRINTF:
                    std::cout << std::to_string(bit_cast<double>(oStack.pop())) << "\n";
                    //oStack.pop();
                    break;

                default:
                    throw std::runtime_error("Unknown opcode");
            }
        }
    }
};
