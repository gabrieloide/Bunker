using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionSettings : MonoBehaviour
{
    [SerializeField]
    private Slider sliderBrightness;
    [SerializeField]
    private Image imageBrightness;

    void Start()
    {
        sliderBrightness.value = PlayerPrefs.GetFloat("brillo", 0.8f);

        //imageBrightness.color = new Color(0f, 0f, 0f, sliderBrightness.value);
    }

    public void ChangeSliderBrightness(float value)
    {
        PlayerPrefs.SetFloat("brillo", value);
        imageBrightness.color = new Color(0f, 0f, 0f, sliderBrightness.value);
    }

}
