using PurrNet.UI;
using UnityEngine;

public class TestPushWindow : MonoBehaviour
{
    [SerializeField] private ViewStack _stack;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _stack.Push<DialogView>().Setup("Hello", "This is a test dialog.");
        }
    }
}
