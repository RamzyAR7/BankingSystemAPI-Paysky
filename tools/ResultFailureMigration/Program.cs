using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Simple file-based migrator: replaces Result<T>.Failure(new[] { new ResultError(ErrorType.Validation, "msg") }) and Result<T>.Failure(new[] { new ResultError(ErrorType.Validation, ApiResponseMessages.X) })
// with structured Result<T>.Failure(new ResultError(ErrorType.Validation, "msg")) or preserve ApiResponseMessages by creating ResultErrors.

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
Console.WriteLine($"Scanning workspace: {root}");

var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" ) && !p.Contains(Path.DirectorySeparatorChar + "obj")).ToList();

int replaced = 0;

// pattern: Result<...>.Failure(new[] { new ResultError(ErrorType.Validation, ...) }) or Result.Failure(new[] { new ResultError(ErrorType.Validation, ...) })
var pattern = new Regex(@"(?<prefix>Result(?:<[^>]+>)?\.Failure)\(new\s*\[\s*\]\s*\{(?<inner>[^}]+)\}\)", RegexOptions.Compiled);

foreach(var file in files)
{
    var text = File.ReadAllText(file);
    var newText = pattern.Replace(text, m =>
    {
        var prefix = m.Groups["prefix"].Value;
        var inner = m.Groups["inner"].Value.Trim();
        // split items by comma
        var items = inner.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        var converted = string.Join(", ", items.Select(it =>
        {
            // If item is a string literal keep it, else keep expression
            if (it.StartsWith("\"") || it.StartsWith("@\""))
                return $"new ResultError(ErrorType.Validation, {it})";
            return $"new ResultError(ErrorType.Validation, {it})";
        }));

        replaced++;
        return $"{prefix}(new[] {{ {converted} }})";
    });

    if (newText != text)
        File.WriteAllText(file, newText);
}

Console.WriteLine($"Replaced {replaced} occurrences.");
