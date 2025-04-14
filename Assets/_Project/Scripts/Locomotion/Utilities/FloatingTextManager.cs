using System.Collections.Generic;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private int poolSize = 10;
    
    private static FloatingTextManager _instance;
    public static FloatingTextManager Instance => _instance;
    
    private Queue<FloatingDamageText> textPool = new Queue<FloatingDamageText>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        
        // Create text pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, targetCanvas.transform);
            FloatingDamageText floatingText = textObj.GetComponent<FloatingDamageText>();
            textObj.SetActive(false);
            textPool.Enqueue(floatingText);
        }
    }
    
    public void ShowDamageText(string text, Transform origin)
    {
        if (textPool.Count == 0)
        {
            // If pool is empty, create a new object
            GameObject textObj = Instantiate(floatingTextPrefab, targetCanvas.transform);
            FloatingDamageText floatingText = textObj.GetComponent<FloatingDamageText>();
            floatingText.gameObject.SetActive(true);
            floatingText.Initialize(text, origin, targetCanvas);
        }
        else
        {
            // Get text from pool
            FloatingDamageText floatingText = textPool.Dequeue();
            floatingText.gameObject.SetActive(true);
            floatingText.Initialize(text, origin, targetCanvas);
        }
    }
    
    // Return text to pool (called by FloatingDamageText when done)
    public void ReturnToPool(FloatingDamageText text)
    {
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }
}