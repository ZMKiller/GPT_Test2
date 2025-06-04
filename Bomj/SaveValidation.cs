using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система валидации и проверки целостности сохранений
    /// </summary>
    public static class SaveValidation
    {
        // Константы для валидации
        private const string CURRENT_SAVE_VERSION = "1.0.0";
        private const int MIN_SUPPORTED_VERSION_MAJOR = 1;
        private const int MIN_SUPPORTED_VERSION_MINOR = 0;
        
        // Ключи для проверки целостности
        private const string INTEGRITY_KEY = "HomelessToMillionaire_SaveIntegrity";
        
        /// <summary>
        /// Результат валидации сохранения
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public ValidationIssue[] Issues { get; set; } = new ValidationIssue[0];
            public bool CanBeRepaired { get; set; }
            public CompleteSaveData RepairedData { get; set; }
            
            public ValidationResult(bool isValid, string errorMessage = "")
            {
                IsValid = isValid;
                ErrorMessage = errorMessage;
            }
        }
        
        /// <summary>
        /// Проблема валидации
        /// </summary>
        public class ValidationIssue
        {
            public ValidationIssueType Type { get; set; }
            public string Description { get; set; }
            public string Path { get; set; }
            public object ExpectedValue { get; set; }
            public object ActualValue { get; set; }
            public bool IsCritical { get; set; }
            
            public ValidationIssue(ValidationIssueType type, string description, string path = "", bool isCritical = false)
            {
                Type = type;
                Description = description;
                Path = path;
                IsCritical = isCritical;
            }
        }
        
        /// <summary>
        /// Типы проблем валидации
        /// </summary>
        public enum ValidationIssueType
        {
            InvalidVersion,
            CorruptedData,
            MissingField,
            InvalidValue,
            ChecksumMismatch,
            UnknownFormat,
            OutdatedVersion,
            SecurityViolation
        }

        #region Main Validation Methods

        /// <summary>
        /// Валидировать JSON строку сохранения
        /// </summary>
        public static ValidationResult ValidateJsonString(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                return new ValidationResult(false, "Пустые данные сохранения");
            }

            try
            {
                // Проверка валидности JSON
                JObject jsonObject = JObject.Parse(jsonData);
                
                // Десериализация в объект
                CompleteSaveData saveData = JsonConvert.DeserializeObject<CompleteSaveData>(jsonData);
                
                if (saveData == null)
                {
                    return new ValidationResult(false, "Не удалось десериализовать данные сохранения");
                }

                // Полная валидация объекта
                return ValidateSaveData(saveData);
            }
            catch (JsonException ex)
            {
                return new ValidationResult(false, $"Ошибка парсинга JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, $"Ошибка валидации: {ex.Message}");
            }
        }

        /// <summary>
        /// Валидировать объект сохранения
        /// </summary>
        public static ValidationResult ValidateSaveData(CompleteSaveData saveData)
        {
            var issues = new System.Collections.Generic.List<ValidationIssue>();
            bool canBeRepaired = true;

            if (saveData == null)
            {
                return new ValidationResult(false, "Объект сохранения равен null");
            }

            // Проверка версии
            var versionResult = ValidateVersion(saveData.gameState?.version);
            if (!versionResult.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.InvalidVersion, versionResult.ErrorMessage, "gameState.version", true));
                canBeRepaired = false;
            }

            // Проверка базовой валидности
            if (!saveData.IsValid())
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, "Базовая валидация не пройдена", "", true));
            }

            // Валидация данных игрока
            var playerValidation = ValidatePlayerData(saveData.playerData);
            if (!playerValidation.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, playerValidation.ErrorMessage, "playerData"));
            }

            // Валидация данных локаций
            var locationValidation = ValidateLocationData(saveData.locationData);
            if (!locationValidation.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, locationValidation.ErrorMessage, "locationData"));
            }

            // Валидация настроек
            var settingsValidation = ValidateSettingsData(saveData.settingsData);
            if (!settingsValidation.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, settingsValidation.ErrorMessage, "settingsData"));
            }

            // Валидация статистики
            var statsValidation = ValidateStatisticsData(saveData.statisticsData);
            if (!statsValidation.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, statsValidation.ErrorMessage, "statisticsData"));
            }

            // Валидация состояния игры
            var gameStateValidation = ValidateGameStateData(saveData.gameState);
            if (!gameStateValidation.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.CorruptedData, gameStateValidation.ErrorMessage, "gameState"));
            }

            // Проверка целостности
            var integrityResult = ValidateIntegrity(saveData);
            if (!integrityResult.IsValid)
            {
                issues.Add(new ValidationIssue(ValidationIssueType.ChecksumMismatch, integrityResult.ErrorMessage, "integrity", true));
                canBeRepaired = false;
            }

            // Результат валидации
            bool isValid = issues.Count == 0 || issues.TrueForAll(i => !i.IsCritical);
            
            var result = new ValidationResult(isValid);
            result.Issues = issues.ToArray();
            result.CanBeRepaired = canBeRepaired && !isValid;

            // Попытка автоматического исправления
            if (result.CanBeRepaired)
            {
                result.RepairedData = RepairSaveData(saveData, issues);
            }

            return result;
        }

        /// <summary>
        /// Проверить контрольную сумму файла
        /// </summary>
        public static ValidationResult ValidateFileChecksum(string filePath, string expectedChecksum)
        {
            if (!File.Exists(filePath))
            {
                return new ValidationResult(false, "Файл не существует");
            }

            try
            {
                string actualChecksum = CalculateFileChecksum(filePath);
                
                if (actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(true);
                }
                else
                {
                    return new ValidationResult(false, "Контрольная сумма не совпадает");
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, $"Ошибка при проверке контрольной суммы: {ex.Message}");
            }
        }

        #endregion

        #region Individual Data Validation

        /// <summary>
        /// Валидация данных игрока
        /// </summary>
        private static ValidationResult ValidatePlayerData(PlayerSaveData playerData)
        {
            if (playerData == null)
            {
                return new ValidationResult(false, "Данные игрока отсутствуют");
            }

            if (!playerData.IsValid())
            {
                return new ValidationResult(false, "Данные игрока невалидны");
            }

            // Проверка границ значений
            if (playerData.level < 1 || playerData.level > 100)
            {
                return new ValidationResult(false, $"Недопустимый уровень: {playerData.level}");
            }

            if (playerData.health < 0 || playerData.health > 100)
            {
                return new ValidationResult(false, $"Недопустимое здоровье: {playerData.health}");
            }

            if (playerData.hunger < 0 || playerData.hunger > 100)
            {
                return new ValidationResult(false, $"Недопустимый голод: {playerData.hunger}");
            }

            if (playerData.mood < 0 || playerData.mood > 100)
            {
                return new ValidationResult(false, $"Недопустимое настроение: {playerData.mood}");
            }

            if (playerData.money < 0)
            {
                return new ValidationResult(false, $"Недопустимые деньги: {playerData.money}");
            }

            if (playerData.experience < 0)
            {
                return new ValidationResult(false, $"Недопустимый опыт: {playerData.experience}");
            }

            return new ValidationResult(true);
        }

        /// <summary>
        /// Валидация данных локаций
        /// </summary>
        private static ValidationResult ValidateLocationData(LocationSaveData locationData)
        {
            if (locationData == null)
            {
                return new ValidationResult(false, "Данные локаций отсутствуют");
            }

            if (!locationData.IsValid())
            {
                return new ValidationResult(false, "Данные локаций невалидны");
            }

            // Проверка существования текущей локации в разблокированных
            if (!string.IsNullOrEmpty(locationData.currentLocation))
            {
                if (locationData.unlockedLocations == null || 
                    !System.Array.Exists(locationData.unlockedLocations, l => l == locationData.currentLocation))
                {
                    return new ValidationResult(false, "Текущая локация не найдена в разблокированных");
                }
            }

            return new ValidationResult(true);
        }

        /// <summary>
        /// Валидация настроек
        /// </summary>
        private static ValidationResult ValidateSettingsData(SettingsSaveData settingsData)
        {
            if (settingsData == null)
            {
                return new ValidationResult(false, "Настройки отсутствуют");
            }

            if (!settingsData.IsValid())
            {
                return new ValidationResult(false, "Настройки невалидны");
            }

            // Проверка диапазонов
            if (settingsData.masterVolume < 0 || settingsData.masterVolume > 1)
            {
                return new ValidationResult(false, $"Недопустимая громкость: {settingsData.masterVolume}");
            }

            if (settingsData.musicVolume < 0 || settingsData.musicVolume > 1)
            {
                return new ValidationResult(false, $"Недопустимая громкость музыки: {settingsData.musicVolume}");
            }

            if (settingsData.sfxVolume < 0 || settingsData.sfxVolume > 1)
            {
                return new ValidationResult(false, $"Недопустимая громкость эффектов: {settingsData.sfxVolume}");
            }

            return new ValidationResult(true);
        }

        /// <summary>
        /// Валидация статистики
        /// </summary>
        private static ValidationResult ValidateStatisticsData(StatisticsSaveData statisticsData)
        {
            if (statisticsData == null)
            {
                return new ValidationResult(false, "Статистика отсутствует");
            }

            if (!statisticsData.IsValid())
            {
                return new ValidationResult(false, "Статистика невалидна");
            }

            // Проверка логичности значений
            if (statisticsData.totalPlayTime < 0)
            {
                return new ValidationResult(false, $"Недопустимое время игры: {statisticsData.totalPlayTime}");
            }

            if (statisticsData.totalMoneyEarned < 0)
            {
                return new ValidationResult(false, $"Недопустимые заработанные деньги: {statisticsData.totalMoneyEarned}");
            }

            if (statisticsData.timesPlayed < 0)
            {
                return new ValidationResult(false, $"Недопустимое количество игр: {statisticsData.timesPlayed}");
            }

            return new ValidationResult(true);
        }

        /// <summary>
        /// Валидация состояния игры
        /// </summary>
        private static ValidationResult ValidateGameStateData(GameStateSaveData gameStateData)
        {
            if (gameStateData == null)
            {
                return new ValidationResult(false, "Состояние игры отсутствует");
            }

            if (!gameStateData.IsValid())
            {
                return new ValidationResult(false, "Состояние игры невалидно");
            }

            // Проверка версии
            var versionResult = ValidateVersion(gameStateData.version);
            if (!versionResult.IsValid)
            {
                return versionResult;
            }

            // Проверка времени игры
            if (gameStateData.gameTime < 0)
            {
                return new ValidationResult(false, $"Недопустимое время игры: {gameStateData.gameTime}");
            }

            // Проверка даты сохранения
            try
            {
                DateTime saveDate = DateTime.FromBinary(gameStateData.saveDate);
                if (saveDate > DateTime.Now.AddDays(1)) // Проверка на будущую дату
                {
                    return new ValidationResult(false, "Дата сохранения из будущего");
                }
            }
            catch
            {
                return new ValidationResult(false, "Недопустимая дата сохранения");
            }

            return new ValidationResult(true);
        }

        #endregion

        #region Version Validation

        /// <summary>
        /// Валидация версии сохранения
        /// </summary>
        private static ValidationResult ValidateVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new ValidationResult(false, "Версия сохранения не указана");
            }

            try
            {
                Version saveVersion = new Version(version);
                Version currentVersion = new Version(CURRENT_SAVE_VERSION);
                Version minVersion = new Version(MIN_SUPPORTED_VERSION_MAJOR, MIN_SUPPORTED_VERSION_MINOR, 0);

                if (saveVersion < minVersion)
                {
                    return new ValidationResult(false, $"Версия сохранения {version} не поддерживается. Минимальная версия: {minVersion}");
                }

                if (saveVersion > currentVersion)
                {
                    return new ValidationResult(false, $"Версия сохранения {version} новее текущей версии игры {currentVersion}");
                }

                return new ValidationResult(true);
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, $"Недопустимый формат версии: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверить совместимость версии
        /// </summary>
        public static bool IsVersionCompatible(string version)
        {
            return ValidateVersion(version).IsValid;
        }

        /// <summary>
        /// Получить текущую версию сохранения
        /// </summary>
        public static string GetCurrentSaveVersion()
        {
            return CURRENT_SAVE_VERSION;
        }

        #endregion

        #region Integrity Validation

        /// <summary>
        /// Валидация целостности данных
        /// </summary>
        private static ValidationResult ValidateIntegrity(CompleteSaveData saveData)
        {
            try
            {
                // Если контрольная сумма не указана, пропускаем проверку
                if (string.IsNullOrEmpty(saveData.gameState?.checksum))
                {
                    return new ValidationResult(true);
                }

                // Вычисляем контрольную сумму данных
                string calculatedChecksum = CalculateDataChecksum(saveData);
                
                if (calculatedChecksum.Equals(saveData.gameState.checksum, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(true);
                }
                else
                {
                    return new ValidationResult(false, "Контрольная сумма данных не совпадает");
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, $"Ошибка проверки целостности: {ex.Message}");
            }
        }

        /// <summary>
        /// Вычислить контрольную сумму данных
        /// </summary>
        public static string CalculateDataChecksum(CompleteSaveData saveData)
        {
            try
            {
                // Создаем копию без контрольной суммы для расчета
                var dataForChecksum = new CompleteSaveData
                {
                    playerData = saveData.playerData,
                    locationData = saveData.locationData,
                    settingsData = saveData.settingsData,
                    statisticsData = saveData.statisticsData,
                    gameState = new GameStateSaveData
                    {
                        version = saveData.gameState?.version ?? GetCurrentSaveVersion(),
                        saveDate = saveData.gameState?.saveDate ?? DateTime.Now.ToBinary(),
                        gameTime = saveData.gameState?.gameTime ?? 0f,
                        checksum = "" // Не включаем в расчет
                    }
                };

                string jsonData = JsonConvert.SerializeObject(dataForChecksum, Formatting.None);
                return CalculateStringChecksum(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка расчета контрольной суммы: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Вычислить контрольную сумму строки
        /// </summary>
        public static string CalculateStringChecksum(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input + INTEGRITY_KEY);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Вычислить контрольную сумму файла
        /// </summary>
        public static string CalculateFileChecksum(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return Convert.ToBase64String(hashBytes);
            }
        }

        #endregion

        #region Data Repair

        /// <summary>
        /// Попытка автоматического исправления данных
        /// </summary>
        private static CompleteSaveData RepairSaveData(CompleteSaveData originalData, System.Collections.Generic.List<ValidationIssue> issues)
        {
            try
            {
                var repairedData = JsonConvert.DeserializeObject<CompleteSaveData>(
                    JsonConvert.SerializeObject(originalData));

                foreach (var issue in issues)
                {
                    if (issue.IsCritical) continue; // Критические ошибки не исправляем

                    switch (issue.Type)
                    {
                        case ValidationIssueType.MissingField:
                            RepairMissingField(repairedData, issue);
                            break;
                            
                        case ValidationIssueType.InvalidValue:
                            RepairInvalidValue(repairedData, issue);
                            break;
                            
                        case ValidationIssueType.CorruptedData:
                            RepairCorruptedData(repairedData, issue);
                            break;
                    }
                }

                // Обновляем контрольную сумму
                if (repairedData.gameState != null)
                {
                    repairedData.gameState.checksum = CalculateDataChecksum(repairedData);
                }

                return repairedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка исправления данных: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Исправить отсутствующее поле
        /// </summary>
        private static void RepairMissingField(CompleteSaveData data, ValidationIssue issue)
        {
            switch (issue.Path)
            {
                case "playerData":
                    if (data.playerData == null)
                        data.playerData = new PlayerSaveData();
                    break;
                    
                case "locationData":
                    if (data.locationData == null)
                        data.locationData = new LocationSaveData();
                    break;
                    
                case "settingsData":
                    if (data.settingsData == null)
                        data.settingsData = new SettingsSaveData();
                    break;
                    
                case "statisticsData":
                    if (data.statisticsData == null)
                        data.statisticsData = new StatisticsSaveData();
                    break;
                    
                case "gameState":
                    if (data.gameState == null)
                        data.gameState = new GameStateSaveData();
                    break;
            }
        }

        /// <summary>
        /// Исправить недопустимое значение
        /// </summary>
        private static void RepairInvalidValue(CompleteSaveData data, ValidationIssue issue)
        {
            // Здесь можно добавить логику исправления конкретных значений
            // Например, приведение к допустимым границам
            if (data.playerData != null)
            {
                data.playerData.health = Mathf.Clamp(data.playerData.health, 0, 100);
                data.playerData.hunger = Mathf.Clamp(data.playerData.hunger, 0, 100);
                data.playerData.mood = Mathf.Clamp(data.playerData.mood, 0, 100);
                data.playerData.money = Mathf.Max(0, data.playerData.money);
                data.playerData.experience = Mathf.Max(0, data.playerData.experience);
                data.playerData.level = Mathf.Clamp(data.playerData.level, 1, 100);
            }

            if (data.settingsData != null)
            {
                data.settingsData.masterVolume = Mathf.Clamp01(data.settingsData.masterVolume);
                data.settingsData.musicVolume = Mathf.Clamp01(data.settingsData.musicVolume);
                data.settingsData.sfxVolume = Mathf.Clamp01(data.settingsData.sfxVolume);
            }
        }

        /// <summary>
        /// Исправить поврежденные данные
        /// </summary>
        private static void RepairCorruptedData(CompleteSaveData data, ValidationIssue issue)
        {
            // Сброс до значений по умолчанию для поврежденных секций
            switch (issue.Path)
            {
                case "playerData":
                    data.playerData = new PlayerSaveData();
                    break;
                    
                case "locationData":
                    data.locationData = new LocationSaveData();
                    break;
                    
                case "settingsData":
                    data.settingsData = new SettingsSaveData();
                    break;
                    
                case "statisticsData":
                    data.statisticsData = new StatisticsSaveData();
                    break;
            }
        }

        #endregion

        #region Migration

        /// <summary>
        /// Мигрировать старое сохранение к новой версии
        /// </summary>
        public static CompleteSaveData MigrateSaveData(CompleteSaveData oldData, string targetVersion)
        {
            if (oldData == null || oldData.gameState == null)
                return null;

            try
            {
                Version oldVersion = new Version(oldData.gameState.version);
                Version newVersion = new Version(targetVersion);

                if (oldVersion >= newVersion)
                    return oldData; // Миграция не нужна

                var migratedData = JsonConvert.DeserializeObject<CompleteSaveData>(
                    JsonConvert.SerializeObject(oldData));

                // Применяем миграции по версиям
                // Здесь можно добавить логику миграции для конкретных версий

                // Обновляем версию
                migratedData.gameState.version = targetVersion;
                migratedData.gameState.checksum = CalculateDataChecksum(migratedData);

                return migratedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка миграции сохранения: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}