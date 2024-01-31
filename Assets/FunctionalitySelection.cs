using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Features;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Toggle))]
    public class FunctionalitySelection : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text caption;
       
        private Feature feature;
        private Toggle toggle;
        public Toggle Toggle { get => toggle; private set => toggle = value; }
       
        public void Init(Feature feature)
        {
            this.feature = feature;

            title.text = feature.Title;
            caption.text = feature.Caption;

            Toggle = GetComponent<Toggle>();
            Toggle.isOn = this.feature.IsEnabled;
        }
    }
}
