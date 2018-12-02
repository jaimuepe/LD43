﻿
using UnityEngine;

public class Jetpack : MonoBehaviour
{
    [SerializeField] float m_strength;
    public float Strength { get { return m_strength; } }

    [SerializeField] int m_AmmoPerSecond;
    public int AmmoPerSecond { get { return m_AmmoPerSecond; } }

    [SerializeField] float m_PushForce;
    [SerializeField] float m_PushTorque;

    float minionsSpawned;
    int totalMinionsSpawned;

    Animator animatorL;
    Animator animatorR;

    [SerializeField] JetpackPipe jetpackL;
    [SerializeField] JetpackPipe jetpackR;

    GameManager gm;

    public bool Ready { get; private set; }

    private void Start()
    {
        Ready = true;
        gm = FindObjectOfType<GameManager>();
        animatorL = jetpackL.GetComponent<Animator>();
        animatorR = jetpackR.GetComponent<Animator>();
    }

    private void Update()
    {
        if (!Input.GetButton("Jump") || gm.GetAmmo() <= 0.0f)
        {
            minionsSpawned = 0;
            totalMinionsSpawned = 0;
            CancelJetpack();
        }
    }

    public void SetReady()
    {
        Ready = true;
    }

    public void StartJetpack()
    {
        animatorR.SetBool("active", true);
        animatorL.SetBool("active", true);
    }

    public void CancelJetpack()
    {
        animatorR.SetBool("active", false);
        animatorL.SetBool("active", false);
        AnimatorClipInfo[] clipInfo = animatorR.GetCurrentAnimatorClipInfo(0);
        if (clipInfo[0].clip.name == "LoopJetPack")
        {
            Ready = false;
        }
    }

    //true = L, false = R
    bool lastJetPack;

    public void SpawnMinions()
    {
        float t = Time.deltaTime;
        minionsSpawned += m_AmmoPerSecond * t;

        if (Mathf.Ceil(minionsSpawned) > totalMinionsSpawned)
        {
            int spawnAmmount = (int)Mathf.Ceil(minionsSpawned) - totalMinionsSpawned;

            for (int i = 0; i < spawnAmmount; i++)
            {
                Minion m = MinionsPool.Instance.Get(true);
                m.explosive = true;

                if (lastJetPack)
                {
                    m.transform.position = jetpackL.spawnPoint.position;
                }
                else
                {
                    m.transform.position = jetpackR.spawnPoint.position;
                }

                m.transform.localScale = 0.5f * Vector3.one;
                m.transform.rotation = Random.rotation;
                m.AddForce(m_PushForce * Vector3.down, ForceMode.Impulse);
                m.AddTorque(m_PushTorque * Random.insideUnitSphere, ForceMode.Impulse);

                lastJetPack = !lastJetPack;
            }
        }

        totalMinionsSpawned = (int)Mathf.Ceil(minionsSpawned);
    }
}
