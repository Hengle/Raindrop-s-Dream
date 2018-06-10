﻿using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelEditor : MonoBehaviour
{
    public static LevelEditor instance = null;
    /*UI*/
    public GameObject tileButton;//tile按钮预制体
    public GameObject levelButton;//已有关卡按钮
    //Left
    public GameObject layerOneButton;//图层按钮
    public GameObject layerTwoButton;
    public GameObject layerThreeButton;
    public GameObject hideOtherToggle;//隐藏其它图层
    //Right
    public GameObject bgScrollView;//用于切页
    public GameObject phcScrollView;
    public GameObject pncScrollView;
    public GameObject itemScrollView;
    public GameObject npcScrollView;
    public GameObject enemyScrollView;

    public GameObject backgroundPage;//用于设置tileButton父对象
    public GameObject platformHasColliderPage;
    public GameObject platformNoColliderPage;
    public GameObject itemsPage;
    public GameObject npcPage;
    public GameObject enemyPage;

    public GameObject backgroundPageBtn;//背景tile分页按钮
    public GameObject platformHasColliderPageBtn;//可碰撞地形组成分页按钮
    public GameObject platformNoColliderPageBtn;//不可碰撞地形组成分页按钮
    public GameObject itemsPageBtn;//道具分页按钮
    public GameObject npcPageBtn;//NPC分页按钮
    public GameObject enemyPageBtn;//enemy分页按钮

    public GameObject nowTileImage;//当前tile图标
    //Up
    public GameObject levelNameInputField;//关卡名输入框
    public GameObject makerNameInputField;//关卡制作者输入框
    public GameObject saveButton;//保存按钮
    //Down
    public GameObject LevelPanel;//已有level面板
    /*Data*/
    private Dictionary<int, GameObject> tilePrefabs;//Tile预制体
    private int nowTileId;//当前选中TileID
    private int nowLevelId;//当前编辑关卡ID
    private string levelName;//当前关卡名称
    private string makerName;//当前关卡制作者名

    /*Object*/
    private Camera mainCamera;//主相机

    public GameObject layerOne;//Layer1对象
    public GameObject layerTwo;//Layer2对象
    public GameObject layerThree;//Layer3对象

    private GameObject nowTileObject;//当前选中tile
    /*Mouse*/
    private Vector3Int mousePos;//鼠标指针位置坐标
    /*图层*/
    private int nowLayer;
    /*运行*/
    private bool isPlaying;//是否开始测试运行

    /*TileType*/
    private const int TILE_BACKGROUND = 0;
    private const int TILE_PLATFORMHASCOLLIDER = 1;
    private const int TILE_PLATFORMNOCOLLIDER = 2;
    private const int TILE_ITEM = 3;
    private const int TILE_NPC = 4;
    private const int TILE_ENEMY = 5;

    /*地图大小*/
    private const int MAX_WIDTH = 100;
    private const int MAX_HEIGHT = 50;
    private const int MAX_LAYERS = 5;

    /*自动保存*/
    public float saveSpan;//自动保存时间间隔
    private float lastSaveTime;//上次保存时间
    void Awake()
    {
        //单例，关卡切换不销毁
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

    }
    // Use this for initialization
    void Start()
    {
        nowLayer = -3;
        nowTileId = -1;
        nowLevelId = -1;
        tilePrefabs = new Dictionary<int, GameObject>();
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        //初始化工具栏
        InitToolbars();
        //初始化Tile预制体
        InitTilePrefabs();
        //初始化Tile选择按钮
        InitTileButtons();
        //初始化已有Level列表
        InitLevelList();

    }

    // Update is called once per frame
    void Update()
    {
        //跟随鼠标移动，更新Tile位置
        if (mousePos != Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition)))
        {
            mousePos = Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (nowTileObject != null)
            {
                nowTileObject.transform.position = new Vector3Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y), nowLayer);
            }
        }
        //左键点击放置Tile,已经有物体的位置不能放置
        if (nowTileObject != null)
        {
            if (Input.GetMouseButton(0) && IsValidPosition(mousePos))
            {
                if (FindTileInChild(new Vector3Int(mousePos.x, mousePos.y, nowLayer)) == null)
                {
                    SetNowTileToTileMap(nowTileId);
                }
            }
        }
        //右键清除当前选择or橡皮擦
        if (Input.GetMouseButton(1))
        {
            if (nowTileObject != null)
            {
                Destroy(nowTileObject);
                nowTileObject = null;
                nowTileImage.GetComponent<Image>().sprite = null;
                nowTileId = -1;
            }
            mousePos = Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (IsValidPosition(mousePos))
            {
                GameObject des = FindTileInChild(new Vector3Int(mousePos.x, mousePos.y, nowLayer));
                if (des != null)
                {
                    Destroy(des.gameObject);
                }
            }
        }
        //鼠标滑轮缩放
        if (Input.GetAxis("Mouse ScrollWheel") != 0 && !EventSystem.current.IsPointerOverGameObject())
        {
            if (mainCamera.orthographicSize > 0.5f)
            {
                mainCamera.orthographicSize += Input.GetAxis("Mouse ScrollWheel");
            }
        }
        //未开始运行水平、竖直输入移动地图
        if (!isPlaying)
        {
            mainCamera.transform.position += new Vector3(Input.GetAxis("Horizontal") * Time.deltaTime * 5f, Input.GetAxis("Vertical") * Time.deltaTime * 5f, 0);
        }
        StartCoroutine(AutoSave());
    }

    /*各种初始化*/
    //初始化工具栏
    void InitToolbars()
    {
        layerOneButton.GetComponent<Button>().onClick.AddListener(() => SetNowLayer(-1));
        layerTwoButton.GetComponent<Button>().onClick.AddListener(() => SetNowLayer(-2));
        layerThreeButton.GetComponent<Button>().onClick.AddListener(() => SetNowLayer(-3));

        hideOtherToggle.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => HideOtherLayer(hideOtherToggle.GetComponent<Toggle>().isOn));
    }
    //加载TilePrefabs
    void InitTilePrefabs()
    {
        AssetBundle load = AssetBundle.LoadFromFile(PublicDataManager.DATA_PATH + "\\test.obj");
        foreach (int key in PublicDataManager.instance.GetTilePrefabTableKeys())
        {
            GameObject prefab = load.LoadAsset<GameObject>(PublicDataManager.instance.GetTilePrefabName(key));
            tilePrefabs.Add(key, prefab);
        }
    }
    //初始化TileButton
    void InitTileButtons()
    {
        //分页按钮
        backgroundPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_BACKGROUND); });
        platformHasColliderPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_PLATFORMHASCOLLIDER); });
        platformNoColliderPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_PLATFORMNOCOLLIDER); });
        itemsPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_ITEM); });
        npcPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_NPC); });
        enemyPageBtn.GetComponent<Button>().onClick.AddListener(() => { SwitchTilePanel(TILE_ENEMY); });

        foreach (int key in PublicDataManager.instance.GetTilePrefabTableKeys())
        {

            GameObject btn = null;
            //创建按钮绑定点击函数,根据Tpye加到不同的分页下
            switch (PublicDataManager.instance.GetTilePrefabType(key))
            {
                case TILE_BACKGROUND: btn = Instantiate(tileButton, backgroundPage.transform); break;
                case TILE_PLATFORMHASCOLLIDER: btn = Instantiate(tileButton, platformHasColliderPage.transform); break;
                case TILE_PLATFORMNOCOLLIDER: btn = Instantiate(tileButton, platformNoColliderPage.transform); break;
                case TILE_ITEM: btn = Instantiate(tileButton, itemsPage.transform); break;
                case TILE_NPC: btn = Instantiate(tileButton, npcPage.transform); break;
                case TILE_ENEMY: btn = Instantiate(tileButton, enemyPage.transform); break;
                default: break;
            }
            if (btn != null)
            {
                btn.GetComponent<Button>().name = PublicDataManager.instance.GetTilePrefabName(key);
                btn.GetComponent<Image>().sprite = tilePrefabs[key].GetComponent<SpriteRenderer>().sprite;
                btn.GetComponent<Button>().onClick.AddListener(() => { OnTileButtonClick(key); });
            }
            //初始显示背景分页
            SwitchTilePanel(TILE_BACKGROUND);
        }
    }
    //初始化已有Level列表
    void InitLevelList()
    {
        foreach (int key in PublicDataManager.instance.GetLevelTableKeys())
        {
            GameObject btn = Instantiate(levelButton, LevelPanel.transform);
            btn.name = PublicDataManager.instance.GetLevelName(key);
            btn.GetComponent<Image>().sprite = LoadLevelImage(key);
            btn.GetComponent<Button>().onClick.AddListener(() => { LoadLevel(key); });
        }

    }

    /*各种按钮响应函数*/
    //隐藏其它图层按钮
    void HideOtherLayer(bool isOn)
    {
        if (isOn)
        {
            layerOne.SetActive(false);
            layerTwo.SetActive(false);
            layerThree.SetActive(false);
            GetLayerObject(nowLayer).SetActive(true);
        }
        else
        {
            layerOne.SetActive(true);
            layerTwo.SetActive(true);
            layerThree.SetActive(true);
        }
    }
    //保存按钮
    void OnSaveButtonClick()
    {
        SaveLevel();
    }
    //tile按钮
    void OnTileButtonClick(int _ID)
    {
        if (nowTileObject != null)
        {
            Destroy(nowTileObject);
        }
        //设置当前ID
        nowTileId = _ID;
        //设置当前选中Tile图标
        nowTileImage.GetComponent<Image>().sprite = EventSystem.current.currentSelectedGameObject.GetComponent<Image>().sprite;
        //创建tile
        mousePos = Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        nowTileObject = Instantiate(tilePrefabs[_ID], new Vector3Int(mousePos.x, mousePos.y, nowLayer), Quaternion.identity);
        nowTileObject.name = _ID.ToString();
    }
    //切换tile分页
    void SwitchTilePanel(int _panelType)
    {
        bgScrollView.SetActive(false);
        phcScrollView.SetActive(false);
        pncScrollView.SetActive(false);
        itemScrollView.SetActive(false);
        npcScrollView.SetActive(false);
        enemyScrollView.SetActive(false);
        switch (_panelType)
        {
            case TILE_BACKGROUND: bgScrollView.SetActive(true); break;
            case TILE_PLATFORMHASCOLLIDER: phcScrollView.SetActive(true); break;
            case TILE_PLATFORMNOCOLLIDER: pncScrollView.SetActive(true); break;
            case TILE_ITEM: itemScrollView.SetActive(true); break;
            case TILE_NPC: npcScrollView.SetActive(true); break;
            case TILE_ENEMY: enemyScrollView.SetActive(true); break;
            default: break;
        }
    }
    /*各种find*/
    //根据图层查找子对象
    GameObject FindTileInChild(Vector3Int _mpos)
    {
        GameObject pos, father;
        int childCount = 0;
        childCount = GetLayerObject(_mpos.z).transform.childCount;
        father = GetLayerObject(_mpos.z);
        for (int i = 0; i < childCount; i++)
        {
            pos = father.transform.GetChild(i).gameObject;
            if (Vector3Int.RoundToInt(pos.transform.position) == _mpos)
            {
                return pos;
            }
        }
        return null;
    }
    /*各种Set/Get*/
    //根据当前图层放置tile
    void SetNowTileToTileMap(int _ID)
    {
        nowTileObject.transform.SetParent(GetLayerObject(nowLayer).transform);
        mousePos = Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        nowTileObject = Instantiate(tilePrefabs[_ID], new Vector3Int(mousePos.x, mousePos.y, nowLayer), Quaternion.identity);
        nowTileObject.name = _ID.ToString();
    }
    //设置当前图层
    void SetNowLayer(int _layer)
    {
        nowLayer = _layer;
    }
    //获取当前层对象
    GameObject GetLayerObject(int _layer)
    {
        switch (_layer)
        {
            case -1: return layerOne; break;
            case -2: return layerTwo; break;
            case -3: return layerThree; break;
            default: return layerThree; break;
        }
    }
    /*各种判断*/
    //放置位置是否合法
    bool IsValidPosition(Vector3 pos)
    {
        return pos.x >= 0 && pos.x <= MAX_WIDTH && pos.y >= 0 && pos.y <= MAX_HEIGHT;
    }

    /*读写Level*/
    //保存地图
    public void SaveLevel()
    {
        levelName = levelNameInputField.GetComponent<InputField>().text;
        if (levelName == null)
        {
            //请输入关卡名
            return;
        }
        makerName = makerNameInputField.GetComponent<InputField>().text;
        if (makerName == null)
        {
            //请输入制作者名
            return;
        }
        if (nowLevelId < 0)
        {
            nowLevelId = PublicDataManager.instance.GetLevelTableCount() + 1;
        }
        //文件夹路径：/Level/作者名/地图名文件夹
#if UNITY_IOS || UNITY_ANDROID      
        string saveDirPath = Application.persistentDataPath + "\\Level\\" +  makerName  + "\\" + levelName;
#elif UNITY_STANDALONE_WIN
        string saveDirPath = Application.streamingAssetsPath + "\\Level\\" + makerName + "\\" + levelName;
#endif
        try
        {
            if (!Directory.Exists(saveDirPath))
            {
                Directory.CreateDirectory(saveDirPath);
            }
            FileStream fs = new FileStream(saveDirPath + "\\" + levelName + ".level", FileMode.Create);
            StreamWriter writer = new StreamWriter(fs);
            writer.WriteLine(nowLevelId);
            writer.WriteLine(levelName);
            writer.WriteLine(makerName);
            GameObject obj;
            for (int i = 0; i < layerOne.transform.childCount; i++)
            {
                obj = layerOne.transform.GetChild(i).gameObject;
                writer.WriteLine(obj.name + "#" + Mathf.Round(obj.transform.position.x) + "," + Mathf.Round(obj.transform.position.y) + "," + Mathf.Round(obj.transform.position.z));

            }
            for (int i = 0; i < layerTwo.transform.childCount; i++)
            {
                obj = layerTwo.transform.GetChild(i).gameObject;
                writer.WriteLine(obj.name + "#" + Mathf.Round(obj.transform.position.x) + "," + Mathf.Round(obj.transform.position.y) + "," + Mathf.Round(obj.transform.position.z));

            }
            for (int i = 0; i < layerThree.transform.childCount; i++)
            {
                obj = layerThree.transform.GetChild(i).gameObject;
                writer.WriteLine(obj.name + "#" + Mathf.Round(obj.transform.position.x) + "," + Mathf.Round(obj.transform.position.y) + "," + Mathf.Round(obj.transform.position.z));

            }
            //关卡封面
            writer.Close();
        }
        catch (Exception e)
        {
            //文件写入失败

        }

    }
    //从level文件读取Level
    public void LoadLevel(int _mapId)
    {
        try
        {
#if UNITY_IOS || UNITY_ANDROID
                FileStream fs = new FileStream(Application.persistentDataPath +"\\Level\\"+PublicDataManager.instance.GetLevelFilePath(_mapId)+".level", FileMode.Open);

#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            FileStream fs = new FileStream(Application.streamingAssetsPath + "\\Level\\" + PublicDataManager.instance.GetLevelFilePath(_mapId) + ".level", FileMode.Open);
#endif
            StreamReader reader = new StreamReader(fs);
            nowLevelId = int.Parse(reader.ReadLine());
            levelNameInputField.GetComponent<InputField>().text = reader.ReadLine();
            makerNameInputField.GetComponent<InputField>().text = reader.ReadLine();
            string tileInfoLine;//读取的一行
            string[] tileInfo;//以#分二段
            string[] posInfo;//position以,分三段
            while ((tileInfoLine = reader.ReadLine()) != null)
            {
                tileInfo = tileInfoLine.Split('#');

                posInfo = tileInfo[1].Split(',');
                Vector3Int position = new Vector3Int(int.Parse(posInfo[0]), int.Parse(posInfo[1]), int.Parse(posInfo[2]));

                GameObject obj = Instantiate(tilePrefabs[int.Parse(tileInfo[0])], position, Quaternion.identity);
                obj.name = tileInfo[0];
                obj.transform.SetParent(GetLayerObject(position.z).transform);
            }

        }
        catch (Exception e)
        {
            //读取level文件失败\
            Debug.Log(e.ToString());
        }

    }
    //读取level封面
    private Sprite LoadLevelImage(int _mapId)
    {
        WWW www = new WWW("file:///" + PublicDataManager.DATA_PATH + PublicDataManager.instance.GetLevelFilePath(_mapId) + ".png");
        if (www != null && string.IsNullOrEmpty(www.error))
        {
            return Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), Vector2.zero);
        }
        else
            return null;
    }
    //自动保存
    IEnumerator AutoSave()
    {
        levelName = levelNameInputField.GetComponent<InputField>().text;
        makerName = makerNameInputField.GetComponent<InputField>().text;
        if (levelName != null && makerName != null)
        {
            if (Time.time - lastSaveTime >= saveSpan)
            {
                if (layerOne.transform.childCount != 0 || layerTwo.transform.childCount != 0 || layerThree.transform.childCount != 0)
                {
                    SaveLevel();
                }
            }
            yield return null;
        }
    }
}
