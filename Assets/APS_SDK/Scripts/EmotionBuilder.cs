using System;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(EmotionBuilder))]
public class EmotionBuilderEditor : Editor
{

    SerializedProperty blendShapeWeights;
    private ArmatureLinker m_linkerArmature;
    private EmotionBuilderBase m_builderBase;

    private bool m_showWeights;
    private int thumbnailSize;
    
    int[] thumbnailSizes =
    {
        128,
        512,
    };

    void OnEnable()
    {
        blendShapeWeights = serializedObject.FindProperty("blendShapeWeights");
    }

    private string[] GetMeshBlendNames(ArmatureLinker linker)
    {
        if (linker.faceRenderer == null)
        {
            return null;
        }

        var mesh = linker.faceRenderer.sharedMesh;
        var blendshapeCount = mesh.blendShapeCount;
        var blendNames = new string[blendshapeCount];
        for (int i = 0; i < mesh.blendShapeCount; ++i)
        {
            blendNames[i] = mesh.GetBlendShapeName(i);
        }

        return blendNames;
    }

    private int updateCount = 0;

    protected virtual void OnEditorUpdate()
    {
        if (updateCount > 0
        ) //After editor updates once then it is okay to render and blendshapes are finally applied (HACK).
        {
            updateCount = 0;
            EditorApplication.update -= OnEditorUpdate;

            EmotionBuilder builder = (EmotionBuilder) target;
            EmotionBuilderBaseEditor.SaveChanges(builder);

            return;
        }

        updateCount++; //let the editor update - EXTREMLEY HACKY - But works...
    }

    public override void OnInspectorGUI()
    {
        EmotionBuilder builder = (EmotionBuilder) target;


        if (m_builderBase == null)
            m_builderBase = builder.GetComponent<EmotionBuilderBase>();

        if (m_linkerArmature == null)
            m_linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
        if (m_linkerArmature == null)
        {
            GUILayout.Space(15);
            GUIStyle customLabel = new GUIStyle("Label");
            customLabel.fontSize = 12;
            customLabel.normal.textColor = Color.red;
            customLabel.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("No \"ArmatureLinker\" component found on the avatar.", customLabel);
            GUILayout.Space(15);
            return;
        }

        if (m_linkerArmature.faceRenderer == null)
        {
            GUILayout.Space(15);
            GUIStyle customLabel = new GUIStyle("Label");
            customLabel.fontSize = 12;
            customLabel.normal.textColor = Color.red;
            customLabel.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("Missing \"faceRenderer\" on the \"ArmatureLinker\".", customLabel);
            GUILayout.Space(15);
            return;
        }

        var blendShapeCount = m_linkerArmature.faceRenderer.sharedMesh.blendShapeCount;
        if (builder.blendShapeWeights == null || builder.blendShapeWeights.Length != blendShapeCount)
            builder.blendShapeWeights = new float[blendShapeCount];

        if (thumbnailSize == 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
        }

        var thumbSize = thumbnailSizes[thumbnailSize];
        if (GUILayout.Button(builder.thumbnail, GUILayout.Width(thumbSize), GUILayout.Height(thumbSize)))
        {
            thumbnailSize++;
            thumbnailSize %= thumbnailSizes.Length;
        }

        if (thumbnailSize != 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
        }

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name:", EditorStyles.boldLabel, GUILayout.Width(42));
        builder.emotionName = EditorGUILayout.TextArea(builder.emotionName, GUILayout.Width(82));
        GUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(builder.emotionName) && m_builderBase.templatesNames != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Template:", EditorStyles.boldLabel, GUILayout.Width(74));
            var selection = EditorGUILayout.Popup(0, m_builderBase.templatesNames, GUILayout.Width(50));
            if (selection != 0)
            {
                if (CheckNameInUse(builder, m_builderBase.templatesNames[selection]))
                {
                    EditorUtility.DisplayDialog("Emotion Name Already In Use",
                        string.Format(
                            "An emotion with the name \"{0}\" already exists. Please change the name before saving changes.",
                            m_builderBase.templatesNames[selection]), "OK");
                }
                else
                {
                    builder.emotionName = m_builderBase.templatesNames[selection];
                    EmotionBuilderBase.SetupFromTemplate(m_linkerArmature, builder,
                        m_builderBase.templates[selection - 1]);
                    EditorApplication.update += OnEditorUpdate;
                }
            }

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Save Changes", GUILayout.Width(128)))
        {
            if (string.IsNullOrEmpty(builder.emotionName))
            {
                EditorUtility.DisplayDialog("Emotion Name Is Empty",
                    "Please enter a name for this emotion before saving.", "OK");
                return;
            }

            if (CheckNameInUse(builder, builder.emotionName))
            {
                EditorUtility.DisplayDialog("Emotion Name Already In Use",
                    string.Format(
                        "An emotion with the name \"{0}\" already exists. Please change the name before saving changes.",
                        builder.emotionName), "OK");
                return;
            }

            /*
            foreach (var emotionBuilder in builder.GetComponents<EmotionBuilder>())
            {
                if (emotionBuilder == builder)
                    continue;
                if (string.IsNullOrEmpty(emotionBuilder.emotionName))
                    continue;
                if (emotionBuilder.emotionName.Equals(builder.emotionName))
                {
                    EditorUtility.DisplayDialog("Emotion Name Already In Use",
                        string.Format("An emotion with the name \"{0}\" already exists. Please change the name before saving changes.", builder.emotionName), "OK");
                    return;
                }
            }
            */

            EmotionBuilderBaseEditor.SetWeights(builder, false);
            //SaveChanges(builder);
            EditorApplication.update += OnEditorUpdate;
        }

        if (GUILayout.Button("Set Weights", GUILayout.Width(128)))
        {
            EmotionBuilderBaseEditor.SetWeights(builder);

            if (builder.audioClip != null)
                PlayClip(builder.audioClip);
        }

        GUILayout.EndVertical();
        GUILayout.BeginVertical();

        //builder.key = (EmotionBuilder.RawKey)EditorGUILayout.EnumPopup("Keyboard Binding:", m_builderBase.templates);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("User Controls:", EditorStyles.boldLabel);
        GUILayout.Space(5);
        builder.key = (EmotionBuilder.RawKey) EditorGUILayout.EnumPopup("Keyboard Binding:", builder.key);


        builder.audioClip =
            (AudioClip) EditorGUILayout.ObjectField("Sound Effect", builder.audioClip, typeof(AudioClip));
        GUILayout.Space(10);

        builder.ignoreBlink = EditorGUILayout.Toggle("Ignore Blinking:", builder.ignoreBlink);
        GUILayout.Space(10);
        builder.onTime =
            EditorGUILayout.Slider(string.Format("On Timer ({0}):", builder.onTime == 0 ? "forever" : "seconds"),
                builder.onTime, 0, 3);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        EditorGUILayout.Separator();

        m_showWeights = EditorGUILayout.Foldout(m_showWeights, "Blendshape Weights");

        if (m_showWeights)
        {
            var blendNames = GetMeshBlendNames(m_linkerArmature);
            EditorGUI.indentLevel++;
            for (int i = 0; i < blendShapeCount; ++i)
            {
                BlendNameProperty(builder, blendShapeWeights.GetArrayElementAtIndex(i), i, blendNames);
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        //DrawDefaultInspector();
    }

    bool CheckNameInUse(EmotionBuilder builder, string emotionName)
    {
        foreach (var emotionBuilder in builder.GetComponents<EmotionBuilder>())
        {
            if (emotionBuilder == builder)
                continue;
            if (string.IsNullOrEmpty(emotionBuilder.emotionName))
                continue;
            if (emotionBuilder.emotionName.Equals(emotionName))
                return true;
        }

        return false;
    }

    private void BlendNameProperty(EmotionBuilder builder, SerializedProperty prop, int shapeArrayIndex,
        string[] blendNames)
    {
        if (blendNames == null)
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(name));
            return;
        }

        var shapeWeight = m_linkerArmature.faceRenderer.GetBlendShapeWeight(shapeArrayIndex);

        GUIContent content = new GUIContent()
        {
            text = String.Format("{0}%", shapeWeight),
            tooltip = "Test blendshape."
        };

        GUIStyle centeredTextStyle = new GUIStyle("label");
        centeredTextStyle.alignment = TextAnchor.MiddleCenter;

        var blendName = string.IsNullOrEmpty(blendNames[shapeArrayIndex])
            ? shapeArrayIndex.ToString()
            : blendNames[shapeArrayIndex];

        EditorGUILayout.LabelField(blendName, centeredTextStyle);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(content, GUILayout.ExpandWidth(false), GUILayout.Width(50)))
        {
            var isOn = shapeWeight != 0;
            //builder = (EmotionBuilder) FindObjectOfType(typeof(EmotionBuilder));
            m_linkerArmature.faceRenderer.SetBlendShapeWeight(shapeArrayIndex, isOn ? 0 : 100);
            builder.blendShapeWeights[shapeArrayIndex] = isOn ? 0 : 1;

            Debug.Log(String.Format("{0} (Idx: {1})", blendName, shapeArrayIndex));
        }

        var newSliderval = EditorGUILayout.Slider(builder.blendShapeWeights[shapeArrayIndex], 0, 1);
        if (newSliderval != builder.blendShapeWeights[shapeArrayIndex]) //Something changed
        {
            builder.blendShapeWeights[shapeArrayIndex] = newSliderval;
            m_linkerArmature.faceRenderer.SetBlendShapeWeight(shapeArrayIndex, newSliderval * 100);
        }

        GUILayout.EndHorizontal();
    }

    //play audio in editor - https://forum.unity.com/threads/way-to-play-audio-in-editor-using-an-editor-script.132042/#post-4767824
    public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
    {
        System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
            "PlayClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new System.Type[] {typeof(AudioClip), typeof(int), typeof(bool)},
            null
        );
        method.Invoke(
            null,
            new object[] {clip, startSample, loop}
        );
    }


}
#endif

[RequireComponent(typeof(EmotionBuilderBase))]
public class EmotionBuilder : MonoBehaviour
{
    public Texture thumbnail;

    public string emotionName;

    public bool ignoreBlink;

    public float onTime = 0;

    public float[] blendShapeWeights;

    public AudioClip audioClip;

    public RawKey key = 0x00;

    public Vector3 jawRotation = Vector3.zero;
    public Vector3 jawPosition = Vector3.zero;

    //Same as UnityRawInput bindings
    public enum RawKey : ushort
    {
        None = 0x00,
        LeftButton = 0x01,
        RightButton = 0x02,
        Cancel = 0x03,
        MiddleButton = 0x04,
        ExtraButton1 = 0x05,
        ExtraButton2 = 0x06,
        Back = 0x08,
        Tab = 0x09,
        Clear = 0x0C,
        Return = 0x0D,
        Shift = 0x10,
        Control = 0x11,
        Menu = 0x12,
        Pause = 0x13,
        CapsLock = 0x14,
        Kana = 0x15,
        Hangeul = 0x15,
        Hangul = 0x15,
        Junja = 0x17,
        Final = 0x18,
        Hanja = 0x19,
        Kanji = 0x19,
        Escape = 0x1B,
        Convert = 0x1C,
        NonConvert = 0x1D,
        Accept = 0x1E,
        ModeChange = 0x1F,
        Space = 0x20,
        Prior = 0x21,
        Next = 0x22,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        Select = 0x29,
        Print = 0x2A,
        Execute = 0x2B,
        Snapshot = 0x2C,
        Insert = 0x2D,
        Delete = 0x2E,
        Help = 0x2F,
        N0 = 0x30,
        N1 = 0x31,
        N2 = 0x32,
        N3 = 0x33,
        N4 = 0x34,
        N5 = 0x35,
        N6 = 0x36,
        N7 = 0x37,
        N8 = 0x38,
        N9 = 0x39,
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,
        LeftWindows = 0x5B,
        RightWindows = 0x5C,
        Application = 0x5D,
        Sleep = 0x5F,
        Numpad0 = 0x60,
        Numpad1 = 0x61,
        Numpad2 = 0x62,
        Numpad3 = 0x63,
        Numpad4 = 0x64,
        Numpad5 = 0x65,
        Numpad6 = 0x66,
        Numpad7 = 0x67,
        Numpad8 = 0x68,
        Numpad9 = 0x69,
        Multiply = 0x6A,
        Add = 0x6B,
        Separator = 0x6C,
        Subtract = 0x6D,
        Decimal = 0x6E,
        Divide = 0x6F,
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        F13 = 0x7C,
        F14 = 0x7D,
        F15 = 0x7E,
        F16 = 0x7F,
        F17 = 0x80,
        F18 = 0x81,
        F19 = 0x82,
        F20 = 0x83,
        F21 = 0x84,
        F22 = 0x85,
        F23 = 0x86,
        F24 = 0x87,
        NumLock = 0x90,
        ScrollLock = 0x91,
        NEC_Equal = 0x92,
        Fujitsu_Jisho = 0x92,
        Fujitsu_Masshou = 0x93,
        Fujitsu_Touroku = 0x94,
        Fujitsu_Loya = 0x95,
        Fujitsu_Roya = 0x96,
        LeftShift = 0xA0,
        RightShift = 0xA1,
        LeftControl = 0xA2,
        RightControl = 0xA3,
        LeftMenu = 0xA4,
        RightMenu = 0xA5,
        BrowserBack = 0xA6,
        BrowserForward = 0xA7,
        BrowserRefresh = 0xA8,
        BrowserStop = 0xA9,
        BrowserSearch = 0xAA,
        BrowserFavorites = 0xAB,
        BrowserHome = 0xAC,
        VolumeMute = 0xAD,
        VolumeDown = 0xAE,
        VolumeUp = 0xAF,
        MediaNextTrack = 0xB0,
        MediaPrevTrack = 0xB1,
        MediaStop = 0xB2,
        MediaPlayPause = 0xB3,
        LaunchMail = 0xB4,
        LaunchMediaSelect = 0xB5,
        LaunchApplication1 = 0xB6,
        LaunchApplication2 = 0xB7,
        OEM1 = 0xBA,
        OEMPlus = 0xBB,
        OEMComma = 0xBC,
        OEMMinus = 0xBD,
        OEMPeriod = 0xBE,
        OEM2 = 0xBF,
        OEM3 = 0xC0,
        OEM4 = 0xDB,
        OEM5 = 0xDC,
        OEM6 = 0xDD,
        OEM7 = 0xDE,
        OEM8 = 0xDF,
        OEMAX = 0xE1,
        OEM102 = 0xE2,
        ICOHelp = 0xE3,
        ICO00 = 0xE4,
        ProcessKey = 0xE5,
        ICOClear = 0xE6,
        Packet = 0xE7,
        OEMReset = 0xE9,
        OEMJump = 0xEA,
        OEMPA1 = 0xEB,
        OEMPA2 = 0xEC,
        OEMPA3 = 0xED,
        OEMWSCtrl = 0xEE,
        OEMCUSel = 0xEF,
        OEMATTN = 0xF0,
        OEMFinish = 0xF1,
        OEMCopy = 0xF2,
        OEMAuto = 0xF3,
        OEMENLW = 0xF4,
        OEMBackTab = 0xF5,
        ATTN = 0xF6,
        CRSel = 0xF7,
        EXSel = 0xF8,
        EREOF = 0xF9,
        Play = 0xFA,
        Zoom = 0xFB,
        Noname = 0xFC,
        PA1 = 0xFD,
        OEMClear = 0xFE
    }

}
