using System;
using KimbapGame.Gameplay;
using KimbapGame.Order;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour
{
    [SerializeField] private MouseThrow2D mouseThrow;
    [SerializeField] private BoxCollider2D pickupCollider;

    private bool a=true;

    private bool b = false;
    private bool orderDeliveryConfigured;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pickupCollider != null && pickupCollider.enabled&&a)
        {
            a=false;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
            var m=gameObject.GetComponentsInChildren<MonoBehaviour>();
            Debug.Log(m.Length);
            foreach (var item in m)
                if(item.gameObject!=gameObject)
                    item.enabled = false;
        }

        if (SceneManager.GetActiveScene().name == "order")
        {
            b = true;
            ConfigureOrderDelivery();
        }
        if(SceneManager.GetActiveScene().name=="kitchen"&&b)
            Destroy(gameObject);
    }


    private void ConfigureOrderDelivery()
    {
        if (orderDeliveryConfigured)
        {
            return;
        }

        orderDeliveryConfigured = true;
        OrderKimbapDelivery delivery = GetComponent<OrderKimbapDelivery>();
        if (delivery == null)
        {
            delivery = gameObject.AddComponent<OrderKimbapDelivery>();
        }

        delivery.ConfigureForOrderDelivery();
    }
    private void OnMouseDown()
    {
        OrderKimbapDelivery delivery = GetComponent<OrderKimbapDelivery>();
        if (delivery != null)
        {
            delivery.BeginDeliveryDragFromCurrentMouse();
            return;
        }

        if (pickupCollider != null && pickupCollider.enabled && mouseThrow != null && !mouseThrow.enabled)
        {
            mouseThrow.enabled = true;
            mouseThrow.BeginDragFromCurrentMouse();
        }
    }
}
