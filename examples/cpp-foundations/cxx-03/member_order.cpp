#include <cassert>
struct Row {
    int id;
    int twice;
    explicit Row(int n) : id(n), twice(id * 2) {}
};
int main() { Row row{3}; assert(row.id == 3 && row.twice == 6); }
