# Ownership and native inputs

`NGXParameter` and `NGXHandle` are borrowed readonly values with a `Value` address, `IsNull`, equality and hashing. Construct them from an existing native address when interoperating with another binding.

Release owned parameter maps and features explicitly with the matching backend's `DestroyParameters` and `ReleaseFeature` methods. The legacy `GetParameters` result belongs to the SDK and must not be destroyed. Serialize SDK calls and finish submitted GPU work before releasing resources, views, command buffers, devices or features. Conversion does not take ownership of those external objects.

Initialization strings, paths and logging callbacks remain alive until successful backend shutdown. Evaluate helpers keep only converted memory that the SDK still references with their parameter map. D3D11/D3D12 DLSS and DLISP inputs use stack conversion because the helpers retain only caller-owned resource addresses and scalar values. Successful replacement, `Parameter.Reset`, `DestroyParameters` or backend shutdown releases retained inputs. A failed helper may have partially changed the parameter map, so its storage remains retained until replacement or cleanup. Omitting DLSSG optional settings preserves the previous matrix storage.

`GetVoidPointer` returns a borrowed address; do not keep it across parameter mutation or cleanup. String arrays supply their own counts. Vulkan extension queries return managed arrays. Optional structure pointers become nullable values, while fixed native text buffers become strings. Application and resource unions use nullable members selected by the enclosing discriminator. Structure inputs use `in`; callers can omit the argument modifier. Optional structure pointers have nullable-value overloads. External devices, GPU resources and Vulkan procedure addresses remain `nint`.

Strings follow the .NET UTF-8 and wide-character conversion behavior: embedded NUL has native C-string truncation semantics, and invalid Unicode uses replacement encoding. Parameter-key constants retain their literal byte and terminator semantics. Conversion and retained memory are internal; callers do not allocate or free binding-owned strings.

CUDA descriptors use a stable native address for each context and stream pair, including failed initialization attempts, until backend shutdown. The library retains scope-owned allocations and callback delegates for their SDK lifetime. A finalizer releases a scope when conversion stops before native code receives its addresses.

Logging and progress delegates passed to `Ngx` contain exceptions and report them through `Trace`. Logging handlers must support concurrent invocation. Callbacks registered manually as raw addresses require the caller to retain the delegate and contain exceptions throughout native registration.
