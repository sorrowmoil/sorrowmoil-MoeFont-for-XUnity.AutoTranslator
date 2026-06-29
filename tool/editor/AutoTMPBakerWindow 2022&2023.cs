#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public enum AutoTMPRenderMode { SMOOTH_HINTED, SMOOTH, RASTER_HINTED, RASTER, SDF, SDF8, SDF16, SDF32, SDFAA_HINTED, SDFAA }
public enum AutoTMPPointSizeMode { Auto, Custom }
public enum CharacterSource { ParentAsset, TXTFile }

public class AutoTMPBakerWindow : EditorWindow
{
    [Serializable]
    public class BakeTask
    {
        public string TaskName = "New Task";
        public Font SourceFont;
        public CharacterSource CharSource = CharacterSource.ParentAsset;
        public TMP_FontAsset ParentAsset;
        public TextAsset CharTxtFile;
        public string OutputFolder = "Assets/TMPGenerated";

        public int AtlasWidth = 8192;
        public int AtlasHeight = 8192;

        public AutoTMPPointSizeMode SizeMode = AutoTMPPointSizeMode.Auto;
        public int PointSize = 72;
        public int Padding = 5;

        public AutoTMPRenderMode RenderMode = AutoTMPRenderMode.SDF32;
        public bool SaveGlyphReport = true;
        public bool ImportKerningPairs = true;

        public string CustomAssetName = "";

        public bool Foldout = true;
        [HideInInspector] public bool isDone = false;
        [HideInInspector] public bool isFailed = false;
    }

    private List<BakeTask> tasks = new List<BakeTask>();
    private Vector2 scroll;
    private GUIStyle headerStyle;
    private GUIStyle cardStyle;

    private readonly int[] atlasSizeValues = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
    private readonly string[] atlasSizeNames = { "8", "16", "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };

    // 状态机（增加分帧添加状态）
    private enum MainState { Idle, Setup, AutoSizing, FinalBake_Init, FinalBake_Adding, FinalBake_Save }
    private MainState mainState = MainState.Idle;
    private int currentTaskIndex = 0;

    private int autoSizeMin = 9;
    private int autoSizeMax = 1000;
    private int autoSizeBest = 9;
    private uint[] currentCharCodes;
    private GlyphRenderMode currentRenderMode;

    private TMP_FontAsset currentAsset;
    private uint[] remainingCodes;            // 分帧添加时剩余的待添加字符
    private List<uint> missingCharsList = new List<uint>();
    private const int CHARS_PER_FRAME = 150; // 每帧处理字符数

    private string progressMessage = "";
    private float progressValue = 0f;
    private bool showProgressBar = false;

    [MenuItem("Tools/Auto TMP Baker (分帧添加 150)")]
    public static void Open() { GetWindow<AutoTMPBakerWindow>("Auto TMP Baker"); }

    void OnEnable() { if (tasks.Count == 0) tasks.Add(new BakeTask()); }

    void InitStyles()
    {
        if (headerStyle == null) headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
        if (cardStyle == null) cardStyle = new GUIStyle("HelpBox") { padding = new RectOffset(10, 10, 10, 10) };
    }

    void OnGUI()
    {
        InitStyles();

        DrawHeader();
        GUILayout.Space(8);

        if (!HasTMPEssentialResources())
        {
            EditorGUILayout.HelpBox(
                "未检测到 TMP 必要资源，烘焙可能失败。请点击下方按钮导入。",
                MessageType.Warning);

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("导入 TMP 必要资源", GUILayout.Height(30)))
            {
                ImportTMPEssentialResources();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);
        }

        DrawToolbar();
        GUILayout.Space(8);

        if (showProgressBar)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"进度: {progressMessage}", EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(r, progressValue, progressMessage);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        int taskToRemoveIndex = -1;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < tasks.Count; i++)
        {
            if (DrawTask(tasks[i], i)) taskToRemoveIndex = i;
        }
        EditorGUILayout.EndScrollView();

        if (taskToRemoveIndex >= 0)
        {
            tasks.RemoveAt(taskToRemoveIndex);
            GUIUtility.ExitGUI();
        }
    }

    bool HasTMPEssentialResources()
    {
        return TMP_Settings.defaultFontAsset != null;
    }

    void ImportTMPEssentialResources()
    {
        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        Debug.Log("已执行 TMP Essential Resources 导入命令。");
    }

    void DrawHeader()
    {
        GUILayout.Space(10);
        GUILayout.Label("Auto TMP Baker (分帧添加)", headerStyle);
        GUILayout.Label($"Unity {Application.unityVersion} | 每帧 150 字符 · 无阻塞");
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = mainState == MainState.Idle;
        if (GUILayout.Button("Add Task", GUILayout.Height(30))) tasks.Add(new BakeTask());
        GUI.enabled = true;

        bool isRunning = mainState != MainState.Idle;
        GUI.backgroundColor = isRunning ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.8f, 0.2f);

        string btnText = isRunning ? $"⏹ 强制停止 - [{currentTaskIndex + 1}/{tasks.Count}]" : "▶ 开始异步烘焙 (Run All)";
        if (GUILayout.Button(btnText, GUILayout.Height(30)))
        {
            if (isRunning) StopBaking(true);
            else StartBaking();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    bool DrawTask(BakeTask task, int index)
    {
        bool requestRemove = false;
        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.BeginHorizontal();
        task.Foldout = EditorGUILayout.Foldout(task.Foldout, task.TaskName, true);
        string status = task.isDone ? "✅完成" : (task.isFailed ? "❌失败" : "⏳等待");
        GUILayout.Label(status, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (!task.Foldout) { EditorGUILayout.EndVertical(); return false; }

        EditorGUI.indentLevel++;
        task.TaskName = EditorGUILayout.TextField("Task Name", task.TaskName);
        task.SourceFont = (Font)EditorGUILayout.ObjectField("Source Font", task.SourceFont, typeof(Font), false);

        task.CharSource = (CharacterSource)EditorGUILayout.EnumPopup("Character Source", task.CharSource);
        if (task.CharSource == CharacterSource.ParentAsset)
            task.ParentAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Parent Asset", task.ParentAsset, typeof(TMP_FontAsset), false);
        else
            task.CharTxtFile = (TextAsset)EditorGUILayout.ObjectField("TXT File", task.CharTxtFile, typeof(TextAsset), false);

        task.OutputFolder = EditorGUILayout.TextField("Output Folder", task.OutputFolder);

        task.CustomAssetName = EditorGUILayout.TextField("Custom Asset Name", task.CustomAssetName);
        if (string.IsNullOrWhiteSpace(task.CustomAssetName))
        {
            EditorGUILayout.LabelField("  (留空将自动使用字体名)");
        }

        GUILayout.Space(4);
        EditorGUILayout.LabelField("Atlas & Render Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        task.AtlasWidth = EditorGUILayout.IntPopup("Atlas Width", task.AtlasWidth, atlasSizeNames, atlasSizeValues);
        task.AtlasHeight = EditorGUILayout.IntPopup("Atlas Height", task.AtlasHeight, atlasSizeNames, atlasSizeValues);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        task.SizeMode = (AutoTMPPointSizeMode)EditorGUILayout.EnumPopup("Point Size", task.SizeMode);
        if (task.SizeMode == AutoTMPPointSizeMode.Custom) task.PointSize = EditorGUILayout.IntField(task.PointSize);
        else EditorGUILayout.LabelField("(后台分帧推演，上限 1000)");
        EditorGUILayout.EndHorizontal();

        task.Padding = EditorGUILayout.IntField("Padding", task.Padding);

        EditorGUILayout.BeginHorizontal();
        task.RenderMode = (AutoTMPRenderMode)EditorGUILayout.EnumPopup("Render Mode", task.RenderMode);
        task.SaveGlyphReport = EditorGUILayout.Toggle("Save Glyph Report", task.SaveGlyphReport);
        EditorGUILayout.EndHorizontal();

        task.ImportKerningPairs = EditorGUILayout.Toggle("Import Kerning Pairs", task.ImportKerningPairs);
        if (task.CharSource == CharacterSource.TXTFile && task.ImportKerningPairs)
        {
            EditorGUILayout.HelpBox("TXT 字符源不支持自动导入 Kerning，将跳过。", MessageType.Warning);
        }

        GUILayout.Space(5);
        GUI.enabled = mainState == MainState.Idle;
        if (GUILayout.Button("Remove (删除)")) requestRemove = true;
        GUI.enabled = true;

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        return requestRemove;
    }

    void StartBaking()
    {
        if (!HasTMPEssentialResources())
        {
            EditorUtility.DisplayDialog("缺少 TMP 必要资源",
                "请先导入 TMP Essential Resources（窗口顶部有按钮）。", "确定");
            return;
        }

        foreach (var t in tasks) { t.isDone = false; t.isFailed = false; }
        currentTaskIndex = 0;
        mainState = MainState.Setup;
        showProgressBar = true;
        progressMessage = "准备中...";
        progressValue = 0f;
        EditorApplication.update += AsyncBakeRoutine;
    }

    void StopBaking(bool manualCancel = false)
    {
        if (currentAsset != null && mainState != MainState.FinalBake_Save)
        {
            DestroyImmediate(currentAsset);
            currentAsset = null;
        }
        mainState = MainState.Idle;
        EditorApplication.update -= AsyncBakeRoutine;
        showProgressBar = false;
        Repaint();
        if (manualCancel) Debug.LogWarning("🛑 已强制停止烘焙任务！");
    }

    void AsyncBakeRoutine()
    {
        if (currentTaskIndex >= tasks.Count)
        {
            progressMessage = "全部完成!";
            progressValue = 1f;
            Debug.Log("🎉 [AutoTMP] 所有列队任务已完成！");
            StopBaking();
            return;
        }

        BakeTask task = tasks[currentTaskIndex];
        if (task.isDone || task.isFailed)
        {
            currentTaskIndex++;
            mainState = MainState.Setup;
            return;
        }

        try
        {
            switch (mainState)
            {
                case MainState.Setup:
                    if (task.SourceFont == null) throw new Exception("源字体为空！");
                    currentCharCodes = GetCharacterCodes(task);
                    if (currentCharCodes.Length == 0) throw new Exception("未能提取到任何有效字符！");
                    Enum.TryParse(task.RenderMode.ToString(), out currentRenderMode);

                    progressMessage = $"开始处理: {task.TaskName}";
                    progressValue = (float)currentTaskIndex / tasks.Count;

                    if (task.SizeMode == AutoTMPPointSizeMode.Auto)
                    {
                        autoSizeMin = 9;
                        autoSizeMax = 1000;
                        autoSizeBest = 9;
                        mainState = MainState.AutoSizing;
                    }
                    else
                    {
                        mainState = MainState.FinalBake_Init;
                    }
                    break;

                case MainState.AutoSizing:
                    if (autoSizeMin <= autoSizeMax)
                    {
                        int midSize = (autoSizeMin + autoSizeMax) / 2;
                        progressMessage = $"自动推算字号: 测试 {midSize}px (范围 9~1000)";
                        progressValue = Mathf.Lerp(0.3f, 0.7f, (float)(midSize - 9) / (1000 - 9));

                        TMP_FontAsset tempAsset = TMP_FontAsset.CreateFontAsset(
                            task.SourceFont, midSize, task.Padding, currentRenderMode,
                            task.AtlasWidth, task.AtlasHeight, AtlasPopulationMode.Dynamic);

                        // 一次性测试所有字符（字号推算阶段必须全量测试）
                        SafeAddCharacters(tempAsset, currentCharCodes, out uint[] missing);

                        if (missing == null || missing.Length == 0)
                        {
                            autoSizeBest = midSize;
                            autoSizeMin = midSize + 1;
                        }
                        else
                        {
                            autoSizeMax = midSize - 1;
                        }

                        DestroyImmediate(tempAsset);
                        Repaint();
                        return;
                    }
                    else
                    {
                        task.PointSize = autoSizeBest;
                        Debug.Log($"💡 [{task.TaskName}] 最佳字号: {autoSizeBest}");
                        mainState = MainState.FinalBake_Init;
                    }
                    break;

                case MainState.FinalBake_Init:
                    progressMessage = $"创建图集: {task.TaskName}";
                    progressValue = 0.8f;
                    Repaint();

                    currentAsset = TMP_FontAsset.CreateFontAsset(
                        task.SourceFont, task.PointSize, task.Padding, currentRenderMode,
                        task.AtlasWidth, task.AtlasHeight, AtlasPopulationMode.Dynamic);

                    // 计算待添加字符（排除已自动包含的）
                    HashSet<uint> existing = new HashSet<uint>();
                    foreach (var c in currentAsset.characterTable) existing.Add(c.unicode);
                    remainingCodes = currentCharCodes.Where(u => !existing.Contains(u)).ToArray();
                    missingCharsList.Clear();

                    mainState = MainState.FinalBake_Adding;
                    // 直接返回，下一帧开始分批添加
                    return;

                case MainState.FinalBake_Adding:
                    // 分批添加字符，每帧最多 CHARS_PER_FRAME 个
                    if (remainingCodes.Length > 0)
                    {
                        int batchCount = Mathf.Min(CHARS_PER_FRAME, remainingCodes.Length);
                        uint[] batch = new uint[batchCount];
                        Array.Copy(remainingCodes, batch, batchCount);

                        // 尝试添加本批字符
                        uint[] batchMissing;
                        try
                        {
                            currentAsset.TryAddCharacters(batch, out batchMissing);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"批量添加 {batchCount} 字符异常: {ex.Message}，切换为逐个添加...");
                            List<uint> fallbackMissing = new List<uint>();
                            foreach (uint code in batch)
                            {
                                try
                                {
                                    currentAsset.TryAddCharacters(new uint[] { code }, out uint[] singleMissing);
                                    if (singleMissing != null) fallbackMissing.AddRange(singleMissing);
                                }
                                catch (Exception innerEx)
                                {
                                    Debug.LogWarning($"跳过 U+{code:X4}: {innerEx.Message}");
                                    fallbackMissing.Add(code);
                                }
                            }
                            batchMissing = fallbackMissing.ToArray();
                        }

                        if (batchMissing != null) missingCharsList.AddRange(batchMissing);

                        // 更新剩余数组
                        int newLength = remainingCodes.Length - batchCount;
                        if (newLength > 0)
                        {
                            uint[] newRemaining = new uint[newLength];
                            Array.Copy(remainingCodes, batchCount, newRemaining, 0, newLength);
                            remainingCodes = newRemaining;
                        }
                        else
                        {
                            remainingCodes = new uint[0];
                        }

                        int total = currentCharCodes.Length;
                        int processed = total - remainingCodes.Length;
                        progressMessage = $"添加字符... ({processed}/{total})";
                        progressValue = Mathf.Lerp(0.8f, 0.95f, (float)processed / total);
                        Repaint();
                        return; // 本帧结束，等下一帧
                    }
                    else
                    {
                        // 全部添加完成，进入保存状态
                        mainState = MainState.FinalBake_Save;
                        // 继续在当前帧执行保存（也可不调用直接等下一帧，但保存很快）
                        // 这里直接 break 让 switch 结束，下一帧会执行到 Save 状态
                        break;
                    }

                case MainState.FinalBake_Save:
                    FinishFinalBake(task);
                    task.isDone = true;
                    progressMessage = $"完成: {task.TaskName}";
                    progressValue = (float)(currentTaskIndex + 1) / tasks.Count;
                    currentTaskIndex++;
                    mainState = MainState.Setup;
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoTMP] 处理 {task.TaskName} 异常：{e.Message}\n{e.StackTrace}");
            task.isFailed = true;
            if (currentAsset != null) { DestroyImmediate(currentAsset); currentAsset = null; }
            currentTaskIndex++;
            mainState = MainState.Setup;
        }

        Repaint();
    }

    // ---------- 字符提取 ----------
    uint[] GetCharacterCodes(BakeTask task)
    {
        if (task.CharSource == CharacterSource.ParentAsset)
        {
            if (task.ParentAsset == null) throw new Exception("未指定父 TMP 资产！");
            return ExtractCharacterCodes(task.ParentAsset);
        }
        else
        {
            if (task.CharTxtFile == null) throw new Exception("未指定 TXT 文件！");
            return ParseTxtCharacterCodes(task.CharTxtFile.text);
        }
    }

    uint[] ExtractCharacterCodes(TMP_FontAsset parent)
    {
        if (parent.characterTable == null) return new uint[0];
        HashSet<uint> uniqueCodes = new HashSet<uint>();
        foreach (var c in parent.characterTable) uniqueCodes.Add(c.unicode);
        return uniqueCodes.ToArray();
    }

    uint[] ParseTxtCharacterCodes(string content)
    {
        HashSet<uint> codes = new HashSet<uint>();
        using (StringReader reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Length == 1) { codes.Add(line[0]); continue; }

                uint code;
                if (line.StartsWith("U+", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    string hex = line.Substring(2);
                    if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                        codes.Add(code);
                    continue;
                }
                if (uint.TryParse(line, out code)) codes.Add(code);
                else if (line.Length > 1) foreach (char c in line) codes.Add(c);
            }
        }
        return codes.ToArray();
    }

    // 安全添加（用于自动推算阶段的全量测试）
    void SafeAddCharacters(TMP_FontAsset asset, uint[] unicodes, out uint[] missing)
    {
        HashSet<uint> existing = new HashSet<uint>();
        foreach (var c in asset.characterTable) existing.Add(c.unicode);
        uint[] toAdd = unicodes.Where(u => !existing.Contains(u)).ToArray();
        if (toAdd.Length == 0)
        {
            missing = new uint[0];
            return;
        }
        try
        {
            asset.TryAddCharacters(toAdd, out missing);
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"[SafeAdd] 批量添加触发字典冲突，切换为逐个添加...");
            List<uint> missingList = new List<uint>();
            foreach (uint code in toAdd)
            {
                try
                {
                    asset.TryAddCharacters(new uint[] { code }, out uint[] singleMissing);
                    if (singleMissing != null) missingList.AddRange(singleMissing);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SafeAdd] 跳过 U+{code:X4}: {ex.Message}");
                    missingList.Add(code);
                }
            }
            missing = missingList.ToArray();
        }
    }

    // ---------- Kerning 导入 ----------
    void TryCopyKerningFromParent(TMP_FontAsset targetAsset, BakeTask task)
    {
        if (!task.ImportKerningPairs) return;
        if (task.CharSource != CharacterSource.ParentAsset || task.ParentAsset == null)
        {
            Debug.LogWarning("[Kerning] TXT 字符源不支持自动导入 kerning，已跳过。");
            return;
        }

        try
        {
            PropertyInfo parentProp = typeof(TMP_FontAsset).GetProperty("kerningTable", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo targetProp = typeof(TMP_FontAsset).GetProperty("kerningTable", BindingFlags.Public | BindingFlags.Instance);
            if (parentProp == null || targetProp == null) return;

            var parentKerning = parentProp.GetValue(task.ParentAsset) as System.Collections.IList;
            if (parentKerning == null || parentKerning.Count == 0) return;

            Type kerningListType = parentKerning.GetType();
            var newList = Activator.CreateInstance(kerningListType) as System.Collections.IList;
            foreach (var item in parentKerning) newList.Add(item);

            targetProp.SetValue(targetAsset, newList);
            Debug.Log($"🔤 已从父资产复制 {newList.Count} 个 kerning pairs (反射)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Kerning] 导入失败: {e.Message}");
        }
    }

    void FinishFinalBake(BakeTask task)
    {
        if (currentAsset.characterTable == null || currentAsset.characterTable.Count == 0)
            throw new Exception("生成失败！可能 TTF 字体本身不包含所需字符。");

        TryCopyKerningFromParent(currentAsset, task);

        currentAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        Texture2D atlas = currentAsset.atlasTexture;
        Material mat = currentAsset.material;

        atlas.hideFlags = HideFlags.None;
        mat.hideFlags = HideFlags.None;
        currentAsset.hideFlags = HideFlags.None;

        string assetFileName = task.SourceFont.name;
        if (!string.IsNullOrWhiteSpace(task.CustomAssetName))
            assetFileName = task.CustomAssetName.Trim();

        atlas.name = $"{assetFileName}_Atlas";
        mat.name = $"{assetFileName}_Material";
        currentAsset.name = assetFileName;

        if (!AssetDatabase.IsValidFolder(task.OutputFolder)) CreateFolders(task.OutputFolder);
        string savePath = $"{task.OutputFolder}/{currentAsset.name}.asset";

        AssetDatabase.CreateAsset(currentAsset, savePath);
        AssetDatabase.AddObjectToAsset(atlas, currentAsset);
        AssetDatabase.AddObjectToAsset(mat, currentAsset);

        EditorUtility.SetDirty(currentAsset);
        EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(mat);

        if (task.SaveGlyphReport)
        {
            int totalRequested = currentCharCodes.Length;
            int totalBaked = currentAsset.characterTable.Count;
            GenerateAndSaveReport(task, totalRequested, totalBaked, missingCharsList.ToArray());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ [{task.TaskName}] 烘焙成功！已保存至 {savePath}");
        currentAsset = null;
    }

    void GenerateAndSaveReport(BakeTask task, int totalRequested, int totalBaked, uint[] missingChars)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=========================================");
        report.AppendLine("   Auto TMP Baker - Glyph Report (Async) ");
        report.AppendLine("=========================================");
        report.AppendLine($"Source Font    : {task.SourceFont.name}");
        report.AppendLine($"Render Mode    : {task.RenderMode}");
        report.AppendLine($"Atlas Size     : {task.AtlasWidth} x {task.AtlasHeight}");
        report.AppendLine($"Final PointSize: {task.PointSize} {(task.SizeMode == AutoTMPPointSizeMode.Auto ? "(Auto)" : "(Custom)")}");
        report.AppendLine("-----------------------------------------");
        report.AppendLine($"Characters Requested : {totalRequested}");
        report.AppendLine($"Characters Baked     : {totalBaked}");

        if (missingChars != null && missingChars.Length > 0)
        {
            report.AppendLine($"Missing/Excluded     : {missingChars.Length}");
            report.AppendLine("\n[Missing/Excluded Characters]:");
            for (int i = 0; i < missingChars.Length; i++)
            {
                try { report.Append(char.ConvertFromUtf32((int)missingChars[i])); }
                catch { report.Append($"[U+{missingChars[i]:X4}]"); }
                if ((i + 1) % 50 == 0) report.AppendLine();
            }
            report.AppendLine();
        }
        else report.AppendLine($"Missing/Excluded     : 0 (完美烘焙)");

        report.AppendLine("=========================================");
        string reportPath = $"{task.OutputFolder}/{task.SourceFont.name}_GlyphReport.txt";
        File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
        Debug.Log($"📄 [{task.TaskName}] 排版报告已生成: {reportPath}");
    }

    void CreateFolders(string path)
    {
        string[] folders = path.Split('/');
        string current = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            string next = current + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, folders[i]);
            current = next;
        }
    }
}
#endif