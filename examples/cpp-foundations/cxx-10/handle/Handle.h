#pragma once
#include <memory>
class Handle {
    struct Impl;
    std::unique_ptr<Impl> impl_;
public:
    explicit Handle(int value);
    ~Handle();
    Handle(Handle&&) noexcept;
    Handle& operator=(Handle&&) noexcept;
    Handle(const Handle&) = delete;
    Handle& operator=(const Handle&) = delete;
    int Value() const;
};
