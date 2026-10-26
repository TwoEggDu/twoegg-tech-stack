#pragma once
#include <cassert>
struct Item { int id; bool active; };
class Observer {
    const Item* value_ = nullptr; // borrowed; never deletes or extends lifetime
public:
    Observer() = default;
    explicit Observer(const Item* value) : value_(value) {}
    const Item& operator*() const { assert(value_); return *value_; }
    const Item* operator->() const { assert(value_); return value_; }
    explicit operator bool() const { return value_ != nullptr; }
};
