using Mono.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PhotoFrame : MonoBehaviour
{
    public RawImage image;
    public TMPro.TextMeshPro filenameLabel;
    public TMPro.TextMeshPro fileCountLabel;
    public TMPro.TextMeshPro fileMetadata;

    string dir = "/Users/cls/Projects/stable-diffusion/outputs/img-samples/";
    string DATABASE_NAME = "/images.sqlite3";

    string conn;
    string[] allImages;
    int imageIdx = 0;
    string filename;
    string? rating;
    string? is_me;
    IDbConnection dbconn;
    IDbCommand dbcmd;
    IDataReader dbreader;

    void Start()
    {
        string filepath = Application.dataPath + DATABASE_NAME;
        Debug.Log($"filepath={filepath}");
        conn = "URI=file:" + filepath;
        CreateTable();

        allImages = Directory.GetFiles(dir, "*.png");
        LoadImage();
        LoadStats();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ImageNext();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ImagePrevious();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            is_me = "no";
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            is_me = "yes";
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            is_me = "very";
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            rating = "1";
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            rating = "2";
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            rating = "3";
            UpdateUI();
        }
    }

    private void ImagePrevious()
    {
        SaveImageStats();
        // C# modulo doesn't wrap around, thanks, I hate it.
        imageIdx = (imageIdx - 1 + allImages.Length) % allImages.Length;
        LoadImage();
        LoadStats();
    }

    private void ImageNext()
    {
        SaveImageStats();
        imageIdx = (imageIdx + 1) % allImages.Length;
        LoadImage();
        LoadStats();
    }

    void LoadImage()
    {
        string path = allImages[imageIdx];
        var splitPath = path.Split("/");
        filename = splitPath[splitPath.Length - 1];

        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        image.GetComponent<RawImage>().texture = texture;
        UpdateUI();
    }

    void SaveImageStats()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            dbcmd.CommandText = $"INSERT INTO IMAGES(PATH, RATING, IS_ME) VALUES ('{filename}', '{rating}', '{is_me}') " +
                $"ON CONFLICT(PATH) DO UPDATE SET RATING = excluded.RATING, IS_ME = excluded.IS_ME; ";
            dbcmd.ExecuteScalar();
            dbconn.Close();
        }
    }

    void LoadStats()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            dbcmd.CommandText = $"SELECT RATING, IS_ME FROM IMAGES WHERE PATH = '{filename}'";
            IDataReader reader = dbcmd.ExecuteReader();

            rating = null;
            is_me = null;

            while (reader.Read())
            {
                rating = reader.GetString(reader.GetOrdinal("RATING"));
                is_me = reader.GetString(reader.GetOrdinal("IS_ME"));
            }

            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        filenameLabel.text = filename;
        fileMetadata.text = $"\nCurrent Rating:    {rating}\n\nIs me?:          {is_me}";
        fileCountLabel.text = $"Image {imageIdx} out of {allImages.Length}";
    }

    private void CreateTable()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            dbcmd.CommandText = "CREATE TABLE IF NOT EXISTS IMAGES (PATH text unique, RATING text, IS_ME text)";
            dbcmd.ExecuteScalar();
            dbconn.Close();
        }
    }
}
