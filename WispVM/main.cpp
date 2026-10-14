#include <iostream>
#include <chrono>

#include "vm/vm.hpp"

int main(){
    using clock = std::chrono::steady_clock;

    std::vector<uint8_t> bytecode = {
        0x01, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA,
        0x02
    };

    constexpr int iterations = 1'000'000;

    for (int i = 0; i < 1000; ++i) {
        vm::execute(bytecode);
    }

    std::cout << "Executing..\n";
    auto start = clock::now();

    for (int i = 0; i < iterations; ++i) {
        vm::execute(bytecode);
    }

    auto end = clock::now();

    auto total_ns = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start).count();
    double avg_ns = static_cast<double>(total_ns) / iterations;

    std::cout << "Total Time: " << total_ns << " ns\n";
    std::cout << "Average per execution: " << avg_ns << " ns\n";

    std::string x;
    std::cin >> x;
    
    return 0;
}
