#include "Handle.h"
struct Handle::Impl { int value; };
Handle::Handle(int value) : impl_(std::make_unique<Impl>(Impl{value})) {}
Handle::~Handle() = default;
Handle::Handle(Handle&&) noexcept = default;
Handle& Handle::operator=(Handle&&) noexcept = default;
int Handle::Value() const { return impl_->value; }
