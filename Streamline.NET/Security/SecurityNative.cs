using System.Runtime.InteropServices;

namespace Streamline.NET;

internal static unsafe partial class SecurityNative
{
    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CryptQueryObject(uint objectType, void* value, uint contentFlags, uint formatFlags, uint flags,
        uint* encoding, uint* contentType, uint* formatType, nint* store, nint* message, nint* context);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CryptMsgGetParam(nint message, uint parameter, uint index, void* value, uint* size);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial nint CryptMsgOpenToDecode(uint encoding, uint flags, uint messageType, nint provider, void* recipient, void* stream);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CryptMsgUpdate(nint message, byte* data, uint size, int final);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CryptMsgClose(nint message);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial nint CertOpenStore(sbyte* provider, uint encoding, nint cryptProvider, uint flags, void* parameter);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CertCloseStore(nint store, uint flags);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial CertContext* CertFindCertificateInStore(nint store, uint encoding, uint flags, uint findType, void* parameter, CertContext* previous);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CertFreeCertificateContext(CertContext* certificate);

    [LibraryImport("crypt32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CryptDecodeObjectEx(uint encoding, sbyte* structureType, byte* encoded, uint size, uint flags, void* parameters, void* decoded, uint* decodedSize);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial void* LocalAlloc(uint flags, nuint bytes);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial nint LocalFree(void* memory);

    [LibraryImport("wintrust.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int WinVerifyTrust(nint window, System.Guid* action, WintrustData* data);

    internal static CmsgSignerInfo* ReadSigner(nint message)
    {
        uint size = 0;
        if (CryptMsgGetParam(message, CmsgSignerInfoParam, 0, null, &size) == 0 || size == 0)
        {
            return null;
        }
        CmsgSignerInfo* signer = (CmsgSignerInfo*)LocalAlloc(Lptr, size);
        if (signer != null && CryptMsgGetParam(message, CmsgSignerInfoParam, 0, signer, &size) == 0)
        {
            LocalFree(signer);
            return null;
        }
        return signer;
    }
}
