using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace Codex.Utilities;

public static class MiscUtilities
{
    public static void UpdateEnvironmentVariable(string name, Func<string, string> update, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var value = Environment.GetEnvironmentVariable(name, target);
        var newValue = update(value);
        Environment.SetEnvironmentVariable(name, newValue, target);
    }

    public static string ExpandVariableTokens(string text, Func<string, string> getEnvironmentVariable)
    {
        var result = new ValueStringBuilder(stackalloc char[128]);

        int lastPos = 0, pos;
        while (lastPos < text.Length && (pos = text.IndexOf('%', lastPos + 1)) >= 0)
        {
            if (text[lastPos] == '%')
            {
                string key = text.Substring(lastPos + 1, pos - lastPos - 1);
                string? value = getEnvironmentVariable(key);
                if (value != null)
                {
                    result.Append(value);
                    lastPos = pos + 1;
                    continue;
                }
            }
            result.Append(text.AsSpan(lastPos, pos - lastPos));
            lastPos = pos;
        }
        result.Append(text.AsSpan(lastPos));

        return result.ToString();
    }

    public static bool TryGetEnvironmentVariable(string name, out string value)
    {
        if (string.IsNullOrEmpty(name))
        {
            value = null;
            return false;
        }

        value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrEmpty(value);
    }

    public static string GetEnvironmentVariableOrDefault(string name)
    {
        return TryGetEnvironmentVariable(name, out var value) ? value : null;
    }

    public static void CreateZipFromDirectory(string sourceDirectory, string zipFileName, string password = null, string publicKey = null, Out<string> generatedPassword = default, Out<string> encryptedPassword = default)
    {
        generatedPassword.Ensure();
        encryptedPassword.Ensure();

        if (publicKey != null)
        {
            generatedPassword.Set(password = EncryptionUtilities.GetGeneratedPassword());
        }

        var fastZip = new FastZip()
        {
            EntryEncryptionMethod = ZipEncryptionMethod.AES256,
            Password = password,
        };

        fastZip.CreateZip(zipFileName: zipFileName, sourceDirectory: sourceDirectory, recurse: true, fileFilter: null);

        if (publicKey != null)
        {
            encryptedPassword.Value = EncryptionUtilities.EncryptWithPublicKey(password, publicKeyBase64: publicKey);
            using var zip = new ZipFile(zipFileName);
            zip.BeginUpdate();
            zip.SetComment(CodexConstants.ZipEncryptedPasswordCommentPrefix + encryptedPassword.Value);
            zip.CommitUpdate();
        }
    }

    public static string TryGetZipPassword(ZipFile zipFile, string privateKey)
    {
        if (!privateKey.IsNonEmpty()) return null;

        var encryptedPassword = zipFile.ZipFileComment ?? string.Empty;
        if (!encryptedPassword.StartsWith(CodexConstants.ZipEncryptedPasswordCommentPrefix)) return null;

        encryptedPassword = encryptedPassword.Substring(CodexConstants.ZipEncryptedPasswordCommentPrefix.Length);
        var generatedPassword = EncryptionUtilities.DecryptWithPrivateKey(encryptedPassword, privateKeyBase64: privateKey);

        SdkFeatures.TestLogger.Value?.LogDiagnostic("Retrieved zip password");
        SdkFeatures.TestLogger.Value?.LogDiagnostic(JsonSerializationUtilities.SerializeEntity(new
        {
            encryptedPassword,
            generatedPassword
        }, flags: JsonFlags.Indented));

        return generatedPassword;
    }

    private class StringDataSource(string text) : IStaticDataSource
    {
        public Stream GetSource()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }
}