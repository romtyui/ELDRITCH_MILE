using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 這個方法稍後會綁定在 START 按鈕上
    public void LoadUIScene()
    {
        SceneManager.LoadScene("UIScene");
    }
}