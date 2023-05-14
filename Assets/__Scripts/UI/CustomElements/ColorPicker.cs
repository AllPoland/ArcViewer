using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ColorPicker : MonoBehaviour
{
    private Color _value = Color.white;
    public Color Value
    {
        get => _value;
        set
        {
            _value = value;
            Color.RGBToHSV(_value, out HSVColor[0], out HSVColor[1], out HSVColor[2]);

            UpdatePreview();
            UpdateHexField();
            OnValueChanged?.Invoke(_value);
        }
    }

    private float[] _hsvColor = new float[3];
    public float[] HSVColor
    {
        get => _hsvColor;
        private set
        {
            _hsvColor = value;
        }
    }

    [SerializeField] private bool _interactable;
    public bool Interactable
    {
        get => _interactable;
        set
        {
            _interactable = value;
            UpdateInteractable();
        }
    }

    [Header("Options")]
    [SerializeField] private Color enabledTextColor;
    [SerializeField] private Color disabledTextColor;

    [Header("Elements")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button dropdownButton;
    [SerializeField] private Image dropdownImage;
    [SerializeField] private Image colorPreviewImage;
    [SerializeField] private RectTransform headerBackground;
    [SerializeField] private RectTransform pickerBackground;
    [SerializeField] private TMP_InputField hexInputField;

    public UnityEvent<Color> OnValueChanged;

    private bool opened;
    private RectTransform rectTransform;


    public void SetHue(float hue)
    {
        Value = Value.SetHue(hue);
    }


    public void SetSaturation(float saturation)
    {
        Value = Value.SetSaturation(saturation);
    }


    public void SetBrightness(float value)
    {
        Value = Value.SetValue(value);
    }


    public void SetColorHex(string hex)
    {
        if(!hex.StartsWith('#'))
        {
            //Makes sure the color is parsed as a hex code
            hex = '#' + hex;
        }

        Color newColor;
        if(ColorUtility.TryParseHtmlString(hex, out newColor))
        {
            //Hex input field gets updated when setting the color
            Value = newColor;
        }
        else
        {
            //The color failed to parse, so just reset the field
            UpdateHexField();
        }
    }


    private void UpdateHexField()
    {
        hexInputField.text = '#' + ColorUtility.ToHtmlStringRGB(Value);
    }


    private void UpdatePreview()
    {
        colorPreviewImage.color = Value;
    }


    private void UpdateInteractable()
    {
        label.color = Interactable ? enabledTextColor : disabledTextColor;
        dropdownButton.interactable = Interactable;

        if(opened)
        {
            SetOpen(false);
        }
    }


    private void SetOpen(bool open)
    {
        opened = open;

        pickerBackground.gameObject.SetActive(opened);

        Vector2 sizeDelta = rectTransform.sizeDelta;
        sizeDelta.y = headerBackground.sizeDelta.y;
        if(opened)
        {
            sizeDelta.y += pickerBackground.sizeDelta.y;
        }
        rectTransform.sizeDelta = sizeDelta;

        Vector3 dropdownRotation = dropdownImage.transform.localEulerAngles;
        dropdownRotation.z = opened ? -90f : 0f;
        dropdownImage.transform.localEulerAngles = dropdownRotation;

        //Forces a layout group update since this element changes size
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
    }


    public void ToggleDropdown()
    {
        SetOpen(!opened);
    }


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        UpdateInteractable();
        SetOpen(false);
        UpdatePreview();
    }
}