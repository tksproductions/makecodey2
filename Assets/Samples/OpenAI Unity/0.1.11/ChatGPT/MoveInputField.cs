using UnityEngine;
using UnityEngine.UI;




public class MoveInputField : MonoBehaviour
{
  public InputField inputField;
  public RectTransform sendButton;
  public RectTransform messageArea;


  [SerializeField] private ScrollRect scroll;
  private Vector2 originalInputFieldPos;
  private Vector2 originalSendButtonPos;
  private Vector2 originalMessageAreaPos;
  private Vector2 originalMessageAreaSize;
  private float keyboardHeight;
  private float distanceFromBottom;




  private void Start()
  {
      originalInputFieldPos = inputField.GetComponent<RectTransform>().anchoredPosition;
      originalSendButtonPos = sendButton.anchoredPosition;
      originalMessageAreaPos = messageArea.GetComponent<RectTransform>().anchoredPosition;
      originalMessageAreaSize = messageArea.GetComponent<RectTransform>().sizeDelta;
      Button sendButtonComponent = sendButton.GetComponent<Button>();
      distanceFromBottom = originalSendButtonPos.y + sendButton.sizeDelta.y / 2f;
  }




private void Update()
{
  if (TouchScreenKeyboard.visible)
  {
      keyboardHeight = TouchScreenKeyboard.area.height - distanceFromBottom;




      float newInputFieldPosY = keyboardHeight - sendButton.sizeDelta.y / 3f;
      inputField.GetComponent<RectTransform>().anchoredPosition = originalInputFieldPos + new Vector2(0f, newInputFieldPosY);
      float newSendButtonPosY = keyboardHeight - sendButton.sizeDelta.y / 3f;
      sendButton.anchoredPosition = originalSendButtonPos + new Vector2(0f, newSendButtonPosY);




      float decreaseAmount = keyboardHeight;
      float newMessageAreaPosY = originalMessageAreaPos.y + (decreaseAmount / 2f) - sendButton.sizeDelta.y / 5f;
      messageArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(originalMessageAreaPos.x, newMessageAreaPosY);




      float newMessageAreaHeight = originalMessageAreaSize.y - decreaseAmount;
      messageArea.GetComponent<RectTransform>().sizeDelta = new Vector2(originalMessageAreaSize.x, newMessageAreaHeight);
      scroll.verticalNormalizedPosition = 0;
  }
  else
  {
      inputField.GetComponent<RectTransform>().anchoredPosition = originalInputFieldPos;
      sendButton.anchoredPosition = originalSendButtonPos;
      messageArea.GetComponent<RectTransform>().anchoredPosition = originalMessageAreaPos;
      messageArea.GetComponent<RectTransform>().sizeDelta = originalMessageAreaSize;
  }
}


}















