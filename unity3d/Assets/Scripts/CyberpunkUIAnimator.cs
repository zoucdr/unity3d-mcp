using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CyberpunkUIAnimator : MonoBehaviour
{
    [Header("UI元素")]
    public RectTransform logoPanel;
    public RectTransform menuPanel;
    public Image background;
    public RectTransform cursorEffect;

    [Header("HUD元素")]
    public RectTransform topRightHUD;
    public RectTransform bottomLeftHUD;

    [Header("按钮")]
    public RectTransform[] menuButtons;

    [Header("动画设置")]
    public float fadeInTime = 1.5f;
    public float buttonDelay = 0.2f;
    public float hudAnimTime = 1.0f;
    public float pulseSpeed = 1.0f;
    public float pulseAmount = 0.2f;
    public float rotateSpeed = 10f;

    // 原始位置和大小
    private Vector2 cursorOriginalPos;
    private Vector2 topRightOriginalPos;
    private Vector2 bottomLeftOriginalPos;

    private void Start()
    {
        // 保存原始位置
        if (cursorEffect != null)
            cursorOriginalPos = cursorEffect.anchoredPosition;
        if (topRightHUD != null)
            topRightOriginalPos = topRightHUD.anchoredPosition;
        if (bottomLeftHUD != null)
            bottomLeftOriginalPos = bottomLeftHUD.anchoredPosition;

        // 开始UI动画
        StartCoroutine(PlayIntroAnimation());
    }

    private void Update()
    {
        // 持续动画效果
        AnimatePulsingEffects();
        AnimateRotatingElements();
        AnimateHUDMovement();
    }

    private IEnumerator PlayIntroAnimation()
    {
        // 初始化透明度
        SetInitialAlpha();

        // 背景淡入
        if (background != null)
        {
            StartCoroutine(FadeIn(background, fadeInTime * 0.8f));
        }

        yield return new WaitForSeconds(0.3f);

        // Logo淡入
        if (logoPanel != null)
        {
            StartCoroutine(ScaleIn(logoPanel, fadeInTime * 0.5f));
        }

        yield return new WaitForSeconds(0.5f);

        // 菜单面板淡入
        if (menuPanel != null)
        {
            StartCoroutine(ScaleIn(menuPanel, fadeInTime * 0.7f));
        }

        yield return new WaitForSeconds(0.3f);

        // 按钮依次淡入
        if (menuButtons != null && menuButtons.Length > 0)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] != null)
                {
                    StartCoroutine(SlideIn(menuButtons[i], fadeInTime * 0.5f, i * buttonDelay));
                }
            }
        }
    }

    private void SetInitialAlpha()
    {
        // 设置初始透明度
        if (background != null)
        {
            Color color = background.color;
            background.color = new Color(color.r, color.g, color.b, 0f);
        }

        if (logoPanel != null)
        {
            logoPanel.localScale = Vector3.zero;
        }

        if (menuPanel != null)
        {
            menuPanel.localScale = Vector3.zero;
        }

        if (menuButtons != null)
        {
            foreach (var button in menuButtons)
            {
                if (button != null)
                {
                    button.anchoredPosition = new Vector2(-500, button.anchoredPosition.y);
                }
            }
        }
    }

    private IEnumerator FadeIn(Image image, float duration)
    {
        float elapsed = 0;
        Color startColor = image.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsed < duration)
        {
            image.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        image.color = targetColor;
    }

    private IEnumerator ScaleIn(RectTransform rect, float duration)
    {
        float elapsed = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (elapsed < duration)
        {
            rect.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = targetScale;
    }

    private IEnumerator SlideIn(RectTransform rect, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0;
        Vector2 startPos = new Vector2(-500, rect.anchoredPosition.y);
        Vector2 targetPos = new Vector2(0, rect.anchoredPosition.y);

        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }

    private void AnimatePulsingEffects()
    {
        // 按钮脉动效果
        if (menuButtons != null)
        {
            foreach (var button in menuButtons)
            {
                if (button != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * 0.1f;
                    button.localScale = new Vector3(pulse, pulse, 1f);
                }
            }
        }

        // Logo脉动效果
        if (logoPanel != null)
        {
            float logoPulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 0.5f) * pulseAmount * 0.05f;
            logoPanel.localScale = new Vector3(logoPulse, logoPulse, 1f);
        }
    }

    private void AnimateRotatingElements()
    {
        // 旋转光标效果
        if (cursorEffect != null)
        {
            cursorEffect.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }
    }

    private void AnimateHUDMovement()
    {
        // HUD元素轻微移动
        if (topRightHUD != null)
        {
            float offsetX = Mathf.Sin(Time.time * 0.5f) * 10f;
            float offsetY = Mathf.Cos(Time.time * 0.7f) * 10f;
            topRightHUD.anchoredPosition = topRightOriginalPos + new Vector2(offsetX, offsetY);
        }

        if (bottomLeftHUD != null)
        {
            float offsetX = Mathf.Sin(Time.time * 0.7f) * 10f;
            float offsetY = Mathf.Cos(Time.time * 0.5f) * 10f;
            bottomLeftHUD.anchoredPosition = bottomLeftOriginalPos + new Vector2(offsetX, offsetY);
        }

        // 光标效果移动
        if (cursorEffect != null)
        {
            float cursorX = Mathf.Sin(Time.time * 0.3f) * 20f;
            float cursorY = Mathf.Cos(Time.time * 0.4f) * 20f;
            cursorEffect.anchoredPosition = cursorOriginalPos + new Vector2(cursorX, cursorY);
        }
    }
}
