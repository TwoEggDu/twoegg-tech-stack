#include "Handle.h"
#include <cassert>
#include <utility>
int main() {
    Handle first{7};
    Handle next{std::move(first)};
    Handle third{2};
    third = std::move(next);
    assert(third.Value() == 7);
}
