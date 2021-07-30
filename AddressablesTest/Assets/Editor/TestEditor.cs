using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TestEditor
{
    private const string DLLPATH = "Assets/HotUpdate_DLL/HotUpdate.dll";
    private const string PDBPATH = "Assets/HotUpdate_DLL/HotUpdate.pdb";

    [MenuItem("Tools/Change DLL suffix name to bytes")]
    public static void ChangeDllName()
    {
        if (File.Exists(DLLPATH))
        {
            string targetPath = DLLPATH + ".bytes";
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(DLLPATH, targetPath);
        }

        if (File.Exists(PDBPATH))
        {
            string targetPath = PDBPATH + ".bytes";
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(PDBPATH, targetPath);
        }

        AssetDatabase.Refresh();
    }
}