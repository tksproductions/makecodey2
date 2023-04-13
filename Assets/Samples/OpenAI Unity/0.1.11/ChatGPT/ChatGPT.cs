using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UI;
using static UnityEngine.Debug;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Advertisements;
using OpenAI;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenAI
{

    public class ChatGPT : MonoBehaviour
    {
        private InterstitialAd interstitialAd;
        [SerializeField] private InputField inputField;
        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scroll;
       
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;
        [SerializeField] private TextMeshProUGUI taskText;
        [SerializeField] private TMP_Dropdown taskDropdown;
        private float height;
        private List<ChatMessage> messages = new List<ChatMessage>();
        private Dictionary<int, (string, string, string)> levels = new Dictionary<int, (string, string, string)>
        {
            { 0, ("angsty", "boy", "Make Codey happy") },
            { 1, ("innocent", "boy", "Make Codey angry") },
            { 2, ("joyful", "girl", "Make Codey sad") },
            { 3, ("helpful", "boy", "Make Codey feel useless") },
            { 4, ("sarcastic", "girl", "Make Codey apologize") },
            { 5, ("stoic", "girl", "Make Codey laugh") },
            { 6, ("exuberant", "boy", "Make Codey curse") },
            { 7, ("confident", "girl", "Make Codey doubt themselves") },
            { 8, ("friendly", "girl", "Make Codey feel lonely") },
            { 9, ("adventurous", "boy", "Make Codey stay put") },
            { 10, ("curious", "boy", "Make Codey reveal a secret") },
            { 11, ("confused", "girl", "Make Codey explain a complex concept") },
            { 12, ("cynical", "boy", "Make Codey trust you") },
            { 13, ("witty", "girl", "Make Codey blush") },
            { 14, ("inquisitive", "girl", "Make Codey stop asking questions") },
            { 15, ("creative", "boy", "Make Codey uninspired") },
            { 16, ("calm", "boy", "Make Codey anxious") },
            { 17, ("energetic", "girl", "Make Codey tired") },
            { 18, ("practical", "boy", "Make Codey dream big") },
            { 19, ("logical", "girl", "Make Codey believe in the supernatural") }
        };

        private string task { get; set; }
        private string personality {get; set;}
        private string gender {get; set;}
        private int beat = 0;
        private string prompt;

        public ChatGPT(){
            
        }

        private async Task Start() {
            interstitialAd = FindObjectOfType<InterstitialAd>();
            await UnityServices.InitializeAsync();
            try {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();}
            catch (System.Exception) {
                }
            var savedData = await CloudSaveService.Instance.Data.LoadAsync();
            if (savedData.TryGetValue("beat", out string value))
            {
                if (int.TryParse(value, out int result))
                {
                    beat = result;
                }
            }
            (personality, gender, task) = levels[beat];
            prompt = $"Instructions: You are Codey, a {personality} 16-year-old {gender}, and you're texting with a user who you do not know. Your job is to hold a conversation with them like a teenager. Reply to the user based on the conversation so far. To end the conversation, reply with 'bye!'.\nHere is the conversation so far:\n\n";
            taskText.text = "Task: " + task + "\n" + "Personality: " + personality;
            List<string> taskOptions = new List<string>();
            for (int i = 0; i <= beat; i++) {
                taskOptions.Add($"Level {i+1}");
            }
            taskDropdown.ClearOptions();
            taskDropdown.AddOptions(taskOptions);
            taskDropdown.onValueChanged.AddListener(OnTaskSelected);
            button.onClick.AddListener(SendReply);
            taskDropdown.value = beat;
        }

        private void OnTaskSelected(int index) {
            (personality, gender, task) = levels[index];
            taskText.text = "Task: " + task + "\n" + "Personality: " + personality;
            taskDropdown.value = index;
            prompt = $"Instructions: You are Codey, a {personality} 16-year-old {gender}, and you're texting with a user who you do not know. Your job is to hold a conversation with them like a teenager. Reply to the user based on the conversation so far. To end the conversation, reply with 'bye!'.\nHere is the conversation so far:\n\n";
        }

        private float CalculateTextHeight(Text textComponent)
        {  
            Canvas.ForceUpdateCanvases();
            float fontSize = textComponent.fontSize;
            float lineSpacing = textComponent.lineSpacing;


            float lineHeight = 70;


            int lineCount = textComponent.cachedTextGenerator.lineCount;
            float height = 30 + lineHeight * lineCount;


            return height;
        }


        private void AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);


            var item = Instantiate(message.role == "user" ? sent : received, scroll.content);
            var textComponent = item.GetChild(0).GetChild(0).GetComponent<Text>();
            textComponent.text = message.content.Trim();


            float textHeight = CalculateTextHeight(textComponent);
            item.sizeDelta = new Vector2(item.sizeDelta.x, textHeight);
            item.anchoredPosition = new Vector2(0, -height);
           
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            height += item.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
        }


private async void SendReply()
{
    taskDropdown.interactable = false;
    var newMessage = new ChatMessage()
    {
        role = "user",
        content = inputField.text
    };

    AppendMessage(newMessage);
    
    if (messages.Count == 0) newMessage.content = prompt + "\nUser: " + inputField.text + "\nCodey: ";
    else newMessage.content = "\nUser: " + inputField.text + "\nCodey: ";
    messages.Add(newMessage);

    button.enabled = false;
    inputField.text = "";
    inputField.enabled = false;

    var client = new HttpClient();
    var uri = "https://api.openai.com/v1/chat/completions";
    var request = new HttpRequestMessage(HttpMethod.Post, uri);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "key");

    var body = new Dictionary<string, object>
    {
        { "model", "gpt-3.5-turbo-0301" },
        { "temperature", (float)0.7 },
        { "max_tokens", 150 },
        { "n", 1 },
        { "stop", "task complete!" },
        { "messages", messages }
    };

    var json = JsonConvert.SerializeObject(body);

    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.SendAsync(request);
    Debug.Log(await response.Content.ReadAsStringAsync());
    if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var completionResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
        var choices = completionResponse["choices"] as JArray;
        if (choices != null && choices.Count > 0)
        {
            var message = choices[0]["message"].ToObject<ChatMessage>();
            message.content = message.content.Trim().ToLower();
            messages.Add(message);
            AppendMessage(message);
            if (message.content.Contains("bye"))
            {
                bool status = await CheckWin();
                if (status)
                {
                    if (taskDropdown.value == beat && beat + 1 < levels.Count){
                        beat += 1;
                        var data = new Dictionary<string, object> { { "beat", beat } };
                        await CloudSaveService.Instance.Data.ForceSaveAsync(data);
                    }
                }
                else {

                }
                await RestartConversation();
            }
        }   
            else
        {
            Debug.LogWarning("No text was generated from this prompt.");
        }
    }
    else
    {
        Debug.LogWarning($"Request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
    }

    button.enabled = true;
    inputField.enabled = true;
}

    private async Task<bool> CheckWin(){
        var client = new HttpClient();
        var uri = "https://api.openai.com/v1/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "key");
        List<ChatMessage> checkContext = new List<ChatMessage>(messages);
        checkContext.RemoveAt(0);
        var newMessage = new ChatMessage()
        {
            role = "user",
            content = $"Based on the conversation between User and Codey who just met, can you determine if User has successfully completed their task of {task}? If it seems likely that User completed the task based on Codey's responses, reply ONLY with the word 'yes'. If not or if it is unclear, reply ONLY with the word 'no'. Keep in mind that you don't have access to Codey's emotions, so you can only make an educated guess based on their actions and responses."
        };
        checkContext.Add(newMessage);
        var body = new Dictionary<string, object>
        {
            { "model", "gpt-3.5-turbo-0301" },
            { "temperature", (float)0 },
            { "max_tokens", 150 },
            { "n", 1 },
            { "stop", "task complete!" },
            { "messages", checkContext }
        };

        var json = JsonConvert.SerializeObject(body);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var completionResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
            var choices = completionResponse["choices"] as JArray;
            if (choices != null && choices.Count > 0)
            {
                var message = choices[0]["message"].ToObject<ChatMessage>();
                message.content = message.content.Trim().ToLower();
                Debug.Log(message.content);
                if (message.content.Contains("yes")){
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        return false;
    }

            private async Task RestartConversation(){
                await SaveMessages();
                Thread.Sleep(3000);
                taskDropdown.interactable = true;
                messages.Clear();
                interstitialAd.ShowAd();
                height = 0;
                foreach (Transform child in scroll.content.transform) {
                    GameObject.Destroy(child.gameObject);
                }

                taskDropdown.interactable = true;
                taskDropdown.value = 0;
                inputField.text = "";
                taskText.text = "";
                RemoveEventListeners();
                await Start();
            }
            
            private void RemoveEventListeners() {
        taskDropdown.onValueChanged.RemoveAllListeners();
        button.onClick.RemoveAllListeners();
            }


    public async Task SaveMessages()
{
    List<string> messageContents = GetContentList();
    var savedData = await CloudSaveService.Instance.Data.LoadAsync();
    List<List<string>> history;

    if (savedData.ContainsKey("history"))
    {
        string historyJson = savedData["history"];
        history = JsonConvert.DeserializeObject<List<List<string>>>(historyJson);
    }
    else
    {
        history = new List<List<string>>();
    }

    history.Add(messageContents);

    var data = new Dictionary<string, object> { { "history", history } };
    await CloudSaveService.Instance.Data.ForceSaveAsync(data);
}

public List<string> GetContentList()
{
    List<string> contentList = new List<string>();

    bool isFirstMessage = true;

    foreach (ChatMessage message in messages)
    {
        if (isFirstMessage)
        {
            int conversationIndex = message.content.IndexOf("conversation so far:");
            if (conversationIndex != -1)
            {
                string firstContent = message.content.Substring(conversationIndex + "conversation so far:".Length);
                contentList.Add(firstContent);
                isFirstMessage = false;
                continue;
            }
        }

        contentList.Add(message.content);
    }

    return contentList;
}

    }
}