using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        private WMSTileDataLayer wmsProjectionLayer;
        public WMSTileDataLayer WMSProjectionLayer
        {
            get
            {
                if (wmsProjectionLayer == null)
                    wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
                return wmsProjectionLayer;
            }
        }

        protected LayerURLPropertyData urlPropertyData = new();
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;

        public bool TransparencyEnabled { get => WMSProjectionLayer.TransparencyEnabled; }
        public int DefaultEnabledLayersMax { get => WMSProjectionLayer.DefaultEnabledLayersMax; }
        public Vector2Int PreferredImageSize { get => WMSProjectionLayer.PreferredImageSize; }

        protected override void Awake() 
        {
            base.Awake();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
            LayerData.RootIndexChanged.AddListener(SetRenderOrder);
        }

        public void SetURL(string url)
        {
            this.urlPropertyData.url = url;
            wmsProjectionLayer.WmsUrl = url;
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            wmsProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                wmsProjectionLayer.WmsUrl = urlProperty.url;
            }
        }

        public override void DestroyLayerGameObject()
        {
            base.DestroyLayerGameObject();
            LayerData.RootIndexChanged.RemoveListener(SetRenderOrder);
        }
    }
}