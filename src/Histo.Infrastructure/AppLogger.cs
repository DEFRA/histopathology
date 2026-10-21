using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Histo.Infrastructure;

/// <summary>
/// <see cref="IAppLogger"/> implementation backed by
/// <c>Microsoft.Extensions.Logging.ILogger</c>.
///
/// Serilog is the concrete provider wired at the composition root in
/// <c>Histo.Web/Program.cs</c> via <c>UseSerilog()</c>. When Azure Monitor is configured, an
/// <c>ILogger</c> error that carries an exception is exported as an exception record, so callers
/// that catch and swallow an exception still get visibility through <see cref="LogError"/>.
///
/// Messages arrive as runtime templates, so they are written through <see cref="ILogger.Log{TState}"/>
/// with a <see cref="TemplateState"/> that carries the same named properties and <c>{OriginalFormat}</c>
/// as the framework's own template formatting.
/// </summary>
public sealed partial class AppLogger : IAppLogger
{
    private readonly ILogger _logger;

    public AppLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInfo(string message, params object[] args) => Write(LogLevel.Information, null, message, args);

    public void LogWarning(string message, params object[] args) => Write(LogLevel.Warning, null, message, args);

    public void LogError(string message, Exception? ex = null, params object[] args) => Write(LogLevel.Error, ex, message, args);

    private void Write(LogLevel level, Exception? ex, string message, object[] args)
    {
        if (!_logger.IsEnabled(level)) return;
        _logger.Log(level, default, new TemplateState(message, args), ex, static (state, _) => state.ToString());
    }

    /// <summary>Log state for a message template: named properties from the placeholders plus <c>{OriginalFormat}</c>.</summary>
    internal sealed partial class TemplateState : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private const string OriginalFormatKey = "{OriginalFormat}";

        private readonly string _template;
        private readonly List<KeyValuePair<string, object?>> _properties = [];
        private readonly string _compositeFormat;
        private readonly object?[] _args;

        public TemplateState(string template, object?[] args)
        {
            _template = template;
            _args = args;

            var composite = new StringBuilder();
            var last = 0;
            var index = 0;
            foreach (Match match in PlaceholderRegex().Matches(template))
            {
                composite.Append(template, last, match.Index - last);
                last = match.Index + match.Length;

                if (!match.Groups["body"].Success)
                {
                    composite.Append(match.Value);
                    continue;
                }

                var body = match.Groups["body"].Value;
                var suffixStart = body.IndexOfAny([',', ':']);
                var name = (suffixStart < 0 ? body : body[..suffixStart]).Trim().TrimStart('@', '$');
                var suffix = suffixStart < 0 ? string.Empty : body[suffixStart..];

                _properties.Add(new KeyValuePair<string, object?>(name, index < args.Length ? args[index] : null));
                composite.Append('{').Append(index.ToString(CultureInfo.InvariantCulture)).Append(suffix).Append('}');
                index++;
            }

            composite.Append(template, last, template.Length - last);
            _compositeFormat = composite.ToString();
            _properties.Add(new KeyValuePair<string, object?>(OriginalFormatKey, template));
        }

        public int Count => _properties.Count;

        public KeyValuePair<string, object?> this[int index] => _properties[index];

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _properties.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, _compositeFormat, _args);
            }
            catch (FormatException)
            {
                return _template;
            }
        }

        // Matches an escaped brace pair or a {Name[,align][:format]} placeholder.
        [GeneratedRegex(@"\{\{|\}\}|\{(?<body>[^{}]+)\}")]
        private static partial Regex PlaceholderRegex();
    }
}
