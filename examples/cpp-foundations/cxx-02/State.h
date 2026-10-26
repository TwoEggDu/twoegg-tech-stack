#pragma once
#include <stdexcept>
#include <utility>
#include <vector>
struct Ticket {
    inline static int alive = 0;
    Ticket() { ++alive; }
    ~Ticket() noexcept { --alive; }
    Ticket(const Ticket&) = delete;
    Ticket& operator=(const Ticket&) = delete;
};
enum class Failure { None, Return, Throw };
class State {
    std::vector<int> values_;
public:
    const std::vector<int>& Values() const noexcept { return values_; }
    bool Replace(std::vector<int> proposed, Failure fault) {
        Ticket resource;
        for (int value : proposed) if (value < 0) return false;
        if (fault == Failure::Return) return false;
        if (fault == Failure::Throw) throw std::runtime_error("controlled preparation failure");
        values_.swap(proposed); // std::vector<int> with std::allocator: nonthrowing commit
        return true;
    }
};
