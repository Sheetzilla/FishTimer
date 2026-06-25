using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class PasswordCheck : MonoBehaviour
{
    public TMP_InputField passwordField;
    public string nextScene = "GameScene";
    public TextMeshProUGUI statusText;
    const string UnlockKey = "unlocked";
    const string DeviceIdKey = "DeviceId";

    void Start()
    {
        if (PlayerPrefs.GetInt(UnlockKey, 0) == 1)
            SceneManager.LoadScene(nextScene);
    }

    public void Submit()
    {
        string key = passwordField.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(key))
            return;
        statusText.text = "Checking...";
        StartCoroutine(ValidateKey(key));
    }

    IEnumerator ValidateKey(string key)
    {
        string deviceId = PlayerPrefs.GetString(DeviceIdKey, "");
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(DeviceIdKey, deviceId);
            PlayerPrefs.Save();
        }

        string url =
            "https://shopkofi.sleetsheet-st.workers.dev/validate?key=" +
            UnityWebRequest.EscapeURL(key) +
            "&device=" +
            UnityWebRequest.EscapeURL(deviceId);

        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success &&
            req.downloadHandler.text == "valid")
        {
            PlayerPrefs.SetInt(UnlockKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            if (req.downloadHandler.text == "limit")
                statusText.text = "Device limit reached. Contact support.";
            else
                statusText.text = "Invalid key. Please try again.";
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey(UnlockKey);
        PlayerPrefs.Save();
    }
}