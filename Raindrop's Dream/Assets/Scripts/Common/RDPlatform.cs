﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RDPlatform
{
    public static string DATA_PATH
    {
        get
        {
            return RDPlatform.isOSXEditor ? Application.persistentDataPath : Application.streamingAssetsPath;
        }
    }
    public static bool isOSXEditor = Application.platform == RuntimePlatform.OSXEditor;

    public static bool isWindowsEditor = Application.platform == RuntimePlatform.WindowsEditor;

    //拼接路径
    public static string SplitPath(string[] _paths){

        string splitCharacter = isOSXEditor ? "/" : "\\";

        string path = _paths[0];

        for (var i = 1; i < _paths.Length; i++){
            path += splitCharacter + _paths[i];
        }

        return path;
    }
}
