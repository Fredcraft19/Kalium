#include <iostream>
#include <chrono>

#include <fstream>

#include "vm/vm.hpp"

int main(){  
    using clock = std::chrono::steady_clock;
    
    std::ifstream file("bytecode.wbc", std::ios::binary | std::ios::ate);
    if(!file){
        throw std::runtime_error("file 'bytecode.wbc' does not exist?");
    }

    const auto fileSize = file.tellg();

    if (fileSize < 0)
        throw std::runtime_error("failed to determine file size");

    std::vector<uint8_t> bytecode(static_cast<size_t>(fileSize));
    file.seekg(0, std::ios::beg);

    if (!bytecode.empty() && !file.read(reinterpret_cast<char*>(bytecode.data()), static_cast<std::streamsize>(bytecode.size()))) {
        throw std::runtime_error("failed to read bytecode");
    }

    constexpr int iterations = 1;

    //for (int i = 0; i < 1000; ++i) {
    //    vm::execute(bytecode);
    //}

    auto start = clock::now();

    for (int i = 0; i < iterations; ++i) {
        vm::execute(bytecode);
    }

    auto end = clock::now();

    auto total_ns = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start).count();
    double avg_ns = static_cast<double>(total_ns) / iterations;

    std::cout << "Average runtime: " << avg_ns << " ns\n";

    std::string x;
    std::cin >> x;
    
    return 0;
}
