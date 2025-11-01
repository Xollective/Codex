using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Codex.Utilities
{
    public static class Paths
    {
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();
        public static readonly string RelativeParentDir = ".." + Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// Returns a path to <paramref name="filePath"/> if you start in a folder where the file
        /// <paramref name="relativeToPath"/> is located.
        /// </summary>
        /// <param name="filePath">C:\A\B\1.txt</param>
        /// <param name="relativeToPath">C:\C\D\2.txt</param>
        /// <returns>..\..\A\B\1.txt</returns>
        public static string MakeRelativeToFile(string filePath, string relativeToPath)
        {
            relativeToPath = Path.GetDirectoryName(relativeToPath);
            string result = MakeRelativeToFolder(filePath, relativeToPath);
            return result;
        }

        /// <summary>
        /// Returns a path to <paramref name="filePath"/> if you start in folder <paramref name="relativeToPath"/>.
        /// </summary>
        /// <param name="filePath">C:\A\B\1.txt</param>
        /// <param name="relativeToPath">C:\C\D</param>
        /// <returns>..\..\A\B\1.txt</returns>
        public static string MakeRelativeToFolder(string filePath, string relativeToPath)
        {
            if (relativeToPath.EndsWith(DirectorySeparator))
            {
                relativeToPath = relativeToPath.TrimEnd(DirectorySeparatorChar);
            }

            StringBuilder result = new StringBuilder();
            while (!EnsureTrailingSlash(filePath).StartsWith(EnsureTrailingSlash(relativeToPath), StringComparison.OrdinalIgnoreCase))
            {
                result.Append(RelativeParentDir);
                relativeToPath = Path.GetDirectoryName(relativeToPath);
            }

            if (filePath.Length > relativeToPath.Length)
            {
                filePath = filePath.Substring(relativeToPath.Length);
                if (filePath.StartsWith(DirectorySeparator))
                {
                    filePath = filePath.Substring(1);
                }

                result.Append(filePath);
            }

            return result.ToString();
        }

        private static char[] invalidFileChars = Path.GetInvalidFileNameChars();
        private static char[] invalidPathChars = Path.GetInvalidPathChars();

        public static string SanitizeFileName(string fileName)
        {
            return ReplaceInvalidChars(fileName, invalidFileChars);
        }

        public static string QuoteIfNeeded(this string path)
        {
            if (path != null && path.Contains(" ") && !(path.StartsWith("\"") && path.EndsWith("\"")))
            {
                path = "\"" + path + "\"";
            }

            return path;
        }

        private static string ReplaceInvalidChars(string fileName, char[] invalidChars)
        {
            var sb = new StringBuilder(fileName.Length);
            for (int i = 0; i < fileName.Length; i++)
            {
                if (invalidChars.Contains(fileName[i]))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(fileName[i]);
                }
            }

            return sb.ToString();
        }

        public static string SanitizeFolder(string folderName)
        {
            string result = folderName;

            if (folderName == ".")
            {
                result = "current";
            }
            else if (folderName == "..")
            {
                result = "parent";
            }
            else if (folderName.EndsWith(":"))
            {
                result = folderName.TrimEnd(':');
            }
            else
            {
                result = folderName;
            }

            result = ReplaceInvalidChars(result, invalidPathChars);
            return result;
        }

        private static bool IsValidFolder(string folderName)
        {
            return !string.IsNullOrEmpty(folderName) &&
                folderName != "." &&
                folderName != ".." &&
                !folderName.EndsWith(":");
        }

        public static string NormalizeSlashes(this string path, char? slash = null)
        {
            slash ??= DirectorySeparatorChar;
            if (slash == '/')
            {
                return path?.Replace('\\', '/');
            }
            else
            {
                return path?.Replace('/', '\\');
            }
        }

        public static string EnsureTrailingSlash(this string path, char? slash = null, bool normalize = false)
        {
            slash ??= DirectorySeparatorChar;
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (normalize)
            {
                path = PathUtilities.NormalizePath(path, directorySeparator: slash);
            }

            if (!path.EndsWith(slash.Value))
            {
                path += slash;
            }

            return path;
        }

        public static string GetCssPathFromFile(string solutionDestinationPath, string fileName)
        {
            string result = MakeRelativeToFile(solutionDestinationPath, fileName);
            result = Path.Combine(result, "styles.css");
            result = result.Replace('\\', '/');
            return result;
        }

        public static string StripExtension(string fileName)
        {
            return Path.ChangeExtension(fileName, null);
        }

        public static string CalculateRelativePathToRoot(string filePath, string rootFolder)
        {
            var relativePath = filePath.Substring(rootFolder.Length + 1);
            var depth = relativePath.Count(c => c == '\\') + relativePath.Count(c => c == '/');
            var sb = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                sb.Append(RelativeParentDir);
            }

            return sb.ToString();
        }

        public static bool IsSelfOrParentOf(this string folder, string filePath)
        {
            var folderSpan = folder.TrimEnd(Path.DirectorySeparatorChar);
            if (filePath.Length < folderSpan.Length) return false;

            if (!filePath.AsSpan().StartsWith(folderSpan, StringComparison.OrdinalIgnoreCase)) return false;

            if (filePath.Length == folderSpan.Length) return true;

            if (filePath[folderSpan.Length] == Path.DirectorySeparatorChar) return true;

            return false;
        }
    }
}
