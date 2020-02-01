﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

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

	//--------private game fields
	private List<LevelSettings> levelSettings;
	private List<HammerSettings> hammerSettings;
	private List<FurnaceSettings> furnaceSettings;
	private int curLevel = 1;
	private int curAction = 1;
	private float timer;


	public float MAX_TIME = 20f;
	public float FURNACE_REWARD = 2f;
	public float HAMMER_REWARD = 5f;

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
            case GAMESTATES.INIT:
                if(gameStateCallOnce)
                {
					// -- Put codes that are needed to be called only once -- //
					//Do the setup for the game here.
                    
					curLevel = 1;
					UpdateTimerUI(timer = MAX_TIME);
                    
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
        if(b_pass)
        {
            // If user has won
        }
        else
        {
            // If user has failed 
        }
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
							hammerBar.gameObject.SetActive(true);
							StartShowHammerAction();
							break;
						case 1:
							furnaceBar.gameObject.SetActive(true);
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
						curLevel++;
						ChangeLevelState(LEVELSTATES.STARTLEVEL);
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

    private void StartShowHammerAction()
	{
		HammerSettings hsToSet = hammerSettings[(int)levelSettings[curLevel - 1].sequence[curAction - 1].y];
		hammerBar.SetHammerBar(hsToSet);
	}

    private void StartShowFurnaceAction()
	{
		FurnaceSettings fsToSet = furnaceSettings[(int)levelSettings[curLevel - 1].sequence[curAction - 1].y];
		furnaceBar.SetFurnaceBar(fsToSet);
	}

    private void OnHammerSuccess()
	{
		UpdateTimerUI(timer += HAMMER_REWARD);
		hammerBar.gameObject.SetActive(false);
		OnActionSuccess();
	}

    private IEnumerator OnHammerSuccess_IEnum()
	{
		yield return null;
	}

    private void OnHammerFailed()
	{
		
	}

	private IEnumerator OnHammerFailed_IEnum()
    {
        yield return null;
    }


	private void OnFurnaceSuccess()
    {
		UpdateTimerUI(timer += FURNACE_REWARD);
		furnaceBar.gameObject.SetActive(false);
		OnActionSuccess();
    }

	private IEnumerator OnFurnaceSuccess_IEnum()
    {
        yield return null;
    }
   
    private void OnFurnaceFailed()
    {
		furnaceBar.gameObject.SetActive(false);
		ChangeActionState(ACTIONSTATES.ENDACTION);
    }

	private IEnumerator OnFurnaceFailed_IEnum()
    {
        yield return null;
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
		timerUI.text = ((int)timeLeft).ToString();
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

}
