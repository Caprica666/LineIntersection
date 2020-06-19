using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class XSelector : MonoBehaviour, IPointerUpHandler
{
    public GameObject XIndicator;

    private Slider UISlider;

    public void Start()
    {
        UISlider = gameObject.GetComponent<Slider>() as Slider;
        //Adds a listener to the main slider and invokes a method when the value changes.
        UISlider.onValueChanged.AddListener(delegate { MoveIndicatorLine(); });
    }

    // Invoked when the value of the slider changes.
    public void MoveIndicatorLine()
    {
        float v = UISlider.value;
        Vector3 newpos = XIndicator.transform.localPosition;

        newpos.x = v;
        XIndicator.transform.localPosition = newpos;
        Debug.Log("Slider value = " + v.ToString());
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        Vector3 pos = XIndicator.transform.localPosition;
        XIndicator.SendMessageUpwards("FindIntersections", pos.x);
    }
}

