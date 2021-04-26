using UnityEditor;
using UnityEngine;

public class VideoURLFiller : EditorWindow
{
    Vector2 scrollPos;
    
    string googleFormsURL = null;

    private bool showImages = false;
    
    private string convertedUrl;
    
    [MenuItem("Fax Analytics/How-to: Google Forms")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = (VideoURLFiller)EditorWindow.GetWindow(typeof(VideoURLFiller));
        window.Show();
    }
    
    private void OnGUI()
    {
        var labelWrap = new GUIStyle(GUI.skin.GetStyle("label"))
        {
            wordWrap = true
        };
        GUILayout.Label(
            "This guide will help you make a Google Forms URL to use analytics in your world.",
            EditorStyles.helpBox);
        
        GUILayout.Space(14);
        showImages = GUILayout.Toggle(showImages, "Show me example images, I'm a visual learner.");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("1. Create a form on forms.google.com", labelWrap); 
        
        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Blank.png"));
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("2. Add a question. It should be \"Multiple choice\".", labelWrap); 

        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Question.png"));
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("3. Click the three dots in the top-right and click \"Get pre-filled link\".", labelWrap);

        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Dots.png"));

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("4. Fill the form, selecting a single answer. Then click \"Get Link\".", labelWrap);

        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Prefill.png"));
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("5. In the bottom left, click \"Copy link\".", labelWrap);
        
        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_CopyLink.png"));

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("6. Modify the URL to auto-submit. You can do this manually, or convert it automatically below.");
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUILayout.Label("Automatic", EditorStyles.boldLabel);

        googleFormsURL = EditorGUILayout.TextField("URL from step 5", googleFormsURL);

        var button = GUILayout.Button("Convert the URL!");
        if (button)
        {
            if (googleFormsURL.StartsWith("https://docs.google.com/forms/d/e/") && googleFormsURL.Contains("/viewform?"))
            {
                convertedUrl = googleFormsURL.Replace("viewform", "formResponse")  + "&submit=Submit";
            }
            else
            {
                convertedUrl = "Hmm... That URL looks wrong. Are you sure you performed steps 4 and 5 correctly?";
            }
        }

        GUILayout.Label("Result (copy this to your clipboard):", EditorStyles.boldLabel);
        GUILayout.TextArea(convertedUrl);
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUILayout.Label("... or do it manually", EditorStyles.boldLabel);
        
        GUILayout.Label("In the URL you got from step 5:\n" +
                        "- Change viewform to formResponse\n" +
                        "- Add this to the end of the link: &submit=Submit", labelWrap);
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUILayout.Label("7. Paste the URL into the Url field of an AreaAnalytics prefab in your Unity scene.");
        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Component.png"));
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("8. Done!");
        
        GUILayout.Label(
            "You can test the URL by opening it in your browser. "+
            "If it's correct, you should see one new response in your form!"
            , labelWrap);
        
        if (showImages) GUILayout.Box(AssetDatabase.LoadAssetAtPath<Texture>("Assets/FaxAnalytics/Textures/Faxample_Response.png"));
        
        GUILayout.Label(
            "If you want more analytics areas, simple place more prefabs and create more questions in your form."+
            "And generate more pre-filled forms as described above. Go nuts!", labelWrap);
        
        GUILayout.Label("Please credit 'Faxmachine' in your world. Contact me on Discord if you have any questions: Fax#6041");
        
        GUILayout.EndScrollView();
    }
}
 