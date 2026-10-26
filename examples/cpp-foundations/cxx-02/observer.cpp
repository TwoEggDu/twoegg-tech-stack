#include "Observer.h"
#include <cassert>
#include <type_traits>
int main() {
    Item item{7, true};
    Observer observation{&item};
    static_assert(std::is_same_v<decltype(*observation), const Item&>);
    static_assert(!std::is_convertible_v<Observer, bool>);
    assert(observation && observation->id == 7);
    assert(&*observation == &item);
    item.active = false;
    assert(observation && !observation->active);
    Observer empty;
    assert(!empty);
}
