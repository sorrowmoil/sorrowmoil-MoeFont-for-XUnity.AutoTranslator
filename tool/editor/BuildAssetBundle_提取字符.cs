using UnityEngine;
using TMPro;
using UnityEditor;
using System.IO;
using System.Text;

public class TMPCharacterExporter
{
    [MenuItem("Tools/Export TMP Characters")]
    static void Export()
    {
        TMP_FontAsset font = Selection.activeObject as TMP_FontAsset;

        if (font == null)
        {
            Debug.LogError("请选择 TMP Font Asset");
            return;
        }

        StringBuilder sb = new StringBuilder();

        foreach (var c in font.characterTable)
        {
            sb.Append(char.ConvertFromUtf32((int)c.unicode));
        }

        string path = EditorUtility.SaveFilePanel(
            "导出字符集",
            "",
            font.name + "_charset.txt",
            "txt"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log("导出完成: " + path);
        }
    }
}