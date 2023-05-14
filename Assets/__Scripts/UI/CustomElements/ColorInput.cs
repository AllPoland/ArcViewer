using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ColorInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{ 
    [SerializeField] private ColorPicker parentPicker;
    [SerializeField] private Slider hueSlider;
    [SerializeField] private Image cursorImage;
    [SerializeField] private Shader saturationValueShader;

    private Color color => parentPicker.Value;
    private float hue => parentPicker.HSVColor[0];
    private float saturation => parentPicker.HSVColor[1];
    private float value => parentPicker.HSVColor[2];
    private Vector2 currentCursorValues = new Vector2();

    private RectTransform rectTransform;
    private Image image;
    private Material material = null;
    private Canvas parentCanvas;
    private RectTransform cursorRectTransform;

    private float pickerSize => rectTransform.rect.width;
    private bool hovered;
    private bool clicked;


    public void SetSaturationAndValue(float s, float v)
    {
        float h = hueSlider.value;
        parentPicker.Value = Color.HSVToRGB(hueSlider.value, s, v);

        //Need to override GUI cause information gets lost on gray and full black
        SetGUI(h, s, v);
    }


    public void SetHue(float newHue)
    {
        Vector2 cursorValues = currentCursorValues;
        parentPicker.SetHue(newHue);
        SetGUI(newHue, cursorValues.x, cursorValues.y);
    }


    public void UpdateColor(Color newColor)
    {   
        float cursorBrightness = 1 - (value - saturation);
        cursorImage.color = new Color(cursorBrightness, cursorBrightness, cursorBrightness);
        SetGUI(hue, saturation, value);
    }


    private void SetGUI(float h, float s, float v)
    {
        image.materialForRendering.SetFloat("_Hue", h);
        hueSlider.SetValueWithoutNotify(h);

        currentCursorValues = new Vector2(s, v);
        cursorRectTransform.anchoredPosition = currentCursorValues * pickerSize;
    }


    private void UpdateMouseInput()
    {
        //Get the mouse's relative position on the picker
        Vector2 mousePos = (Vector2)Input.mousePosition;
        Vector2 localMousePos;

        //Returns the mouse position relative to pivot
        bool gotPosition = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePos,
            parentCanvas.worldCamera,
            out localMousePos
        );

        if(!gotPosition)
        {
            Debug.LogError("Failed to get mouse position on color picker!");
            return;
        }

        Vector2 input = localMousePos / pickerSize;
        input += rectTransform.pivot;

        float s = Mathf.Clamp(input.x, 0, 1);
        float v = Mathf.Clamp(input.y, 0, 1);

        SetSaturationAndValue(s, v);
    }


    private void Update()
    {
        //Weird hack to keep the input going when the mouse leaves
        if(hovered && Input.GetMouseButtonDown(0))
        {
            clicked = true;
        }
        else if(clicked && !Input.GetMouseButton(0))
        {
            clicked = false;
        }

        if(clicked)
        {
            UpdateMouseInput();
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }


    private void OnEnable()
    {
        UpdateColor(color);
        parentPicker.OnValueChanged.AddListener(UpdateColor);
    }


    private void OnDisable()
    {
        clicked = false;
        parentPicker.OnValueChanged.RemoveListener(UpdateColor);
    }


    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        cursorRectTransform = cursorImage.GetComponent<RectTransform>();

        //Need to create a new material because propertyblocks don't work on images
        material = new Material(saturationValueShader);
        material.SetFloat("_Hue", hue);
        image.material = material;
    }
}