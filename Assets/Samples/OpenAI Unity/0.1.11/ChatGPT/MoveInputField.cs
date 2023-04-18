using UnityEngine;
using UnityEngine.UI;

public class MoveInputField : MonoBehaviour
{
    public InputField inputField;
    public RectTransform sendButton;

    private Vector2 originalInputFieldPos;
    private Vector2 originalSendButtonPos;
    private float keyboardHeight;

    private void Start()
    {
        originalInputFieldPos = inputField.GetComponent<RectTransform>().anchoredPosition;
        originalSendButtonPos = sendButton.anchoredPosition;

        Button sendButtonComponent = sendButton.GetComponent<Button>();
        sendButtonComponent.onClick.AddListener(OnEndEdit);
    }

    private void Update()
    {
        //if (TouchScreenKeyboard.visible)
        //{
            keyboardHeight = TouchScreenKeyboard.area.height;

            float newInputFieldPosY = (Screen.height - keyboardHeight) / 2 - inputField.GetComponent<RectTransform>().sizeDelta.y / 2;
            inputField.GetComponent<RectTransform>().anchoredPosition = new Vector2(originalInputFieldPos.x, newInputFieldPosY);
            
            float newSendButtonPosY = (Screen.height - keyboardHeight) / 2 - sendButton.sizeDelta.y / 2;
            sendButton.anchoredPosition = new Vector2(originalSendButtonPos.x, newSendButtonPosY);
            
        //}
    }

    public void OnEndEdit()
    {
        inputField.GetComponent<RectTransform>().anchoredPosition = originalInputFieldPos;
        sendButton.anchoredPosition = originalSendButtonPos;
    }
}
