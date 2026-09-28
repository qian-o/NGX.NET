namespace Streamline.NET;

public static unsafe partial class SL
{
    /// <summary>
    /// Checks the first nested signature against the exact NVIDIA public-key identity in the bound SDK.
    /// </summary>
    /// <remarks>
    /// This is an identity check, not an operating-system trust-chain check. It does not load Streamline.
    /// Returns false if the required Windows functions are unavailable.
    /// </remarks>
    public static bool IsSignedByNVIDIA(char* pathToFile)
    {
        try
        {
            return CheckNvidiaIdentity(pathToFile);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Fixes a UTF-16 file path for the duration of the signature-identity check.
    /// </summary>
    public static bool IsSignedByNVIDIA(string pathToFile)
    {
        fixed (char* path = pathToFile)
        {
            return IsSignedByNVIDIA(path);
        }
    }

    /// <summary>
    /// Applies the native OS trust policy, requires exactly one secondary signature, and checks its NVIDIA identity.
    /// </summary>
    /// <remarks>
    /// Preserves the upstream no-revocation policy and diagnostic output. Does not load Streamline or initialize the SDK.
    /// </remarks>
    public static bool VerifyEmbeddedSignature(char* pathToFile)
    {
        try
        {
            return CheckEmbeddedSignature(pathToFile);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Fixes a UTF-16 file path for the duration of the native trust and identity checks.
    /// </summary>
    public static bool VerifyEmbeddedSignature(string pathToFile)
    {
        fixed (char* path = pathToFile)
        {
            return VerifyEmbeddedSignature(path);
        }
    }

    private static bool CheckNvidiaIdentity(char* path)
    {
        nint store = 0;
        nint message = 0;
        SecurityNative.CmsgSignerInfo* signer = null;
        uint encoding = 0;
        uint content = 0;
        uint format = 0;

        try
        {
            if (SecurityNative.CryptQueryObject(SecurityNative.CertQueryObjectFile,
                                                path,
                                                SecurityNative.CertQueryContentFlagPkcs7SignedEmbed,
                                                SecurityNative.CertQueryFormatFlagBinary,
                                                0,
                                                &encoding,
                                                &content,
                                                &format,
                                                &store,
                                                &message,
                                                null) == 0)
            {
                return false;
            }

            signer = SecurityNative.ReadSigner(message);

            if (signer == null)
            {
                return false;
            }

            for (uint index = 0; index < signer->UnauthAttrs.CAttr; index++)
            {
                SecurityNative.CryptAttribute* attribute = &signer->UnauthAttrs.RgAttr[index];

                if (NativeStrings.Equals(attribute->PszObjId, "1.3.6.1.4.1.311.2.4.1"u8))
                {
                    return attribute->CValue != 0 && CheckNestedSignature(attribute->RgValue);
                }
            }

            return false;
        }
        finally
        {
            if (signer != null)
            {
                SecurityNative.LocalFree(signer);
            }

            if (message != 0)
            {
                SecurityNative.CryptMsgClose(message);
            }

            if (store != 0)
            {
                SecurityNative.CertCloseStore(store, SecurityNative.CertCloseStoreForceFlag);
            }
        }
    }

    private static bool CheckNestedSignature(SecurityNative.CryptoapiBlob* blob)
    {
        uint encoding = SecurityNative.X509AsnEncoding | SecurityNative.Pkcs7AsnEncoding;
        nint message = SecurityNative.CryptMsgOpenToDecode(encoding, 0, 0, 0, null, null);

        if (message == 0)
        {
            return false;
        }

        nint store = 0;
        SecurityNative.CmsgSignerInfo* signer = null;
        SecurityNative.CertContext* certificate = null;
        void* decoded = null;

        try
        {
            if (SecurityNative.CryptMsgUpdate(message, blob->PbData, blob->CbData, 1) == 0)
            {
                return false;
            }

            signer = SecurityNative.ReadSigner(message);

            if (signer == null)
            {
                return false;
            }

            store = SecurityNative.CertOpenStore((sbyte*)SecurityNative.CertStoreProvPkcs7, encoding, 0, 0, blob);

            if (store == 0)
            {
                return false;
            }

            SecurityNative.CertInfo info = new()
            {
                Issuer = signer->Issuer,
                SerialNumber = signer->SerialNumber
            };
            certificate = SecurityNative.CertFindCertificateInStore(store,
                                                                    encoding,
                                                                    0,
                                                                    SecurityNative.CertFindSubjectCert,
                                                                    &info,
                                                                    null);

            if (certificate == null)
            {
                return false;
            }

            SecurityNative.CryptBitBlob key = certificate->PCertInfo->SubjectPublicKeyInfo.PublicKey;
            uint decodedLength = 0;

            if (SecurityNative.CryptDecodeObjectEx(encoding,
                                                   (sbyte*)SecurityNative.CngRsaPublicKeyBlob,
                                                   key.PbData,
                                                   key.CbData,
                                                   SecurityNative.CryptEncodeAllocFlag,
                                                   null,
                                                   &decoded,
                                                   &decodedLength) == 0)
            {
                return false;
            }

            ReadOnlySpan<byte> expected = SecurityNative.NvidiaPublicKey;

            return decodedLength == expected.Length && new ReadOnlySpan<byte>(decoded, expected.Length).SequenceEqual(expected);
        }
        finally
        {
            if (decoded != null)
            {
                SecurityNative.LocalFree(decoded);
            }

            if (certificate != null)
            {
                SecurityNative.CertFreeCertificateContext(certificate);
            }

            if (store != 0)
            {
                SecurityNative.CertCloseStore(store, SecurityNative.CertCloseStoreForceFlag);
            }

            if (signer != null)
            {
                SecurityNative.LocalFree(signer);
            }

            SecurityNative.CryptMsgClose(message);
        }
    }

    private static bool CheckEmbeddedSignature(char* path)
    {
        SecurityNative.WintrustFileInfo file = new()
        {
            CbStruct = (uint)sizeof(SecurityNative.WintrustFileInfo),
            PcwszFilePath = path
        };
        SecurityNative.CertStrongSignPara policy = new()
        {
            CbSize = (uint)sizeof(SecurityNative.CertStrongSignPara),
            DwInfoChoice = SecurityNative.CertStrongSignOidInfoChoice
        };
        SecurityNative.WintrustSignatureSettings signatures = new()
        {
            CbStruct = (uint)sizeof(SecurityNative.WintrustSignatureSettings),
            DwFlags = SecurityNative.WssGetSecondarySigCount | SecurityNative.WssVerifySpecific,
            PCryptoPolicy = &policy
        };
        SecurityNative.WintrustData data = new()
        {
            CbStruct = (uint)sizeof(SecurityNative.WintrustData),
            DwUIChoice = SecurityNative.WtdUiNone,
            FdwRevocationChecks = SecurityNative.WtdRevokeNone,
            DwUnionChoice = SecurityNative.WtdChoiceFile,
            DwStateAction = SecurityNative.WtdStateactionVerify,
            PFile = &file,
            PSignatureSettings = &signatures
        };
        Guid action = SecurityNative.WintrustActionGenericVerifyV2;

        fixed (byte* oid = SecurityNative.SzoidCertStrongSignOsCurrent)
        {
            policy.PszOID = (sbyte*)oid;

            try
            {
                if (SecurityNative.WinVerifyTrust(0, &action, &data) != SecurityNative.ErrorSuccess)
                {
                    Console.WriteLine($"File '{new string(path)}' is NOT correctly signed - Streamline will not load unsecured modules");

                    return false;
                }

                if (signatures.CSecondarySigs != 1)
                {
                    Console.WriteLine($"File '{new string(path)}' does not have the secondary NVIDIA signature - Streamline will not load unsecured modules");

                    return false;
                }

                bool valid = IsSignedByNVIDIA(path);

                Console.WriteLine(valid
                    ? $"File '{new string(path)}' is signed by NVIDIA and the signature was verified."
                    : $"File '{new string(path)}' is NOT correctly signed - Streamline will not load unsecured modules");

                return valid;
            }
            finally
            {
                data.DwStateAction = SecurityNative.WtdStateactionClose;
                SecurityNative.WinVerifyTrust(0, &action, &data);
            }
        }
    }
}
