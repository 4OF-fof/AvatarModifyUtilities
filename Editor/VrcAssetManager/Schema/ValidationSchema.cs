using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// 検証結果の重要度レベル
    /// </summary>
    public enum ValidationLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 検証結果の項目
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        [SerializeField] private ValidationLevel _level;
        [SerializeField] private string _message;
        [SerializeField] private string _fieldName;
        [SerializeField] private string _suggestion;
        [SerializeField] private DateTime _timestamp;

        public ValidationLevel Level
        {
            get => _level;
            set => _level = value;
        }

        public string Message
        {
            get => _message ?? string.Empty;
            set => _message = value?.Trim() ?? string.Empty;
        }

        public string FieldName
        {
            get => _fieldName ?? string.Empty;
            set => _fieldName = value?.Trim() ?? string.Empty;
        }

        public string Suggestion
        {
            get => _suggestion ?? string.Empty;
            set => _suggestion = value?.Trim() ?? string.Empty;
        }

        public DateTime Timestamp
        {
            get => _timestamp == default ? DateTime.Now : _timestamp;
            set => _timestamp = value;
        }

        public bool IsValid => _level == ValidationLevel.Info;
        public bool HasSuggestion => !string.IsNullOrEmpty(_suggestion);

        public ValidationResult()
        {
            _level = ValidationLevel.Info;
            _message = string.Empty;
            _fieldName = string.Empty;
            _suggestion = string.Empty;
            _timestamp = DateTime.Now;
        }

        public ValidationResult(ValidationLevel level, string message, string fieldName = "", string suggestion = "") : this()
        {
            _level = level;
            _message = message?.Trim() ?? string.Empty;
            _fieldName = fieldName?.Trim() ?? string.Empty;
            _suggestion = suggestion?.Trim() ?? string.Empty;
        }

        public static ValidationResult Info(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Info, message, fieldName, suggestion);

        public static ValidationResult Warning(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Warning, message, fieldName, suggestion);

        public static ValidationResult Error(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Error, message, fieldName, suggestion);

        public static ValidationResult Critical(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Critical, message, fieldName, suggestion);
    }

    /// <summary>
    /// 検証結果のコレクション
    /// </summary>
    [Serializable]
    public class ValidationResults
    {
        [SerializeField] private List<ValidationResult> _results;
        [SerializeField] private DateTime _validationTime;
        [SerializeField] private string _targetName;

        public IReadOnlyList<ValidationResult> Results => _results ?? new List<ValidationResult>();

        public DateTime ValidationTime
        {
            get => _validationTime == default ? DateTime.Now : _validationTime;
            set => _validationTime = value;
        }

        public string TargetName
        {
            get => _targetName ?? string.Empty;
            set => _targetName = value?.Trim() ?? string.Empty;
        }

        public int Count => _results?.Count ?? 0;
        public bool HasResults => Count > 0;
        public bool IsValid => !HasErrors && !HasCritical;
        public bool HasErrors => _results?.Any(r => r.Level == ValidationLevel.Error) ?? false;
        public bool HasWarnings => _results?.Any(r => r.Level == ValidationLevel.Warning) ?? false;
        public bool HasCritical => _results?.Any(r => r.Level == ValidationLevel.Critical) ?? false;

        public int ErrorCount => _results?.Count(r => r.Level == ValidationLevel.Error) ?? 0;
        public int WarningCount => _results?.Count(r => r.Level == ValidationLevel.Warning) ?? 0;
        public int CriticalCount => _results?.Count(r => r.Level == ValidationLevel.Critical) ?? 0;

        public ValidationResults()
        {
            _results = new List<ValidationResult>();
            _validationTime = DateTime.Now;
            _targetName = string.Empty;
        }

        public ValidationResults(string targetName) : this()
        {
            _targetName = targetName?.Trim() ?? string.Empty;
        }

        public void Add(ValidationResult result)
        {
            if (result == null) return;

            _results ??= new List<ValidationResult>();
            _results.Add(result);
        }

        public void AddRange(IEnumerable<ValidationResult> results)
        {
            if (results == null) return;

            _results ??= new List<ValidationResult>();
            _results.AddRange(results.Where(r => r != null));
        }

        public void Clear()
        {
            _results?.Clear();
            _validationTime = DateTime.Now;
        }

        public IEnumerable<ValidationResult> GetByLevel(ValidationLevel level)
        {
            return _results?.Where(r => r.Level == level) ?? Enumerable.Empty<ValidationResult>();
        }

        public IEnumerable<ValidationResult> GetByField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return Enumerable.Empty<ValidationResult>();

            return _results?.Where(r => r.FieldName.Equals(fieldName.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? Enumerable.Empty<ValidationResult>();
        }

        public ValidationLevel GetHighestLevel()
        {
            if (_results == null || _results.Count == 0) return ValidationLevel.Info;

            if (HasCritical) return ValidationLevel.Critical;
            if (HasErrors) return ValidationLevel.Error;
            if (HasWarnings) return ValidationLevel.Warning;
            return ValidationLevel.Info;
        }
    }
}
