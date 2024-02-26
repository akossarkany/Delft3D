using System.Linq;
using System.Collections;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class StandardLayerToggle : LayerToggle
    {
        private CartesianTiles.TileHandler tileHandler;

        protected override void Awake()
        {
            base.Awake();
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>(FindObjectsInactive.Include);

            layer = tileHandler.layers.FirstOrDefault(l => l.name == prefab.name)?.GetComponent<Tile3DLayer>();
        }

        private void OnEnable()
        {
            toggle.isOn = layer != null;
            ShowBin(false);

            toggle.onValueChanged.AddListener(CreateOrDestroyObject);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(CreateOrDestroyObject);
        }

        private void CreateOrDestroyObject(bool isOn)
        {
            if (isOn)
                layer = CreateObject();
            else
                layer.DestroyLayer();
        }

        private Tile3DLayer CreateObject()
        {
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, tileHandler.transform);
            newObject.name = prefab.name;
            tileHandler.AddLayer(newObject.GetComponent<CartesianTiles.Layer>());

            var layerComponent = newObject.GetComponent<Tile3DLayer>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<Tile3DLayer>();

            StartCoroutine(SelectAndHoverAtEndOfFrame());//wait until layer and UI are initialized.
            
            return layerComponent;
        }

        private IEnumerator SelectAndHoverAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame(); 
            layer.ReferencedProxy.UI.Select();
            HighlightLayer(true);
            layer.ReferencedProxy.name = prefab.name;
            layer.ReferencedProxy.UI.MarkLayerUIAsDirty();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowBin(toggle.isOn);
            GetComponent<Image>().sprite = hoverSprite;
            HighlightLayer(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ShowBin(false);
            GetComponent<Image>().sprite = defaultSprite;
            HighlightLayer(false);
        }

        //also called in the inspector to update after a press
        public void ShowBin(bool isOn)
        {
            binImage.SetActive(isOn);
        }

        private void HighlightLayer(bool isOn)
        {
            if (!layer || !layer.ReferencedProxy || !layer.ReferencedProxy.UI)
                return;

            var layerState = isOn ? InteractionState.Hover : InteractionState.Default;
            layer.ReferencedProxy.UI.SetHighlight(layerState);
        }
    }
}