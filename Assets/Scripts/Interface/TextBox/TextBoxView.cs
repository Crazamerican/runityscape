﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System;

/**
 * This class manages a TextBox prefab
 */
public class TextBoxView : MonoBehaviour {

    [SerializeField]
    Text text;
    [SerializeField]
    AudioSource characterSound;
    float timePerLetter; //Speed at which the letters appear

    int index; //Which letter to make visible
    float timer; //Needs to be >= timePerLetter for a letter to appear

    public const int BLIP_INTERVAL = 1; //Letters needed for a sound to occur
    public const int CHARS_PER_LINE = 44; //Needed for word wrapping function

    public void WriteText(string fullText, Color color, TextEffect effect = TextEffect.NONE, string soundLocation = "Blip_0", float timePerLetter = 0, Action callBack = null) {
        WriteText(new TextBox(fullText, color, effect, soundLocation, timePerLetter), callBack);
    }

    public void WriteText(TextBox textBox, Action callBack = null) {
        StartCoroutine(TypeWriter(text, textBox, callBack));
    }

    IEnumerator TypeWriter(Text text, TextBox textBox, Action callBack) {
        text.color = textBox.Color;
        switch (textBox.Effect) {
            case TextEffect.NONE:
                text.text = textBox.RawText;
                Game.Instance.Sound.Play(textBox.SoundLocation);
                break;
            case TextEffect.FADE_IN:
                text.text = textBox.RawText;
                Color color = text.color;
                color.a = 0;
                text.color = color;
                while (text.color.a < 1) {
                    Color c = text.color;
                    c.a += Time.deltaTime * 3;
                    text.color = c;
                    yield return null;
                }
                break;
            case TextEffect.TYPE:
                string[] currentTextArray = new string[textBox.TextArray.Length];
                bool[] taggedText = new bool[currentTextArray.Length];

                //Prescreen and enable all html tags and spaces
                for (int i = 0; i < currentTextArray.Length; i++) {
                    if (Regex.IsMatch(textBox.TextArray[i], "(<.*?>)")) {
                        currentTextArray[i] = textBox.TextArray[i];
                        taggedText[i] = true;
                    }
                }
                float timer = 0;
                int index = 0;
                while (text.text.Length < textBox.RawText.Length) {
                    text.text = string.Join("", currentTextArray);
                    timer += Time.deltaTime;
                    if (timer >= textBox.TimePerLetter) {

                        //Don't reset timer or make sound on taggedText and spaces
                        if (!taggedText[index] && !Regex.IsMatch(textBox.TextArray[index], " ")) {
                            timer = 0;
                            Game.Instance.Sound.Play(characterSound);
                        }
                        if (!taggedText[index]) {
                            int start = index;
                            while (start < textBox.TextArray.Length && !textBox.TextArray[start].Equals(" ")) {
                                currentTextArray[start++] = "\u00A0";
                            }
                            currentTextArray[index] = textBox.TextArray[index];
                        }
                        index++;
                    }
                    yield return null;
                }
                break;
        }
        if (callBack != null) {
            callBack.Invoke();
        }
        yield break;
    }
}
