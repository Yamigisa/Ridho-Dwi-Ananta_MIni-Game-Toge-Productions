using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadInteraction : Interactable
{
    [SerializeField] private string destinationSpawnId;

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
