using System;
using UnityEngine;

public class HighlightOutline : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] _meshRenderers;
    
    [SerializeField] private string _outlineLayer;
    [SerializeField] private string _noOutlineLayer;

    public void HighlightAvailable() => SetOutline(_outlineLayer);
    public void HighlightUnavailable() => SetOutline(_outlineLayer);
    public void RemoveHighlight() => SetOutline(_noOutlineLayer);

    private void SetOutline(string newLayer)
    {
        int layer = LayerMask.NameToLayer(newLayer);

        foreach (MeshRenderer rend in _meshRenderers)  
        {            
            rend.gameObject.layer = layer; 
        }    
    }
}
