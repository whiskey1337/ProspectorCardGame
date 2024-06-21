using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Prospector : MonoBehaviour
{
    static public Prospector S;
    
    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f;
    public Text gameOverText, roundResultText, highScoreText;

    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    private void Awake()
    {
        S = this;
        SetUpUITexts();
    }

    void SetUpUITexts()
    {
        GameObject go = GameObject.Find("HighScore");

        if (go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }

        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;

        go = GameObject.Find("GameOver");
        if (go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }

        go = GameObject.Find("RoundResult");
        if (go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }

        ShowResultsUI(false);
    }

    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }

    private void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        /* Card c;
        for (int cNum = 0; cNum < deck.cards.Count; cNum++)
        {
            c = deck.cards[cNum];
            c.transform.localPosition = new Vector3(cNum % 13, cNum / 13 * 4, 0);
        } */

        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);
        drawPile = ConvertListCardToListCardProspector(deck.cards);
        LayoutGame();
    }

    List<CardProspector> ConvertListCardToListCardProspector(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return lCP;
    }

    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return cd;
    }

    void LayoutGame()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;
        foreach (SlotDef tSD in layout.slotDefs)
        {
            cp = Draw();
            cp.faceUp = tSD.faceUp;
            cp.transform.parent = layoutAnchor;
            cp.transform.localPosition = new Vector3(
                layout.multiplier.x * tSD.x, 
                layout.multiplier.y * tSD.y, 
                -tSD.layerID);
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = ECardState.tableau;
            cp.SetSortingLayerName(tSD.layerName);
            tableau.Add(cp);
        }

        foreach (CardProspector tCP in tableau)
        {
            foreach (int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach (CardProspector tCP in tableau)
        {
            if (tCP.layoutID == layoutID)
            {
                return tCP;
            }
        }
        return null;
    }

    void SetTableauFaces()
    {
        foreach (CardProspector cd in tableau)
        {
            bool faceUp = true;
            foreach (CardProspector cover in cd.hiddenBy)
            {
                if (cover.state == ECardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;
        }
    }

    void MoveToDiscard(CardProspector cd)
    {
        cd.state = ECardState.discard;
        discardPile.Add(cd);
        cd.transform.parent = layoutAnchor;
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x, 
            layout.multiplier.y * layout.discardPile.y, 
            layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    void MoveToTarget(CardProspector cd)
    {
        if (target != null)
        {
            MoveToDiscard(target);
        }
        target = cd;
        cd.state = ECardState.target;
        cd.transform.parent = layoutAnchor;

        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x, 
            layout.multiplier.y * layout.discardPile.y, 
            -layout.discardPile.layerID);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    void UpdateDrawPile()
    {
        CardProspector cd;
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), 
                layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), 
                -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = ECardState.drawpile;
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardProspector cd)
    {
        switch (cd.state)
        {
            case ECardState.target:
                break;

            case ECardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager.EVENT(EScoreEvent.draw);
                FloatingScoreHandler(EScoreEvent.draw);
                break;

            case ECardState.tableau:
                bool validMatch = true;

                if (!cd.faceUp)
                {
                    validMatch = false;
                }

                if (!AdjacentRank(cd, target))
                {
                    validMatch = false;
                }

                if (!validMatch)
                    return;

                tableau.Remove(cd);
                MoveToTarget(cd);
                SetTableauFaces();
                ScoreManager.EVENT(EScoreEvent.mine);
                FloatingScoreHandler(EScoreEvent.mine);
                break;
        }
        CheckForGameOver();
    }

    void CheckForGameOver()
    {
        if (tableau.Count == 0)
        {
            GameOver(true);
            return;
        }

        if (drawPile.Count > 0)
        {
            return;
        }

        foreach (CardProspector cd in tableau)
        {
            if (AdjacentRank(cd, target))
            {
                return;
            }
        }

        GameOver(false);
    }

    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;
        if (won)
        {
            gameOverText.text = "Round over!";
            roundResultText.text = "Победа в этом раунде!\nЛучший счет: " + score;
            ShowResultsUI(true);

            //Debug.Log("Game Over. You won!");
            ScoreManager.EVENT(EScoreEvent.gameWin);
            FloatingScoreHandler(EScoreEvent.gameWin);
        }
        else
        {
            gameOverText.text = "Game over!";
            if (ScoreManager.HIGH_SCORE <= score)
            {
                roundResultText.text = "You got the high score!\nHigh score: " + score;
            }
            else
            {
                roundResultText.text = "Your final score was: " + score;
            }
            ShowResultsUI(true);

            //Debug.Log("Game Over. You lost!");
            ScoreManager.EVENT(EScoreEvent.gameLoss);
            FloatingScoreHandler(EScoreEvent.gameLoss);
        }
        //SceneManager.LoadScene("__Prospector_Scene_0");
        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        if (!c0.faceUp || !c1.faceUp) return false;

        if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return true;
        }

        if (c0.rank == 1 && c1.rank == 13) return true;
        if (c0.rank == 13 && c1.rank == 1) return true;

        return false;
    }

    void FloatingScoreHandler(EScoreEvent evt)
    {
        List<Vector2> fsPts;

        switch (evt)
        {
            case EScoreEvent.draw:
            case EScoreEvent.gameWin:
            case EScoreEvent.gameLoss:
                if (fsRun != null)
                {
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;
                }
                break;

            case EScoreEvent.mine:
                FloatingScore fs;

                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;

                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);

                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });

                if (fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }
}
