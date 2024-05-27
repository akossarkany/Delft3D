using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Netherlands3D.Coordinates;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace Netherlands3D.Twin.Projects
{
    /// <summary>
    /// This class manages the state of the project (undo/redo) and handles saving and loading of the project as a file
    /// </summary>
    public class ProjectDataHandler : MonoBehaviour
    {
        [SerializeField] private ProjectData projectData;

        public List<ProjectData> undoStack = new();
        public List<ProjectData> redoStack = new();

        public int undoStackSize = 10;

        private void Awake() {
            if(projectData == null) {
                Debug.LogError("ProjectData object reference is not set in ProjectStateHandler", this.gameObject);
                return;
            }

            projectData.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnProjectDataChanged(ProjectData project)
        {
            // Add new undo state
            if (undoStack.Count == undoStackSize)
                undoStack.RemoveAt(0);
            
            // Copy the current projectData to a new project instance for our undo history
            var newProject = ScriptableObject.CreateInstance<ProjectData>();
            newProject.CopyFrom(projectData);
            undoStack.Add(newProject);

            // Clear the redo stack
            redoStack.Clear();
        }

        private void Update()
        {
            var ctrlModifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

            // Save shortcut
            if (Input.GetKeyDown(KeyCode.S) && ctrlModifier)
                SaveProject();

            // Undo/redo shortcuts
            if (Input.GetKeyDown(KeyCode.Z) && ctrlModifier)
                Undo();

            if (Input.GetKeyDown(KeyCode.Y) && ctrlModifier)
                Redo();

        }

        public void SaveProject()
        {
            projectData.SaveAsFile(this);
        }

        public void LoadFromFile(string filePaths)
        {
            var files = filePaths.Split(',');
            foreach (var filePath in files)
            {
                if(filePath.EndsWith(".nl3d"))
                {
                    projectData.LoadFromFile(filePath);
                    return;
                }
            }  
        }

        public void Redo()
        {
            // Overwrite current projectData with the one from the redostack copy
            if (redoStack.Count > 0)
            {
                var lastState = redoStack[redoStack.Count - 1];
                projectData.CopyUndoFrom(lastState);
                redoStack.RemoveAt(redoStack.Count - 1);
            }       
        }
        public void Undo()
        {
            // Overwrite current projectData with the one from the undostack copy
            if (undoStack.Count > 0)
            {
                var lastState = undoStack[undoStack.Count - 1];
                projectData.CopyUndoFrom(lastState);
                undoStack.RemoveAt(undoStack.Count - 1);
            }
        }

        /// <summary>
        /// Receiver for the ProjectData to notify the ProjectDataHandler that the project has been saved to IndexedDB
        /// </summary>
        public void ProjectSavedToIndexedDB()
        {
            projectData.ProjectSavedToIndexedDB();
        }
    }
}