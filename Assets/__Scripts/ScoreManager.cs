using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EScoreEvent
{
    draw = 0,
    mine,
    mineGold,
    gameWin,
    gameLoss
}

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int HIGH_SCORE = 0;

    [Header("Set Dynamically")]
    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;

    private void Awake()
    {
        if (S == null)
        {
            S = this;
        }
        else
        {
            Debug.LogError("ERROR: ScoreManager.Awake(): S is alreasy set!");
        }

        if (PlayerPrefs.HasKey("ProspectorHighScore"))
        {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }

        score += SCORE_FROM_PREV_ROUND;
        SCORE_FROM_PREV_ROUND = 0;
    }

    static public void EVENT(EScoreEvent evt)
    {
        try
        {
            S.Event(evt);
        }
        catch (System.NullReferenceException nre)
        {
            Debug.LogError("ScoreManager.EVENT() called while S=null.\n" + nre);
        }
    }

    void Event(EScoreEvent evt)
    {
        switch (evt)
        {
            case EScoreEvent.draw:
            case EScoreEvent.gameWin:
            case EScoreEvent.gameLoss:
                chain = 0;
                score += scoreRun;
                scoreRun = 0;
                break;

            case EScoreEvent.mine:
                chain++;
                scoreRun += chain;
                break;
        }

        switch (evt)
        {
            case EScoreEvent.gameWin:
                SCORE_FROM_PREV_ROUND = score;
                Debug.Log("You won this round! Round score: " + score);
                break;

            case EScoreEvent.gameLoss:
                if (HIGH_SCORE <= score)
                {
                    Debug.Log("You got the high score! High score: " + score);
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                }
                else
                {
                    Debug.Log("Your final score for the game was: " + score);
                }
                break;

            default:
                Debug.Log($"Score: {score} ScoreRun: {scoreRun} Chain: {chain}");
                break;
        }
    }

    static public int CHAIN { get { return S.chain; } }
    static public int SCORE { get { return S.score; } }
    static public int SCORE_RUN { get { return S.scoreRun; } }
}
