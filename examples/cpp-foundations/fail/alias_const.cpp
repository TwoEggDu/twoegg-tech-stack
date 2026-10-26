using Pointer = int*;
int main() { int a = 1, b = 2; const Pointer p = &a; p = &b; }
