#nullable enable
using DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Domain.Common.Validators
{
    /// <summary>
    /// Validator for Hive-related operations
    /// </summary>
    public class HiveValidator
    {
        private readonly IHiveRepository _hiveRepository;
        private readonly ILogger<HiveValidator> _logger;

        public HiveValidator(IHiveRepository hiveRepository, ILogger<HiveValidator> logger)
        {
            _hiveRepository = hiveRepository;
            _logger = logger;
        }

        public ValidationResult ValidateHiveExists(string hiveId)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                return ValidationResult.Failure("Hive ID cannot be null or empty");
            }

            if (!_hiveRepository.Exists(hiveId))
            {
                _logger.LogWarning("Hive {HiveId} does not exist", hiveId);
                return ValidationResult.Failure($"Hive {hiveId} does not exist. Please create the Hive first.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }

        private ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}

