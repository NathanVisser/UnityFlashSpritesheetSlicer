using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Uses the XML file to slice a sprite sheet into individual sprites
/// </summary>
public class SpriteXMLSlicer : EditorWindow
{
    private Object xmlFile;
    private Texture2D spriteSheetFile;

    private string xmlPath;
    private string spriteSheetPath;

    private XmlReader xmlReader;

    private string errorString = string.Empty;

    [MenuItem("Custom/XML Slicer")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SpriteXMLSlicer));
    }

    private void OnGUI()
    {
        xmlFile = EditorGUILayout.ObjectField("xml file", xmlFile, typeof(Object), false);
        spriteSheetFile =
            EditorGUILayout.ObjectField("sprite sheet", spriteSheetFile, typeof(Texture2D), false)
            as Texture2D;

        if (xmlFile != null && spriteSheetFile != null)
        {
            if (GUILayout.Button("Process"))
            {
                Process();
            }
        }

        if (!string.IsNullOrEmpty(errorString))
        {
            GUI.color = Color.red;
            GUILayout.Label(errorString);
            GUI.color = Color.white;
        }
    }

    private void Process()
    {
        errorString = string.Empty;
        SubTexture[] subTextures = ReadSubtextures();

        if (subTextures == null)
        {
            return;
        }
        SliceSprite(subTextures);
    }

    private SubTexture[] ReadSubtextures()
    {
        Debug.Log("Reading subtextures from XML file");
        var path = GetTruePath(xmlFile);

        var fileStream = new FileStream(Application.dataPath + path, FileMode.Open);
        xmlReader = new XmlTextReader(fileStream);
        var subTextures = new List<SubTexture>();
        while (xmlReader.Read())
        {
            if (xmlReader.Name == "SubTexture")
            {
                var subtexture = new SubTexture();
                subtexture.x = int.Parse(xmlReader.GetAttribute("x") ?? string.Empty);
                subtexture.y = int.Parse(xmlReader.GetAttribute("y"));
                subtexture.width = int.Parse(xmlReader.GetAttribute("width"));
                subtexture.height = int.Parse(xmlReader.GetAttribute("height"));
                subtexture.name = xmlReader.GetAttribute("name");
                subTextures.Add(subtexture);
            }
        }

        if (subTextures.Count == 0)
        {
            errorString = "No subtextures found in XML";
            return null;
        }
        else
        {
            errorString = string.Empty;
            Debug.Log($"Found {subTextures.Count} subtextures");
            fileStream.Close();
            return subTextures.ToArray();
        }
    }

    private void SliceSprite(SubTexture[] subTextures)
    {
        Debug.Log($"Starting slicing with {subTextures.Length} subtextures");
        var path = AssetDatabase.GetAssetPath(spriteSheetFile);
        AssetDatabase.StartAssetEditing();

        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (textureImporter == null)
        {
            errorString = "Invalid sprite sheet";
            Debug.Log("TextureImporter is null");
            AssetDatabase.StopAssetEditing();
            return;
        }

        var height = spriteSheetFile.height;
        Debug.Log("Height: " + height);

        var metaDatas = new List<SpriteMetaData>();
        foreach (var subTexture in subTextures)
        {
            SpriteMetaData metaData = new SpriteMetaData();
            Vector2 position = new Vector2(subTexture.x, height - subTexture.y - subTexture.height);

            metaData.rect = new Rect(position.x, position.y, subTexture.width, subTexture.height);
            metaData.name = subTexture.name;
            Debug.Log(
                "Adding metadata for " + metaData.name + " at (data) rect " + metaData.rect + ""
            );
            Debug.Log("Data position: " + subTexture.x + ", " + subTexture.y);
            metaDatas.Add(metaData);
        }

        Debug.Log($"Applying {metaDatas.Count} metadatas to {path}");

        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        var spritesheet = metaDatas.ToArray();
        Debug.Log("Setting spritesheet " + spritesheet.Length);
        textureImporter.spritesheet = spritesheet;
        Debug.Log("Spritesheet set " + textureImporter.spritesheet.Length);
        textureImporter.SaveAndReimport();
        errorString = string.Empty;

        AssetDatabase.StopAssetEditing();
    }

    private string GetTruePath(Object target)
    {
        var path = AssetDatabase.GetAssetPath(xmlFile);
        var lastDot = path.LastIndexOf('.');

        if (path.Substring(lastDot) != ".xml")
        {
            errorString = "Invalid XML file";
            return null;
        }

        //Remove Assets from the path
        return path.Substring(6);
    }

    public struct SubTexture
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public string name;
    }
}
