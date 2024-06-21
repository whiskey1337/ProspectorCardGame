using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EFSState
{
    idle,
    pre,
    active,
    post
}

public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public EFSState state = EFSState.idle;

    [SerializeField]
    protected int _score = 0;
    public string scoreString;

    public int score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            scoreString = _score.ToString("N0");
            GetComponent<Text>().text = scoreString;
        }
    }

    public List<Vector2> bezierPts;
    public List<float> fontSizes;

    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut;

    public GameObject reportFinishTo = null;

    private RectTransform rectTransform;
    private Text txt;

    public void Init(List<Vector2> ePts, float eTimeS = 0, float eTimeD = 1)
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;

        txt = GetComponent<Text>();

        bezierPts = new List<Vector2>(ePts);

        if (ePts.Count == 1)
        {
            transform.position = ePts[0];
            return;
        }

        if (eTimeS == 0) eTimeS = Time.time;
        timeStart = eTimeS;
        timeDuration = eTimeD;

        state = EFSState.pre;
    }

    private void Update()
    {
        if (state == EFSState.idle) return;

        float u = (Time.time - timeStart) / timeDuration;
        float uC = Easing.Ease(u, easingCurve);

        if (u < 0)
        {
            state = EFSState.pre;
            txt.enabled = false;
        }
        else
        {
            if (u >= 1)
            {
                uC = 1;
                state = EFSState.post;
                if (reportFinishTo != null)
                {
                    reportFinishTo.SendMessage("FSCallback", this);
                    Destroy(gameObject);
                }
                else
                {
                    state = EFSState.idle;
                }
            }
            else
            {
                state = EFSState.active;
                txt.enabled = true;
            }

            Vector2 pos = Utils.Bezier(uC, bezierPts);
            rectTransform.anchorMin = rectTransform.anchorMax = pos;

            if (fontSizes != null && fontSizes.Count > 0)
            {
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
                GetComponent<Text>().fontSize = size;
            }
        }
    }

    public void FSCallback(FloatingScore fs)
    {
        score += fs.score;
    }
}
