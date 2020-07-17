﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragonManager : MonoBehaviour {

    public static DragonManager Instance;

    private ZMain m_ZMain;

    private bool m_Initialized = false;

    public bool Playing = false;

    // 是否已经开始等待游戏开始
    private bool showReadyBtnYet = false;
    // 准备状态
    private bool ready = false;
    // 动画播放状态
    private bool AnimEnd = false;

    #region file

    public Button ReadyBtn;
    public Text Label;

    public Animator DragonAnim;

    #endregion

    #region Unity Internal

    private void Awake()
    {
        Instance = this;
        ReadyBtn.onClick.AddListener(ShowReadyBtnClk);

        
    }

    // Update is called once per frame
    void Update () {
        onUpdate();
	}

    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="main"></param>
    public void Init(ZMain main)
    {
        if (m_Initialized)
            return;

        m_ZMain = main;
        m_Initialized = true;
    }

    private void onUpdate()
    {
        if (!m_Initialized)
            return;

        // 扫码marker成功后 显示准备按钮
        if (m_ZMain.IS_MATCH && !showReadyBtnYet)
        {
            ShowReadyBtn();
        }

    }
    #region UILogic

    public void ShowReadyBtn()
    {
        ReadyBtn.gameObject.SetActive(true);
        showReadyBtnYet = true;
    }

    public void ShowReadyBtnClk()
    {
        if (ready)
        {
            Label.text = "点击准备";
            ready = false;
        }
        else
        {
            Label.text = "已准备";
            ready = true;
        }
        ZMessageManager.Instance.SendMsg(MsgId.__READY_PLAY_MSG_, string.Format("{0},{1}", ZClient.Instance.PlayerID, ready ? "1" : "0"));
    }

    #endregion

    /// <summary>
    /// 有人准备
    /// </summary>
    public void ReadyPlay(string playerId,string r)
    {
        bool allready = ZPlayerMe.Instance.SetPlayerReadyDic(playerId,r);

        if(allready && ZPlayerMe.Instance.PlayerMap[playerId].isHouseOwner)
        {
            ZMessageManager.Instance.SendMsg(MsgId.__PLAY_GAME_MSG_, "Go");
        }
    }

    /// <summary>
    /// 开始 QAQ
    /// </summary>
    public void PlayAnim()
    {
        ReadyBtn.gameObject.SetActive(false);
        Playing = true;
        Debug.Log("playing");

        DragonAnim.Play("");
    }

    private void EndEvent()
    {
        AnimEnd = true;
        playFight();
    }

    private void playFight()
    {

    }
}
