using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Панель настроек игры
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Звуковые настройки")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteToggle;
        
        [Header("Графические настройки")]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Slider fpsLimitSlider;
        [SerializeField] private Text fpsLimitText;
        
        [Header("Игровые настройки")]
        [SerializeField] private Toggle autoSaveToggle;
        [SerializeField] private Slider autoSaveIntervalSlider;
        [SerializeField] private Text autoSaveIntervalText;
        [SerializeField] private Toggle showNotificationsToggle;
        [SerializeField] private Toggle warningAnimationsToggle;
        
        [Header("Управление")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;
        
        [Header("Информация")]
        [SerializeField] private Text versionText;
        [SerializeField] private Text statisticsText;
        
        [Header("Анимация")]
        [SerializeField] private Animator panelAnimator;
        [SerializeField] private CanvasGroup canvasGroup;
        
        // Настройки по умолчанию
        private const float DEFAULT_MASTER_VOLUME = 0.8f;
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SFX_VOLUME = 0.8f;
        private const int DEFAULT_QUALITY = 2;
        private const bool DEFAULT_FULLSCREEN = true;
        private const int DEFAULT_FPS_LIMIT = 60;
        private const bool DEFAULT_AUTO_SAVE = true;
        private const float DEFAULT_AUTO_SAVE_INTERVAL = 60f;
        private const bool DEFAULT_SHOW_NOTIFICATIONS = true;
        private const bool DEFAULT_WARNING_ANIMATIONS = true;
        
        // Текущие настройки (для отмены изменений)
        private SettingsData currentSettings;
        private SettingsData tempSettings;
        
        // Разрешения экрана
        private Resolution[] resolutions;

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
            LoadSettings();
        }

        private void Start()
        {
            SetupEventHandlers();
            PopulateDropdowns();
            ApplySettingsToUI();
            UpdateInfoDisplays();
        }

        private void OnEnable()
        {
            StartCoroutine(FadeIn());
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Получить CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Инициализировать настройки
            currentSettings = new SettingsData();
            tempSettings = new SettingsData();

            // Получить разрешения экрана
            resolutions = Screen.resolutions;
        }

        /// <summary>
        /// Настроить обработчики событий
        /// </summary>
        private void SetupEventHandlers()
        {
            // Звуковые настройки
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteToggled);

            // Графические настройки
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
            
            if (fpsLimitSlider != null)
                fpsLimitSlider.onValueChanged.AddListener(OnFPSLimitChanged);

            // Игровые настройки
            if (autoSaveToggle != null)
                autoSaveToggle.onValueChanged.AddListener(OnAutoSaveToggled);
            
            if (autoSaveIntervalSlider != null)
                autoSaveIntervalSlider.onValueChanged.AddListener(OnAutoSaveIntervalChanged);
            
            if (showNotificationsToggle != null)
                showNotificationsToggle.onValueChanged.AddListener(OnShowNotificationsToggled);
            
            if (warningAnimationsToggle != null)
                warningAnimationsToggle.onValueChanged.AddListener(OnWarningAnimationsToggled);

            // Кнопки управления
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
            
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplyClicked);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        /// <summary>
        /// Заполнить выпадающие списки
        /// </summary>
        private void PopulateDropdowns()
        {
            // Качество графики
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            }

            // Разрешения экрана
            if (resolutionDropdown != null && resolutions != null)
            {
                resolutionDropdown.ClearOptions();
                var resolutionOptions = new System.Collections.Generic.List<string>();
                
                foreach (var resolution in resolutions)
                {
                    string resolutionString = $"{resolution.width} x {resolution.height} @ {resolution.refreshRate}Hz";
                    resolutionOptions.Add(resolutionString);
                }
                
                resolutionDropdown.AddOptions(resolutionOptions);
            }
        }

        #endregion

        #region Загрузка и сохранение настроек

        /// <summary>
        /// Загрузить настройки
        /// </summary>
        private void LoadSettings()
        {
            // Звуковые настройки
            currentSettings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", DEFAULT_MASTER_VOLUME);
            currentSettings.musicVolume = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
            currentSettings.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", DEFAULT_SFX_VOLUME);
            currentSettings.isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

            // Графические настройки
            currentSettings.qualityLevel = PlayerPrefs.GetInt("QualityLevel", DEFAULT_QUALITY);
            currentSettings.resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", GetCurrentResolutionIndex());
            currentSettings.isFullscreen = PlayerPrefs.GetInt("IsFullscreen", DEFAULT_FULLSCREEN ? 1 : 0) == 1;
            currentSettings.fpsLimit = PlayerPrefs.GetInt("FPSLimit", DEFAULT_FPS_LIMIT);

            // Игровые настройки
            currentSettings.autoSave = PlayerPrefs.GetInt("AutoSave", DEFAULT_AUTO_SAVE ? 1 : 0) == 1;
            currentSettings.autoSaveInterval = PlayerPrefs.GetFloat("AutoSaveInterval", DEFAULT_AUTO_SAVE_INTERVAL);
            currentSettings.showNotifications = PlayerPrefs.GetInt("ShowNotifications", DEFAULT_SHOW_NOTIFICATIONS ? 1 : 0) == 1;
            currentSettings.warningAnimations = PlayerPrefs.GetInt("WarningAnimations", DEFAULT_WARNING_ANIMATIONS ? 1 : 0) == 1;

            // Копировать в временные настройки
            tempSettings = currentSettings.Copy();
        }

        /// <summary>
        /// Сохранить настройки
        /// </summary>
        private void SaveSettings()
        {
            // Звуковые настройки
            PlayerPrefs.SetFloat("MasterVolume", currentSettings.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", currentSettings.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", currentSettings.sfxVolume);
            PlayerPrefs.SetInt("IsMuted", currentSettings.isMuted ? 1 : 0);

            // Графические настройки
            PlayerPrefs.SetInt("QualityLevel", currentSettings.qualityLevel);
            PlayerPrefs.SetInt("ResolutionIndex", currentSettings.resolutionIndex);
            PlayerPrefs.SetInt("IsFullscreen", currentSettings.isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt("FPSLimit", currentSettings.fpsLimit);

            // Игровые настройки
            PlayerPrefs.SetInt("AutoSave", currentSettings.autoSave ? 1 : 0);
            PlayerPrefs.SetFloat("AutoSaveInterval", currentSettings.autoSaveInterval);
            PlayerPrefs.SetInt("ShowNotifications", currentSettings.showNotifications ? 1 : 0);
            PlayerPrefs.SetInt("WarningAnimations", currentSettings.warningAnimations ? 1 : 0);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Применить настройки к UI
        /// </summary>
        private void ApplySettingsToUI()
        {
            // Звуковые настройки
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = tempSettings.masterVolume;
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = tempSettings.musicVolume;
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = tempSettings.sfxVolume;
            
            if (muteToggle != null)
                muteToggle.isOn = tempSettings.isMuted;

            // Графические настройки
            if (qualityDropdown != null)
                qualityDropdown.value = tempSettings.qualityLevel;
            
            if (resolutionDropdown != null)
                resolutionDropdown.value = tempSettings.resolutionIndex;
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = tempSettings.isFullscreen;
            
            if (fpsLimitSlider != null)
                fpsLimitSlider.value = tempSettings.fpsLimit;

            // Игровые настройки
            if (autoSaveToggle != null)
                autoSaveToggle.isOn = tempSettings.autoSave;
            
            if (autoSaveIntervalSlider != null)
                autoSaveIntervalSlider.value = tempSettings.autoSaveInterval;
            
            if (showNotificationsToggle != null)
                showNotificationsToggle.isOn = tempSettings.showNotifications;
            
            if (warningAnimationsToggle != null)
                warningAnimationsToggle.isOn = tempSettings.warningAnimations;

            // Обновить текстовые элементы
            UpdateTextDisplays();
        }

        #endregion

        #region Обработчики изменений

        /// <summary>
        /// Изменение общей громкости
        /// </summary>
        private void OnMasterVolumeChanged(float value)
        {
            tempSettings.masterVolume = value;
            
            if (audioMixer != null)
            {
                float dbValue = value > 0 ? Mathf.Log10(value) * 20 : -80f;
                audioMixer.SetFloat("MasterVolume", dbValue);
            }
        }

        /// <summary>
        /// Изменение громкости музыки
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            tempSettings.musicVolume = value;
            
            if (audioMixer != null)
            {
                float dbValue = value > 0 ? Mathf.Log10(value) * 20 : -80f;
                audioMixer.SetFloat("MusicVolume", dbValue);
            }
        }

        /// <summary>
        /// Изменение громкости звуковых эффектов
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            tempSettings.sfxVolume = value;
            
            if (audioMixer != null)
            {
                float dbValue = value > 0 ? Mathf.Log10(value) * 20 : -80f;
                audioMixer.SetFloat("SFXVolume", dbValue);
            }
        }

        /// <summary>
        /// Переключение отключения звука
        /// </summary>
        private void OnMuteToggled(bool isMuted)
        {
            tempSettings.isMuted = isMuted;
            
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", isMuted ? -80f : 
                    tempSettings.masterVolume > 0 ? Mathf.Log10(tempSettings.masterVolume) * 20 : -80f);
            }
        }

        /// <summary>
        /// Изменение качества графики
        /// </summary>
        private void OnQualityChanged(int qualityIndex)
        {
            tempSettings.qualityLevel = qualityIndex;
        }

        /// <summary>
        /// Изменение разрешения
        /// </summary>
        private void OnResolutionChanged(int resolutionIndex)
        {
            tempSettings.resolutionIndex = resolutionIndex;
        }

        /// <summary>
        /// Переключение полноэкранного режима
        /// </summary>
        private void OnFullscreenToggled(bool isFullscreen)
        {
            tempSettings.isFullscreen = isFullscreen;
        }

        /// <summary>
        /// Изменение лимита FPS
        /// </summary>
        private void OnFPSLimitChanged(float value)
        {
            tempSettings.fpsLimit = Mathf.RoundToInt(value);
            UpdateTextDisplays();
        }

        /// <summary>
        /// Переключение автосохранения
        /// </summary>
        private void OnAutoSaveToggled(bool autoSave)
        {
            tempSettings.autoSave = autoSave;
        }

        /// <summary>
        /// Изменение интервала автосохранения
        /// </summary>
        private void OnAutoSaveIntervalChanged(float value)
        {
            tempSettings.autoSaveInterval = value;
            UpdateTextDisplays();
        }

        /// <summary>
        /// Переключение показа уведомлений
        /// </summary>
        private void OnShowNotificationsToggled(bool showNotifications)
        {
            tempSettings.showNotifications = showNotifications;
        }

        /// <summary>
        /// Переключение предупреждающих анимаций
        /// </summary>
        private void OnWarningAnimationsToggled(bool warningAnimations)
        {
            tempSettings.warningAnimations = warningAnimations;
        }

        #endregion

        #region Обработчики кнопок

        /// <summary>
        /// Обработчик кнопки "Сбросить"
        /// </summary>
        private void OnResetClicked()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Обработчик кнопки "Применить"
        /// </summary>
        private void OnApplyClicked()
        {
            ApplySettings();
        }

        /// <summary>
        /// Обработчик кнопки "Назад"
        /// </summary>
        private void OnBackClicked()
        {
            CancelChanges();
        }

        #endregion

        #region Применение настроек

        /// <summary>
        /// Применить настройки
        /// </summary>
        private void ApplySettings()
        {
            // Копировать временные настройки в текущие
            currentSettings = tempSettings.Copy();

            // Применить графические настройки
            ApplyGraphicsSettings();

            // Применить игровые настройки
            ApplyGameSettings();

            // Сохранить настройки
            SaveSettings();

            // Показать уведомление
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("Настройки применены", NotificationType.Success);
            }
        }

        /// <summary>
        /// Применить графические настройки
        /// </summary>
        private void ApplyGraphicsSettings()
        {
            // Качество графики
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);

            // Разрешение и полноэкранный режим
            if (resolutions != null && currentSettings.resolutionIndex < resolutions.Length)
            {
                Resolution resolution = resolutions[currentSettings.resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, currentSettings.isFullscreen, resolution.refreshRate);
            }

            // Лимит FPS
            Application.targetFrameRate = currentSettings.fpsLimit;
        }

        /// <summary>
        /// Применить игровые настройки
        /// </summary>
        private void ApplyGameSettings()
        {
            // Настройки GameManager
            if (GameManager.Instance != null)
            {
                // Автосохранение (нужно добавить метод в GameManager)
                // GameManager.Instance.SetAutoSave(currentSettings.autoSave, currentSettings.autoSaveInterval);
            }

            // Настройки уведомлений
            if (UIManager.Instance != null && UIManager.Instance.NotificationSystem != null)
            {
                // Включить/выключить уведомления (нужно добавить метод)
                // UIManager.Instance.NotificationSystem.SetEnabled(currentSettings.showNotifications);
            }

            // Настройки предупреждающих анимаций
            if (UIManager.Instance != null && UIManager.Instance.StatsPanel != null)
            {
                UIManager.Instance.StatsPanel.SetWarningAnimations(currentSettings.warningAnimations);
            }
        }

        /// <summary>
        /// Сбросить настройки к значениям по умолчанию
        /// </summary>
        private void ResetToDefaults()
        {
            tempSettings.masterVolume = DEFAULT_MASTER_VOLUME;
            tempSettings.musicVolume = DEFAULT_MUSIC_VOLUME;
            tempSettings.sfxVolume = DEFAULT_SFX_VOLUME;
            tempSettings.isMuted = false;
            tempSettings.qualityLevel = DEFAULT_QUALITY;
            tempSettings.resolutionIndex = GetCurrentResolutionIndex();
            tempSettings.isFullscreen = DEFAULT_FULLSCREEN;
            tempSettings.fpsLimit = DEFAULT_FPS_LIMIT;
            tempSettings.autoSave = DEFAULT_AUTO_SAVE;
            tempSettings.autoSaveInterval = DEFAULT_AUTO_SAVE_INTERVAL;
            tempSettings.showNotifications = DEFAULT_SHOW_NOTIFICATIONS;
            tempSettings.warningAnimations = DEFAULT_WARNING_ANIMATIONS;

            ApplySettingsToUI();
        }

        /// <summary>
        /// Отменить изменения
        /// </summary>
        private void CancelChanges()
        {
            // Восстановить текущие настройки
            tempSettings = currentSettings.Copy();
            ApplySettingsToUI();

            // Закрыть панель настроек
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseSettings();
            }
        }

        #endregion

        #region Обновление UI

        /// <summary>
        /// Обновить текстовые отображения
        /// </summary>
        private void UpdateTextDisplays()
        {
            // FPS лимит
            if (fpsLimitText != null)
            {
                if (tempSettings.fpsLimit <= 0)
                    fpsLimitText.text = "Без ограничений";
                else
                    fpsLimitText.text = $"{tempSettings.fpsLimit} FPS";
            }

            // Интервал автосохранения
            if (autoSaveIntervalText != null)
            {
                int minutes = Mathf.FloorToInt(tempSettings.autoSaveInterval / 60f);
                int seconds = Mathf.FloorToInt(tempSettings.autoSaveInterval % 60f);
                autoSaveIntervalText.text = $"{minutes:D2}:{seconds:D2}";
            }
        }

        /// <summary>
        /// Обновить информационные дисплеи
        /// </summary>
        private void UpdateInfoDisplays()
        {
            // Версия игры
            if (versionText != null)
            {
                versionText.text = $"Версия: {Application.version}";
            }

            // Статистика
            if (statisticsText != null)
            {
                string stats = $"Unity: {Application.unityVersion}\n";
                stats += $"Платформа: {Application.platform}\n";
                stats += $"Разрешение: {Screen.currentResolution.width}x{Screen.currentResolution.height}\n";
                stats += $"Качество: {QualitySettings.names[QualitySettings.GetQualityLevel()]}";
                
                statisticsText.text = stats;
            }
        }

        #endregion

        #region Анимация

        /// <summary>
        /// Анимация появления
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("FadeIn");
            }

            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        #endregion

        #region Утилиты

        /// <summary>
        /// Получить индекс текущего разрешения
        /// </summary>
        private int GetCurrentResolutionIndex()
        {
            if (resolutions == null) return 0;

            Resolution currentResolution = Screen.currentResolution;
            
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == currentResolution.width &&
                    resolutions[i].height == currentResolution.height &&
                    resolutions[i].refreshRate == currentResolution.refreshRate)
                {
                    return i;
                }
            }

            return resolutions.Length - 1; // Возвращаем максимальное разрешение как fallback
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Получить текущие настройки
        /// </summary>
        public SettingsData GetCurrentSettings()
        {
            return currentSettings.Copy();
        }

        /// <summary>
        /// Установить настройки
        /// </summary>
        public void SetSettings(SettingsData settings)
        {
            currentSettings = settings.Copy();
            tempSettings = settings.Copy();
            ApplySettingsToUI();
            ApplySettings();
        }

        #endregion
    }

    /// <summary>
    /// Структура данных настроек
    /// </summary>
    [System.Serializable]
    public class SettingsData
    {
        // Звуковые настройки
        public float masterVolume = 0.8f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public bool isMuted = false;

        // Графические настройки
        public int qualityLevel = 2;
        public int resolutionIndex = 0;
        public bool isFullscreen = true;
        public int fpsLimit = 60;

        // Игровые настройки
        public bool autoSave = true;
        public float autoSaveInterval = 60f;
        public bool showNotifications = true;
        public bool warningAnimations = true;

        /// <summary>
        /// Создать копию настроек
        /// </summary>
        public SettingsData Copy()
        {
            return new SettingsData
            {
                masterVolume = this.masterVolume,
                musicVolume = this.musicVolume,
                sfxVolume = this.sfxVolume,
                isMuted = this.isMuted,
                qualityLevel = this.qualityLevel,
                resolutionIndex = this.resolutionIndex,
                isFullscreen = this.isFullscreen,
                fpsLimit = this.fpsLimit,
                autoSave = this.autoSave,
                autoSaveInterval = this.autoSaveInterval,
                showNotifications = this.showNotifications,
                warningAnimations = this.warningAnimations
            };
        }
    }
}