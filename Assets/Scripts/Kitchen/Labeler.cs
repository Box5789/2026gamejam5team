using System;
using KimbapGame.Kitchen;
using TMPro;
using UnityEngine;

public class Labeler : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private GameObject label;
    TextMeshProUGUI labelText;
    KitchenIngredientSource kitchenIngredientSource;
    private float fontSize = 0.2f;

    private void Start()
    {
        var l=Instantiate(label,transform.parent);
        l.transform.position=pivot.position;
        labelText=l.GetComponentInChildren<TextMeshProUGUI>();
        if(kitchenIngredientSource == null)
            kitchenIngredientSource = GetComponent<KitchenIngredientSource>();
        labelText.text = kitchenIngredientSource.definition.DisplayName;
        labelText.fontSize = fontSize;
    }

    private void LateUpdate()
    {
        
    }
}
