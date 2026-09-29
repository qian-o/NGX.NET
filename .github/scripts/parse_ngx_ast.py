"""Extract application declarations and ABI layouts on the target Actions runner."""

from __future__ import annotations

import ctypes
import json
import os
from pathlib import Path
import re


# These headers implement the optional header-only CUDA loader, not additional
# application APIs. Its Core signatures must never enter the application ABI.
IMPLEMENTATION_HEADERS = {
    "nvsdk_ngx_loader.h", "nvsdk_ngx_standalone_common.h", "nvsdk_ngx_standalone_cuda.h"
}


def extract(sdk: Path, scratch: Path, rid: str):
    from clang import cindex as cx

    windows = rid.startswith("win-")
    if windows:
        # Use the runner's matching LLVM library and builtin headers with its
        # current MSVC headers (Clang 18 predates the ARM64 intrinsics).
        import subprocess
        llvm = Path(os.environ["ProgramFiles"]) / "LLVM"
        cx.Config.set_compatibility_check(False)
        cx.Config.set_library_file(str(llvm / "bin/libclang.dll"))
        resource = subprocess.check_output([str(llvm / "bin/clang.exe"), "-print-resource-dir"], text=True).strip()

    target = ("aarch64" if rid.endswith("arm64") else "x86_64") + ("-pc-windows-msvc" if windows else "-linux-gnu")
    flags = ["-x", "c++", "-std=c++17", "--target=" + target,
             "-I" + str(sdk / "include"), "-I" + str(sdk / "vulkan" / "include"),
             "-DNGX_ENABLE_DEPRECATED_SHUTDOWN", "-DNGX_ENABLE_DEPRECATED_GET_PARAMETERS"]
    if windows:
        flags += ["-resource-dir=" + resource, "-fms-extensions", "-fms-compatibility", "-fms-compatibility-version=19.40",
                  "-D_ALLOW_COMPILER_AND_STL_VERSION_MISMATCH", "-DNOMINMAX"]
        for path in os.environ["INCLUDE"].split(";"):
            flags += ["-isystem", path]
    else:
        import subprocess
        search = subprocess.run(["g++", "-E", "-x", "c++", "-", "-v"], input="", text=True, capture_output=True, check=True).stderr
        for line in search.split("#include <...> search starts here:")[1].split("End of search list.")[0].splitlines():
            flags += ["-isystem", line.strip()]

    cx.conf.lib.clang_getFunctionTypeCallingConv.argtypes = [cx.Type]
    cx.conf.lib.clang_getFunctionTypeCallingConv.restype = ctypes.c_uint
    records, enums, functions, aliases, macros, inventory = {}, {}, {}, {}, {}, {}
    units = {}

    def source(cursor):
        if not cursor.location.file:
            return None
        p = Path(cursor.location.file.name)
        return p.name if p.parent == sdk / "include" else None

    def type_info(t):
        canonical = t.get_canonical()
        kind = canonical.kind.name
        info = {"cpp": t.spelling, "kind": kind, "size": canonical.get_size(), "align": canonical.get_align()}
        if kind in ("POINTER", "LVALUEREFERENCE", "RVALUEREFERENCE"):
            info["element"] = type_info(canonical.get_pointee())
        elif kind == "CONSTANTARRAY":
            info.update(element=type_info(canonical.element_type), count=canonical.element_count)
        elif kind in ("FUNCTIONPROTO", "FUNCTIONNOPROTO"):
            info.update(result=type_info(canonical.get_result()), arguments=[type_info(a) for a in canonical.argument_types()],
                        callingConvention=cx.conf.lib.clang_getFunctionTypeCallingConv(canonical))
        elif kind in ("RECORD", "ENUM"):
            decl = canonical.get_declaration()
            info["name"] = decl.spelling
            if decl.is_anonymous():
                info["name"] = "Anonymous_" + str(decl.location.line) + "_" + str(decl.location.column)
            collect(decl, info["name"])
        return info

    def collect(c, forced_name=None):
        name = forced_name or c.spelling
        kind = c.kind.name
        header = source(c)
        if kind in ("STRUCT_DECL", "UNION_DECL", "CLASS_DECL"):
            if name in records and (records[name].get("fields") or not c.is_definition()):
                return
            record = {"name": name, "header": header, "kind": kind, "size": c.type.get_size(),
                      "align": c.type.get_align(), "opaque": not c.is_definition() or name in ("NVSDK_NGX_Handle", "NVSDK_NGX_Parameter"), "fields": []}
            records[name] = record
            if name == "NVSDK_NGX_Parameter" and c.is_definition():
                record["members"] = []
                suffixes = {"unsigned long long": "ULL", "float": "F", "double": "D", "unsigned int": "UI", "int": "I", "ID3D11Resource *": "D3d11Resource", "ID3D12Resource *": "D3d12Resource", "void *": "VoidPointer"}
                for method in c.get_children():
                    if method.kind != cx.CursorKind.CXX_METHOD:
                        continue
                    parameters = list(method.get_arguments())
                    if method.spelling == "Reset":
                        binding = "NGX_Bridge_Parameter_Reset"
                    elif method.spelling in ("Set", "Get") and len(parameters) == 2:
                        value = parameters[1].type
                        if method.spelling == "Get":
                            value = value.get_pointee()
                        binding = "NVSDK_NGX_Parameter_" + method.spelling + suffixes[value.get_canonical().spelling]
                    else:
                        raise RuntimeError("Unclassified Parameter member: " + method.displayname)
                    record["members"].append({"name": method.displayname, "binding": binding})
                    inventory[method.get_usr()] = {"header": header, "line": method.location.line, "kind": "CXX_METHOD", "name": method.displayname, "classification": "C-ABI-adapter", "binding": binding}
            if not record["opaque"]:
                for field in c.get_children():
                    if field.kind == cx.CursorKind.FIELD_DECL:
                        record["fields"].append({"name": field.spelling, "offset": field.get_field_offsetof(), "type": type_info(field.type), "declaration": " ".join(t.spelling for t in field.get_tokens())})
        elif kind == "ENUM_DECL" and name not in enums:
            enums[name] = {"name": name, "header": header, "underlying": c.enum_type.spelling,
                           "values": [{"name": e.spelling, "value": e.enum_value} for e in c.get_children() if e.kind == cx.CursorKind.ENUM_CONSTANT_DECL]}

    def walk(c):
        header = source(c)
        if header:
            key = c.get_usr() or f"{header}:{c.location.line}:{c.kind.name}:{c.spelling}"
            inventory[key] = {"header": header, "line": c.location.line, "kind": c.kind.name, "name": c.spelling}
            if c.kind in (cx.CursorKind.STRUCT_DECL, cx.CursorKind.UNION_DECL, cx.CursorKind.ENUM_DECL, cx.CursorKind.CLASS_DECL):
                collect(c)
            elif c.kind == cx.CursorKind.TYPEDEF_DECL:
                aliases[c.spelling] = type_info(c.underlying_typedef_type)
            elif c.kind == cx.CursorKind.FUNCTION_DECL:
                name = c.spelling
                if not windows and "D3D" in name:
                    inventory[key]["classification"] = "windows-only"
                else:
                    f = {"name": name, "header": header, "line": c.location.line,
                         "result": type_info(c.result_type),
                         "parameters": [{"name": p.spelling or f"arg{i}", "type": type_info(p.type)} for i, p in enumerate(c.get_arguments())],
                         "callingConvention": cx.conf.lib.clang_getFunctionTypeCallingConv(c.type),
                         "inline": c.is_definition(), "comment": c.raw_comment,
                         "body": " ".join(t.spelling for t in c.get_tokens()) if c.is_definition() else None}
                    if name not in functions:
                        functions[name] = f
                    elif functions[name]["parameters"] != f["parameters"]:
                        raise RuntimeError("Conflicting function signatures: " + name)
            elif c.kind == cx.CursorKind.MACRO_DEFINITION:
                tokens = list(c.get_tokens())
                macros[c.spelling] = {"name": c.spelling, "header": header, "tokens": [t.spelling for t in tokens[1:]],
                                     "functionLike": len(tokens) > 1 and tokens[1].spelling == "(" and tokens[0].extent.end.offset == tokens[1].extent.start.offset}
        if c.kind in (cx.CursorKind.TRANSLATION_UNIT, cx.CursorKind.LINKAGE_SPEC, cx.CursorKind.UNEXPOSED_DECL, cx.CursorKind.NAMESPACE):
            for child in c.get_children():
                walk(child)

    # Parse helpers individually: upstream repeats some shared inline helpers in
    # the D3D/CUDA/Vulkan headers. Compiling all headers together is not supported.
    headers = sorted(p.name for p in (sdk / "include").glob("*.h") if p.name not in IMPLEMENTATION_HEADERS)
    for header in headers:
        if not windows and ("_d3d" in header or header in ("nvsdk_ngx_helpers_dlssd.h", "nvsdk_ngx_helpers_dlssg.h")):
            continue
        unit = scratch / (header + ".cpp")
        prologue = "#include <stdint.h>\n#include <stddef.h>\n#include <wchar.h>\n#include <string.h>\n#include <vulkan/vulkan.h>\n#include \"nvsdk_ngx.h\"\n"
        if "dlssd_vk" in header:
            prologue += '#include "nvsdk_ngx_helpers_vk.h"\n'
        if "dlssd_d3d" in header or header == "nvsdk_ngx_helpers_dlssd.h":
            prologue += '#include "nvsdk_ngx_helpers_d3d.h"\n'
        if "dlssd_cuda" in header:
            prologue += '#include "nvsdk_ngx_helpers_cuda.h"\n'
        unit.write_text(prologue + '#include "' + header + '"\n')
        tu = cx.Index.create().parse(str(unit), args=flags, options=cx.TranslationUnit.PARSE_DETAILED_PROCESSING_RECORD)
        errors = [str(d) for d in tu.diagnostics if d.severity >= cx.Diagnostic.Error]
        if errors:
            raise RuntimeError(header + "\n" + "\n".join(errors))
        walk(tu.cursor)
        units[header] = str(unit)
    return {"rid": rid, "target": target, "wcharSize": 2 if windows else 4,
            "functions": sorted(functions.values(), key=lambda f: f["name"]),
            "records": sorted(records.values(), key=lambda r: r["name"]),
            "enums": sorted(enums.values(), key=lambda e: e["name"]),
            "aliases": aliases, "macros": sorted(macros.values(), key=lambda m: m["name"]),
            "declarations": sorted(inventory.values(), key=lambda d: (d["header"], d["line"], d["kind"], d["name"]))}, units
