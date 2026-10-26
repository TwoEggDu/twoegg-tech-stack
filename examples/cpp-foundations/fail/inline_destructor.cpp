#include <memory>
struct Impl;
struct Broken {
    std::unique_ptr<Impl> value;
    ~Broken() = default;
};
int main() { Broken broken; }
