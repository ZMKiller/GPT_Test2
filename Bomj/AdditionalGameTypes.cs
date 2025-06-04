using System;
using System.Collections.Generic;
using UnityEngine;


namespace HomelessToMillionaire

{
    // Extra enums missing from the original scripts
    public enum EducationCategory
    {
        School,
        University,
        SelfStudy
    }




    public enum NotificationPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Center
    }

    public enum TooltipAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }


    public enum ActivityType
    {
        Work,
        Education,
        Rest,
        Exercise
    }


    public enum LoadingState
    {
        Loading,
        Ready,
        Error
    }

    public enum DataFormat
    {
        JSON,
        Binary,
        XML
    }

    // Extra POCO classes missing from the project
    [Serializable]
    public class ShopSystemData
    {
        public List<ShopItem> shopItems = new List<ShopItem>();
        public int currentInflationLevel;
    }

    [Serializable]
    public class ProgressionData
    {
        public List<SkillUpgradeEventData> skillUpgrades = new List<SkillUpgradeEventData>();
        public List<ShopItem> purchasedItems = new List<ShopItem>();
        public List<JobType> completedJobs = new List<JobType>();
        public List<EducationType> completedCourses = new List<EducationType>();
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<StatType, float> currentStats = new Dictionary<StatType, float>();
    }

    [Serializable]
    public class SettingsOption
    {
        public string optionName;
        public object value;
        public Action<object> OnValueChanged;
    }

    [Serializable]
    public class CombatUIData
    {
        public bool isVisible;
        public string currentLog;
        public float healthBarValue;
        public float enemyHealthBarValue;
    }

    [Serializable]
    public class EducationSystemData
    {
        public List<EducationCourse> availableCourses = new List<EducationCourse>();
        public Dictionary<EducationType, int> completedLevels = new Dictionary<EducationType, int>();
    }


    [Serializable]
    public class CompletedJobData
    {
        public string title;
        public string jobType;
        public double payment;
        public long completionTime;
    }

    [Serializable]
    public class PurchasedItemData
    {
        public string name;
        public string category;
        public long purchaseTime;
    }

}
