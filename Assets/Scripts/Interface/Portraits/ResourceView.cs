﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/**
 * This class manages the Resource prefab
 */
public class ResourceView : MonoBehaviour {

    Text resourceName; //Name of the Resource. Should be only 2 letters.
    Image underBar; //Bar that represents max resource
    Image overBar; //Bar that represents current resource
    Text fraction; //Text on bar that describes the amount

    // Use this for initialization
    void Awake() {
        resourceName = gameObject.GetComponentsInChildren<Text>()[0];
        underBar = gameObject.GetComponentsInChildren<Image>()[0];
        overBar = gameObject.GetComponentsInChildren<Image>()[1];
        fraction = gameObject.GetComponentsInChildren<Text>()[1];
    }

    public void SetResourceName(string name) {
        resourceName.text = name;
    }

    public void SetUnderBarColor(Color color) {
        underBar.color = color;
    }

    public void SetOverBarColor(Color color) {
        overBar.color = color;
    }

    /**
     * Scale should be in the range [0, 1]
     * This sets the OverBar's scale
     */
    public void SetBarScale(float scale) {
        Vector3 v = overBar.gameObject.GetComponent<RectTransform>().localScale;
        v.x = scale;
        overBar.gameObject.GetComponent<RectTransform>().localScale = v;
    }

    /**
     * This sets the text on the bar describing
     * current / total resource
     */
    public void SetFraction(int numerator, int denominator) {
        fraction.text = numerator + "/" + denominator;
    }

    /**
     * Sets the fraction as a string, for RP segments
     */
    public void SetFractionString(string s) {
        fraction.text = s;
    }
}