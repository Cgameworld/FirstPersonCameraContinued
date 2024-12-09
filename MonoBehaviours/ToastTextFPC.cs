using FirstPersonCameraContinued;
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
    private string displayedText;
    private Vector2 textSize;

    private float lineSpacingMultiplier = 1.35f;

    public void Initialize(string text)
    {
        displayedText = text;
    }

    private void Start()
    {
        style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.wordWrap = true;
        float screenHeight = Screen.height;
        float referenceFontSize = 20f;
        // Calculate the scaling factor for font size
        float fontSizeScale = screenHeight / 1080f;
        style.fontSize = Mathf.RoundToInt(referenceFontSize * fontSizeScale);

        CalculateTextSize();
    }

    private void CalculateTextSize()
    {
        string[] lines = displayedText.Split('\n');
        float totalHeight = 0;
        float maxWidth = 0;

        foreach (string line in lines)
        {
            Vector2 lineSize = style.CalcSize(new GUIContent(line));
            totalHeight += lineSize.y * lineSpacingMultiplier;
            maxWidth = Mathf.Max(maxWidth, lineSize.x);
        }

        textSize = new Vector2(maxWidth, totalHeight);
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
        if (Mod.FirstPersonModSettings != null)
        {
            float currentY = Mod.FirstPersonModSettings.ShowGameUI ? 60 : 20;
            string[] lines = displayedText.Split('\n');

            foreach (string line in lines)
            {
                Vector2 lineSize = style.CalcSize(new GUIContent(line));
                GUI.Label(new Rect(15, currentY, lineSize.x, lineSize.y), line, style);
                currentY += lineSize.y * lineSpacingMultiplier;
            }
        }
    }
}