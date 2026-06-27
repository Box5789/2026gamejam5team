using System;
using KimbapGame.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour
{
    [SerializeField] private MouseThrow2D mouseThrow;
    [SerializeField] private BoxCollider2D pickupCollider;

    private bool a=true;

    private bool b = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pickupCollider.enabled&&a)
        {
            a=false;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        if(SceneManager.GetActiveScene().name=="order")
            b = true;
        if(SceneManager.GetActiveScene().name=="kitchen"&&b)
            Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        if(pickupCollider.enabled&&!mouseThrow.enabled)
            mouseThrow.enabled = true;
        
    }
}
