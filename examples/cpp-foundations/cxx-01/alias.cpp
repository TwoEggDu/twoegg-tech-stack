#include <cassert>
#include <type_traits>
namespace reading { using Pointer = int*; using Callback = int (*)(int); }
int Twice(int n) { return n * 2; }
int main() {
    int value = 3;
    const reading::Pointer p = &value;
    static_assert(std::is_same_v<decltype(p), int* const>);
    *p = 4;
    reading::Callback callback = &Twice;
    assert(callback(*p) == 8);
}
