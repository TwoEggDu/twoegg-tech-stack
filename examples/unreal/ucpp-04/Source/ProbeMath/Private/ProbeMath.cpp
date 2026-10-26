#include "ProbeMath.h"
#include "Modules/ModuleManager.h"
int32 ProbeMath::Next(int32 Value) { return Value + 7; }
IMPLEMENT_MODULE(FDefaultModuleImpl, ProbeMath)
