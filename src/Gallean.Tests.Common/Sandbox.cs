using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Gallean.Tests.Common;

public sealed class Sandbox : IDisposable
{
    private bool disposed;

    private Sandbox(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;

            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    public static Sandbox Create()
    {
        string testIdentity = GetTestIdentity();
        string directoryName = GetDirectoryName(testIdentity);
        string rootPath = Path.Combine(Path.GetTempPath(), "Gallean.Tests", directoryName);

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }

        Directory.CreateDirectory(rootPath);

        return new Sandbox(rootPath);
    }

    public string GetPath(string fileName)
    {
        return Path.Combine(RootPath, fileName);
    }

    public string CreateDirectory(string directoryName)
    {
        string path = GetPath(directoryName);
        Directory.CreateDirectory(path);

        return path;
    }

    public string WriteTextFile(string fileName, string contents)
    {
        string path = GetPath(fileName);
        File.WriteAllText(path, contents);

        return path;
    }

    public string WriteTextFile(string fileName, IReadOnlyList<string> lines)
    {
        string path = GetPath(fileName);
        File.WriteAllLines(path, lines);

        return path;
    }

    public string ReadTextFile(string fileName)
    {
        return File.ReadAllText(GetPath(fileName));
    }

    public IEnumerable<string> GetTree()
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(RootPath, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetRelativePath(RootPath, path).Replace(Path.DirectorySeparatorChar, '/');

            if (Directory.Exists(path))
            {
                yield return $"{name}/";
            }
            else
            {
                yield return name;
            }
        }
    }

    private static string GetTestIdentity()
    {
        StringBuilder builder = new();
        builder.Append(TestContext.CurrentContext.Test.FullName);

        object?[] arguments = TestContext.CurrentContext.Test.Arguments;
        if (arguments.Length > 0)
        {
            builder.Append('(');

            for (int index = 0; index < arguments.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(Convert.ToString(arguments[index], CultureInfo.InvariantCulture));
            }

            builder.Append(')');
        }

        return builder.ToString();
    }

    private static string GetDirectoryName(string testIdentity)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(testIdentity));
        string hashText = Convert.ToHexString(hash)[..12].ToLowerInvariant();
        string readableName = Sanitize(testIdentity);

        if (readableName.Length > 80)
        {
            readableName = readableName[..80].TrimEnd('-');
        }

        if (readableName.Length <= 0)
        {
            readableName = "test";
        }

        return $"{readableName}-{hashText}-{Environment.ProcessId}";
    }

    private static string Sanitize(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else
            {
                AppendSeparator(builder);
            }
        }

        return builder.ToString().Trim('-').ToLowerInvariant();
    }

    private static void AppendSeparator(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != '-')
        {
            builder.Append('-');
        }
    }
}
