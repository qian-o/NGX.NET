"""Extract the application-facing Streamline contract. Run only in GitHub Actions.

libclang supplies declaration identities, canonical types, values and native layouts.
Source ranges retain inline bodies and macro expressions that libclang does not lower.
Only the normalized JSON is a deliverable; SDK inputs stay in the runner scratch space.
"""

from __future__ import annotations

import argparse
import ctypes
import faulthandler
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import urllib.request


def request_json(url):
    headers = {"User-Agent": "Streamline.NET-Extractor"}
    if os.environ.get("GH_TOKEN"):
        headers["Authorization"] = "Bearer " + os.environ["GH_TOKEN"]
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers)) as response:
        return json.load(response)


def download(url, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(url) as response:
        path.write_bytes(response.read())


def main():
    if os.environ.get("GITHUB_ACTIONS") != "true":
        raise SystemExit("Interface extraction runs exclusively in GitHub Actions.")
    faulthandler.enable()

    from clang import cindex as cx

    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--scratch", required=True, type=Path)
    args = parser.parse_args()
    root = args.scratch.resolve()
    root.mkdir(parents=True, exist_ok=True)
    sdk = root / "streamline"
    vk = root / "vulkan"
    api = "https://api.github.com/repos/NVIDIA-RTX/Streamline"
    release = request_json(api + "/releases/latest")
    commit = request_json(api + "/commits/" + release["tag_name"])["sha"]
    tree = request_json(api + "/git/trees/" + commit + "?recursive=1")
    if tree.get("truncated"):
        raise RuntimeError("Upstream input inventory was truncated.")

    public_headers = sorted(item["path"] for item in tree["tree"]
                            if item["path"].startswith("include/")
                            and item["path"].endswith((".h", ".hpp")))
    if not public_headers:
        raise RuntimeError("No public SDK headers discovered.")
    inputs = []
    for path in public_headers + ["project.xml", "source/core/sl.interposer/exports.def"]:
        download(f"https://raw.githubusercontent.com/NVIDIA-RTX/Streamline/{commit}/{path}", sdk / path)
        inputs.append({"path": path, "sha256": hashlib.sha256((sdk / path).read_bytes()).hexdigest(),
                       "classification": "application" if path in public_headers else "extraction-dependency"})
    for item in tree["tree"]:
        path = item["path"]
        if path.startswith(("source/", "tests/")) and path.endswith((".h", ".hpp", ".cpp")):
            inputs.append({"path": path, "classification": "implementation",
                           "reason": "SDK implementation or plugin implementation; referenced application declarations are taken from public headers."})

    manifest = (sdk / "project.xml").read_text(encoding="utf-8-sig")
    version_match = re.search(r'name="VulkanSDK"\s+version="([0-9]+\.[0-9]+\.[0-9]+)', manifest)
    if not version_match:
        raise RuntimeError("Vulkan header dependency version is absent from the SDK manifest.")
    vk_tag = "v" + version_match[1]
    vk_api = "https://api.github.com/repos/KhronosGroup/Vulkan-Headers"
    vk_commit = request_json(vk_api + "/commits/" + vk_tag)["sha"]
    vk_tree = request_json(vk_api + "/git/trees/" + vk_commit + "?recursive=1")
    if vk_tree.get("truncated"):
        raise RuntimeError("Vulkan dependency inventory was truncated.")
    for item in vk_tree["tree"]:
        path = item["path"]
        if path.startswith("include/") and path.endswith(".h"):
            download(f"https://raw.githubusercontent.com/KhronosGroup/Vulkan-Headers/{vk_commit}/{path}", vk / path)

    # Inputs are ordered by their role, not used as an allow-list for discovery.
    includes = ["#include <windows.h>", "#include <vector>", "#include <type_traits>",
                "#include <cmath>", "#include <algorithm>", "#include <vulkan/vulkan.h>", '#include "sl.h"']
    ordered = sorted(public_headers, key=lambda p: ("helper" in p or "security" in p, p))
    includes.extend('#include "' + p.removeprefix("include/") + '"' for p in ordered if p != "include/sl.h")
    unit = root / "application.cpp"
    unit.write_text("\n".join(includes) + "\n", encoding="utf-8")
    system_includes = [p for p in os.environ.get("INCLUDE", "").split(";") if p]
    common_args = ["-x", "c++", "-std=c++20", "--target=x86_64-pc-windows-msvc",
                   "-fms-extensions", "-fms-compatibility", "-fms-compatibility-version=19.40",
                   "-D_ALLOW_COMPILER_AND_STL_VERSION_MISMATCH", "-DNOMINMAX", "-DWIN32", "-DWIN64",
                   "-I" + str(sdk / "include"), "-I" + str(vk / "include")]
    for path in system_includes:
        common_args.extend(["-isystem", path])

    lib = cx.conf.lib
    lib.clang_getFunctionTypeCallingConv.argtypes = [cx.Type]
    lib.clang_getFunctionTypeCallingConv.restype = ctypes.c_uint
    lib.clang_Cursor_Evaluate.argtypes = [cx.Cursor]
    lib.clang_Cursor_Evaluate.restype = ctypes.c_void_p
    lib.clang_EvalResult_getKind.argtypes = [ctypes.c_void_p]
    lib.clang_EvalResult_getKind.restype = ctypes.c_int
    lib.clang_EvalResult_getAsLongLong.argtypes = [ctypes.c_void_p]
    lib.clang_EvalResult_getAsLongLong.restype = ctypes.c_longlong
    lib.clang_EvalResult_getAsDouble.argtypes = [ctypes.c_void_p]
    lib.clang_EvalResult_getAsDouble.restype = ctypes.c_double
    lib.clang_EvalResult_dispose.argtypes = [ctypes.c_void_p]
    source_cache = {}

    def relative(path):
        if not path:
            return ""
        path = Path(path)
        for base, prefix in [(sdk, ""), (vk, "Vulkan-Headers/")]:
            try:
                return prefix + path.relative_to(base).as_posix()
            except ValueError:
                pass
        return "system/" + path.name

    def source(cursor):
        start, end = cursor.extent.start, cursor.extent.end
        if not start.file or not end.file or start.file.name != end.file.name:
            return " ".join(token.spelling for token in cursor.get_tokens())
        path = start.file.name
        if path not in source_cache:
            source_cache[path] = Path(path).read_bytes()
        return source_cache[path][start.offset:end.offset].decode("utf-8", errors="replace").replace("\r\n", "\n")

    def qualified(cursor):
        names = [cursor.spelling]
        parent = cursor.semantic_parent
        while parent and parent.kind != cx.CursorKind.TRANSLATION_UNIT:
            if parent.spelling:
                names.append(parent.spelling)
            parent = parent.semantic_parent
        return "::".join(reversed(names))

    dependencies = {}
    security_types = {}
    function_kinds = {cx.TypeKind.FUNCTIONPROTO, cx.TypeKind.FUNCTIONNOPROTO}

    def describe_type(t, depth=0):
        canonical = t.get_canonical()
        result = {"spelling": t.spelling, "canonical": canonical.spelling,
                  "kind": canonical.kind.name, "size": t.get_size(), "alignment": t.get_align(),
                  "const": t.is_const_qualified()}
        declaration = canonical.get_declaration()
        if declaration and declaration.spelling:
            result["declaration"] = qualified(declaration)
            file = relative(declaration.location.file.name if declaration.location.file else None)
            if file and not file.startswith("include/") and (declaration.spelling.startswith("Vk")
                    or canonical.kind == cx.TypeKind.ENUM or declaration.spelling in {"tagRECT", "_LUID"}):
                if canonical.kind in {cx.TypeKind.RECORD, cx.TypeKind.ENUM} and declaration.is_definition():
                    dependencies[declaration.get_usr()] = declaration
        if depth < 6:
            if canonical.kind in {cx.TypeKind.POINTER, cx.TypeKind.LVALUEREFERENCE, cx.TypeKind.RVALUEREFERENCE}:
                pointee = t.get_pointee()
                if pointee.kind == cx.TypeKind.INVALID:
                    pointee = canonical.get_pointee()
                result["element"] = describe_type(pointee, depth + 1)
            elif canonical.kind in {cx.TypeKind.CONSTANTARRAY, cx.TypeKind.INCOMPLETEARRAY}:
                result["element"] = describe_type(canonical.element_type, depth + 1)
                result["count"] = canonical.element_count
            elif canonical.kind in function_kinds:
                result["callingConvention"] = lib.clang_getFunctionTypeCallingConv(canonical)
                result["result"] = describe_type(canonical.get_result(), depth + 1)
                result["parameters"] = [describe_type(a, depth + 1) for a in canonical.argument_types()]
        return result

    def expression(cursor):
        result = {"kind": cursor.kind.name, "text": source(cursor), "type": cursor.type.spelling}
        evaluated = lib.clang_Cursor_Evaluate(cursor)
        if evaluated:
            kind = lib.clang_EvalResult_getKind(evaluated)
            if kind == 1:
                result["value"] = str(lib.clang_EvalResult_getAsLongLong(evaluated))
            elif kind == 2:
                result["value"] = repr(lib.clang_EvalResult_getAsDouble(evaluated))
            lib.clang_EvalResult_dispose(evaluated)
        result["children"] = [expression(child) for child in cursor.get_children()
                              if child.kind.is_expression()]
        return result

    declaration_kinds = {cx.CursorKind.STRUCT_DECL, cx.CursorKind.CLASS_DECL, cx.CursorKind.UNION_DECL,
                         cx.CursorKind.ENUM_DECL, cx.CursorKind.ENUM_CONSTANT_DECL, cx.CursorKind.FIELD_DECL,
                         cx.CursorKind.FUNCTION_DECL, cx.CursorKind.CXX_METHOD, cx.CursorKind.CONSTRUCTOR,
                         cx.CursorKind.DESTRUCTOR, cx.CursorKind.CONVERSION_FUNCTION,
                         cx.CursorKind.TYPEDEF_DECL, cx.CursorKind.TYPE_ALIAS_DECL, cx.CursorKind.VAR_DECL,
                         cx.CursorKind.CLASS_TEMPLATE, cx.CursorKind.FUNCTION_TEMPLATE, cx.CursorKind.PARM_DECL,
                         cx.CursorKind.CXX_BASE_SPECIFIER, cx.CursorKind.TEMPLATE_TYPE_PARAMETER}
    record_kinds = {cx.CursorKind.STRUCT_DECL, cx.CursorKind.CLASS_DECL, cx.CursorKind.UNION_DECL, cx.CursorKind.CLASS_TEMPLATE}
    callable_kinds = {cx.CursorKind.FUNCTION_DECL, cx.CursorKind.CXX_METHOD, cx.CursorKind.CONSTRUCTOR,
                      cx.CursorKind.DESTRUCTOR, cx.CursorKind.CONVERSION_FUNCTION, cx.CursorKind.FUNCTION_TEMPLATE}

    def serialize(cursor):
        file = relative(cursor.location.file.name if cursor.location.file else None)
        result = {"id": cursor.get_usr() or f"{file}:{cursor.location.line}:{cursor.location.column}:{cursor.kind.name}",
                  "kind": cursor.kind.name, "name": cursor.spelling, "qualifiedName": qualified(cursor),
                  "file": file, "line": cursor.location.line, "type": describe_type(cursor.type),
                  "access": cursor.access_specifier.name, "comment": cursor.raw_comment or "",
                  "definition": cursor.is_definition(),
                  "deprecated": cursor.availability == cx.AvailabilityKind.DEPRECATED}
        if cursor.kind in record_kinds:
            result["source"] = source(cursor)
        if cursor.kind in callable_kinds:
            result["source"] = source(cursor)
            result["resultType"] = describe_type(cursor.result_type)
            result["callingConvention"] = lib.clang_getFunctionTypeCallingConv(cursor.type)
            result["mangledName"] = cursor.mangled_name
            result["virtual"] = cursor.is_virtual_method()
            result["static"] = cursor.is_static_method()
            result["constMethod"] = cursor.is_const_method()
        if cursor.kind == cx.CursorKind.FIELD_DECL:
            result["offsetBits"] = cursor.get_field_offsetof()
            result["bitWidth"] = cursor.get_bitfield_width() if cursor.is_bitfield() else None
        if cursor.kind == cx.CursorKind.ENUM_DECL:
            result["underlyingType"] = describe_type(cursor.enum_type)
            result["flags"] = any(re.search(r"SL_ENUM_OPERATORS_(?:32|64)\s*\(\s*" + re.escape(cursor.spelling) + r"\s*\)",
                                              (sdk / path).read_text(encoding="utf-8-sig")) for path in public_headers)
        if cursor.kind == cx.CursorKind.ENUM_CONSTANT_DECL:
            result["value"] = str(cursor.enum_value)
        if cursor.kind in {cx.CursorKind.TYPEDEF_DECL, cx.CursorKind.TYPE_ALIAS_DECL}:
            result["underlyingType"] = describe_type(cursor.underlying_typedef_type)
        if cursor.kind in {cx.CursorKind.FIELD_DECL, cx.CursorKind.VAR_DECL, cx.CursorKind.PARM_DECL}:
            result["source"] = source(cursor)
            result["expressions"] = [expression(c) for c in cursor.get_children() if c.kind.is_expression()]
        result["children"] = [serialize(c) for c in cursor.get_children() if c.kind in declaration_kinds]
        if file == "include/sl_template.h":
            result["classification"] = "plugin-template"
            result["reason"] = "Example plugin declarations, not an SDK application feature."
        elif result["qualifiedName"].startswith("sl::test::"):
            result["classification"] = "test"
            result["reason"] = "Friend declaration for upstream ABI tests."
        else:
            result["classification"] = "application" if file.startswith("include/") else "dependency"
        if file == "include/sl_security.h" and cursor.kind in {cx.CursorKind.TYPEDEF_DECL, cx.CursorKind.VAR_DECL}:
            result["classification"] = "implementation"
            result["reason"] = "Private Windows function-loader implementation of the public signature helpers."
        return result

    declarations = {}
    configurations = []
    index = cx.Index.create()
    # Both Windows character configurations are legal application include contexts.
    for config_name, defines in [("windows-x64", []), ("windows-x64-unicode", ["-DUNICODE", "-D_UNICODE"])]:
        print("Parsing", config_name, flush=True)
        tu = index.parse(str(unit), args=common_args + defines,
                         options=cx.TranslationUnit.PARSE_DETAILED_PROCESSING_RECORD)
        print("Parsed translation unit", flush=True)
        diagnostics = [str(d) for d in tu.diagnostics]
        failures = [str(d) for d in tu.diagnostics if d.severity >= cx.Diagnostic.Error]
        if failures:
            raise RuntimeError("\n".join(failures))
        configurations.append({"name": config_name, "target": "x86_64-pc-windows-msvc", "language": "c++20",
                               "defines": ["WIN32", "WIN64", "NOMINMAX"] + [d[2:] for d in defines],
                               "diagnostics": diagnostics})

        def discover(cursor):
            for child in cursor.get_children():
                file = relative(child.location.file.name if child.location.file else None)
                if child.kind in {cx.CursorKind.NAMESPACE, cx.CursorKind.LINKAGE_SPEC}:
                    discover(child)
                elif file.startswith("include/") and child.kind in declaration_kinds:
                    print("Extracting", qualified(child), child.kind.name, flush=True)
                    item = serialize(child)
                    key = item["id"]
                    if key in declarations:
                        previous = declarations[key]
                        comparable = {k: v for k, v in previous.items() if k != "configurations"}
                        if comparable != item:
                            raise RuntimeError(f"Unmerged conditional declaration: {key} in {config_name}")
                        previous["configurations"].append(config_name)
                    else:
                        item["configurations"] = [config_name]
                        declarations[key] = item

        discover(tu.cursor)
        # Helper bodies name Vulkan records not present in function signatures.
        def helper_dependencies(cursor):
            for child in cursor.get_children():
                file = relative(child.location.file.name if child.location.file else None)
                if file.startswith("include/") or child.kind in {cx.CursorKind.NAMESPACE, cx.CursorKind.LINKAGE_SPEC}:
                    # Dependent C++ expressions do not have a concrete layout. Asking
                    # libclang for sizeof on them can crash; follow named Vulkan type
                    # references instead of measuring every expression in a template.
                    if child.kind == cx.CursorKind.TYPE_REF and child.referenced:
                        referenced = child.referenced
                        if referenced.spelling.startswith("Vk"):
                            dependency = referenced.type.get_canonical().get_declaration()
                            if dependency and dependency.is_definition():
                                dependencies[dependency.get_usr()] = dependency
                    if file == "include/sl_security.h" and child.kind in {cx.CursorKind.TYPE_REF, cx.CursorKind.MEMBER_REF_EXPR} and child.referenced:
                        referenced = child.referenced
                        native_type = referenced.type if child.kind == cx.CursorKind.TYPE_REF else referenced.semantic_parent.type
                        native_type = native_type.get_canonical()
                        while native_type.kind in {cx.TypeKind.POINTER, cx.TypeKind.LVALUEREFERENCE}:
                            native_type = native_type.get_pointee().get_canonical()
                        dependency = native_type.get_declaration()
                        if native_type.kind == cx.TypeKind.RECORD and dependency and dependency.is_definition():
                            security_types[dependency.get_usr()] = dependency
                    helper_dependencies(child)

        helper_dependencies(tu.cursor)
        while dependencies:
            _, dependency = dependencies.popitem()
            if dependency.get_usr() in declarations:
                continue
            item = serialize(dependency)
            item["configurations"] = [config_name]
            declarations[item["id"]] = item

    security_records = {}
    while security_types:
        identity, cursor = security_types.popitem()
        if identity in security_records:
            continue
        item = serialize(cursor)
        item["classification"] = "implementation"
        item["reason"] = "Private Windows data layout required by the signature helpers."
        layout_fields = []
        def collect_layout_fields(record):
            for child in record.get_children():
                if child.kind == cx.CursorKind.FIELD_DECL:
                    field = serialize(child)
                    field["offsetBits"] = cursor.type.get_offset(child.spelling)
                    layout_fields.append(field)
                elif child.kind == cx.CursorKind.UNION_DECL and child.is_anonymous():
                    collect_layout_fields(child)
        collect_layout_fields(cursor)
        item["layoutFields"] = layout_fields
        security_records[identity] = item
        for field in cursor.get_children():
            if field.kind == cx.CursorKind.FIELD_DECL:
                field_type = field.type.get_canonical()
                if field_type.kind == cx.TypeKind.RECORD:
                    nested = field_type.get_declaration()
                    if nested and nested.is_definition():
                        security_types[nested.get_usr()] = nested
    security_source = (sdk / "include/sl_security.h").read_text(encoding="utf-8-sig")
    required_macros = set(re.findall(r"\b(?:CERT_|CMSG_|WTD_|WSS_|CRYPT_|PKCS_|X509_|CNG_|szOID_|WINTRUST_ACTION_)\w+\b", security_source))
    macro_cursors = {c.spelling: c for c in tu.cursor.get_children() if c.kind == cx.CursorKind.MACRO_DEFINITION}
    security_macros = {}
    def include_macro(name):
        if name in security_macros or name not in macro_cursors:
            return
        cursor = macro_cursors[name]
        tokens = [token.spelling for token in cursor.get_tokens()][1:]
        security_macros[name] = " ".join(tokens)
        for token in tokens:
            if token in macro_cursors:
                include_macro(token)
    for name in sorted(required_macros):
        include_macro(name)

    macros = []
    for path in public_headers:
        text = (sdk / path).read_text(encoding="utf-8-sig")
        logical_text = text.replace("\\\n", " ")
        for match in re.finditer(r"^\s*#\s*define\s+(\w+)([^\n]*)", logical_text, re.MULTILINE):
            name, body = match[1], match[2]
            macros.append({"id": f"macro:{path}:{name}", "name": name, "file": path,
                           "body": body.strip(),
                           "classification": "unclassified"})

    # Keep only the Vulkan constants actually used by the application helper bodies.
    helper_text = "\n".join((sdk / path).read_text(encoding="utf-8-sig") for path in public_headers)
    vk_constants = set(re.findall(r"\bVK_STRUCTURE_TYPE_[A-Z0-9_]+\b", helper_text))
    for item in declarations.values():
        if item["qualifiedName"] in {"VkStructureType", "VkResult"}:
            item["mappedAs"] = "int"
            item["children"] = [child for child in item["children"] if child["name"] in vk_constants]

    compiler = shutil.which("clang++")
    if not compiler:
        raise RuntimeError("clang++ is required to record virtual dispatch and callback ABI.")
    probe = root / "abi.cpp"
    probe.write_text("\n".join(includes) + '''
struct FrameProbe final : sl::FrameToken { operator uint32_t() const override { return 0; } };
struct AllocatorProbe final : sl::IAllocator {
    void* allocate(uint32_t) override { return nullptr; }
    void free(void*) override { }
};
FrameProbe frameProbe;
AllocatorProbe allocatorProbe;
using StreamlineArrayLayout = sl::Array<uint8_t>;
static_assert(sizeof(StreamlineArrayLayout) > 0);
extern "C" __declspec(dllexport) sl::Resource invokeAllocate(sl::PFun_ResourceAllocateCallback* callback,
    const sl::ResourceAllocationDesc* desc, void* device) { return callback(desc, device); }
extern "C" __declspec(dllexport) void destroyAllocator(sl::IAllocator* allocator) { allocator->~IAllocator(); }
''', encoding="utf-8")
    result = subprocess.run([compiler, *common_args, "-Xclang", "-fdump-vtable-layouts", "-S", "-emit-llvm",
                             str(probe), "-o", str(root / "abi.ll")], text=True, capture_output=True, check=True)
    virtual_tables = result.stdout
    print(virtual_tables, flush=True)
    ir = (root / "abi.ll").read_text(encoding="utf-8")
    callback_ir = re.search(r"^define[^\n]*@invokeAllocate\b[\s\S]*?^}", ir, re.MULTILINE)
    if not callback_ir:
        raise RuntimeError("Callback return ABI was not emitted.")
    destructor_ir = re.search(r"^define[^\n]*@destroyAllocator\b[\s\S]*?^}", ir, re.MULTILINE)
    if not destructor_ir:
        raise RuntimeError("Allocator destructor ABI was not emitted.")
    slots = {}
    for probe_name, prefix in [("FrameProbe", "frameToken"), ("AllocatorProbe", "allocator")]:
        table = re.search(r"VFTable indices for '" + probe_name + r"'[^\n]*\n([\s\S]*?)(?:\n\s*\n|$)", virtual_tables)
        if not table:
            raise RuntimeError("Missing virtual dispatch layout for " + probe_name)
        for line in table[1].splitlines():
            match = re.match(r"\s*(\d+)\s*\|\s*(.*)", line)
            if match:
                slots[prefix + ":" + match[2]] = int(match[1])
    layout_tu = index.parse(str(probe), args=common_args)
    array_layout = next(c for c in layout_tu.cursor.get_children() if c.spelling == "StreamlineArrayLayout")
    array_type = array_layout.underlying_typedef_type.get_canonical()
    array_definition = array_type.get_declaration()
    array_data = {"size": array_type.get_size(), "alignment": array_type.get_align(),
                  "fields": [{"name": c.spelling, "offsetBits": c.get_field_offsetof(), "type": describe_type(c.type)}
                             for c in array_type.get_fields()]}
    if array_data["size"] <= 0 or len(array_data["fields"]) != 3:
        raise RuntimeError("Array specialization layout was not extracted.")
    snapshot = {"schemaVersion": 1,
                "source": {"repository": "NVIDIA-RTX/Streamline", "release": release["tag_name"],
                           "commit": commit, "releaseUrl": release["html_url"]},
                "dependencies": [{"repository": "KhronosGroup/Vulkan-Headers", "tag": vk_tag,
                                  "commit": vk_commit, "requestedBy": "project.xml"}],
                "toolchain": {"libclang": "18.1.1", "windowsSdk": os.environ.get("WindowsSDKVersion", "").strip("\\"),
                              "msvc": os.environ.get("VCToolsVersion", ""),
                              "clang": subprocess.check_output([compiler, "--version"], text=True).splitlines()[0]},
                "abi": {"virtualSlots": slots, "resourceAllocateCallback": callback_ir[0],
                        "allocatorDestructor": destructor_ir[0], "arrayLayout": array_data},
                "security": {"types": sorted(security_records.values(), key=lambda item: item["qualifiedName"]),
                             "constants": dict(sorted(security_macros.items()))},
                "configurations": configurations, "inputs": sorted(inputs, key=lambda i: (i["path"], i["classification"])),
                "declarations": sorted(declarations.values(), key=lambda i: (i["file"], i["line"], i["id"])),
                "macros": macros,
                "exports": re.findall(r"^\s*(sl\w+)\s*$", (sdk / "source/core/sl.interposer/exports.def").read_text(encoding="utf-8-sig"), re.MULTILINE)}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(snapshot, indent=2, ensure_ascii=False, allow_nan=False)
    for path, replacement in [(sdk, ""), (vk, "Vulkan-Headers")]:
        text = text.replace(json.dumps(str(path) + os.sep)[1:-1], replacement + ("/" if replacement else ""))
    args.output.write_text(text + "\n", encoding="utf-8")
    print(f"Wrote {len(declarations)} declarations, {len(macros)} macros from {len(public_headers)} public inputs.")


if __name__ == "__main__":
    main()
