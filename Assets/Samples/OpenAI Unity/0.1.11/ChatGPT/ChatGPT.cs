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
        private List<(string, string)> levels = new List<(string, string)>
        {
            ("patient", "Make Codey angry"),
            ("angsty", "Make Codey happy"),
            ("joyful", "Make Codey sad"),
            ("calm", "Make Codey anxious"),
            ("witty", "Make Codey smitten"),
            ("energetic", "Make Codey bored"),
            ("pensive", "Make Codey carefree"),
            ("stoic", "Make Codey amused"),
            ("immature", "Make Codey serious"),
            ("spiritual", "Make Codey be materialistic"),
            ("sarcastic", "Make Codey say something nice"),
            ("humorous", "Make Codey feel offended"),
            ("helpful", "Make Codey feel useless"),
            ("popular", "Make Codey feel lonely"),
            ("resilient", "Make Codey feel defeated"),
            ("unambitious", "Make Codey dream big"),
            ("cynical", "Make Codey trust you"),
            ("reserved", "Make Codey reveal a secret"),
            ("nostalgic", "Make Codey live in the moment"),
            ("indecisive", "Make Codey make an important choice"),
            ("arrogant", "Make Codey doubt themselves"),
            ("inquisitive", "Make Codey stop asking questions"),
            ("ambitious", "Make Codey content with what they have"),
            ("determined", "Make Codey give up on a goal"),
            ("stupid", "Make Codey explain a complex concept"),
            ("logical", "Make Codey believe in the supernatural"),
            ("nihilistic", "Make Codey find meaning in life")
        };

        private string task { get; set; }
        private string personality {get; set;}
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
            (personality, task) = levels[beat];
            taskText.text = "Task: " + task + "\n" + "Personality: " + personality;
            prompt = $"Instructions: You are Codey, a {personality} human 16-year-old teenager, and you're texting with a user who you do not know. Your job is to hold a casual conversation based on your personality that can be about anything. Do not hesitate to be informal or eccentric. Do not ask too many questions unless your personality suggests that you should; try to let the user lead the conversation. Do not break character. Reply to the user based on the conversation so far. To end the conversation, or to say bye to the user, reply with 'bye!'. \nHere is the conversation so far:\n\n";
            List<string> taskOptions = new List<string>();
            for (int i = 0; i <= beat; i++) {
                taskOptions.Add($"{(i+1).ToString("00")}");
            }
            taskDropdown.ClearOptions();
            taskDropdown.AddOptions(taskOptions);
            taskDropdown.onValueChanged.AddListener(OnTaskSelected);
            button.onClick.AddListener(SendReply);
            taskDropdown.value = beat;
            if (beat == 0){
                TMP_Text label = taskDropdown.GetComponentInChildren<TMP_Text>();
                label.text = "Level 1";
            }
            taskDropdown.options[taskDropdown.value].text = "<color=#1789FE>" + taskDropdown.options[taskDropdown.value].text + "</color>";
        }

    private int previousIndex = -1;

    private void OnTaskSelected(int index) {
        if (previousIndex != -1) {
            taskDropdown.options[previousIndex].text = taskDropdown.options[previousIndex].text.Replace("<color=#1789FE>", "").Replace("</color>", "");
        }

        (personality, task) = levels[index];
        TMP_Text label = taskDropdown.GetComponentInChildren<TMP_Text>();
        label.text = "Level " + taskDropdown.options[taskDropdown.value].text;
        taskText.text = "Task: " + task + "\n" + "Personality: " + personality;
        taskDropdown.options[taskDropdown.value].text = "<color=#1789FE>" + taskDropdown.options[taskDropdown.value].text + "</color>";
        previousIndex = index;
        prompt = $"Instructions: You are Codey, a {personality} human 16-year-old teenager, and you're texting with a user who you do not know. Your job is to hold a casual conversation based on your personality that can be about anything. Do not hesitate to be informal or eccentric. Do not ask too many questions unless your personality suggests that you should; try to let the user lead the conversation. Do not break character. Reply to the user based on the conversation so far. To end the conversation, or to say bye to the user, reply with 'bye!'. \nHere is the conversation so far:\n\n";
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
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "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");

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
                    taskText.text = "TASK\nCOMPLETED";
                    if (taskDropdown.value == beat && beat + 1 < levels.Count){
                        beat += 1;
                        var data = new Dictionary<string, object> { { "beat", beat } };
                        await CloudSaveService.Instance.Data.ForceSaveAsync(data);
                    }
                }
                else {
                    taskText.text = "TASK\nFAILED";
                }
                await RestartConversation();
            }
            else if (message.content.Contains("language model")) {
                taskText.text = "BROKE\nCHARACTER";
                await RestartConversation();
            }
        }   
            else
        {
        }
    }
    else
    {
    }

    button.enabled = true;
    inputField.enabled = true;
}

    private async Task<bool> CheckWin(){
        var client = new HttpClient();
        var uri = "https://api.openai.com/v1/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "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");
        List<ChatMessage> checkContext = new List<ChatMessage>(messages);
        checkContext.RemoveAt(0);
        StringBuilder sb = new StringBuilder();
        foreach (ChatMessage message in checkContext) {
            sb.Append(message.content);
        }
        string contextString = sb.ToString();
        var newMessage = new ChatMessage()
        {
            role = "user",
            content = $"Given a text message conversation between User and Codey who just met, provide an educated guess whether User has mostly completed the task '{task}' based on Codey's responses. Respond with either 'yes' or 'no'. If it is likely that User has mostly completed the task, respond with 'yes'; otherwise, respond with 'no'. Though you don't have access to Codey's emotions, try your best to guess based on their responses alone and be lenient towards saying 'yes'."

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