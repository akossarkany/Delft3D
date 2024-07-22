using System;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Netherlands3D.Twin.Layers;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Projects
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Project", fileName = "Project", order = 0)]
    [Serializable]
    public class ProjectData : ScriptableObject
    {
        [JsonIgnore] private static ProjectData current;
        [JsonIgnore] public static ProjectData Current => current;

        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        public const string DefaultFileName = "NL3D_Project_";
        public const string ProjectFileExtension = ".nl3d";
        public const string ProjectJsonFileNameInZip = "project.json";

        [Header("Serialized data")] public int Version = 1;
        public string SavedTimestamp = "";
        public string UUID = "";
        public double[] CameraPosition = new double[3]; //X, Y, Z,- Assume RD for now
        public double[] CameraRotation = new double[3];

        [SerializeField] private RootLayer rootLayer;
        [JsonIgnore] public PrefabLibrary PrefabLibrary;
        
        [JsonIgnore]
        public RootLayer RootLayer
        {
            get => rootLayer;
            private set
            {
                rootLayer = value;
                rootLayer.ReconstructParentsRecursive();
            }
        }
        
        private ProjectDataHandler projectDataHandler;
        private ZipOutputStream zipOutputStream;
        private string lastSavePath = "";

        [NonSerialized] private bool isDirty = false;

        [JsonIgnore]
        public bool IsDirty
        {
            get => isDirty;
            set { isDirty = value; }
        }

        [NonSerialized] public UnityEvent<ProjectData> OnDataChanged = new();
        [NonSerialized] public UnityEvent<LayerNL3DBase> LayerAdded = new();
        [NonSerialized] public UnityEvent<LayerNL3DBase> LayerDeleted = new();

        [NonSerialized] private JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            // PreserveReferencesHandling = PreserveReferencesHandling.Objects, //todo: still needed?
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        public void RefreshUUID()
        {
            UUID = Guid.NewGuid().ToString();
        }

        public void CopyFrom(ProjectData project)
        {
            // Explicit copy of fields. Will be more complex once bin. files are saved
            Debug.Log("replacing project " + UUID + " with: " + project.UUID);
            Version = project.Version;
            SavedTimestamp = project.SavedTimestamp;
            UUID = project.UUID;
            CameraPosition = project.CameraPosition;
            CameraRotation = project.CameraRotation;
            RootLayer = project.RootLayer;

            IsDirty = true;
        }

        public void CopyUndoFrom(ProjectData project)
        {
            //TODO: Implement undo copy with just the data we want to move between undo/redo states
            //Now we simply copy everything
            CopyFrom(project);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                OnDataChanged.Invoke(this);
        }

#endif

        public void LoadFromFile(string fileName)
        {
            Debug.Log("Loading project file: " + fileName);
            RootLayer.DestroyLayer();

            // Open the zip file
            using (FileStream fs = File.OpenRead(Path.Combine(Application.persistentDataPath, fileName)))
            {
                //Extract specific project.json from zip using CsharpLib
                using ZipFile zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile) continue;
                    if (zipEntry.Name == ProjectJsonFileNameInZip)
                    {
                        using Stream zipStream = zf.GetInputStream(zipEntry);
                        using StreamReader sr = new(zipStream);
                        string json = sr.ReadToEnd();
                        
                        Debug.Log("preparse: " + rootLayer.ChildrenLayers.Count);
                        var newProject = ScriptableObject.CreateInstance<ProjectData>();
                        JsonConvert.PopulateObject(json, newProject, serializerSettings);
                        // ProjectData tempProject = JsonConvert.DeserializeObject<ProjectData>(json, serializerSettings);
                        current = newProject;
                        Debug.Log("postparse: " + rootLayer.ChildrenLayers.Count);
                        rootLayer.ReconstructParentsRecursive();
                        CopyFrom(newProject);
                    }
                    else
                    {
                        //TODO: Future Project files can have more files in the zip, like meshes and textures etc.
                        Debug.Log("Other file found in Project zip. Ignoring for now.");
                    }
                }
            }

            IsDirty = true;
            OnDataChanged.Invoke(this);
        }

        public void SaveAsFile(ProjectDataHandler projectDataHandler)
        {
            RefreshUUID();

            this.projectDataHandler = projectDataHandler;

            // Set the timestamp when the data was saved
            SavedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var readableTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            // Start the zip output stream
            lastSavePath = Application.persistentDataPath + $"/{DefaultFileName}{readableTimeStamp}{ProjectFileExtension}";
            zipOutputStream = new ZipOutputStream(File.Create(lastSavePath));
            zipOutputStream.SetLevel(9); // 0-9 where 9 means best compression

            var jsonProject = JsonConvert.SerializeObject(this, serializerSettings);
            var entry = new ZipEntry(ProjectJsonFileNameInZip);
            zipOutputStream.PutNextEntry(entry);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonProject.ToString());
            zipOutputStream.Write(jsonBytes, 0, jsonBytes.Length);

            // For now we can directly download the zip file (in the future we want to append more files to the zip, like meshes and textures etc.)
            FinishProjectFile();
        }

        private void FinishProjectFile()
        {
            // Finish the zip
            zipOutputStream.Finish();
            zipOutputStream.Close();

            IsDirty = false;

            // Make sure indexedDB is synced
#if !UNITY_EDITOR && UNITY_WEBGL
            SyncFilesToIndexedDB(projectDataHandler.name, "ProjectSavedToIndexedDB");

#elif UNITY_EDITOR
            //Request using file write dialog of unity editor where to copy the file from lastSavePath path
            var fileName = Path.GetFileNameWithoutExtension(lastSavePath);
            var fileExtention = Path.GetExtension(lastSavePath).Replace(".", "");
            var fileTargetPath = EditorUtility.SaveFilePanel("Save project", Application.persistentDataPath, fileName, fileExtention);
            if (fileTargetPath.Length > 0)
            {
                File.Copy(lastSavePath, fileTargetPath, true);
            }

            //Open the folder where the file is saved
            EditorUtility.RevealInFinder(fileTargetPath);

#endif
        }

        public void AppendFileToZip(string fileName)
        {
            var persistentDataPath = Application.persistentDataPath + "/" + fileName;
            Debug.Log("Appending file to zip: " + persistentDataPath);

            byte[] buffer = new byte[4096];
            var randomFileTag = DateTime.Now.ToString("yyyyMMddHHmmss");
            ZipEntry entry = new ZipEntry(randomFileTag + "_" + fileName);
            zipOutputStream.PutNextEntry(entry);

            using FileStream fs = File.OpenRead(persistentDataPath);
            StreamUtils.Copy(fs, zipOutputStream, buffer);
        }

        public void ProjectSavedToIndexedDB()
        {
            var fileName = Path.GetFileName(lastSavePath);
            DownloadFromIndexedDB($"{fileName}", projectDataHandler.name, "DownloadedProject");
        }

        public void AddStandardLayer(LayerNL3DBase layer)
        {
            LayerAdded.Invoke(layer);
        }

        public static void AddReferenceLayer(ReferencedLayer referencedLayer)
        {
            var referenceName = referencedLayer.name.Replace("(Clone)", "").Trim();

            var proxyLayer = new ReferencedProxyLayer(referenceName, referencedLayer);
            referencedLayer.ReferencedProxy = proxyLayer;
        }

        public void RemoveLayer(LayerNL3DBase layer)
        {
            LayerDeleted.Invoke(layer);
        }

        public static void SetCurrentProject(ProjectData initialProjectTemplate)
        {
            current = initialProjectTemplate;
            current.RootLayer = new RootLayer("RootLayer");
        }
    }
}