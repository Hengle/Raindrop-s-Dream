﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RDUI;
public class PlayerProperties: PublicProperties
{

    public const int HP_MaxLimit_Value = 8;//血量上限最大值
    [SerializeField]
    private int hpMaxValue;//血量上限
    public int HpMaxValue
    {
        get
        {
            return hpMaxValue;
        }
        set
        {
            hpMaxValue = value;
            hpCurrentValue = hpMaxValue; //提升血上限时恢复到满血
            UIDelegateManager.NotifyUI(UIMessageType.Updata_HpMax, hpMaxValue);
        }
    }
    [SerializeField]
    private int hpCurrentValue;//当前血量
    public int HpCurrentValue
    {
        get
        {
            return hpCurrentValue;
        }
        set
        {
            hpCurrentValue = value;
            UIDelegateManager.NotifyUI(UIMessageType.Updata_Hp, hpCurrentValue);
        }
    }
    public float moveBasicSpeed = 2;//基础移动速度
    public float moveSpeed = 2;//移动速度
    public float gravityScale = 4f;
    public float maxJumpSpeed;//跳跃最小，最大速度
    public float maxJumpTime = 1f;//最长跳跃时间
    public float shootSpan;//射击间隔
    public float invincibleTime;
    public bool canDoubleJump = false;//是否解锁二段跳

    //道具列表
    public int itemsMaxAmount = 5;
    public Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();

    //装备列表 { "装备位置" ，"装备名"}
    public Dictionary<string, string> equipments = new Dictionary<string, string>();

    void Start()
    {
        equipments.Add("Weapon", "WatterBullet");
    }
    public override EditorProperty[] GetEditorProperties()
    {
        return base.GetEditorProperties();
    }
    public override void SetEditorProperty(EditorProperty _property)
    {
        this.GetType().GetProperty(_property.name).SetValue(this, _property.value, null);
    }
}