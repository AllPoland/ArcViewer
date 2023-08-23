using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class SliderPointerUpHandler : MonoBehaviour, IPointerUpHandler
{
    public UnityEvent<float> OnSliderEnd;

    private Slider slider;


    public void OnPointerUp(PointerEventData eventData)
    {
        OnSliderEnd?.Invoke(slider.value);
    }


    private void Awake()
    {
        slider = GetComponent<Slider>();
    }
}