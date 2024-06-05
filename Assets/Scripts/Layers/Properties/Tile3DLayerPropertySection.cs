using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Tile3DLayerPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField urlInputField;
        [SerializeField] private Image colorFeedbackImage;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color warningColor;

        private Tile3DLayer2 layer;
        public Tile3DLayer2 Layer
        {
            get => layer;
            set
            {
                layer = value;

                if(layer == null || !IsValidURL(layer.URL))
                    return;

                urlInputField.text = layer.URL;
            }
        }

        private void OnEnable()
        {
            urlInputField.onEndEdit.AddListener(HandleURLChange);
        }
        
        private void OnDisable()
        {
            urlInputField.onEndEdit.RemoveListener(HandleURLChange);
        }

        private void HandleURLChange(string newValue)
        {
            var sanitizedURL = SanitizeURL(newValue);
            urlInputField.text = sanitizedURL;

            //Make sure its long enough to contain a domain
            if (!IsValidURL(sanitizedURL))
            {
                colorFeedbackImage.color = warningColor;
                return;
            }        

            colorFeedbackImage.color = defaultColor;
            layer.URL = sanitizedURL;
        }

        private string SanitizeURL(string url)
        {
            //Append https:// if http:// or https:// is not present
            if (url.Length > 5 && !url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            return url;
        }

        private bool IsValidURL(string url)
        {
            if(url.Length < 10)
            {
                return false;
            }

            return true;
        }
    }
}
