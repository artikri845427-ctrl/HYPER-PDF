// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Globalization;
using System.Text;
using HyperPDF.Models;

namespace HyperPDF.Services;

/// <summary>
/// Extracts the document information dictionary (title, author, producer, ...) of a PDF file.
/// </summary>
/// <remarks>
/// PDFium does not expose the info dictionary in the pinned engine build, therefore HyperPDF
/// reads the dictionary directly from the file. The parser is intentionally defensive: every
/// failure degrades to "information not available" instead of raising an error, so a damaged
/// document can still be displayed.
/// </remarks>
public static class PdfMetadataScanner
{
    private const long MaxScanBytes = 96L * 1024 * 1024;

    private static readonly string[] InfoKeys =
    {
        "Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate"
    };

    /// <summary>Reads the document information of a PDF file.</summary>
    /// <param name="filePath">Path of the PDF file.</param>
    /// <param name="info">Optional instance that receives the values.</param>
    /// <returns>The populated document information.</returns>
    public static DocumentInfo Scan(string filePath, DocumentInfo? info = null)
    {
        var result = info ?? new DocumentInfo();
        result.FilePath = filePath;
        result.FileName = Path.GetFileName(filePath);

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                result.FileSize = fileInfo.Length;
                result.LastModified = fileInfo.LastWriteTime;
            }
        }
        catch (IOException)
        {
            // Ignore: the information simply stays empty.
        }

        byte[] data;
        try
        {
            data = ReadAllBytes(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return result;
        }

        if (data.Length == 0)
        {
            return result;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryReadInfoDictionary(data, values))
        {
            ScanForKeys(data, values);
        }

        result.Title = Get(values, "Title");
        result.Author = Get(values, "Author");
        result.Subject = Get(values, "Subject");
        result.Keywords = Get(values, "Keywords");
        result.Creator = Get(values, "Creator");
        result.Producer = Get(values, "Producer");
        result.CreationDate = Get(values, "CreationDate");
        result.ModDate = Get(values, "ModDate");
        return result;
    }

    private static string Get(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) ? value : string.Empty;

    private static byte[] ReadAllBytes(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length <= MaxScanBytes)
        {
            return File.ReadAllBytes(filePath);
        }

        // Very large files: only scan the head and the tail of the document.
        const int chunk = 8 * 1024 * 1024;
        using FileStream stream = File.OpenRead(filePath);
        var head = new byte[chunk];
        int headRead = stream.Read(head, 0, chunk);
        stream.Seek(-chunk, SeekOrigin.End);
        var tail = new byte[chunk];
        int tailRead = stream.Read(tail, 0, chunk);

        var buffer = new byte[headRead + tailRead];
        Buffer.BlockCopy(head, 0, buffer, 0, headRead);
        Buffer.BlockCopy(tail, 0, buffer, headRead, tailRead);
        return buffer;
    }

    /// <summary>
    /// Locates the info dictionary through the /Info entry of the trailer and parses it.
    /// </summary>
    private static bool TryReadInfoDictionary(byte[] data, Dictionary<string, string> values)
    {
        int infoPosition = FindLast(data, "/Info");
        if (infoPosition < 0)
        {
            return false;
        }

        int pos = infoPosition + 5;
        SkipWhiteSpace(data, ref pos);
        if (!TryReadInteger(data, ref pos, out int objectNumber))
        {
            return false;
        }

        SkipWhiteSpace(data, ref pos);
        if (!TryReadInteger(data, ref pos, out int generation))
        {
            return false;
        }

        SkipWhiteSpace(data, ref pos);
        if (pos >= data.Length || data[pos] != 'R')
        {
            return false;
        }

        int objectStart = FindObject(data, objectNumber, generation);
        if (objectStart < 0)
        {
            return false;
        }

        pos = objectStart;
        SkipWhiteSpace(data, ref pos);
        if (pos + 1 >= data.Length || data[pos] != '<' || data[pos + 1] != '<')
        {
            return false;
        }

        pos += 2;
        return ReadDictionary(data, ref pos, values);
    }

    private static int FindObject(byte[] data, int objectNumber, int generation)
    {
        string header = objectNumber.ToString(CultureInfo.InvariantCulture) + " " +
                        generation.ToString(CultureInfo.InvariantCulture) + " obj";

        int index = FindLast(data, header);
        while (index > 0)
        {
            // Make sure the match is not the tail of a longer number (e.g. "12 0 obj" inside "112 0 obj").
            if (data[index - 1] < '0' || data[index - 1] > '9')
            {
                return index + header.Length;
            }

            index = FindLast(data, header, index - 1);
        }

        return index >= 0 ? index + header.Length : -1;
    }

    /// <summary>
    /// Fallback: scan the whole file for the well known information keys.
    /// </summary>
    private static void ScanForKeys(byte[] data, Dictionary<string, string> values)
    {
        foreach (string key in InfoKeys)
        {
            if (values.ContainsKey(key))
            {
                continue;
            }

            byte[] pattern = Encoding.ASCII.GetBytes("/" + key);
            int index = FindFirst(data, pattern);
            if (index < 0)
            {
                continue;
            }

            int pos = index + pattern.Length;
            SkipWhiteSpace(data, ref pos);
            if (pos < data.Length && data[pos] == '(')
            {
                if (TryReadLiteralString(data, ref pos, out string value))
                {
                    values[key] = value;
                }
            }
            else if (pos < data.Length && data[pos] == '<')
            {
                if (TryReadHexString(data, ref pos, out string hexValue))
                {
                    values[key] = hexValue;
                }
            }
        }
    }

    private static bool ReadDictionary(byte[] data, ref int pos, Dictionary<string, string> values)
    {
        while (pos < data.Length)
        {
            SkipWhiteSpace(data, ref pos);
            if (pos >= data.Length)
            {
                return false;
            }

            if (data[pos] == '>' && pos + 1 < data.Length && data[pos + 1] == '>')
            {
                return true;
            }

            if (data[pos] != '/')
            {
                return false;
            }

            if (!TryReadName(data, ref pos, out string key))
            {
                return false;
            }

            SkipWhiteSpace(data, ref pos);
            if (pos >= data.Length)
            {
                return false;
            }

            if (data[pos] == '(')
            {
                if (TryReadLiteralString(data, ref pos, out string value) && !values.ContainsKey(key))
                {
                    values[key] = value;
                }
            }
            else if (data[pos] == '<')
            {
                if (TryReadHexString(data, ref pos, out string value) && !values.ContainsKey(key))
                {
                    values[key] = value;
                }
            }
            else
            {
                if (!TrySkipValue(data, ref pos))
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static bool TryReadName(byte[] data, ref int pos, out string name)
    {
        name = string.Empty;
        if (pos >= data.Length || data[pos] != '/')
        {
            return false;
        }

        pos++;
        var builder = new StringBuilder();
        while (pos < data.Length)
        {
            byte current = data[pos];
            if (current == '/' || current == '<' || current == '>' || current == '(' || current == ')' ||
                current == '{' || current == '}' || current == '[' || current == ']' || current == '%' ||
                current == ' ' || current == '\r' || current == '\n' || current == '\t')
            {
                break;
            }

            builder.Append((char)current);
            pos++;
        }

        name = builder.ToString();
        return name.Length > 0;
    }

    private static bool TryReadLiteralString(byte[] data, ref int pos, out string value)
    {
        value = string.Empty;
        if (pos >= data.Length || data[pos] != '(')
        {
            return false;
        }

        pos++;
        var bytes = new List<byte>();
        int depth = 1;

        while (pos < data.Length)
        {
            byte current = data[pos++];

            if (current == '\\')
            {
                if (pos >= data.Length)
                {
                    break;
                }

                byte escaped = data[pos++];
                switch ((char)escaped)
                {
                    case 'n': bytes.Add((byte)'\n'); break;
                    case 'r': bytes.Add((byte)'\r'); break;
                    case 't': bytes.Add((byte)'\t'); break;
                    case 'b': bytes.Add(8); break;
                    case 'f': bytes.Add(12); break;
                    case '(': bytes.Add((byte)'('); break;
                    case ')': bytes.Add((byte)')'); break;
                    case '\\': bytes.Add((byte)'\\'); break;
                    case '\r':
                        if (pos < data.Length && data[pos] == '\n')
                        {
                            pos++;
                        }

                        break;
                    case '\n':
                        break;
                    default:
                        if (escaped >= '0' && escaped <= '7')
                        {
                            int octal = escaped - '0';
                            for (int i = 0; i < 2 && pos < data.Length && data[pos] >= '0' && data[pos] <= '7'; i++)
                            {
                                octal = (octal * 8) + (data[pos++] - '0');
                            }

                            bytes.Add((byte)(octal & 0xFF));
                        }
                        else
                        {
                            bytes.Add(escaped);
                        }

                        break;
                }

                continue;
            }

            if (current == '(')
            {
                depth++;
            }
            else if (current == ')')
            {
                depth--;
                if (depth == 0)
                {
                    break;
                }
            }

            bytes.Add(current);
        }

        value = DecodeText(bytes.ToArray());
        return true;
    }

    private static bool TryReadHexString(byte[] data, ref int pos, out string value)
    {
        value = string.Empty;
        if (pos >= data.Length || data[pos] != '<')
        {
            return false;
        }

        pos++;
        var bytes = new List<byte>();
        int high = -1;

        while (pos < data.Length)
        {
            byte current = data[pos++];
            if (current == '>')
            {
                break;
            }

            int digit = HexValue(current);
            if (digit < 0)
            {
                continue;
            }

            if (high < 0)
            {
                high = digit;
            }
            else
            {
                bytes.Add((byte)((high << 4) | digit));
                high = -1;
            }
        }

        if (high >= 0)
        {
            bytes.Add((byte)(high << 4));
        }

        value = DecodeText(bytes.ToArray());
        return true;
    }

    private static bool TrySkipValue(byte[] data, ref int pos)
    {
        if (pos >= data.Length)
        {
            return false;
        }

        switch ((char)data[pos])
        {
            case '(':
                return TryReadLiteralString(data, ref pos, out _);
            case '<':
                if (pos + 1 < data.Length && data[pos + 1] == '<')
                {
                    pos += 2;
                    int depth = 1;
                    while (pos < data.Length && depth > 0)
                    {
                        if (data[pos] == '<' && pos + 1 < data.Length && data[pos + 1] == '<')
                        {
                            depth++;
                            pos += 2;
                            continue;
                        }

                        if (data[pos] == '>' && pos + 1 < data.Length && data[pos + 1] == '>')
                        {
                            depth--;
                            pos += 2;
                            continue;
                        }

                        pos++;
                    }

                    return true;
                }

                return TryReadHexString(data, ref pos, out _);
            case '[':
            {
                pos++;
                while (pos < data.Length && data[pos] != ']')
                {
                    if (!TrySkipValue(data, ref pos))
                    {
                        return false;
                    }

                    SkipWhiteSpace(data, ref pos);
                }

                if (pos < data.Length)
                {
                    pos++;
                }

                return true;
            }
            case '/':
                return TryReadName(data, ref pos, out _);
            default:
            {
                // Number, boolean, null or indirect reference.
                while (pos < data.Length && data[pos] > 0x20)
                {
                    pos++;
                }

                return true;
            }
        }
    }

    private static string DecodeText(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        // PDFDocEncoding is a superset of Latin-1 for everything HyperPDF displays.
        return Encoding.Latin1.GetString(bytes);
    }

    private static int HexValue(byte value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    private static void SkipWhiteSpace(byte[] data, ref int pos)
    {
        while (pos < data.Length)
        {
            byte current = data[pos];
            if (current == '%')
            {
                while (pos < data.Length && data[pos] != '\r' && data[pos] != '\n')
                {
                    pos++;
                }

                continue;
            }

            if (current is 0x20 or 0x0A or 0x0D or 0x09 or 0x00)
            {
                pos++;
                continue;
            }

            return;
        }
    }

    private static bool TryReadInteger(byte[] data, ref int pos, out int value)
    {
        value = 0;
        int start = pos;
        while (pos < data.Length && data[pos] >= '0' && data[pos] <= '9')
        {
            pos++;
        }

        if (pos == start)
        {
            return false;
        }

        return int.TryParse(
            Encoding.ASCII.GetString(data, start, pos - start),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static int FindLast(byte[] data, string pattern) => FindLast(data, pattern, data.Length - 1);

    private static int FindLast(byte[] data, string pattern, int startIndex)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(pattern);
        if (bytes.Length == 0 || data.Length < bytes.Length)
        {
            return -1;
        }

        int limit = Math.Min(startIndex, data.Length - bytes.Length);
        for (int i = limit; i >= 0; i--)
        {
            if (data[i] == bytes[0] && Matches(data, bytes, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFirst(byte[] data, byte[] pattern)
    {
        if (pattern.Length == 0 || data.Length < pattern.Length)
        {
            return -1;
        }

        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            if (data[i] == pattern[0] && Matches(data, pattern, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Matches(byte[] data, byte[] pattern, int offset)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            if (data[offset + i] != pattern[i])
            {
                return false;
            }
        }

        return true;
    }
}
