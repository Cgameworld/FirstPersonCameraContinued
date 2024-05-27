using Game.SceneFlow;
using UnityEngine;

public class ToastTextFPC : MonoBehaviour
{
    private float fadeInDuration = 0f;
    private float displayDuration = 2.5f;
    private float fadeOutDuration = 0.3f;

    private float timer = 0f;
    private bool fadingIn = true;
    private bool fadingOut = false;

    private GUIStyle style;

    private void Start()
    {
        style = new GUIStyle();
        style.normal.textColor = Color.white;

        float screenHeight = Screen.height;

        float referenceFontSize = 20f;

        // Calculate the scaling factor for font size
        float fontSizeScale = screenHeight / 1080f;
        style.fontSize = Mathf.RoundToInt(referenceFontSize * fontSizeScale);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (fadingIn)
        {
            float alpha = Mathf.Clamp01(timer / fadeInDuration);
            style.normal.textColor = new Color(1f, 1f, 1f, alpha);

            if (timer >= fadeInDuration)
            {
                fadingIn = false;
                timer = 0f;
            }
        }
        else if (!fadingOut && timer >= displayDuration)
        {
            fadingOut = true;
            timer = 0f;
        }
        else if (fadingOut)
        {
            float alpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
            style.normal.textColor = new Color(1f, 1f, 1f, alpha);

            if (timer >= fadeOutDuration)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnGUI()
    {
        GameManager.instance.localizationManager.activeDictionary.TryGetValue("FirstPersonCameraContinued.ToastTextEnter", out string translatedText);
        GUI.Label(new Rect(15, 15, 10, 10), translatedText, style);
    }
}
