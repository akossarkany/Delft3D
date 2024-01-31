﻿using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Feature", fileName = "Feature", order = 0)]
    public class Feature : ScriptableObject, ISimpleJsonMapper
    {
        public string Id;
        public string Title;
        public string Caption;
        public ScriptableObject configuration;

        [SerializeField] private bool isEnabled;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                var wasEnabled = isEnabled;
                isEnabled = value;
                switch (wasEnabled)
                {
                    case false when isEnabled:
                        OnEnable.Invoke();
                        break;
                    case true when isEnabled == false:
                        OnDisable.Invoke();
                        break;
                }
            }
        }

        public UnityEvent OnEnable = new();
        public UnityEvent OnDisable = new();

        public void Populate(JSONNode jsonNode)
        {
            IsEnabled = jsonNode["enabled"];
            (configuration as IConfiguration)?.Populate(jsonNode["configuration"]);
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject
            {
                ["enabled"] = isEnabled,
                ["configuration"] = (configuration as IConfiguration)?.ToJsonNode()
            };
        }
    }
}