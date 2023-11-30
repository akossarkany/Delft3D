using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Events;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [Serializable]
    public class FileTypeEvent
    {
        public string Extension;
        public UnityEvent<string> FileReceived;
    }
    
    public class FileOpenTypeManager : MonoBehaviour
    {
        [SerializeField] private StringEvent fileOpenEvent;
        // [SerializeField] private Dictionary<string, UnityEvent<string>> fileTypeEvents = new();
        [SerializeField] private List<FileTypeEvent> fileTypeEvents;
        private void OnEnable()
        {
            fileOpenEvent.AddListenerStarted(ProcessFile);
        }

        private void OnDisable()
        {
            fileOpenEvent.RemoveListenerStarted(ProcessFile);
        }

        public void ProcessFile(string file) //todo: this currently does not support multi select
        {
            if (file.EndsWith(','))
                file = file.Remove(file.Length - 1);
                
            string fileExtension = Path.GetExtension(file).ToLower();
            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);
            
            var fileTypeEvent = fileTypeEvents.FirstOrDefault(fte => fte.Extension == fileExtension);
            
            if(fileTypeEvent != null)
            {
                fileTypeEvent.FileReceived.Invoke(file);
            }
            else
            {
                print("file type " + fileExtension + " does not have an associated processing function");
            }
        }

        public void AddLayerScriptToObj(GameObject parsedObj)
        {
            var objLayer = parsedObj.AddComponent<ObjectLayer>();
            parsedObj.AddComponent<MeshCollider>();
            FindObjectOfType<LayerManager>().RefreshLayerList(); //todo remove findObjectOfType
            objLayer.UI.Select();
        }
    }
}
