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
        slider.value = 1;
        targetValue = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetValue < slider.value)
            slider.value -= Time.deltaTime * slidingVelocity;
    }

    public void setValue(float value)
    {
        targetValue = value;
    }
}
