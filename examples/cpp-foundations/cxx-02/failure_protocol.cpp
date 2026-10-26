#include "State.h"
#include <cassert>
#include <iostream>
int main() {
    {
        State state;
        assert(state.Replace({1, 2}, Failure::None));
        const auto original = state.Values();
        assert(!state.Replace({8, 9}, Failure::Return));
        assert(state.Values() == original && Ticket::alive == 0);
        bool caught = false;
        try { state.Replace({8, 9}, Failure::Throw); }
        catch (const std::runtime_error&) { caught = true; }
        assert(caught && state.Values() == original && Ticket::alive == 0);
        assert(!state.Replace({-1}, Failure::None));
        assert(state.Values() == original && Ticket::alive == 0);
        assert(state.Replace({3, 4}, Failure::None));
        assert((state.Values() == std::vector<int>{3, 4}));
    }
    assert(Ticket::alive == 0);
    std::cout << "return/throw/invalid preserved old state; success committed; tickets=0\n";
}
