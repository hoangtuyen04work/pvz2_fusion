using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecreasingSlider : MonoBehaviour
{
    UnityEngine.UI.Slider slider;   //Component Slider
    float targetValue;   //Giá trị mục tiêu
    float slidingVelocity = 0.1f;  //Tốc độ trượt khi giá trị thanh tiến trình thay đổi

    // Start is called before the first frame update
    void Start()
    {
        slider = transform.GetComponent<UnityEngine.UI.Slider>();
        slider.value = 1f;
        targetValue = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (slider == null) return;
        if (!Mathf.Approximately(slider.value, targetValue))
        {
            slider.value = Mathf.MoveTowards(slider.value, targetValue, Time.deltaTime * slidingVelocity);
        }
    }

    public void setValue(float value)
    {
        targetValue = Mathf.Clamp01(value);
    }

    public void setValueInstant(float value)
    {
        if (slider == null) slider = transform.GetComponent<UnityEngine.UI.Slider>();
        targetValue = Mathf.Clamp01(value);
        if (slider != null) slider.value = targetValue;
    }
}
