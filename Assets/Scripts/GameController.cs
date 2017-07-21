using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.UI;
using VRStandardAssets.Utils;
using System.Linq;
using UnityEngine.Video;



public class GameController : MonoBehaviour
{

    public Settings settings;

    public int countDownTime = 10;     // the time elapsed in room
    public int introCountDownTime = 3; // the time which is counting down in the intro after confirming the start button
    public int displayCountDownTime = 5; //
    public int questionCountDownTime = 10; //

    public int totalTime = 0;
    private int timeToAnswer = 0;

    public GameObject timeToAnswerText;
    public GameObject timeDisplayCanvas;

    /* intro objects */
    public GameObject introCanvas; // intro scene
    public GameObject introText;   //intro text
    public GameObject introTimerText; // timer text to count down the time

    /* room objects */
    private GameObject room;
    public GameObject bedroom;
    public GameObject club;
    public GameObject kitchen;
    public GameObject living;

    public GameObject roomTimerText; // room timer text
    public SelectionSlider confirmButton; // confirm button to skip the intro
    public GameObject lights; // lights

    /* questions game objects */
    public GameObject questionCanvas;
    public TextMesh questionText;

    /* feedback canvas & text*/
    public GameObject feedbackCanvas;
    public TextMesh feedbackText;
    private string feedbackURL;
    public SelectionSlider feedbackButton;
    public VideoPlayer videoPlayer;

    /*sliders of the corresponding buttons*/
    public List<SelectionSlider> answerButtons;
    public SelectionSlider buttonPrefab;

    /*gameobjects to display result*/
    public GameObject resultCanvas;
    public TextMesh correctAnswersText;
    public TextMesh falseAnswersText;
    public TextMesh totalTimeSpentText;

    private int correctAnswers = 0;
    private int falseAnswers = 0;

    private static Vector3[,] BUTTON_POSITONS = 
    {
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)},
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(0f, 70f, 60f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)},
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(-25f, 70f, 60f), new Vector3(25f, 70f, 60f), new Vector3(0f, 0f, 0f)},
        {new Vector3(-25f, 80f, 60f), new Vector3(25f, 80f, 60f), new Vector3(-25f, 70f, 60f), new Vector3(25f, 70f, 60f), new Vector3(0f, 60f, 60f)},
    };

    private IEnumerator Start()
    {
        // get the settings and store them into settings object
        settings = Settings.CreateFromJSON();

        ///////////////////////////////////////////////////////////////////////////////
        //WWW www = new WWW("file:///C:/Users/eminserdar.guzel/Desktop/icerik.json");
        //WWW www = new WWW("file:///C:/Users/eda.mutlu/Desktop/icerik.json");
        //yield return www;
        //settings = JsonUtility.FromJson<Settings>(www.text);
        ///////////////////////////////////////////////////////////////////////////////

       // reset the camera position
       ResetCameraPosition();

        //Make the intro text visible
        SetVisibile(introCanvas, true);

        //Wait for slider to be filled
        yield return StartCoroutine(confirmButton.WaitForBarToFill());

        // make the intro objects invisible 
        SetVisibile(introTimerText, true);
        SetVisibile(introText, false);
        SetVisibile(confirmButton.gameObject, false);
        
        //Countdown before entering the first room
        yield return StartCoroutine(StartTimerWithText(introTimerText, introCountDownTime));

        //Make the intro scene completely invisible
        SetVisibile(introCanvas, false);

        foreach (Scene scene in settings.scenes)
        {

            ResetCameraPosition();

            if (scene.name == "Bedroom")
                room = bedroom;
            else if (scene.name == "Club")
                room = club;
            else if (scene.name == "Kitchen")
                room = kitchen;
            else
                room = living;

            // make the room visible
            SetVisibile(room, true);

            // Countdown in order to give some time to player to look around 
            countDownTime = scene.countdown_time;

            // Show remaining time
            SetVisibile(roomTimerText, true);
            yield return StartCoroutine(StartRoomTimer(roomTimerText, countDownTime));

            //Make invisble the timer and then the room
            SetVisibile(roomTimerText, false);
            SetVisibile(room, false);

            foreach (Question question in scene.questions)
            {

                questionText.text = question.description;

                foreach (SelectionSlider button in answerButtons)
                {
                    Destroy(button.gameObject);
                }
                answerButtons.Clear();

                int i = 0;
                int answersCount = question.answers.Count;
                foreach (Answer answer in question.answers)
                {
                    answerButtons.Add(Instantiate(buttonPrefab, BUTTON_POSITONS[answersCount - 2, i], Quaternion.identity));
                    answerButtons[i].transform.SetParent(questionCanvas.transform);
                    answerButtons[i].m_TrueAnswer = answer.correct;
                    answerButtons[i].GetComponentInChildren<Text>().text = question.answers.ElementAt(i).answer;
                    i++;
                }

                SetVisibile(roomTimerText, true);

                ResetCameraPosition();

                SetVisibile(questionCanvas, true);
                questionCanvas.GetComponent<Animator>().SetTrigger("QuestionCanvasAnimation");
                questionCanvas.GetComponent<Animator>().SetTrigger("StopAnimation");
                                
                //Countdown to answer the given question
                yield return StartCoroutine(StartQuestionTimer(questionCountDownTime ));

                /* if one of the answers is selected and it's the correct one, increment the count of the correct answers. 
                ** Otherwise increment the count of the false answers
                */

                int fbIndex = 0;
                bool flag = false;
                foreach (SelectionSlider answerButton in answerButtons)
                {
                    if (answerButton.m_BarFilled )
                    {
                        if(answerButton.m_TrueAnswer) flag = true;
                        if (question.fb)
                        {
                            foreach (Answer answer in question.answers)
                            {
                                if (answerButton.GetComponentInChildren<Text>().text != question.answers.ElementAt(fbIndex).answer)
                                    fbIndex++;
                            }
                            if (question.fbType == "text") feedbackText.text = question.answers.ElementAt(fbIndex).feedback;
                            else if (question.fbType == "video") feedbackURL = question.answers.ElementAt(fbIndex).feedback;
                        }
                    }                    
                }
                if (flag) correctAnswers += 1;
                else falseAnswers += 1;
                              
               //Make the current question canvas and the timer invisible
                SetVisibile(roomTimerText, false);
                SetVisibile(questionCanvas, false);
                yield return new WaitForSeconds(1.0f);

                if (question.fb)
                {
                    // reset the camera position
                    ResetCameraPosition();
                    SetVisibile(feedbackCanvas, true);
                    feedbackButton.gameObject.SetActive(false);

                    if (question.fbType == "text")
                    {
                        feedbackButton.gameObject.SetActive(true);
                        yield return StartCoroutine(feedbackButton.WaitForBarToFill());
                    } 
                    else if (question.fbType == "video")
                    {
                        GameObject camera = GameObject.Find("VR Camera");
                        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                        videoPlayer.url = feedbackURL;
                        videoPlayer.Play();

                        while (videoPlayer.isPlaying)
                            yield return new WaitForSeconds(1.0f);
                    }
                    
                    SetVisibile(feedbackCanvas, false);
                }

                SetVisibile(timeDisplayCanvas, true);
                timeToAnswerText.GetComponent<TextMesh>().text = "" + timeToAnswer + " saniye kullandınız.";
                yield return new WaitForSeconds(3.0f);
                SetVisibile(timeDisplayCanvas, false);

                totalTime += timeToAnswer;
                timeToAnswer = 0;
                Debug.Log(totalTime);
                //Activate the question canvas again to be able to display the next question
                questionCanvas.GetComponent<Animator>().ResetTrigger("QuestionCanvasAnimation");
                questionCanvas.GetComponent<Animator>().ResetTrigger("StopAnimation");

            }

            // make the question canvas invisible
            SetVisibile(questionCanvas, false);

            yield return StartCoroutine(DisplayResults(displayCountDownTime, correctAnswers, falseAnswers, totalTime)); 

        }

    }
  
    
    private void ResetCameraPosition()
    {
        InputTracking.Recenter();
    }

    // start the intro scene
    private IEnumerator StartIntro()
    {
        yield return StartCoroutine(confirmButton.WaitForBarToFill());
    }

    //
    private void SetVisibile(GameObject g, bool b)
    {
        g.SetActive(b);
    }

    //
    private IEnumerator DisplayResults(int displayCountDownTime, int correctAnswers, int falseAnswers, int totalTime)
    {
        ResetCameraPosition();
        SetVisibile(resultCanvas, true);
        correctAnswersText.text = "Correct answers: " + correctAnswers;
        falseAnswersText.text = "False answers: " + falseAnswers;
        totalTimeSpentText.text = "Sorularda harcanan toplam süre:" + totalTime + " sn.";
        yield return new WaitForSeconds(5.0f);
        SetVisibile(resultCanvas, false);
    }

    // 
    private IEnumerator StartTimerWithText(GameObject timerText, int time)
    {
        TextMesh textMesh = timerText.GetComponent<TextMesh>(); // get the text mesh component from intro timer text game object
        for (int count = time; count > 0; count--)
        {
            textMesh.text = "" + count; // update the text with current time left
            yield return new WaitForSeconds(1.0f); // wait 1 sec
        }
    }
    
    // countdown for the room
    private IEnumerator StartRoomTimer(GameObject timerText, int countDownTime)
    {
        TextMesh textMesh = roomTimerText.GetComponent<TextMesh>(); // get the text mesh component from room timer text game object
        for (int count = countDownTime; count > 0; count--)
        {
            textMesh.text = "Time left: " + count; // update the text with current time left
            if (count == 5)
            {
                roomTimerText.GetComponent<Animator>().SetTrigger("TimeAnimation");
                textMesh.color = Color.red;
            }
            else if (count == 1) roomTimerText.GetComponent<Animator>().SetTrigger("StopAnimation");
            yield return new WaitForSeconds(1.0f); // wait 1 sec
        }
        roomTimerText.GetComponent<Animator>().ResetTrigger("TimeAnimation");
        roomTimerText.GetComponent<Animator>().ResetTrigger("StopAnimation");
        textMesh.color = Color.white;
    }


    // countdown for the room
    private IEnumerator StartQuestionTimer(int questionCountDownTime)
    {
        TextMesh textMesh = roomTimerText.GetComponent<TextMesh>(); // get the text mesh component from room timer text game object
        int count;
        for (count = questionCountDownTime; count > 0; count--)
        {
            textMesh.text = "Time left: " + count; // update the text with current time left
            bool flag = false;
            foreach (SelectionSlider answerButton in answerButtons) if (answerButton.m_BarFilled) flag = true;
            yield return new WaitForSeconds(1.0f); // wait 1 sec
            if (flag) break;
        }
        timeToAnswer = questionCountDownTime - count;
        Debug.Log(timeToAnswer);
    }
    
}
