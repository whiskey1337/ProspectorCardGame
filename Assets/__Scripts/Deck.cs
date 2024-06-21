using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [Header("Set in Inspector")]
    public bool startFaceUp = false;
    public Sprite suitClub;
    public Sprite suitDiamond;
    public Sprite suitHeart;
    public Sprite suitSpade;

    public Sprite[] faceSprites;
    public Sprite[] rankSprites;

    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;

    public GameObject prefabSprite;
    public GameObject prefabCard;

    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;
    public List<string> cardNames;
    public List<Card> cards;
    public List<Decorator> decorators;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictSuits;

    public void InitDeck(string deckXMLText)
    {
        if (GameObject.Find("Deck") == null)
        {
            GameObject anchorGO = new GameObject("Deck");
            deckAnchor = anchorGO.transform;
        }

        dictSuits = new Dictionary<string, Sprite>()
        {
            {"C", suitClub },
            {"D", suitDiamond },
            {"H", suitHeart },
            {"S", suitSpade },
        };

        ReadDeck(deckXMLText);
        MakeCards();
    }

    public void ReadDeck(string deckXMLText)
    {
        xmlr = new PT_XMLReader();
        xmlr.Parse(deckXMLText);

        string s = "xml[0] decorator[0] ";
        s += "type = " + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += " x = " + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += " y = " + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += " scale = " + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);
        decorators = new List<Decorator>();

        PT_XMLHashList xDexos = xmlr.xml["xml"][0]["decorator"];
        Decorator deco;
        for (int i = 0; i < xDexos.Count; i++)
        {
            deco = new Decorator();
            deco.type = xDexos[i].att("type");
            deco.flip = (xDexos[i].att("flip") == "1");
            deco.scale = float.Parse(xDexos[i].att("scale"), CultureInfo.InvariantCulture);
            deco.loc.x = float.Parse(xDexos[i].att("x"), CultureInfo.InvariantCulture);
            deco.loc.y = float.Parse(xDexos[i].att("y"), CultureInfo.InvariantCulture);
            deco.loc.z = float.Parse(xDexos[i].att("z"), CultureInfo.InvariantCulture);

            decorators.Add(deco);
        }

        CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        ci.NumberFormat.CurrencyDecimalSeparator = ".";

        cardDefs = new List<CardDefinition>();
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefs.Count; i++)
        {
            CardDefinition cDef = new CardDefinition();

            cDef.rank = int.Parse(xCardDefs[i].att("rank"));

            PT_XMLHashList xPips = xCardDefs[i]["pip"];
            if (xPips != null)
            {
                for (int j = 0; j < xPips.Count; j++)
                {
                    deco = new Decorator();

                    deco.type = "pip";
                    deco.flip = (xPips[j].att("flip") == "1");
                    deco.loc.x = float.Parse(xPips[j].att("x"), CultureInfo.InvariantCulture);
                    deco.loc.y = float.Parse(xPips[j].att("y"), CultureInfo.InvariantCulture);

                    deco.loc.z = float.Parse(xPips[j].att("z"), CultureInfo.InvariantCulture);

                    if (xPips[j].HasAtt("scale"))
                    {
                        deco.scale = float.Parse(xPips[j].att("scale"), NumberStyles.Any, ci);
                    }

                    cDef.pips.Add(deco);
                }
            }

            if (xCardDefs[i].HasAtt("face"))
            {
                cDef.face = xCardDefs[i].att("face");
            }

            cardDefs.Add(cDef);
        }
    }

    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        foreach (CardDefinition cd in cardDefs)
        {
            if (cd.rank == rnk)
            {
                return (cd);
            }
        }
        return (null);
    }

    public void MakeCards()
    {
        cardNames = new List<string>();
        string[] letters = new string[] { "C", "D", "H", "S" };
        foreach (string s in letters)
        {
            for (int i = 0; i < 13; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }

        cards = new List<Card>();
        for (int i = 0; i < cardNames.Count; i++)
        {
            cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum)
    {
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>();


        cgo.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));
        if (card.suit == "D" || card.suit == "H")
        {
            card.colS = "Red";
            card.color = Color.red;
        }
        else
        {
            card.colS = "Black";
            card.color = Color.black;
        }

        card.def = GetCardDefinitionByRank(card.rank);

        AddDecoratots(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;

    private void AddDecoratots(Card card)
    {
        foreach (Decorator dec in decorators)
        {
            if (dec.type == "suit")
            {
                _tGO = Instantiate<GameObject>(prefabSprite);
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSR.sprite = dictSuits[card.suit];
            }
            else
            {
                _tGO = Instantiate<GameObject>(prefabSprite);
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                _tSp = rankSprites[card.rank];
                _tSR.sprite = _tSp;
                _tSR.color = card.color;
            }

            _tSR.sortingOrder = 1;
            _tGO.transform.SetParent(card.transform);
            _tGO.transform.localPosition = dec.loc;

            if (dec.flip)
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            if (dec.scale != 1)
            {
                _tGO.transform.localScale = Vector3.one * dec.scale;
            }

            _tGO.name = dec.type;
            card.decoGOs.Add(_tGO);
        }
    }

    private void AddPips(Card card)
    {
        foreach (Decorator pip in card.def.pips)
        {
            _tGO = Instantiate<GameObject>(prefabSprite);
            _tGO.transform.SetParent(card.transform);
            _tGO.transform.localPosition = pip.loc;

            if (pip.flip)
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            if (pip.scale != 0)
            {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }
            else
            {
                _tGO.transform.localScale = Vector3.one;
            }

            _tGO.name = "pip";
            _tSR = _tGO.GetComponent<SpriteRenderer>();
            _tSR.sprite = dictSuits[card.suit];
            _tSR.sortingOrder = 1;
            card.pipGOs.Add(_tGO);
        }
    }

    private void AddFace(Card card)
    {
        if (card.def.face == "")
        {
            return;
        }

        _tGO = Instantiate<GameObject>(prefabSprite);
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSp = GetFace(card.def.face + card.suit);
        _tSR.sprite = _tSp;
        _tSR.sortingOrder = 1;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    private Sprite GetFace(string faceS)
    {
        foreach (Sprite _tSP in faceSprites)
        {
            if (_tSP.name == faceS)
            {
                return _tSP;
            }
        }
        return null;
    }

    private void AddBack(Card card)
    {
        _tGO = Instantiate<GameObject>(prefabSprite);
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;

        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;

        card.faceUp = startFaceUp;
    }

    static public void Shuffle(ref List<Card> oCards)
    {
        List<Card> tCards = new List<Card>();

        int index;
        while (oCards.Count > 0)
        {
            index = Random.Range(0, oCards.Count);
            tCards.Add(oCards[index]);
            oCards.RemoveAt(index);
        }

        oCards = tCards;
    }
}
