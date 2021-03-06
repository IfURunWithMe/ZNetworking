﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zrime;

public static class MsgId
{
    public const string __EXHIBIT_SHOW_PNG_MSG_ = "exhibit_show_png_msg";

    #region special id

    public const string __READY_PLAY_MSG = "ready_play_msg";
    public const string __JOIN_NEW_PLAYER_MSG_ = "join_new_player_msg";
    public const string __LEAVE_A_PLAYER_MSG_ = "leave_a_player_msg";
    public const string __PLAY_GAME_MSG = "play_Game_msg";

    #endregion
}

public class ZMessageManager {

    public static ZMessageManager m_Instance;
    public static ZMessageManager Instance
    {
        get
        {
            if(m_Instance == null)
            {
                m_Instance = new ZMessageManager();
            }
            return m_Instance;
        }
    }

    public ZClient client;

    private bool m_Initialized = false;

    public void Init()
    {
        if (m_Initialized) return;

        client = ZClient.Instance;
        client.Persist();

        client.AddListener(MsgId.__READY_PLAY_MSG, _S2C_ReadyPlay);
        client.AddListener(MsgId.__PLAY_GAME_MSG, _S2C_PlayGame);
        client.AddListener(MsgId.__JOIN_NEW_PLAYER_MSG_, _S2C_JoinNewPlayer);

        m_Initialized = true;
    }

    #region C2SFunc

    public void SendConnectAndJoinRoom(string serverIp, string port)
    {
        client.Connect(serverIp, port);
    }

    public void SendMsg(string msdId, string msgContent)
    {
        client.SendMsg(msdId, msgContent);
    }

    #endregion


    #region S2CFunc

    public void _S2C_JoinNewPlayer(object msg)
    {
        Player player = msg as Player;

        Debug.Log("createAAAAAA");
        PlayerEntity pe = GameObject.Instantiate<PlayerEntity>(ZNetworkingManager.Instance.PlayerPrefab);
        pe.Init(player);
        pe.UpdatePoseData();

        ZPlayerMe.Instance.AddPlayer(player.PlayerId, pe);
    }

    public void _S2C_ReadyPlay(object msg)
    {
        // todo 
        Message m = msg as Message;
        Debug.Log(m.Content);
    }

    public void _S2C_PlayGame(object msg)
    {
        Message m = msg as Message;
        Debug.Log(m.Content);
    }

    #endregion
}
