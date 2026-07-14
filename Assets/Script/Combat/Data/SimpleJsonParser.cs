using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKs.Combat
{
    /// <summary>
    /// Small JSON reader used to keep the card pipeline dependency-free in Unity.
    /// It supports the complete JSON value grammar and returns dictionaries/lists/scalars.
    /// </summary>
    internal static class SimpleJsonParser
    {
        public static Dictionary<string, object> ParseObject(string json)
        {
            var value = new Parser(json).ParseDocument();
            if (value is Dictionary<string, object> objectValue) return objectValue;
            throw new FormatException("The JSON root must be an object.");
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json ?? throw new ArgumentNullException(nameof(json));
            }

            public object ParseDocument()
            {
                SkipWhiteSpace();
                var result = ParseValue();
                SkipWhiteSpace();
                if (_index != _json.Length) Fail("Unexpected content after the JSON value.");
                return result;
            }

            private object ParseValue()
            {
                SkipWhiteSpace();
                if (_index >= _json.Length) Fail("Unexpected end of JSON.");

                switch (_json[_index])
                {
                    case '{': return ParseObjectValue();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': ReadLiteral("true"); return true;
                    case 'f': ReadLiteral("false"); return false;
                    case 'n': ReadLiteral("null"); return null;
                    default:
                        if (_json[_index] == '-' || char.IsDigit(_json[_index])) return ParseNumber();
                        Fail("Unexpected JSON token.");
                        return null;
                }
            }

            private Dictionary<string, object> ParseObjectValue()
            {
                Expect('{');
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhiteSpace();
                if (TryConsume('}')) return result;

                while (true)
                {
                    SkipWhiteSpace();
                    if (_index >= _json.Length || _json[_index] != '"') Fail("Object key must be a string.");
                    var key = ParseString();
                    SkipWhiteSpace();
                    Expect(':');
                    if (!result.TryAdd(key, ParseValue())) Fail("Duplicate object key: " + key);
                    SkipWhiteSpace();
                    if (TryConsume('}')) return result;
                    Expect(',');
                }
            }

            private List<object> ParseArray()
            {
                Expect('[');
                var result = new List<object>();
                SkipWhiteSpace();
                if (TryConsume(']')) return result;

                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhiteSpace();
                    if (TryConsume(']')) return result;
                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var result = new StringBuilder();

                while (_index < _json.Length)
                {
                    var c = _json[_index++];
                    if (c == '"') return result.ToString();
                    if (c < 0x20) Fail("Control character in string.");

                    if (c != '\\')
                    {
                        result.Append(c);
                        continue;
                    }

                    if (_index >= _json.Length) Fail("Unterminated escape sequence.");
                    var escape = _json[_index++];
                    switch (escape)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u': result.Append(ParseUnicodeEscape()); break;
                        default: Fail("Invalid escape sequence."); break;
                    }
                }

                Fail("Unterminated string.");
                return string.Empty;
            }

            private char ParseUnicodeEscape()
            {
                if (_index + 4 > _json.Length) Fail("Incomplete unicode escape.");
                var hex = _json.Substring(_index, 4);
                _index += 4;
                if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    Fail("Invalid unicode escape.");
                return (char)value;
            }

            private object ParseNumber()
            {
                var start = _index;
                if (TryConsume('-')) { }
                ReadDigits();

                var floatingPoint = false;
                if (TryConsume('.'))
                {
                    floatingPoint = true;
                    ReadDigits();
                }

                if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
                {
                    floatingPoint = true;
                    _index++;
                    if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-')) _index++;
                    ReadDigits();
                }

                var token = _json.Substring(start, _index - start);
                if (!floatingPoint && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    return integer;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    return number;

                Fail("Invalid number.");
                return 0L;
            }

            private void ReadDigits()
            {
                var start = _index;
                while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                if (_index == start) Fail("Expected digit.");
            }

            private void ReadLiteral(string literal)
            {
                if (_index + literal.Length > _json.Length ||
                    string.CompareOrdinal(_json, _index, literal, 0, literal.Length) != 0)
                    Fail("Invalid literal.");
                _index += literal.Length;
            }

            private bool TryConsume(char expected)
            {
                if (_index >= _json.Length || _json[_index] != expected) return false;
                _index++;
                return true;
            }

            private void Expect(char expected)
            {
                SkipWhiteSpace();
                if (!TryConsume(expected)) Fail("Expected '" + expected + "'.");
            }

            private void SkipWhiteSpace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }

            private void Fail(string message)
            {
                throw new FormatException(message + " Position: " + _index + ".");
            }
        }
    }
}
