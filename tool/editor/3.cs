using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

public class ABBuildWindow : EditorWindow
{
    private string outputPath = "AssetBundles";
    private BuildTarget targetPlatform = BuildTarget.StandaloneWindows64; 
    private int compressionChoice = 1; 
    private bool clearFolderBeforeBuild = false;
    private bool autoFixFontOOM = true; 

    private Vector2 scrollPosition;

    private readonly string[] compressionLabels = new string[] {
        "❌ 不压缩\n(体积大/加载快)",
        "⚡ 推荐 LZ4\n(主流/平衡)",
        "📦 极限 LZMA\n(体积小/解压慢)"
    };

    // 视觉纹理缓存
    private Texture2D bgTexCard;
    private Texture2D bgTexInput;
    private Texture2D bgTexBtnNormal;
    private Texture2D glowButtonBg;

    [MenuItem("Tools/高级 AssetBundle 打包工作台")]
    public static void ShowWindow()
    {
        ABBuildWindow window = GetWindow<ABBuildWindow>("AB 打包面板");
        window.minSize = new Vector2(480, 540); 
    }

    private void OnDisable()
    {
        // 及时释放动态贴图，不给物理内存留一丝泄漏机会
        if (bgTexCard) DestroyImmediate(bgTexCard);
        if (bgTexInput) DestroyImmediate(bgTexInput);
        if (bgTexBtnNormal) DestroyImmediate(bgTexBtnNormal);
        if (glowButtonBg) DestroyImmediate(glowButtonBg);
    }

    private void OnGUI()
    {
        InitTextures();

        // 1. 强制铺上深邃的冷黑窗体底色板
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.12f, 0.13f, 0.14f, 1.0f));

        // 2. 渲染顶部冷黑 Banner
        DrawHeaderBanner();

        // 3. 全局高对比度现代文字样式
        GUIStyle highContrastLabel = new GUIStyle(EditorStyles.label) { fontSize = 12 };
        highContrastLabel.normal.textColor = new Color(0.8f, 0.82f, 0.85f);

        // 内凹扁平化文本输入框
        GUIStyle inputStyle = new GUIStyle(EditorStyles.textField) { fixedHeight = 22, alignment = TextAnchor.MiddleLeft };
        inputStyle.normal.background = bgTexInput;
        inputStyle.normal.textColor = Color.white;

        // 内凹扁平化下拉弹出菜单
        GUIStyle popupStyle = new GUIStyle(EditorStyles.popup) { fixedHeight = 22, alignment = TextAnchor.MiddleLeft };
        popupStyle.normal.background = bgTexInput;
        popupStyle.normal.textColor = Color.white;

        // 小功能按钮（如：browse 浏览按钮）
        GUIStyle subBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        subBtnStyle.normal.background = bgTexBtnNormal;
        subBtnStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        // 模块卡片容器样式
        GUIStyle boxCardStyle = new GUIStyle();
        boxCardStyle.normal.background = bgTexCard;
        boxCardStyle.margin = new RectOffset(16, 16, 6, 6);
        boxCardStyle.padding = new RectOffset(18, 18, 16, 16);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        GUILayout.Space(14);

        // ==================== 模块一：核心交付配置 ====================
        DrawSectionTitle("📁 核心交付配置", new Color(0.15f, 0.62f, 0.92f)); 
        GUILayout.BeginVertical(boxCardStyle);
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("导出相对路径", highContrastLabel, GUILayout.Width(95));
        outputPath = EditorGUILayout.TextField(outputPath, inputStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(6);
        if (GUILayout.Button("📂 browse", subBtnStyle, GUILayout.Width(75), GUILayout.Height(22)))
        {
            string folder = EditorUtility.OpenFolderPanel("选择输出目录", outputPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                    outputPath = "Assets" + folder.Substring(Application.dataPath.Length);
                else
                    outputPath = folder;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("目标发布平台", highContrastLabel, GUILayout.Width(95));
        targetPlatform = (BuildTarget)EditorGUILayout.EnumPopup(targetPlatform, popupStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.Space(12);

        // ==================== 模块二：编译优化策略 ====================
        DrawSectionTitle("⚡ 编译优化策略 (防呆与压缩配置)", new Color(0.02f, 0.78f, 0.58f)); 
        GUILayout.BeginVertical(boxCardStyle);

        GUILayout.Label("压衡打包策略:", highContrastLabel);
        GUILayout.Space(8);

        // 压缩网格选项美化
        GUIStyle gridStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 11, fixedHeight = 42 };
        gridStyle.normal.background = bgTexBtnNormal;
        gridStyle.normal.textColor = new Color(0.6f, 0.62f, 0.65f);
        gridStyle.onNormal.background = bgTexInput;
        gridStyle.onNormal.textColor = Color.white;
        gridStyle.active.background = bgTexInput;
        gridStyle.active.textColor = Color.white;
        compressionChoice = GUILayout.SelectionGrid(compressionChoice, compressionLabels, 3, gridStyle);

        GUILayout.Space(16);

        GUIStyle rightToggleStyle = new GUIStyle(EditorStyles.toggle);
        
        // 选项行 1：清空目录
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("编译前彻底清空目录 (防历史缓存干扰导致的异常)", highContrastLabel, GUILayout.ExpandWidth(true));
        clearFolderBeforeBuild = EditorGUILayout.Toggle(clearFolderBeforeBuild, rightToggleStyle, GUILayout.Width(20));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // 选项行 2：中文字体内存修复器开关
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🧬 拦截 TMP 字体大字库 Kerning 引起的 OOM 崩溃内存泄漏", highContrastLabel, GUILayout.ExpandWidth(true));
        autoFixFontOOM = EditorGUILayout.Toggle(autoFixFontOOM, rightToggleStyle, GUILayout.Width(20));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        // 4. 底部大动作触发区
        DrawBottomActionArea();
    }

    #region 🎨 霓虹核心渲染引擎

    private void InitTextures()
    {
        if (bgTexCard != null) return;

        // 生成低压冷灰纹理
        bgTexCard = CreatePureColorTexture(new Color(0.18f, 0.19f, 0.2f, 1.0f));
        bgTexInput = CreatePureColorTexture(new Color(0.13f, 0.14f, 0.15f, 1.0f));
        bgTexBtnNormal = CreatePureColorTexture(new Color(0.24f, 0.26f, 0.28f, 1.0f));

        // 🔮 纯代码手绘：带极光青碧外发光的渐变大按钮纹理（12x12 像素九宫格切片）
        glowButtonBg = new Texture2D(12, 12);
        Color glowColor = new Color(0.25f, 0.72f, 0.64f, 0.9f); // 极光外发光
        Color innerColor = new Color(0.14f, 0.42f, 0.35f, 1.0f); // 内部渐变碧绿
        
        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                // 边缘 2 像素渲染为高亮霓虹边框，内部渲染为底座色
                if (x < 2 || x > 9 || y < 2 || y > 9)
                    glowButtonBg.SetPixel(x, y, glowColor);
                else
                    glowButtonBg.SetPixel(x, y, innerColor);
            }
        }
        glowButtonBg.Apply();
    }

    private void DrawHeaderBanner()
    {
        Rect bannerRect = GUILayoutUtility.GetRect(position.width, 52);
        EditorGUI.DrawRect(bannerRect, new Color(0.09f, 0.1f, 0.11f, 1.0f));
        
        // 极光碧分割装饰线
        Rect lineRect = new Rect(bannerRect.x, bannerRect.yMax - 2, bannerRect.width, 2);
        EditorGUI.DrawRect(lineRect, new Color(0.05f, 0.75f, 0.65f, 1.0f));

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) {
            fontSize = 13, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal,
            normal = { textColor = new Color(0.92f, 0.95f, 0.98f) }
        };
        GUI.Label(bannerRect, "ASSETBUNDLE 自动化编译分发管线", titleStyle);
    }

    private void DrawSectionTitle(string title, Color accentColor)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(18);
        Rect tagRect = GUILayoutUtility.GetRect(4, 16);
        EditorGUI.DrawRect(tagRect, accentColor);
        GUILayout.Space(8);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, fontStyle = FontStyle.Normal };
        titleStyle.normal.textColor = accentColor;
        GUILayout.Label(title, titleStyle, GUILayout.Height(16));
        GUILayout.EndHorizontal();
    }

    private void DrawBottomActionArea()
    {
        Rect footerRect = GUILayoutUtility.GetRect(position.width, 75);
        EditorGUI.DrawRect(footerRect, new Color(0.09f, 0.1f, 0.11f, 1.0f));

        // 在底座区域中心算出一块完美的按钮矩阵
        Rect btnRect = new Rect(footerRect.x + 16, footerRect.y + 14, footerRect.width - 32, 44);

        // 🛠️ 终极还原：建立具备九宫格（Border）缩进能力的极光按钮 Style
        GUIStyle cyanGlowButtonStyle = new GUIStyle();
        cyanGlowButtonStyle.normal.background = glowButtonBg;
        cyanGlowButtonStyle.normal.textColor = Color.white;
        cyanGlowButtonStyle.fontSize = 15;
        cyanGlowButtonStyle.fontStyle = FontStyle.Normal;
        cyanGlowButtonStyle.alignment = TextAnchor.MiddleCenter;
        
        // 关键对齐属性：指定边框不随拉伸变形，完美复刻无死角霓虹包边
        cyanGlowButtonStyle.border = new RectOffset(3, 3, 3, 3);

        if (GUI.Button(btnRect, "开始执行标准化构建", cyanGlowButtonStyle))
        {
            ExecuteAssetBundleBuild();
        }
    }

    private Texture2D CreatePureColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    #endregion

    #region ⚙️ 自动化打包与大字库 OOM 阻断管线

    private void ExecuteAssetBundleBuild()
    {
        if (autoFixFontOOM)
        {
            FixTextMeshProKerningOOM();
        }

        string exportPath = outputPath;
        if (!Path.IsPathRooted(exportPath))
        {
            exportPath = Path.Combine(Application.dataPath, "../" + outputPath);
        }
        exportPath = Path.GetFullPath(exportPath);

        if (clearFolderBeforeBuild && Directory.Exists(exportPath))
        {
            try { Directory.Delete(exportPath, true); } catch {}
        }

        if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);

        BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
        if (compressionChoice == 0) options = BuildAssetBundleOptions.UncompressedAssetBundle;
        else if (compressionChoice == 1) options = BuildAssetBundleOptions.ChunkBasedCompression;
        else if (compressionChoice == 2) options = BuildAssetBundleOptions.None;

        BuildPipeline.BuildAssetBundles(outputPath, options, targetPlatform);
        AssetDatabase.Refresh();
        
        if (Directory.Exists(exportPath)) System.Diagnostics.Process.Start(exportPath);

        EditorUtility.DisplayDialog("Pipeline Status", "AssetBundle 编译流水线执行完毕！", "完成");
    }

    private void FixTextMeshProKerningOOM()
    {
        Assembly tmpAssembly = null;
        try { tmpAssembly = Assembly.Load("Unity.TextMeshPro"); }
        catch { tmpAssembly = Assembly.Load("Unity.TextMeshPro.Editor"); }

        if (tmpAssembly == null) return;

        System.Type fontAssetType = tmpAssembly.GetType("TMPro.TMP_FontAsset");
        if (fontAssetType == null) return;

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object fontAsset = AssetDatabase.LoadAssetAtPath(path, fontAssetType);
            if (fontAsset == null) continue;

            FieldInfo kerningTableField = fontAssetType.GetField("m_KerningTable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
                                      ?? fontAssetType.GetField("fontKerningTable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (kerningTableField != null)
            {
                object kerningTable = kerningTableField.GetValue(fontAsset);
                if (kerningTable != null)
                {
                    FieldInfo pairsField = kerningTable.GetType().GetField("StaticKerningPairs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        ?? kerningTable.GetType().GetField("m_KerningPair", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (pairsField != null)
                    {
                        System.Collections.IList pairsList = pairsField.GetValue(kerningTable) as System.Collections.IList;
                        if (pairsList != null && pairsList.Count > 500) 
                        {
                            pairsList.Clear(); // 拦截 FontEngine 堆内存爆炸，强行熔断大字库字距拓扑对
                            EditorUtility.SetDirty(fontAsset);
                            fixedCount++;
                        }
                    }
                }
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00FFCC>[FontEngine 安全核心]</color> 已全自动拦截并净化了 {fixedCount} 个大字库资源的 Kerning 内存开销。");
        }
    }

    #endregion
}