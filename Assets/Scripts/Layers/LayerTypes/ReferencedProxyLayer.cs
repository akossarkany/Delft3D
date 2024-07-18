using System;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class ReferencedProxyLayer : LayerNL3DBase
    {
        [SerializeField, JsonProperty] private string prefabId;
        [JsonIgnore] public ReferencedLayer Reference { get; }
        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (!KeepReferenceOnDestroy && Reference)
                GameObject.Destroy(Reference.gameObject);
        }

        public override void SelectLayer(bool deselectOthers = false)
        {
            base.SelectLayer(deselectOthers);
            Reference.OnSelect();
        }

        public override void DeselectLayer()
        {
            base.DeselectLayer();
            Reference.OnDeselect();
        }

        private void OnChildrenChanged()
        {
            Reference.OnProxyTransformChildrenChanged();
        }

        private void OnParentChanged()
        {
            Reference.OnProxyTransformParentChanged();
        }

        private void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            Reference.OnSiblingIndexOrParentChanged(newSiblingIndex);
        }

        public ReferencedProxyLayer(string name, ReferencedLayer reference) : base(name)
        {
            Reference = reference;
            Debug.Log("reference has prefab id: " + reference.PrefabIdentifier);
            prefabId = reference.PrefabIdentifier;
            if (reference == null) //todo: this should never happen
            {
                Debug.LogError("reference not found, creating temp layer");
                Reference = new GameObject("REFERENCENOTFOUND").AddComponent<HierarchicalObjectLayer>();
                // Reference.ReferencedProxy.KeepReferenceOnDestroy = true;
                // Reference.ReferencedProxy.DestroyLayer();
            }

            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
        }

        [JsonConstructor]
        public ReferencedProxyLayer(string name, string prefabId) : base(name)
        {
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            Reference = GameObject.Instantiate(prefab);
            Reference.ReferencedProxy = this;
            
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
        }

        ~ReferencedProxyLayer()
        {
            ParentChanged.RemoveListener(OnParentChanged);
            ChildrenChanged.RemoveListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.RemoveListener(OnSiblingIndexOrParentChanged);
        }
    }
}