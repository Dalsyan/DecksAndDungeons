using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardReduxScript : MonoBehaviour
{
    public GameObject Card;
    private Image CardImage;
    private Image Border;
    private Image Imagen;
    private TextMeshProUGUI NameText;
    private TextMeshProUGUI ManaText;
    private TextMeshProUGUI HpText;
    private TextMeshProUGUI AcText;
    private TextMeshProUGUI DamageText;

    public string Name;
    public string Class;
    public string Race;
    public string Type;
    public int level;
    public int hp;
    public int ac;
    public int damage;
    public int magic;
    public int power;

    private void Start()
    {
        Card.name = Name;
        CardImage = transform.GetComponent<Image>();

        NameText = transform.Find("name").GetComponent<Image>().transform.Find("nameText").GetComponent<TextMeshProUGUI>();
        NameText.text = Name;

        if (Type == "creature")
        {
            ManaText = Border.transform.Find("mana").GetComponent<Image>().transform.Find("manaText").GetComponent<TextMeshProUGUI>();
            ManaText.text = level.ToString();
            HpText = Border.transform.Find("hp").GetComponent<Image>().transform.Find("hpText").GetComponent<TextMeshProUGUI>();
            HpText.text = hp.ToString();
            AcText = Border.transform.Find("ac").GetComponent<Image>().transform.Find("acText").GetComponent<TextMeshProUGUI>();
            AcText.text = ac.ToString();
            DamageText = Border.transform.Find("dmg").GetComponent<Image>().transform.Find("dmgText").GetComponent<TextMeshProUGUI>();
            DamageText.text = damage.ToString();
        }
        else
        {
            ManaText = Border.transform.Find("mana").GetComponent<Image>().transform.Find("manaText").GetComponent<TextMeshProUGUI>();
            ManaText.text = power.ToString();
        }
    }
}
