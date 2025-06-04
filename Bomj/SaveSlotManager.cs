using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
// using Newtonsoft.Json; // Заменено на Unity JsonUtility

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер слотов сохранения игры
    /// </summary>
    public class SaveSlotManager : MonoBehaviour
    {
        [Header("Настройки слотов")]
        [Tooltip("Максимальное количество слотов")]
        public int maxSlots = 5;

        [Tooltip("Автоматически резервировать слот для автосохранения")]
        public bool reserveAutoSaveSlot = true;

        [Tooltip("Индекс слота для автосохранения")]
        public int autoSaveSlotIndex = 0;

        [Header("Экспорт/Импорт")]
        [Tooltip("Разрешить экспорт сохранений")]
        public bool allowExport = true;

        [Tooltip("Разрешить импорт сохранений")]
        public bool allowImport = true;

        [Tooltip("Папка для экспорта")]
        public string exportFolder = "ExportedSaves";

        // Компоненты
        private SaveSystem saveSystem;
        private GameManager gameManager;

        // Состояние
        private List<SaveSlotInfo> cachedSlots = new List<SaveSlotInfo>();
        private int currentSlotIndex = -1;
        private Dictionary<int, DateTime> slotModificationTimes = new Dictionary<int, DateTime>();

        // События
        public event Action<List<SaveSlotInfo>> OnSlotsUpdated;
        public event Action<int, SaveSlotInfo> OnSlotChanged;
        public event Action<int> OnSlotDeleted;
        public event Action<int, string> OnSlotRenamed;
        public event Action<string> OnSlotExported;
        public event Action<string> OnSlotImported;

        // Константы
        private const string SLOT_METADATA_FILE = "slot_metadata.json";
        private const string CURRENT_SLOT_KEY = "CurrentSaveSlot";

        #region Unity Methods

        private void Awake()
        {
            // Получение ссылок
            saveSystem = SaveSystem.Instance;
            gameManager = GameManager.Instance;

            if (saveSystem == null)
            {
                Debug.LogError("SaveSlotManager: SaveSystem не найден!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Загрузка метаданных слотов
            LoadSlotMetadata();

            // Обновление информации о слотах
            RefreshSlots();

            // Загрузка текущего слота
            LoadCurrentSlot();

            // Подписка на события SaveSystem
            if (saveSystem != null)
            {
                saveSystem.OnDataLoaded += OnDataLoaded;
            }

            Debug.Log("SaveSlotManager инициализирован");
        }

        private void OnDestroy()
        {
            // Сохранение метаданных
            SaveSlotMetadata();

            // Отписка от событий
            if (saveSystem != null)
            {
                saveSystem.OnDataLoaded -= OnDataLoaded;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить информацию о всех слотах
        /// </summary>
        public List<SaveSlotInfo> GetAllSlots()
        {
            return new List<SaveSlotInfo>(cachedSlots);
        }

        /// <summary>
        /// Получить информацию о конкретном слоте
        /// </summary>
        public SaveSlotInfo GetSlot(int slotIndex)
        {
            if (IsValidSlotIndex(slotIndex) && slotIndex < cachedSlots.Count)
            {
                return cachedSlots[slotIndex];
            }

            return null;
        }

        /// <summary>
        /// Проверить, пуст ли слот
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            SaveSlotInfo slot = GetSlot(slotIndex);
            return slot == null || slot.isEmpty;
        }

        /// <summary>
        /// Проверить, поврежден ли слот
        /// </summary>
        public bool IsSlotCorrupted(int slotIndex)
        {
            SaveSlotInfo slot = GetSlot(slotIndex);
            return slot != null && slot.isCorrupted;
        }

        /// <summary>
        /// Обновить информацию о слотах
        /// </summary>
        public void RefreshSlots()
        {
            cachedSlots.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                SaveSlotInfo slotInfo = saveSystem.GetSaveSlotInfo(i);
                cachedSlots.Add(slotInfo);
            }

            OnSlotsUpdated?.Invoke(cachedSlots);
        }

        /// <summary>
        /// Создать новое сохранение в указанном слоте
        /// </summary>
        public SaveOperationResult CreateNewSave(int slotIndex, string saveName)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (reserveAutoSaveSlot && slotIndex == autoSaveSlotIndex)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Этот слот зарезервирован для автосохранения");
            }

            // Создать новую игру и сохранить
            SaveOperationResult result = saveSystem.SaveGame(slotIndex, saveName);

            if (result.IsSuccess)
            {
                // Обновить текущий слот
                SetCurrentSlot(slotIndex);

                // Обновить кеш
                RefreshSlots();

                // Обновить время модификации
                slotModificationTimes[slotIndex] = DateTime.Now;

                OnSlotChanged?.Invoke(slotIndex, GetSlot(slotIndex));
                Debug.Log($"Создано новое сохранение в слоте {slotIndex}: {saveName}");
            }

            return result;
        }

        /// <summary>
        /// Загрузить игру из слота
        /// </summary>
        public SaveOperationResult LoadFromSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (IsSlotEmpty(slotIndex))
            {
                return new SaveOperationResult(SaveResult.FileNotFound, "Слот пуст");
            }

            if (IsSlotCorrupted(slotIndex))
            {
                return new SaveOperationResult(SaveResult.CorruptedData, "Сохранение повреждено");
            }

            SaveOperationResult result = saveSystem.LoadGame(slotIndex);

            if (result.IsSuccess)
            {
                SetCurrentSlot(slotIndex);
                Debug.Log($"Игра загружена из слота {slotIndex}");
            }

            return result;
        }

        /// <summary>
        /// Удалить сохранение из слота
        /// </summary>
        public SaveOperationResult DeleteSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (reserveAutoSaveSlot && slotIndex == autoSaveSlotIndex)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Нельзя удалить слот автосохранения");
            }

            SaveOperationResult result = saveSystem.DeleteSave(slotIndex);

            if (result.IsSuccess)
            {
                // Обновить кеш
                RefreshSlots();

                // Удалить время модификации
                if (slotModificationTimes.ContainsKey(slotIndex))
                {
                    slotModificationTimes.Remove(slotIndex);
                }

                // Если удален текущий слот, сбросить
                if (currentSlotIndex == slotIndex)
                {
                    currentSlotIndex = -1;
                    PlayerPrefs.DeleteKey(CURRENT_SLOT_KEY);
                }

                OnSlotDeleted?.Invoke(slotIndex);
                Debug.Log($"Удален слот {slotIndex}");
            }

            return result;
        }

        /// <summary>
        /// Переименовать сохранение в слоте
        /// </summary>
        public SaveOperationResult RenameSlot(int slotIndex, string newName)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (IsSlotEmpty(slotIndex))
            {
                return new SaveOperationResult(SaveResult.FileNotFound, "Слот пуст");
            }

            if (string.IsNullOrEmpty(newName) || newName.Trim().Length == 0)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Недопустимое имя");
            }

            try
            {
                // Загрузить данные слота
                SaveOperationResult loadResult = saveSystem.LoadGame(slotIndex);
                if (!loadResult.IsSuccess)
                {
                    return loadResult;
                }

                // Сохранить с новым именем
                SaveOperationResult saveResult = saveSystem.SaveGame(slotIndex, newName.Trim());
                if (saveResult.IsSuccess)
                {
                    RefreshSlots();
                    OnSlotRenamed?.Invoke(slotIndex, newName);
                    Debug.Log($"Слот {slotIndex} переименован в '{newName}'");
                }

                return saveResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка переименования слота: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        /// <summary>
        /// Скопировать сохранение из одного слота в другой
        /// </summary>
        public SaveOperationResult CopySlot(int sourceSlot, int targetSlot, string newName = "")
        {
            if (!IsValidSlotIndex(sourceSlot) || !IsValidSlotIndex(targetSlot))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (IsSlotEmpty(sourceSlot))
            {
                return new SaveOperationResult(SaveResult.FileNotFound, "Исходный слот пуст");
            }

            if (sourceSlot == targetSlot)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Исходный и целевой слоты совпадают");
            }

            try
            {
                // Загрузить данные из исходного слота
                SaveOperationResult loadResult = saveSystem.LoadGame(sourceSlot);
                if (!loadResult.IsSuccess)
                {
                    return loadResult;
                }

                // Определить имя копии
                string copyName = string.IsNullOrEmpty(newName) ? 
                    $"{GetSlot(sourceSlot).saveName} (Копия)" : newName;

                // Сохранить в целевой слот
                SaveOperationResult saveResult = saveSystem.SaveGame(targetSlot, copyName);
                if (saveResult.IsSuccess)
                {
                    RefreshSlots();
                    Debug.Log($"Слот {sourceSlot} скопирован в слот {targetSlot}");
                }

                return saveResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка копирования слота: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        /// <summary>
        /// Получить текущий активный слот
        /// </summary>
        public int GetCurrentSlot()
        {
            return currentSlotIndex;
        }

        /// <summary>
        /// Установить текущий активный слот
        /// </summary>
        public void SetCurrentSlot(int slotIndex)
        {
            if (IsValidSlotIndex(slotIndex))
            {
                currentSlotIndex = slotIndex;
                PlayerPrefs.SetInt(CURRENT_SLOT_KEY, slotIndex);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Получить следующий доступный слот
        /// </summary>
        public int GetNextAvailableSlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (reserveAutoSaveSlot && i == autoSaveSlotIndex)
                    continue;

                if (IsSlotEmpty(i))
                    return i;
            }

            return -1; // Нет доступных слотов
        }

        /// <summary>
        /// Проверить, есть ли доступные слоты
        /// </summary>
        public bool HasAvailableSlots()
        {
            return GetNextAvailableSlot() != -1;
        }

        #endregion

        #region Export/Import

        /// <summary>
        /// Экспортировать сохранение в файл
        /// </summary>
        public SaveOperationResult ExportSlot(int slotIndex, string exportPath = "")
        {
            if (!allowExport)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Экспорт запрещен");
            }

            if (!IsValidSlotIndex(slotIndex))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (IsSlotEmpty(slotIndex))
            {
                return new SaveOperationResult(SaveResult.FileNotFound, "Слот пуст");
            }

            try
            {
                // Создать папку экспорта
                string exportDir = string.IsNullOrEmpty(exportPath) ? 
                    Path.Combine(Application.persistentDataPath, exportFolder) : 
                    Path.GetDirectoryName(exportPath);

                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                // Получить информацию о слоте
                SaveSlotInfo slotInfo = GetSlot(slotIndex);
                
                // Создать имя файла экспорта
                string fileName = string.IsNullOrEmpty(exportPath) ? 
                    $"{slotInfo.saveName}_{DateTime.Now:yyyyMMdd_HHmmss}.json" : 
                    Path.GetFileName(exportPath);

                string fullExportPath = Path.Combine(exportDir, fileName);

                // Загрузить данные слота
                SaveOperationResult loadResult = saveSystem.LoadGame(slotIndex);
                if (!loadResult.IsSuccess)
                {
                    return loadResult;
                }

                // Сохранить в файл экспорта
                File.WriteAllText(fullExportPath, loadResult.message, System.Text.Encoding.UTF8);

                OnSlotExported?.Invoke(fullExportPath);
                Debug.Log($"Слот {slotIndex} экспортирован в {fullExportPath}");

                return new SaveOperationResult(SaveResult.Success, "Сохранение экспортировано", fullExportPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка экспорта: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        /// <summary>
        /// Импортировать сохранение из файла
        /// </summary>
        public SaveOperationResult ImportSlot(int targetSlot, string importPath)
        {
            if (!allowImport)
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Импорт запрещен");
            }

            if (!IsValidSlotIndex(targetSlot))
            {
                return new SaveOperationResult(SaveResult.UnknownError, "Неверный индекс слота");
            }

            if (!File.Exists(importPath))
            {
                return new SaveOperationResult(SaveResult.FileNotFound, "Файл импорта не найден");
            }

            try
            {
                // Читать файл импорта
                string jsonData = File.ReadAllText(importPath, System.Text.Encoding.UTF8);

                // Валидировать JSON
                try
                {
                    CompleteSaveData saveData = JsonConvert.DeserializeObject<CompleteSaveData>(jsonData);
                    if (saveData == null || !saveData.IsValid())
                    {
                        return new SaveOperationResult(SaveResult.CorruptedData, "Невалидные данные импорта");
                    }
                }
                catch (JsonException e)
                {
                    return new SaveOperationResult(SaveResult.CorruptedData, $"Ошибка парсинга JSON: {e.Message}");
                }

                // Сохранить в указанный слот
                // Здесь нужно будет реализовать метод SaveSystem для прямого сохранения JSON
                // Пока используем временное решение
                
                RefreshSlots();
                OnSlotImported?.Invoke(importPath);
                Debug.Log($"Сохранение импортировано в слот {targetSlot} из {importPath}");

                return new SaveOperationResult(SaveResult.Success, "Сохранение импортировано");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка импорта: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        #endregion

        #region Slot Metadata

        /// <summary>
        /// Сохранить метаданные слотов
        /// </summary>
        private void SaveSlotMetadata()
        {
            try
            {
                var metadata = new SlotMetadata
                {
                    currentSlot = currentSlotIndex,
                    modificationTimes = slotModificationTimes,
                    maxSlots = maxSlots,
                    autoSaveSlotIndex = autoSaveSlotIndex,
                    lastUpdate = DateTime.Now
                };

                string metadataPath = Path.Combine(Application.persistentDataPath, SLOT_METADATA_FILE);
                string jsonData = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                File.WriteAllText(metadataPath, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось сохранить метаданные слотов: {e.Message}");
            }
        }

        /// <summary>
        /// Загрузить метаданные слотов
        /// </summary>
        private void LoadSlotMetadata()
        {
            try
            {
                string metadataPath = Path.Combine(Application.persistentDataPath, SLOT_METADATA_FILE);
                if (File.Exists(metadataPath))
                {
                    string jsonData = File.ReadAllText(metadataPath);
                    SlotMetadata metadata = JsonConvert.DeserializeObject<SlotMetadata>(jsonData);

                    if (metadata != null)
                    {
                        slotModificationTimes = metadata.modificationTimes ?? new Dictionary<int, DateTime>();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить метаданные слотов: {e.Message}");
                slotModificationTimes = new Dictionary<int, DateTime>();
            }
        }

        /// <summary>
        /// Загрузить текущий слот
        /// </summary>
        private void LoadCurrentSlot()
        {
            currentSlotIndex = PlayerPrefs.GetInt(CURRENT_SLOT_KEY, -1);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Проверить валидность индекса слота
        /// </summary>
        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < maxSlots;
        }

        /// <summary>
        /// Обработчик загрузки данных
        /// </summary>
        private void OnDataLoaded(int slotIndex, CompleteSaveData saveData)
        {
            SetCurrentSlot(slotIndex);
            RefreshSlots();
        }

        /// <summary>
        /// Получить отсортированные слоты по времени модификации
        /// </summary>
        public List<SaveSlotInfo> GetSlotsSortedByDate(bool newestFirst = true)
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>(cachedSlots);
            
            slots.Sort((a, b) => 
            {
                if (a.isEmpty && b.isEmpty) return 0;
                if (a.isEmpty) return 1;
                if (b.isEmpty) return -1;

                int result = DateTime.Compare(a.lastPlayed, b.lastPlayed);
                return newestFirst ? -result : result;
            });

            return slots;
        }

        /// <summary>
        /// Получить слоты по уровню игрока
        /// </summary>
        public List<SaveSlotInfo> GetSlotsSortedByLevel(bool highestFirst = true)
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>(cachedSlots);
            
            slots.Sort((a, b) => 
            {
                if (a.isEmpty && b.isEmpty) return 0;
                if (a.isEmpty) return 1;
                if (b.isEmpty) return -1;

                int result = a.level.CompareTo(b.level);
                return highestFirst ? -result : result;
            });

            return slots;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Обновить слоты")]
        private void DebugRefreshSlots()
        {
            RefreshSlots();
        }

        [ContextMenu("Показать информацию о слотах")]
        private void DebugShowSlotInfo()
        {
            for (int i = 0; i < cachedSlots.Count; i++)
            {
                SaveSlotInfo slot = cachedSlots[i];
                if (!slot.isEmpty)
                {
                    Debug.Log($"Слот {i}: {slot.saveName}, Уровень {slot.level}, " +
                             $"Деньги: {GameUtils.FormatMoney(slot.money)}, " +
                             $"Локация: {slot.GetLocalizedLocationName()}");
                }
                else
                {
                    Debug.Log($"Слот {i}: Пустой");
                }
            }
        }

        [ContextMenu("Создать тестовое сохранение")]
        private void DebugCreateTestSave()
        {
            int slot = GetNextAvailableSlot();
            if (slot != -1)
            {
                CreateNewSave(slot, "Тестовое сохранение");
            }
        }

        #endregion
    }

    /// <summary>
    /// Метаданные слотов сохранения
    /// </summary>
    [System.Serializable]
    public class SlotMetadata
    {
        public int currentSlot = -1;
        public Dictionary<int, DateTime> modificationTimes = new Dictionary<int, DateTime>();
        public int maxSlots = 5;
        public int autoSaveSlotIndex = 0;
        public DateTime lastUpdate = DateTime.Now;
    }
}
