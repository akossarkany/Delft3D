using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public class QualitySettings : MonoBehaviour
    {
        public static readonly UnityEvent<QualityLevel> qualityLevelChanged = new();
        private static readonly string[] defaultToHighQualityVendorNames = new[] { "nvidia", "apple" };
        private static readonly string[] defaultToLowQualityVendorNames = new[] { "intel" };

        private const string QUALITY_SETTINGS_KEY = "QualitySettings";

        private void Awake()
        {
            Debug.Log(SystemInfo.graphicsDeviceName);
            Debug.Log(SystemInfo.graphicsDeviceType);
            Debug.Log(SystemInfo.graphicsDeviceVendor);
            Debug.Log(SystemInfo.graphicsDeviceVersion);

            InitializeQualitySettings();
        }

        private static void InitializeQualitySettings()
        {
            if (PlayerPrefs.HasKey(QUALITY_SETTINGS_KEY))
            {
                var savedQualitySettings = (QualityLevel)PlayerPrefs.GetInt(QUALITY_SETTINGS_KEY);
                SetGraphicsQuality(savedQualitySettings, false);
                return; // if the user set something specifically, use this instead of defaults0
            }

            var initialQualitySettings = QualityLevel.Medium;

            var graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
            foreach (var vendor in defaultToHighQualityVendorNames)
            {
                if (!graphicsDeviceVendor.ToLower().Contains(vendor))
                    continue;
                initialQualitySettings = QualityLevel.High;
            }

            foreach (var vendor in defaultToLowQualityVendorNames)
            {
                if (!graphicsDeviceVendor.ToLower().Contains(vendor))
                    continue;

                initialQualitySettings = QualityLevel.Low;
            }

            SetGraphicsQuality(initialQualitySettings, false);
        }

        public static void SetGraphicsQuality(QualityLevel level, bool saveSetting)
        {
            Debug.Log("setting ql to : " + level);
            UnityEngine.QualitySettings.SetQualityLevel((int)level);

            if (saveSetting)
                PlayerPrefs.SetInt(QUALITY_SETTINGS_KEY, (int)level);
            
            qualityLevelChanged.Invoke(level);
        }
    }
}