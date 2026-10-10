# Ownership and native inputs

`NGXParameter` and `NGXHandle` are borrowed readonly values with a `Value` address, `IsNull`, equality, hashing and deconstruction. Construct them from an existing native address when interoperating with another binding.

Release owned parameter maps and features explicitly with the matching backend's `DestroyParameters` and `ReleaseFeature` methods. The legacy `GetParameters` result belongs to the SDK and must not be destroyed. Serialize SDK calls and finish submitted GPU work before releasing resources, views, command buffers, devices or features. Conversion does not take ownership of those external objects.

Initialization strings, paths and logging callbacks remain alive until successful backend shutdown. Evaluate helpers keep converted pointer inputs with their parameter map. Successful replacement, `Parameter.Reset`, `DestroyParameters` or backend shutdown releases retained inputs. A failed helper may have partially changed the parameter map, so its storage remains retained until replacement or cleanup. Omitting DLSSG optional settings preserves the previous matrix storage.

`GetVoidPointer` returns a borrowed address; do not keep it across parameter mutation or cleanup. String arrays supply their own counts. Vulkan extension queries return managed arrays. Optional structure pointers become nullable values, while fixed native text buffers become strings. Application and resource unions use nullable members selected by the enclosing discriminator. Structure inputs use `in`; callers can omit the argument modifier. Optional structure pointers have nullable-value overloads. External devices, GPU resources and Vulkan procedure addresses remain `nint`.

Initialization strings, paths and strings in converted structures reject embedded NUL. Parameter keys preserve their literal byte and terminator semantics. Text encoding rejects invalid Unicode. `NGXMarshal.StringToPtr` allocates a complete encoded string with a terminator; release it with `NGXMarshal.Free` only after native use ends. `PtrToString` copies through the first terminator and does not free the input.

Logging and progress delegates passed to `Ngx` contain exceptions and report them through `Trace`. Logging handlers must support concurrent invocation. Callbacks registered manually as raw addresses require the caller to retain the delegate and contain exceptions throughout native registration.
