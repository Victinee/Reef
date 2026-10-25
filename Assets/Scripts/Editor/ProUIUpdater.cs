using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class ProUIUpdater : EditorWindow
{
    [MenuItem("OceanGuard/Apply Malevolent UI Theme")]
    public static void ApplyFixes()
    {
        GameObject canvas = GameObject.Find("MenuCanvas");
        if (canvas != null)
        {
            TextMeshProUGUI[] textElements = canvas.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (TextMeshProUGUI txt in textElements)
            {
                // Main Title (Bone white fading into deep, bruised crimson)
                if (txt.text == "Reef")
                {
                    txt.enableVertexGradient = true;
                    txt.colorGradient = new VertexGradient(
                        new Color(0.95f, 0.95f, 0.95f), new Color(0.9f, 0.9f, 0.9f), 
                        new Color(0.5f, 0.0f, 0.05f), new Color(0.4f, 0.0f, 0.05f) 
                    );

                    Material titleMat = new Material(txt.fontSharedMaterial);
                    titleMat.EnableKeyword("UNDERLAY_ON");
                    titleMat.SetFloat("_UnderlayOffsetX", 0.9f);
                    titleMat.SetFloat("_UnderlayOffsetY", -0.9f);
                    titleMat.SetFloat("_UnderlayDilate", 0.7f);
                    titleMat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.95f)); 
                    txt.fontMaterial = titleMat;
                }
                // Subtitle (Brightened crimson, forced bold, heavy shadow for visibility)
                else if (txt.text.ToUpper().Contains("MARINE") || txt.text.ToUpper().Contains("CONSERVATION"))
                {
                    txt.color = new Color(1f, 0.25f, 0.3f, 1f); // Much brighter red
                    txt.fontStyle = FontStyles.Bold; // Forces physical thickness
                    txt.characterSpacing = 18f; 
                    
                    Material subMat = new Material(txt.fontSharedMaterial);
                    subMat.EnableKeyword("UNDERLAY_ON");
                    subMat.SetFloat("_UnderlayOffsetX", 0.6f);
                    subMat.SetFloat("_UnderlayOffsetY", -0.6f);
                    subMat.SetFloat("_UnderlayDilate", 0.5f); // Thicker shadow spread
                    subMat.SetColor("_UnderlayColor", new Color(0, 0, 0, 1f)); // Fully opaque black
                    txt.fontMaterial = subMat;
                }
                // Button Text
                else if (txt.text.ToUpper() == "PLAY" || txt.text.ToUpper() == "QUIT")
                {
                    txt.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                    txt.fontStyle = FontStyles.Bold;
                    txt.characterSpacing = 8f; 
                    
                    Material btnMat = new Material(txt.fontSharedMaterial);
                    btnMat.EnableKeyword("UNDERLAY_ON");
                    btnMat.SetFloat("_UnderlayOffsetX", 0.5f);
                    btnMat.SetFloat("_UnderlayOffsetY", -0.5f);
                    btnMat.SetFloat("_UnderlayDilate", 0.4f);
                    btnMat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.9f));
                    txt.fontMaterial = btnMat;
                }
            }

            Button[] buttons = canvas.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = new Color(0.02f, 0.01f, 0.01f, 0.9f); 
                }
                
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white; 
                cb.highlightedColor = new Color(0.9f, 0.1f, 0.15f, 1f); 
                cb.pressedColor = new Color(0.3f, 0.0f, 0.0f, 1f);      
                cb.selectedColor = Color.white;
                cb.fadeDuration = 0.15f; 
                btn.colors = cb;
            }

            Debug.Log("Malevolent UI Applied: Subtitle weight increased for visibility.");
        }
    }
}