﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [HideInInspector]
    public string Path;                     // Path to where the game data will be saved

    private bool b_pass = false;            // Did the game succeed or fail?
    private int gameScore = 0;

    internal static GameManager instance;   // singleton instance
    //Put your game states here
    public enum GAMESTATES
    {
		MAINMENU,
        INIT,
        INGAME,
        PAUSED,
        GAMEOVER
    }

    public GAMESTATES gameState = GAMESTATES.INIT;

    public enum LEVELSTATES
	{
		STARTLEVEL,
        PLAYLEVEL,
        ENDLEVEL
	}

	public LEVELSTATES levelState = LEVELSTATES.STARTLEVEL;

    public enum ACTIONSTATES
	{
		STARTACTION,
        PLAYACTION,
        ENDACTION
	}

	public ACTIONSTATES actionState = ACTIONSTATES.STARTACTION;

    private bool b_gameover;                // Is the game over?

	bool gameStateCallOnce = true;                   // Used when changing the game state bool for calling function/code once in the game
	bool levelStateCallOnce = true;
	bool actionStateCallOnce = true;

	//--------public game fields
	public HammerBar hammerBar;
	public FurnaceBar furnaceBar;
	public TextAsset levelProgressionCSV;
	public TextAsset hammerProgressionCSV;
	public TextAsset furnaceProgressionCSV;
	public Text timerUI;
	public GameObject poofPrefab;
	public GameObject titleCanvas;
	public GameObject ingameCanvas;
	public GameObject titleUI;
	public GameObject pressSpaceKeyUI;
    public GameObject gameoverCanvas;
	public Animator scoreSword;
	public Text scoreText;
    public Text gameoverScore;
	public Animator[] anvils;
	public Animator heavey;
	public Transform anvilSwordPos;
	public GameObject[] swordPrefabs;
	private GameObject[] swordInstance = new GameObject[2];
	private int swordInstanceIndex = 0;

	//--------private game fields
	private List<LevelSettings> levelSettings;
	private List<HammerSettings> hammerSettings;
	private List<FurnaceSettings> furnaceSettings;
	private int curLevel = 1;
	private int curAction = 1;
	private float timer;
	private int score;
	private int lives;

	public float MAX_TIME = 20f;
	public float FURNACE_REWARD = 3f;
	public float HAMMER_REWARD = 6f;
    
    void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
            return;
        }
        //Initialize class
        instance = this;

    }

    void Start()
    {
        SETPATH();

		// Do necessary initialization here
		// put here the initializations that should not be called when game resets (WE DO NOT RELOAD SCENE WHEN RESETTING GAME)
		ReadLevelProgressionFromCsvs();

		hammerBar.Success.AddListener(OnHammerSuccess);
		hammerBar.Failed.AddListener(OnHammerFailed);
		furnaceBar.Success.AddListener(OnFurnaceSuccess);
		furnaceBar.Failed.AddListener(OnFurnaceFailed);

        //for these to work, EventManager.cs must be on the hierarchy
        EventsManager.OnGameReset.AddListener(OnGameReset);
        EventsManager.OnGamePaused.AddListener(OnGamePaused);
        EventsManager.OnGameOver.AddListener(OnGameOver);
    }
    
    private void ReadLevelProgressionFromCsvs()
	{
		levelSettings = new List<LevelSettings>();
		string levelProgressionText = levelProgressionCSV.text;
		string[] levelProgressionLines = levelProgressionText.Split('\n');
		for (int i = 1; i < levelProgressionLines.Length; i++)
		{
			string[] levelProgressionLineContents = levelProgressionLines[i].Split(',');
			levelSettings.Add(new LevelSettings(levelProgressionLineContents[1].Trim(),
												int.Parse(levelProgressionLineContents[2]),
												int.Parse(levelProgressionLineContents[3])));
		}

		hammerSettings = new List<HammerSettings>();
		string hammerProgressionText = hammerProgressionCSV.text;
		string[] hammerProgressionLines = hammerProgressionText.Split('\n');
		for (int i = 1; i < hammerProgressionLines.Length; i++)
		{
			string[] hammerProgressionLineContents = hammerProgressionLines[i].Split(',');
			hammerSettings.Add(new HammerSettings(int.Parse(hammerProgressionLineContents[1]),
			                                      int.Parse(hammerProgressionLineContents[2]),
												  float.Parse(hammerProgressionLineContents[3]),
												  float.Parse(hammerProgressionLineContents[4]),
												 float.Parse(hammerProgressionLineContents[5]),
												  float.Parse(hammerProgressionLineContents[6]),
			                                      hammerProgressionLineContents[7].ToUpper().Trim() == "TRUE"));
		}
        
		furnaceSettings = new List<FurnaceSettings>();
        string furnaceProgressionText = furnaceProgressionCSV.text;
        string[] furnaceProgressionLines = furnaceProgressionText.Split('\n');
        for (int i = 1; i < furnaceProgressionLines.Length; i++)
        {
            string[] furnaceProgressionLineContents = furnaceProgressionLines[i].Split(',');
            furnaceSettings.Add(new FurnaceSettings(float.Parse(furnaceProgressionLineContents[1]),
                                                  float.Parse(furnaceProgressionLineContents[2]),
			                                        float.Parse(furnaceProgressionLineContents[3])));
        }
	}
    
    void OnGameReset()
    {

    }
    void OnGamePaused()
    {

    }
    void OnGameOver()
    {

    }

    #region FSM
    void Update()
    {
        GameFSM();
    }
    void OnEnable()
    {

    }
    void OnDisable()
    {
        EventsManager.OnGameReset.RemoveListener(OnGameReset);
        EventsManager.OnGamePaused.RemoveListener(OnGamePaused);
        EventsManager.OnGameOver.RemoveListener(OnGameOver);
    }
    void GameFSM()
    {
        switch(gameState)
        {
			case GAMESTATES.MAINMENU:
				if(gameStateCallOnce)
				{
					AudioManager.instance.PlayBGMusic(AudioManager.instance.audioClipList[6]);
					AudioManager.instance.PlayAudioClip(27, false, 0.3f);

					gameStateCallOnce = false;
				}

				if (pressSpaceKeyUI.activeInHierarchy)
				{
					if (Input.GetKeyDown(KeyCode.Space))
					{
						RandomCurse();
						titleUI.GetComponent<Animator>().Play("Falldown");
						pressSpaceKeyUI.SetActive(false);
						StartCoroutine(Start_IEnum());
					}
				}

				break;
            case GAMESTATES.INIT:
                if(gameStateCallOnce)
                {
					// -- Put codes that are needed to be called only once -- //
					//Do the setup for the game here.
					AudioManager.instance.PlayBGMusic(AudioManager.instance.audioClipList[2]);
					ingameCanvas.SetActive(true);
					lives = 3;
					for (int i = 0; i < anvils.Length; i++)
					{
						anvils[i].Play("Start");
					}
					curLevel = 1;
					UpdateTimerUI(timer = MAX_TIME);
					UpdateScoreUI(score = 0);
                    
                    //
                    gameStateCallOnce = false;
                    //change gamestate after running init once
                    ChangeGameState(GAMESTATES.INGAME);
                }
                break;
            case GAMESTATES.INGAME:
                if (gameStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //



                    //
                    gameStateCallOnce = false;
                }

				UpdateTimerUI(timer -= Time.deltaTime);
                if(timer <= 0)
				{
					ChangeGameState(GAMESTATES.GAMEOVER);
					return;
				}
                //Game Loop
                GameLoop();
                break;
            case GAMESTATES.PAUSED:
                if (gameStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //

                    EventsManager.OnGamePaused.Invoke();

                    //
                    gameStateCallOnce = false;
                }

                break;
            case GAMESTATES.GAMEOVER:
                if (gameStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //
                    b_gameover = true;

                    StartCoroutine(GameOver());
                    //
                    gameStateCallOnce = false;
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    SceneManager.LoadScene("Main");
                    
                }
                break;

        }
    }
    public void ChangeGameState(int state)  //for button click event (just in case)
    {
        gameState = (GAMESTATES)state;
        gameStateCallOnce = true;
    }
    public void ChangeGameState(GAMESTATES state)
    {
        gameState = state;
        gameStateCallOnce = true;        // Set to true so every time the state change, there's a place to call some code once in the loop
    }

	public void ChangeLevelState(int state)  //for button click event (just in case)
    {
		levelState = (LEVELSTATES)state;
        levelStateCallOnce = true;
    }
	public void ChangeLevelState(LEVELSTATES state)
    {
        levelState = state;
        levelStateCallOnce = true;        // Set to true so every time the state change, there's a place to call some code once in the loop
    }

	public void ChangeActionState(int state)  //for button click event (just in case)
    {
        actionState = (ACTIONSTATES)state;
        actionStateCallOnce = true;
    }
    public void ChangeActionState(ACTIONSTATES state)
    {
        actionState = state;
        actionStateCallOnce = true;        // Set to true so every time the state change, there's a place to call some code once in the loop
    }
    
    #endregion

    IEnumerator GameOver()
    {
		yield return new WaitForSeconds(0.7f);
		AudioManager.instance.PlayAudioClip(UnityEngine.Random.Range(25, 27));
        gameoverScore.text = score.ToString();
        gameoverCanvas.SetActive(true);
        ingameCanvas.SetActive(false);
        yield return null;
    }

    //in-game loop
	void GameLoop()
    {
        // put updates here for when in in-game state
		switch (levelState)
        {
			case LEVELSTATES.STARTLEVEL:
                if (levelStateCallOnce)
                {
					// -- Put codes that are needed to be called only once -- //
					//Do the setup for the game here. 

					if (curLevel > levelSettings.Count)
						curLevel = levelSettings.Count; //repeat level forever if reached final level

					ShowWeapon();
                    curAction = 1;
					ChangeActionState(ACTIONSTATES.STARTACTION);

                    //
                    levelStateCallOnce = false;
                    //change gamestate after running init once
					ChangeLevelState(LEVELSTATES.PLAYLEVEL);
                }
                break; 
			case LEVELSTATES.PLAYLEVEL:
                if (levelStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //



                    //
                    levelStateCallOnce = false;
                }
                //Level Loop
                LevelLoop();
                break;
			case LEVELSTATES.ENDLEVEL:
                if (levelStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //
               
                    //
                    levelStateCallOnce = false;


                    HideWeapon();
					curLevel++;
					ChangeLevelState(LEVELSTATES.STARTLEVEL);
                }

                break;            
        }
    }

	void LevelLoop()
    {
        // put updates here for when in in-game state
        switch (actionState)
        {
			case ACTIONSTATES.STARTACTION:
                if (actionStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //
                    //Do the setup for the game here.
                    
                    //determine what action to display               
					switch((int)levelSettings[curLevel - 1].sequence[curAction - 1].x)
					{
						case 0:
							StartShowHammerAction();
							break;
						case 1:
							StartShowFurnaceAction();
							break;
					}


                    //
                    actionStateCallOnce = false;
                    //change gamestate after running init once
					ChangeActionState(ACTIONSTATES.PLAYACTION);
                }
                break;
			case ACTIONSTATES.PLAYACTION:
                if (actionStateCallOnce)
                {
                    // -- Put codes that are needed to be called only once -- //



                    //
                    actionStateCallOnce = false;
                }
                break;
			case ACTIONSTATES.ENDACTION:
                if (actionStateCallOnce)
                {
					// -- Put codes that are needed to be called only once -- //



                    actionStateCallOnce = false;

					//determine if continue to next action in same level or proceed to next level
					curAction++;
                    
					if(curAction > levelSettings[curLevel - 1].sequence.Count)
					{
						ChangeLevelState(LEVELSTATES.ENDLEVEL);
					}
					else
					{
						ChangeActionState(ACTIONSTATES.STARTACTION);
					}

                    //
                }            
                break;
        }
    }

    private void ShowWeapon()
	{      
        swordInstanceIndex = swordInstanceIndex + 1 > 1 ? 0 : swordInstanceIndex + 1;
		Debug.Log("SHOW " + swordInstanceIndex);
		swordInstance[swordInstanceIndex] = Instantiate(swordPrefabs[UnityEngine.Random.Range(0, swordPrefabs.Length)], anvilSwordPos.parent) as GameObject;
		swordInstance[swordInstanceIndex].transform.position = anvilSwordPos.position;
	}

    private void HideWeapon()
	{
		Debug.Log("HIDE " + swordInstanceIndex);
		swordInstance[swordInstanceIndex].GetComponent<SpriteRenderer>().sprite = swordInstance[swordInstanceIndex].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		swordInstance[swordInstanceIndex].GetComponent<Animator>().Play("Hide");
		Destroy(swordInstance[swordInstanceIndex], .6f);
	}

    private void StartShowHammerAction()
	{
		StartCoroutine(ShowHammerAction_IEnum());
	}
	private IEnumerator ShowHammerAction_IEnum()
	{
		yield return new WaitForSeconds(0.5f);
        hammerBar.gameObject.SetActive(true);
		HammerSettings hsToSet = hammerSettings[(int)levelSettings[curLevel - 1].sequence[curAction - 1].y];
        hammerBar.SetHammerBar(hsToSet);
	}

    private void StartShowFurnaceAction()
	{
		StartCoroutine(ShowFurnaceAction_IEnum());
	}
	private IEnumerator ShowFurnaceAction_IEnum()
	{
		yield return new WaitForSeconds(0.5f);
        furnaceBar.gameObject.SetActive(true);
		FurnaceSettings fsToSet = furnaceSettings[(int)levelSettings[curLevel - 1].sequence[curAction - 1].y];
        furnaceBar.SetFurnaceBar(fsToSet);
	}

    private void OnHammerSuccess()
	{
		UpdateTimerUI(timer += HAMMER_REWARD);
		UpdateScoreUI(++score);
		StartCoroutine(OnHammerSuccess_IEnum());

        int randomAudio = UnityEngine.Random.Range(19, 22);
        
		AudioManager.instance.PlayAudioClip(randomAudio);
    }

    private IEnumerator OnHammerSuccess_IEnum()
	{
		yield return new WaitForSeconds(0.7f);
		InstantiatePoofPrefab(hammerBar.transform.position);
		hammerBar.gameObject.SetActive(false);
		OnActionSuccess();
        AudioManager.instance.PlayAudioClip(14);
	}

    private void OnHammerFailed()
	{
		DecreaseLives();
		OnActionFailed();
	}
    
	private IEnumerator OnHammerFailed_IEnum()
    {
        yield return null;
    }


	private void OnFurnaceSuccess()
    {
		UpdateTimerUI(timer += FURNACE_REWARD);
		UpdateScoreUI(++score);
		furnaceBar.GetComponent<Animator>().Play("Success");
		StartCoroutine(OnFurnaceSuccess_IEnum());

        int randomAudio = UnityEngine.Random.Range(19, 22);
        
		AudioManager.instance.PlayAudioClip(randomAudio);
    }

	private IEnumerator OnFurnaceSuccess_IEnum()
    {
		if (curAction + 1 > levelSettings[curLevel - 1].sequence.Count)
		{
			swordInstance[swordInstanceIndex].GetComponent<SpriteRenderer>().sprite = swordInstance[swordInstanceIndex].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		}
		
		yield return new WaitForSeconds(1f);
		InstantiatePoofPrefab(furnaceBar.transform.position);
		furnaceBar.gameObject.SetActive(false);
        OnActionSuccess();
        AudioManager.instance.PlayAudioClip(14);
    }
   
    private void OnFurnaceFailed()
    {
		DecreaseLives();
		furnaceBar.GetComponent<Animator>().Play("Failed");
		furnaceBar.GetComponent<CameraShake>().Shake(0.2f, 0.02f);
		StartCoroutine(OnFurnaceFailed_IEnum());      
    }

	private IEnumerator OnFurnaceFailed_IEnum()
    {
		if (curAction + 1 > levelSettings[curLevel - 1].sequence.Count)
		{
			swordInstance[swordInstanceIndex].GetComponent<SpriteRenderer>().sprite = swordInstance[swordInstanceIndex].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		}

		yield return new WaitForSeconds(1f);
		InstantiatePoofPrefab(furnaceBar.transform.position);
        furnaceBar.gameObject.SetActive(false);
        OnActionFailed();
		ChangeActionState(ACTIONSTATES.ENDACTION);
    }


    private void OnActionSuccess()
	{
		ChangeActionState(ACTIONSTATES.ENDACTION);
	}

    private void OnActionFailed()
	{
		
	}

    private void UpdateTimerUI(float timeLeft)
	{
		if (timeLeft < 0)
			timeLeft = 0;
		if (timeLeft > MAX_TIME)
			timeLeft = MAX_TIME;

		if (timerUI.text != ((int)timeLeft).ToString())
		{
			timerUI.text = ((int)timeLeft).ToString();
			timerUI.GetComponent<Animator>().Play("TimerTick");
		}
	}

	private void InstantiatePoofPrefab(Vector2 pos)
	{
		GameObject poofInstance = Instantiate(poofPrefab, pos, Quaternion.identity) as GameObject;
		Destroy(poofInstance, 1f);
	}

	private void UpdateScoreUI(int score)
    {
        scoreText.text = score.ToString();
        scoreSword.Play("Pop");

    }
    public void DecreaseLives()
	{
		lives--;
		anvils[lives].Play("Break");
		RandomCurse();
        if(lives <= 0)
		{
			ChangeGameState(GAMESTATES.GAMEOVER);
		}
	}
    private IEnumerator Start_IEnum()
	{
		yield return new WaitForSeconds(0.7f);
		titleUI.SetActive(false);
		titleCanvas.SetActive(false);
		ChangeGameState(GAMESTATES.INIT);
	}

    void SETPATH()
    {
#if UNITY_EDITOR
        Path = Application.dataPath;
#else
		Path = Application.persistentDataPath;
#endif
    }
    
    public void ResetGame()
    {
        ChangeGameState(GAMESTATES.INIT);
    }

    public void RandomCurse()
    {
        int num = UnityEngine.Random.Range(0, 3);
        int audioIndex = 0;
        switch (num)
        {
            case 0:
                audioIndex = 14;
                break;
            case 1:
                audioIndex = 18;
                break;
            case 2:
                audioIndex = 22;
                break;
        }
        AudioManager.instance.PlayAudioClip(audioIndex);
    }

    public void AnimateHammering()
	{
		heavey.Play("Hit");
        swordInstance[swordInstanceIndex].GetComponent<Animator>().Play("Hit");      
	}
    public void AnimateQuenching()
	{
		heavey.Play("Quench");
		swordInstance[swordInstanceIndex].GetComponent<Animator>().Play("Quench");      
	}
    public void AnimateWaiting()
	{
		heavey.Play("Idle");
		swordInstance[swordInstanceIndex].GetComponent<Animator>().Play("Waiting");      
	}
}
