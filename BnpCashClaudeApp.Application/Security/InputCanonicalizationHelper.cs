using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BnpCashClaudeApp.Application.Security
{
    /// <summary>
    /// Shared canonicalization + rich-text sanitization helper for inbound user-controlled string data (FDP_ITC.2.2).
    /// </summary>
    public static class InputCanonicalizationHelper
    {
        private static readonly string[] SensitiveFieldMarkers = new[]
        {
            "password",
            "token",
            "secret",
            "hash",
            "signature",
            "otp",
            "captcha"
        };

        private static readonly string[] RichTextFieldMarkers = new[]
        {
            "description",
            "content",
            "body",
            "html",
            "comment",
            "note",
            "message",
            "reason",
            "template",
            "action",
            "condition"
        };

        private static readonly Regex HtmlTagRegex = new Regex(
            @"<\s*/?\s*[a-zA-Z][^>]*>",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex HtmlCommentRegex = new Regex(
            @"<!--[\s\S]*?-->",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex DangerousTagBlockRegex = new Regex(
            @"<\s*(script|style|iframe|object|embed|link|meta|base|form|input|button|textarea|select)\b[^>]*>(.*?)<\s*/\s*\1\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex DangerousTagSelfClosingRegex = new Regex(
            @"<\s*(script|style|iframe|object|embed|link|meta|base|form|input|button|textarea|select)\b[^>]*?/?>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex EventHandlerAttributeRegex = new Regex(
            @"\s+on[a-z0-9_-]+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex StyleAttributeRegex = new Regex(
            @"\s+style\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex ScriptProtocolAttributeRegex = new Regex(
            @"(\s(?:href|src|xlink:href|action|formaction)\s*=\s*['""]?)\s*(?:javascript|vbscript|data)\s*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex EncodedDangerousTagRegex = new Regex(
            @"&lt;\s*/?\s*(script|style|iframe|object|embed|form|input|meta|link|base)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static void CanonicalizeObjectGraph(object? root, string? rootFieldName = null)
        {
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            CanonicalizeNode(root, rootFieldName, visited);
        }

        public static string CanonicalizeString(string input, string? fieldName = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (IsSensitiveField(fieldName))
            {
                return RemoveDangerousControlChars(input);
            }

            var normalized = input.Normalize(NormalizationForm.FormKC);
            normalized = normalized.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = RemoveDangerousControlChars(normalized);
            normalized = normalized.Trim();

            if (ShouldApplyRichTextSanitization(fieldName, normalized))
            {
                normalized = SanitizeRichText(normalized);
            }

            return normalized;
        }

        public static string SanitizeRichText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sanitized = input;
            sanitized = HtmlCommentRegex.Replace(sanitized, string.Empty);
            sanitized = DangerousTagBlockRegex.Replace(sanitized, string.Empty);
            sanitized = DangerousTagSelfClosingRegex.Replace(sanitized, string.Empty);
            sanitized = EventHandlerAttributeRegex.Replace(sanitized, string.Empty);
            sanitized = StyleAttributeRegex.Replace(sanitized, string.Empty);
            sanitized = ScriptProtocolAttributeRegex.Replace(sanitized, "$1#");
            sanitized = EncodedDangerousTagRegex.Replace(sanitized, "&lt;blocked-$1");

            return sanitized;
        }

        public static string RemoveDangerousControlChars(string input)
        {
            var sb = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (char.IsControl(ch) && ch != '\n' && ch != '\t')
                    continue;

                sb.Append(ch);
            }

            return sb.ToString();
        }

        private static void CanonicalizeNode(
            object? node,
            string? fieldName,
            HashSet<object> visited)
        {
            if (node == null)
                return;

            if (node is string)
                return;

            var type = node.GetType();
            if (IsSimpleType(type))
                return;

            if (type == typeof(byte[]))
                return;

            if (!type.IsValueType && !visited.Add(node))
                return;

            if (node is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    var value = dictionary[key];
                    if (value == null)
                        continue;

                    if (value is string stringValue)
                    {
                        var canonical = CanonicalizeString(stringValue, key?.ToString());
                        if (!string.Equals(stringValue, canonical, StringComparison.Ordinal))
                        {
                            try
                            {
                                dictionary[key] = canonical;
                            }
                            catch
                            {
                                // Ignore read-only dictionary scenarios and keep request flow resilient.
                            }
                        }

                        continue;
                    }

                    CanonicalizeNode(value, key?.ToString(), visited);
                }

                return;
            }

            if (node is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    CanonicalizeNode(item, fieldName, visited);
                }

                return;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            foreach (var property in properties)
            {
                object? value;
                try
                {
                    value = property.GetValue(node);
                }
                catch
                {
                    continue;
                }

                if (value == null)
                    continue;

                if (property.PropertyType == typeof(string))
                {
                    if (!property.CanWrite)
                        continue;

                    var original = (string)value;
                    var canonical = CanonicalizeString(original, property.Name);

                    if (!string.Equals(original, canonical, StringComparison.Ordinal))
                    {
                        try
                        {
                            property.SetValue(node, canonical);
                        }
                        catch
                        {
                            // Ignore non-settable/init-only edge cases and keep request flow resilient.
                        }
                    }

                    continue;
                }

                CanonicalizeNode(value, property.Name, visited);
            }
        }

        private static bool IsSensitiveField(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var lowered = fieldName.ToLowerInvariant();
            return SensitiveFieldMarkers.Any(marker => lowered.Contains(marker));
        }

        private static bool ShouldApplyRichTextSanitization(string? fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (IsRichTextField(fieldName))
                return true;

            return LooksLikeHtml(value);
        }

        private static bool IsRichTextField(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var lowered = fieldName.ToLowerInvariant();
            return RichTextFieldMarkers.Any(marker => lowered.Contains(marker));
        }

        private static bool LooksLikeHtml(string value)
        {
            if (value.IndexOf('<') < 0 || value.IndexOf('>') < 0)
                return false;

            return HtmlTagRegex.IsMatch(value);
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
