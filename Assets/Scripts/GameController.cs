using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.UI;
using VRStandardAssets.Utils;
using System.Linq;

public class GameController : MonoBehaviour
{

    public Settings settings;

    public int countDownTime = 10;     // the time elapsed in room
    public int introCountDownTime = 3; // the time which is counting down in the intro after confirming the start button
    public int displayCountDownTime = 5; //
    public int questionCountDownTime = 10; //

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
    
    /*sliders of the corresponding buttons*/
    public List<SelectionSlider> answerButtons;

    /* answers text */
    public Text answer1Text;
    public Text answer2Text;
    public Text answer3Text;
    public Text answer4Text;

    /*gameobjects to display result*/
    public GameObject resultCanvas;
    public TextMesh correctAnswersText;
    public TextMesh falseAnswersText;

    private int correctAnswers = 0;
    private int falseAnswers = 0;
    
    private IEnumerator Start()
    {
        // get the settings and store them into settings object
        settings = Settings.CreateFromJSON();

        ///////////////////////////////////////////////////////////////////////////////
        //WWW www = new WWW("file:///C:/Users/eminserdar.guzel/Desktop/icerik.json");
        //yield return www;
        //settings = JsonUtility.FromJson<Settings>(www.text);
        ///////////////////////////////////////////////////////////////////////////////

        // reset the camera position
        ResetCameraPosition();

        //Make the intro text visible
        SetVisibile(introCanvas, true);

        //Wait for slider to be filled
        yield return StartCoroutine(StartIntro());

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

                int i = 0;
                foreach (Answer answer in question.answers)
                {
                    ResetButton(answerButtons[i]);
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
                yield return StartCoroutine(StartQuestionTimer(questionCountDownTime));

                /* if one of the answers is selected and it's the correct one, increment the count of the correct answers. 
                ** Otherwise increment the count of the false answers
                */

                bool flag = false;
                foreach (SelectionSlider answerButton in answerButtons) if(answerButton.m_BarFilled && answerButton.m_TrueAnswer) flag = true;
                if(flag) correctAnswers += 1;
                else falseAnswers += 1;


                //Make the current question canvas and the timer invisible
                SetVisibile(roomTimerText, false);
                SetVisibile(questionCanvas, false);
                yield return new WaitForSeconds(1.0f);
                                
                //Activate the question canvas again to be able to display the next question
                questionCanvas.GetComponent<Animator>().ResetTrigger("QuestionCanvasAnimation");
                questionCanvas.GetComponent<Animator>().ResetTrigger("StopAnimation");

            }

            // make the question canvas invisible
            SetVisibile(questionCanvas, false);

            yield return StartCoroutine(DisplayResults(displayCountDownTime, correctAnswers, falseAnswers)); 

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
    private IEnumerator DisplayResults(int displayCountDownTime, int correctAnswers, int falseAnswers)
    {
        ResetCameraPosition();
        SetVisibile(resultCanvas, true);
        correctAnswersText.text = "Correct answers: " + correctAnswers;
        falseAnswersText.text = "False answers: " + falseAnswers;
        yield return new WaitForSeconds(5.0f);
        SetVisibile(resultCanvas, false);
    }

    private void ResetButton(SelectionSlider answerButton)
    {
        answerButton.m_BarFilled = false;
        answerButton.HandleOut();
        answerButton.m_Slider.GetComponentInChildren<Image>().color = new Color(34.0f / 255.0f, 44.0f / 255.0f, 55.0f / 255.0f);
        answerButton.m_FilledBackground.color = new Color(1.0f, 0.0f, 106.0f / 255.0f);
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
            if (count == 6) roomTimerText.GetComponent<Animator>().SetTrigger("TimeAnimation");
            if (count == 5) textMesh.color = Color.red;
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
        for (int count = questionCountDownTime; count > 0; count--)
        {
            textMesh.text = "Time left: " + count; // update the text with current time left
            bool flag = false;
            foreach (SelectionSlider answerButton in answerButtons) if (answerButton.m_BarFilled) flag = true;
            yield return new WaitForSeconds(1.0f); // wait 1 sec
            if(flag) break;
        }
    }
    
}
