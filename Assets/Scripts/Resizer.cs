using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Resizer : ContentSizeFitter
{
    public UIMaster UIM = null;

    protected override void Start()
    {
        UIM = transform.parent.parent.parent.parent.parent.GetComponent<UIMaster>();
    }
    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        if (UIM != null)
            UIM.SetTextWidth(this.GetComponent<RectTransform>().sizeDelta.x);
    }
}
