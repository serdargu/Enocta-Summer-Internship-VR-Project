using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.UI;
using UnityEngine.Video;
using VRStandardAssets.Utils;

public class GameController : MonoBehaviour
{

    public string API_KEY = "AIzaSyAjbTIs3XCTGkhCBGyihJME24l3duua8PI";
    public string LANG = "tr-TR";
    public Settings settings; //

    public int roomCountDownTime = 10; // the time elapsed in room
    public int displayCountDownTime = 5; //
    public int questionCountDownTime = 10; //

    public int totalTime = 0;
    private int timeToAnswer = 0;

    /* Intro */
    public GameObject introCanvas; // intro scene
    public GameObject introText;   //intro text
    public GameObject introTimerText; // timer text to count down the time
    public int introCountDownTime = 3; // the time which is counting down in the intro after confirming the start button

    /* Room */
    public GameObject roomCollapser;
    private GameObject room;
    public GameObject roomTimerText; // room timer text
    public SelectionSlider confirmButton; // confirm button to skip the intro

    /* Questions game objects */
    public GameObject questionCanvas;
    public TextMesh questionText;
    public GameObject stt;
    public TextMesh sttCorrectAnswer;
    public TextMesh sttAnswerText;
    public GameObject sttRecording;
    public GameObject sttProcessing;
    public TextMesh micWarningText;

    /* Feedback canvas & text*/
    public GameObject feedbackCanvas;
    public TextMesh feedbackText;
    public VideoPlayer videoPlayer;
    public GameObject screen;

    /* Sliders of the corresponding buttons */
    public List<SelectionSlider> answerButtons;
    public SelectionSlider buttonPrefab;

    /* Gameobjects to display result */
    public GameObject resultCanvas;
    public TextMesh correctAnswersText;
    public TextMesh falseAnswersText;
    public TextMesh totalTimeSpentText;
    private string result;
    private bool mutex;

    private int correctAnswers = 0;
    private int falseAnswers = 0;

    /* Speech recognition */
    const int HEADER_SIZE = 44;
    private int minFreq;
    private int maxFreq;
    private bool micConnected = false;
    private AudioSource goAudioSource; // a handle to the attached AudioSource
    private string filePath;

    // Button positions for the answers
    private static Vector3[,] BUTTON_POSITONS =
    {
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(0f, 0f, 0f),     new Vector3(0f, 0f, 0f),    new Vector3(0f, 0f, 0f),   new Vector3(0f, 0f, 0f)},   // when there are 2 options
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(0f, 70f, 60f),   new Vector3(0f, 0f, 0f),    new Vector3(0f, 0f, 0f),   new Vector3(0f, 0f, 0f)},   // when there are 3 options
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(-25f, 70f, 60f), new Vector3(25f, 70f, 60f), new Vector3(0f, 0f, 0f),   new Vector3(0f, 0f, 0f)},   // when there are 4 options
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(-25f, 70f, 60f), new Vector3(25f, 70f, 60f), new Vector3(0f, 60f, 60f), new Vector3(0f, 0f, 0f)},   // when there are 5 options
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(-25f, 70f, 60f), new Vector3(25f, 70f, 60f), new Vector3(-25f, 60f, 60f), new Vector3(25, 60f, 60f)}, // when there are 6 options
    };

    // Unity calls this function at the beginning
    private IEnumerator Start()
    {
        // Get the settings and store them into settings object
        settings = Settings.CreateFromJSON();
        API_KEY = settings.api_key;
        LANG = settings.lang;

        // Commented code above provides an opportunity to change questions dynamically with the given json file @given path
        /*
         *  WWW www = new WWW("file:///C:/Users/eminserdar.guzel/Desktop/icerik.json");
         *  WWW www = new WWW("file:///C:/Users/eda.mutlu/Desktop/icerik.json");
         *  yield return www;
         *  settings = JsonUtility.FromJson<Settings>(www.text);
        */

        // Reset the camera position
        ResetCameraPosition();

        // Make the intro text visible
        SetVisibile(introCanvas, true);

        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("Microphone not connected!"); // Throw a warning message at the console if there isn't
            SetVisibile(micWarningText.gameObject, true);
        }

        // Wait for slider to be filled
        yield return StartCoroutine(confirmButton.WaitForBarToFill());
        SetVisibile(micWarningText.gameObject, false);

        // Make the intro objects invisible 
        SetVisibile(introTimerText, true);
        SetVisibile(introText, false);
        SetVisibile(confirmButton.gameObject, false);

        // Countdown before entering the first room
        yield return StartCoroutine(StartTimerWithText(introTimerText, introCountDownTime));

        // Make the intro scene completely invisible
        SetVisibile(introCanvas, false);


        //

        foreach (Scene scene in settings.scenes)
        {

            ResetCameraPosition();

            /*
            if (scene.name == "Bedroom") room = bedroom;
            else if (scene.name == "Club") room = club;
            else if (scene.name == "Kitchen") room = kitchen;
            else room = living;
            */

            GameObject room = Resources.Load(scene.name) as GameObject;
            room = Instantiate(room, room.transform.position, Quaternion.identity);
            room.transform.SetParent(roomCollapser.transform);

            // Countdown in order to give some time to player to look around 
            roomCountDownTime = scene.countdown_time;

            // Show remaining time
            SetVisibile(roomTimerText, true);
            yield return StartCoroutine(StartRoomTimer(roomCountDownTime));

            //Make invisble the timer and then the room
            SetVisibile(roomTimerText, false);
            Destroy(room);

            foreach (Question question in scene.questions)
            {

                questionText.text = question.description;
                result = "";

                foreach (SelectionSlider button in answerButtons)
                {
                    Destroy(button.gameObject);
                }
                answerButtons.Clear();
                // Start room timer
                SetVisibile(roomTimerText, true);

                ResetCameraPosition();
                // Display the question 
                SetVisibile(questionCanvas, true);
                questionCanvas.GetComponent<Animator>().SetTrigger("QuestionCanvasAnimation");
                questionCanvas.GetComponent<Animator>().SetTrigger("StopAnimation");
                SetVisibile(sttCorrectAnswer.gameObject, false);

                // If the question answer type is Speech To Text according to JSON file that we have given
                if (question.answer_type == "stt")
                {
                    SetVisibile(stt, true);

                    // The code block derived from http://answers.unity3d.com/questions/479064/microphone-detect-speech-start-end.html
                    // Check if there is at least one microphone connected
                    if (Microphone.devices.Length <= 0)
                    {
                        SetVisibile(roomTimerText, false);
                        Debug.Log("Microphone not connected!"); // Throw a warning message at the console if there isn't
                        SetVisibile(micWarningText.gameObject, true);
                        yield return new WaitForSeconds(3.0f);
                        SetVisibile(micWarningText.gameObject, false);
                    }
                    else // At least one microphone is present
                    {
                        //Set 'micConnected' to true
                        micConnected = true;

                        //Get the default microphone recording capabilities
                        Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

                        // According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
                        if (minFreq == 0 && maxFreq == 0)
                        {
                            //...Meaning 44100 Hz can be used as the recording sampling rate
                            maxFreq = 44100;
                        }

                        //Get the attached AudioSource component
                        goAudioSource = this.GetComponent<AudioSource>();

                        // Display mic image & recording text
                        SetVisibile(sttRecording, true);

                        // Ready to record 
                        yield return StartCoroutine(StartRecording(5));
                        SetVisibile(roomTimerText, false);
                        // Store the resulting string of SpeechToText func.
                        this.mutex = false;
                        SpeechToText();


                        SetVisibile(sttRecording, false);
                        SetVisibile(sttProcessing, true);

                        while (!this.mutex)
                        {
                            yield return new WaitForSeconds(0.5f);
                        }


                        SetVisibile(sttProcessing, false);

                        // Display the user's answer
                        sttAnswerText.text = result;
                        SetVisibile(sttAnswerText.gameObject, true);


                        // Check if the answer is correct or not
                        // ...Change the color of the text accordingly
                        if (result.ToLower() == question.correct_answer.ToLower())
                        {
                            correctAnswers += 1;
                            sttAnswerText.color = Color.green;
                        }
                        else
                        {
                            falseAnswers += 1;
                            sttAnswerText.color = Color.red;
                            // Display the correct answer
                            SetVisibile(sttCorrectAnswer.gameObject, true);
                            sttCorrectAnswer.text = "Doğru cevap: " + question.correct_answer.ToLower();
                        }

                        yield return new WaitForSeconds(5.0f);
                        SetVisibile(sttAnswerText.gameObject, false);
                        SetVisibile(sttCorrectAnswer.gameObject, false);
                        SetVisibile(questionCanvas, false);

                        // Delete the .wav file used
                       // File.Delete(filePath);


                    }
                    SetVisibile(stt, false);
                }
                else
                {
                    int i = 0;
                    int answersCount = question.answers.Count; // Total number of answers comes from JSON file
                    foreach (Answer answer in question.answers)
                    {
                        answerButtons.Add(Instantiate(buttonPrefab, BUTTON_POSITONS[answersCount - 2, i], Quaternion.identity)); // Create a new answer button for each answer and set position
                        answerButtons[i].transform.SetParent(questionCanvas.transform); // Make the button's parent as question canvas
                        answerButtons[i].m_TrueAnswer = answer.correct; // To check whether the answer is correct, set the true or false according to JSON that we have given
                        answerButtons[i].GetComponentInChildren<Text>().text = question.answers.ElementAt(i).answer; // Set the text of button according to JSON file
                        answerButtons[i].m_Feedback = question.has_feedback && question.answer_type == "multiple_choices" ? question.answers.ElementAt(i).feedback : "";
                        i++;
                    }

                    //Countdown to answer the given question
                    yield return StartCoroutine(StartQuestionTimer(questionCountDownTime));


                    int index = 0;
                    bool flag = false;
                    string feedback = "";
                    // Find the selected answer & it's index
                    foreach (SelectionSlider answerButton in answerButtons)
                    {
                        if (answerButton.m_BarFilled)
                        {
                            feedback = answerButtons[index].m_Feedback;
                            if (answerButton.m_TrueAnswer)
                            {
                                flag = true;
                                break;
                            }
                        }
                        index++;
                    }
                    // If one of the answers is selected and it's the correct one, increment the count of the correct answers. 
                    // Otherwise, increment the count of the false answers
                    if (flag) correctAnswers += 1;
                    else falseAnswers += 1;

                    // Make the current question canvas and the timer invisible
                    SetVisibile(roomTimerText, false);
                    SetVisibile(questionCanvas, false);
                    yield return new WaitForSeconds(1.0f);

                    if (question.has_feedback)
                    {
                        // Reset the camera position
                        ResetCameraPosition();
                        SetVisibile(feedbackCanvas, true);

                        if (question.fb_type == "text")
                        {

                            feedbackText.text = feedback;
                            SetVisibile(feedbackText.gameObject, true);

                            // Create an VR interactive button 
                            SelectionSlider feedbackButton = Instantiate(buttonPrefab, new Vector3(0.0f, 70.0f, 60.0f), Quaternion.identity) as SelectionSlider;
                            feedbackButton.transform.SetParent(feedbackCanvas.transform);
                            feedbackButton.GetComponentInChildren<Text>().text = "Tamam!";

                            // When the feedback button has been filled, skip the feedback part
                            yield return StartCoroutine(feedbackButton.WaitForBarToFill());

                            SetVisibile(feedbackText.gameObject, false);
                            Destroy(feedbackButton.gameObject);

                        }
                        else if (question.fb_type == "video")
                        {
                            SetVisibile(screen, true);
                            SetVisibile(videoPlayer.gameObject, true);

                            /* To play the video full screen and near the camera
                            * GameObject camera = GameObject.Find("Camera");
                            * videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                            */
                            //videoPlayer.url = feedback;
                            /* 
                            * Following line of code is used for making it work in the android device as well
                            * feedback should be something like "mov_bbb.mp4" and it also should be located in the Resoursec directory
                            */
                            videoPlayer.clip = Resources.Load(feedback) as VideoClip;
                            videoPlayer.Play();

                            while (videoPlayer.isPlaying)
                                yield return new WaitForSeconds(1.0f);

                            SetVisibile(videoPlayer.gameObject, false);
                            SetVisibile(screen, false);

                        }

                        SetVisibile(feedbackCanvas, false);

                    }
                    // Calculate total time spent while answering questions
                    totalTime += timeToAnswer;
                    timeToAnswer = 0;

                }

                questionCanvas.GetComponent<Animator>().ResetTrigger("QuestionCanvasAnimation");
                questionCanvas.GetComponent<Animator>().ResetTrigger("StopAnimation");

            }

            // Make the question canvas invisible
            SetVisibile(questionCanvas, false);
            SetVisibile(sttAnswerText.gameObject, false);

            yield return StartCoroutine(DisplayResults());

        }

    }

    /* 
     * The function that used to recenter the camera position
     * That is, when the player looks another direction, the application resets the view as center
     * So that every scene can be always shown on the camera view
    */
    private void ResetCameraPosition()
    {
        InputTracking.Recenter();
    }

    // The function that starts the intro scene
    private IEnumerator StartIntro()
    {
        yield return StartCoroutine(confirmButton.WaitForBarToFill()); // Wait for the bar to fill
    }

    // The function that sets visibility for given game object
    private void SetVisibile(GameObject g, bool b)
    {
        g.SetActive(b);
    }

    // The function to display the time spent, # of correct and wrong answers for 5 seconds
    private IEnumerator DisplayResults()
    {
        ResetCameraPosition();
        SetVisibile(resultCanvas, true);
        correctAnswersText.text = "Doğru cevap sayısı: " + correctAnswers;
        falseAnswersText.text = "Yanlış cevap sayısı: " + falseAnswers;
        totalTimeSpentText.text = "Sorularda harcanan toplam süre: " + totalTime + " sn.";
        yield return new WaitForSeconds(5.0f);  // Wait 5 sec
        SetVisibile(resultCanvas, false);
    }

    // 
    private IEnumerator StartTimerWithText(GameObject timerText, int time)
    {
        TextMesh textMesh = timerText.GetComponent<TextMesh>(); // Get the text mesh component from intro timer text game object
        for (int count = time; count > 0; count--)
        {
            textMesh.text = "" + count; // Update the text with current time left
            yield return new WaitForSeconds(1.0f); // Wait 1 sec
        }
    }

    // Countdown for the room
    private IEnumerator StartRoomTimer(int countDownTime)
    {
        TextMesh textMesh = roomTimerText.GetComponent<TextMesh>(); // Get the text mesh component from room timer text game object
        for (int count = countDownTime; count > 0; count--)
        {
            textMesh.text = "Kalan süre: " + count; // Update the text with current time left
            if (count == 5)
            {
                roomTimerText.GetComponent<Animator>().SetTrigger("TimeAnimation");
                textMesh.color = Color.red;
            }
            else if (count == 1) roomTimerText.GetComponent<Animator>().SetTrigger("StopAnimation");
            yield return new WaitForSeconds(1.0f); // Wait 1 sec
        }
        roomTimerText.GetComponent<Animator>().ResetTrigger("TimeAnimation");
        roomTimerText.GetComponent<Animator>().ResetTrigger("StopAnimation");
        textMesh.color = Color.white;
    }


    // Countdown for the question
    private IEnumerator StartQuestionTimer(int questionCountDownTime)
    {
        TextMesh textMesh = roomTimerText.GetComponent<TextMesh>(); // Get the text mesh component from room timer text game object
        int count;
        for (count = questionCountDownTime; count > 0; count--)
        {
            textMesh.text = "Time left: " + count; // Update the text with current time left
            bool flag = false;
            foreach (SelectionSlider answerButton in answerButtons) if (answerButton.m_BarFilled) flag = true;
            yield return new WaitForSeconds(1.0f); // Wait 1 sec
            if (flag) break;
        }
        timeToAnswer = questionCountDownTime - count;
    }

    // The function to save the voice as a wav file with in the given time as a parameter
    private IEnumerator StartRecording(int questionCountDownTime)
    {
        // If there is a microphone
        if (micConnected)
        {
            // If the audio from any microphone isn't being recorded
            if (!Microphone.IsRecording(null))
            {
                // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
                goAudioSource.clip = Microphone.Start(null, true, questionCountDownTime, maxFreq);

                // Countdown to answer the given question
                yield return StartCoroutine(StartQuestionTimer(questionCountDownTime));

                var filename = "recording_" + UnityEngine.Random.Range(0.0f, 10.0f) + ".wav";

                Microphone.End(null); // Stop the audio recording

                filePath = Path.Combine("temp_records/", filename);
                filePath = Path.Combine(Application.persistentDataPath, filePath);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Make sure directory exists if user is saving to sub dir.
                SavWav.Save(filePath, goAudioSource.clip); // Save a temporary .wav File
            }

        }
    }

    /*
     * https://cloud.google.com/speech/docs/getting-started
     * Google Speech To Text API using HTTP POST
     * First create a Cloud Project from https://console.cloud.google.com/cloud-resource-manager
     * To get api key, visit https://console.cloud.google.com/flows/enableapi?apiid=speech.googleapis.com
     * To see the language list that Google supports currently, visit https://cloud.google.com/speech/docs/languages
     * Currently Google supports .wav, .flac, .raw files well. (Tested with .wav, .flac and .raw)
     * Base64 encoding audio https://cloud.google.com/speech/docs/base64-encoding
     * 
     * The function that takes the audio file and converts it to the byte array
     * then sends it to the Google Speech To Text API
     * then receives the text and returns it
     */
    private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
    {

        HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

        var bytes = File.ReadAllBytes(filePath); // Read the bytes from the path
        string base64 = Convert.ToBase64String(bytes); // Convert the bytes to Base64 string

        // End the operation
        Stream postStream = request.EndGetRequestStream(asynchronousResult);

        string json = "{\n  \"config\": {\n    \"languageCode\":\"" + LANG + "\"\n  },\n  \"audio\":{\n    \"content\":\"" + base64 + "\"\n  }\n}"; // Create JSON that includes encoded bytes and language
        byte[] byteArray = Encoding.UTF8.GetBytes(json);
        // Write to the request stream.

        postStream.Write(byteArray, 0, byteArray.Length);
        postStream.Close();

        // Start the asynchronous operation to get the response
        request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
    }

    private void GetResponseCallback(IAsyncResult asynchronousResult)
    {
        HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;


        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
        string finalResult = ""; // To store the final result

        using (var streamReader = new StreamReader(response.GetResponseStream()))
        {
            SpeechToTextResult speechToTextResult = JsonUtility.FromJson<SpeechToTextResult>(streamReader.ReadToEnd()); // Convert the JSON to the object

            float maxConfidence = 0.0f; // To check whether the result has max confidence

            if (speechToTextResult.results.Count != 0) // If the result is not null
            {
                foreach (Alternative result in speechToTextResult.results.ElementAt(0).alternatives) // Iterate each results and to get the result that has max confidence
                {
                    if (maxConfidence < result.confidence)
                    {
                        finalResult = result.transcript; // Set the new transcript that currently has max confidence level
                        maxConfidence = result.confidence; // Update the confidence with the max
                    }
                }
            }

        }

        this.result = finalResult;
        this.mutex = true;

        // Release the HttpWebResponse
        response.Close();
    }

    private void SpeechToText()
    {

        System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true; // To handle the SSL, we don't know how; but it works, derived from https://ubuntuforums.org/showthread.php?t=1841740

        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://speech.googleapis.com/v1/speech:recognize?key=" + API_KEY);
        httpWebRequest.ContentType = "application/json"; // Content type that Google accepts is JSON
        httpWebRequest.Method = "POST"; // Set method as POST
        httpWebRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), httpWebRequest);

    }

}
