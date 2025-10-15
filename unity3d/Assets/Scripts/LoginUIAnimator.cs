using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoginUIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float fadeInDuration = 1.0f;
    public float elementDelay = 0.2f;
    
    [Header("UI Elements")]
    public Image panelImage;
    public RectTransform[] uiElements;
    
    private Vector3[] originalPositions;
    private Vector3[] originalScales;
    
    void Start()
    {
        // 保存原始位置和缩放
        originalPositions = new Vector3[uiElements.Length];
        originalScales = new Vector3[uiElements.Length];
        
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] != null)
            {
                originalPositions[i] = uiElements[i].localPosition;
                originalScales[i] = uiElements[i].localScale;
                
                // 设置初始状态
                uiElements[i].localPosition = originalPositions[i] + new Vector3(0, -20, 0);
                uiElements[i].localScale = Vector3.zero;
            }
        }
        
        // 开始动画
        StartCoroutine(PlayIntroAnimation());
    }
    
    IEnumerator PlayIntroAnimation()
    {
        // 等待一小段时间再开始动画
        yield return new WaitForSeconds(0.5f);
        
        // 面板淡入
        if (panelImage != null)
        {
            Color targetColor = panelImage.color;
            targetColor.a = 0.95f;
            
            float elapsedTime = 0;
            Color startColor = panelImage.color;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeInDuration);
                panelImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
            
            panelImage.color = targetColor;
        }
        
        // UI元素依次出现
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] != null)
            {
                StartCoroutine(AnimateElement(uiElements[i], originalPositions[i], originalScales[i], i * elementDelay));
            }
        }
    }
    
    IEnumerator AnimateElement(RectTransform element, Vector3 targetPos, Vector3 targetScale, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        float duration = 0.5f;
        float elapsedTime = 0;
        
        Vector3 startPos = element.localPosition;
        Vector3 startScale = element.localScale;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // 使用缓动函数使动画更平滑
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            element.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            element.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            
            yield return null;
        }
        
        element.localPosition = targetPos;
        element.localScale = targetScale;
    }
}
