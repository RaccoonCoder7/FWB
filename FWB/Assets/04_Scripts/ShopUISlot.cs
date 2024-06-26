using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUISlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Image contentImg;
    // public Image stateImg;
    // public Image actionImg;
    public Text contentName;
    public Text priceText;
    public string itemData;
    [HideInInspector]
    public int price;
    public GameObject purchasestate;
    private int previousCredit = -1;
    // public List<Sprite> spriteList = new List<Sprite>();
    void Update()
    {
        if (GameMgr.In.credit != previousCredit) {
            UpdateStateIndicator();
            previousCredit = GameMgr.In.credit;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var ui = CommonTool.In.shopFollowUI;
        ui.gameObject.SetActive(true);
        ui.itemName.text = contentName.text;
        ui.itemData.text = itemData;
        ui.price.text = price.ToString();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CommonTool.In.shopFollowUI.gameObject.SetActive(false);
    }

    private void UpdateStateIndicator()
    {
        if (purchasestate != null)
        {
            purchasestate.SetActive(GameMgr.In.credit >= price);
        }
    }

}
